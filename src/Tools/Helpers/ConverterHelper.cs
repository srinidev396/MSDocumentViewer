// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web;

using Newtonsoft.Json;

using Leadtools;
using Leadtools.Caching;
using Leadtools.Codecs;
using Leadtools.Demos;
using Leadtools.Document;
using Leadtools.Document.Converter;
using Leadtools.Document.Writer;
using Leadtools.Ocr;
using Leadtools.Annotations.Engine;

using Leadtools.Services.Models.Document;
using Leadtools.Services.Tools.Helpers;
using Newtonsoft.Json.Linq;
using Leadtools.DocumentViewer.Controllers;
using System.Threading;
using Leadtools.Services.Models.StatusJobConverter;
using Microsoft.AspNetCore.Http;

namespace Leadtools.Services.Tools.Helpers
{
   class ConversionOutput
   {
      public string[] Files;
      public string[] DocumentFiles;
      public string[] DocumentExtraFiles;
      public string[] AnnotationFiles;

      // If streaming...
      public Stream DocumentStream;
      public Stream AnnotationsStream;
   }


   internal static class ConverterHelper
   {

      /* Conversion can happen in two ways:
       * 
       * 1. In-process conversion
       *    - Uses more memory in the service process
       *    - Can use streams
       *    - No additional setup
       * 2. External-process conversion
       *    - Uses a different process (with its own memory)
       *    - Must use file system
       *    - Service must be able to locate the DocumentConverterDemo executable
       * 
       * See local.config for options.
       */

      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
      public static ConvertResponse Convert(string documentId, IHeaderDictionary headers, ServiceDocumentConverterJobData serviceJobData)
      {
         // Verify input - we must have a document ID and service job data (from the client).
         if (string.IsNullOrEmpty(documentId))
            throw new ArgumentNullException("documentId");
         if (serviceJobData == null)
            throw new ArgumentNullException("serviceJobData");

         // Check the format. DocumentFormat.User and RasterImageFormat.Unknown are provided for custom implementation.
         // One (but not both) must be a standard value - that's what we'll convert to.
         bool isConvertingToDocumentFormat = serviceJobData.DocumentFormat != DocumentFormat.User;
         bool isConvertingToRasterFormat = serviceJobData.RasterImageFormat != RasterImageFormat.Unknown;
         if (!isConvertingToDocumentFormat && !isConvertingToRasterFormat)
            throw new ArgumentException("Either DocumentFormat must not be User or RasterImageFormat must not be Unknown", "serviceJobData");
         if (isConvertingToDocumentFormat && isConvertingToRasterFormat)
            throw new ArgumentException("Either DocumentFormat must be User or RasterImageFormat must be Unknown", "serviceJobData");

         // Check if we'll be running conversion in-process or externally.
         bool useExternalProcess = ServiceHelper.GetSettingBoolean(ServiceHelper.Key_DocumentConverter_UseExternal);

         // Before we start, confirm that if we're running the conversion externally, we have the required DocumentConverterExePath.
         string externalDocumentConverterExePath = null;
         if (useExternalProcess)
         {
            // Check configuration file - if not found, check known setup locations.
            externalDocumentConverterExePath = ServiceHelper.GetAbsolutePath(ServiceHelper.GetSettingValue(ServiceHelper.Key_DocumentConverter_ExePath));
            if (string.IsNullOrEmpty(externalDocumentConverterExePath))
               externalDocumentConverterExePath = CheckSetupDocumentConverterExePath();

            if (!File.Exists(externalDocumentConverterExePath))
               throw new InvalidOperationException("DocumentConverter executable not found. Set a valid path for the 'lt.DocumentConverter.ExePath' key in the .config file.");
         }

         ObjectCache cache = ServiceHelper.CacheManager.DefaultCache;
         if (cache == null)
            throw new InvalidOperationException("Cache not found.");

         // See if the cache supports storing as an external resource (so we don't have to use temp files)
         bool cacheHasExternalResources = (cache.DefaultCacheCapabilities & DefaultCacheCapabilities.ExternalResources) == DefaultCacheCapabilities.ExternalResources;

         // If we're not converting externally, we have the option to use streams instead of external resources or temporary files.
         // This will disallow the user from downloading the result by default, since we won't be using external resources in the cache.
         bool useStreams = !useExternalProcess && (!cacheHasExternalResources || ServiceHelper.GetSettingBoolean(ServiceHelper.Key_DocumentConverter_ForceStreaming));

         // Verify that we can continue with our current arguments.
         CheckCapabilities(useExternalProcess, useStreams, cacheHasExternalResources, serviceJobData);

         /*
          * From now on, we will set properties that will be useful for both in-process and external-process conversion.
          */

         // Load the source document from the provided ID.
         string userToken = ServiceHelper.GetUserToken(headers, null);
         var loadFromCacheOptions = new LoadFromCacheOptions
         {
            Cache = cache,
            DocumentId = documentId,
            UserToken = userToken
         };
         LEADDocument sourceDocument = DocumentFactory.LoadFromCache(loadFromCacheOptions);

         // Get the output file extension to use
         string outputExtension;
         if (isConvertingToDocumentFormat)
            outputExtension = DocumentWriter.GetFormatFileExtension(serviceJobData.DocumentFormat);
         else
            outputExtension = RasterCodecs.GetExtension(serviceJobData.RasterImageFormat);

         string documentName = !string.IsNullOrWhiteSpace(serviceJobData.DocumentName) ? serviceJobData.DocumentName : "file";
         string documentNameWithExtension = documentName;
         if (!string.IsNullOrEmpty(outputExtension))
            documentNameWithExtension += "." + outputExtension;

         // Fill in some of the simple properties between the serviceJobData and DocumentJobData
         DocumentConverterJobData jobData = CopyCommonDocumentConverterJobData(serviceJobData, userToken, documentName, sourceDocument.Pages.Count);

         jobData.Document = sourceDocument;

         DocumentConverterJob job = null;
         DocumentConverter converter = null;

         // Used with external resources
         bool externalResourceSaveSuccessful = false;
         string externalResourceDocumentKey = null;
         string externalResourceAnnotationsKey = null;
         // Conversion will be stored in a new location in the cache
         string externalResourceRegion = Guid.NewGuid().ToString("N");

         // If we don't use external resources or streams, we can use temp files.
         string outputDocumentTempFileName = null;
         string outputAnnotationsTempFileName = null;

         bool outputAnnotationsIsExternal = jobData.AnnotationsMode == DocumentConverterAnnotationsMode.External;

         try
         {

            // Set up our output document/annotations.
            if (useStreams)
            {
               // Streams will be used to get the output document and annotations.
               // However, we won't be able to "host" the converted document in the cache (unless we explicitly do so after).
               jobData.OutputDocumentStream = new MemoryStream();

               if (outputAnnotationsIsExternal)
                  jobData.OutputAnnotationsStream = new MemoryStream();

            }
            else if (cacheHasExternalResources)
            {
               // We will create an external-resource entry in the cache for the conversion result, cached by the region we made and the document name.
               externalResourceDocumentKey = documentNameWithExtension;
               jobData.OutputDocumentFileName = ServiceHelper.GetFileUri(cache.BeginAddExternalResource(externalResourceDocumentKey, externalResourceRegion, true));

               if (outputAnnotationsIsExternal)
               {
                  // We will also create an external-resource entry in the cache for the annotations file.
                  externalResourceAnnotationsKey = documentName + "_ann.xml";
                  jobData.OutputAnnotationsFileName = ServiceHelper.GetFileUri(cache.BeginAddExternalResource(externalResourceAnnotationsKey, externalResourceRegion, true));
               }
            }
            else
            {
               // Use system temp files for this operation. Like with streams, we can't "host" the result.
               outputDocumentTempFileName = RasterDefaults.GetTemporaryFileName();
               jobData.OutputDocumentFileName = outputDocumentTempFileName;

               if (outputAnnotationsIsExternal)
               {
                  outputAnnotationsTempFileName = RasterDefaults.GetTemporaryFileName();
                  jobData.OutputAnnotationsFileName = outputAnnotationsTempFileName;
               }
            }

            if (!string.IsNullOrWhiteSpace(jobData.OutputDocumentFileName))
            {
               jobData.UploadDocumentOptions = null;
            }

            /*
            *  At this point, DocumentConverterJobData has all required properties.
            */

            // Create the document convert instance.
            converter = new DocumentConverter();
            converter.Options.PageNumberingTemplate = serviceJobData.PageNumberingTemplate;
            converter.Options.EnableSvgConversion = serviceJobData.EnableSvgConversion;
            converter.Options.SvgImagesRecognitionMode = serviceJobData.SvgImagesRecognitionMode;
            converter.Options.EmptyPageMode = serviceJobData.EmptyPageMode;
            converter.Options.JobErrorMode = serviceJobData.JobErrorMode;
            converter.Preprocessor.Deskew = serviceJobData.PreprocessorDeskew;
            converter.Preprocessor.Orient = serviceJobData.PreprocessorOrient;
            converter.Preprocessor.Invert = serviceJobData.PreprocessorInvert;
            converter.Diagnostics.EnableTrace = Debugger.IsAttached;

            var rasterCodecsOptionsFilePath = ServiceHelper.GetAbsolutePath(ServiceHelper.GetSettingValue(ServiceHelper.Key_RasterCodecs_OptionsFilePath));

            // If converting to a document format, get the conversion options for this format and add them to the converter.
            if (isConvertingToDocumentFormat)
            {
               // Document Writer
               var documentWriter = new DocumentWriter();
               // Convert the Document Options
               var documentOptions = ConverterHelper.GetDocumentOptions(jobData.DocumentFormat, serviceJobData.DocumentOptions);
               if (documentOptions != null)
                  documentWriter.SetOptions(jobData.DocumentFormat, documentOptions);
               ServiceHelper.SetDocumentWriterOptions(documentWriter, jobData.DocumentFormat);
               converter.SetDocumentWriterInstance(documentWriter);
            }

            ConversionOutput output = null;

            // Run the conversion either externally or in-process.
            if (useExternalProcess)
            {
               output = RunExternalDocumentConversion(converter, jobData, cache, externalDocumentConverterExePath, rasterCodecsOptionsFilePath);
            }
            else
            {
               // In-process

               // RasterCodecs Options
               if (!string.IsNullOrEmpty(rasterCodecsOptionsFilePath))
                  sourceDocument.RasterCodecs.LoadOptions(rasterCodecsOptionsFilePath);

               // OCR
               IOcrEngine ocrEngine = ServiceHelper.GetOCREngine();
               if (ocrEngine != null)
                  converter.SetOcrEngineInstance(ocrEngine, false);

               // Set the annotations rendering engine before starting
               var renderingEngine = ServiceHelper.GetAnnRenderingEngine();
               if (renderingEngine != null)
                  converter.SetAnnRenderingEngineInstance(renderingEngine);

               // Run the conversion
               job = converter.Jobs.CreateJob(jobData);
               converter.Jobs.RunJob(job);

               if (job.Status != DocumentConverterJobStatus.Aborted)
               {
                  output = new ConversionOutput();
                  if (job.OutputFiles != null)
                     output.Files = job.OutputFiles.ToArray();
                  if (job.OutputDocumentFiles != null)
                     output.DocumentFiles = job.OutputDocumentFiles.ToArray();
                  if (job.OutputDocumentExtraFiles != null)
                     output.DocumentExtraFiles = job.OutputDocumentExtraFiles.ToArray();
                  if (job.OutputAnnotationFiles != null)
                     output.AnnotationFiles = job.OutputAnnotationFiles.ToArray();

                  output.DocumentStream = job.JobData.OutputDocumentStream;
                  output.AnnotationsStream = job.JobData.OutputAnnotationsStream;
               }
               else if (job.Errors.Count > 0)
               {
                  // Just return the first error.
                  var error = job.Errors[0];
                  throw new Exception(string.Format("{0} (on Page {1})", error.Error.Message, error.InputDocumentPageNumber), error.Error);
               }
            }

            if (output == null || (!useStreams && (output.Files == null || output.Files.Length < 1)) || (useStreams && output.DocumentStream == null))
               throw new Exception("No output data");

            // Converter was successful.
            var response = new ConvertResponse();

            if (!useStreams && cacheHasExternalResources)
            {
               // Check if we have need to archive the results in a ZIP file before returning them to the user
               bool archiveRequired = IsArchiveRequired(output, outputExtension);
               if (archiveRequired)
               {
                  var archiveName = documentName + ".zip";
                  // Yes, create a new cache item and add the results
                  ArchiveResults(archiveName, cache, documentId, output.Files, externalResourceRegion, response);

                  // And remove the old items (not needed anymore)
                  if (externalResourceDocumentKey != null)
                     cache.EndAddExternalResource(false, externalResourceDocumentKey, false, null, externalResourceRegion);
                  if (externalResourceAnnotationsKey != null)
                     cache.EndAddExternalResource(false, externalResourceAnnotationsKey, false, null, externalResourceRegion);

               }
               else
               {
                  // No archiving, so save the original annotations and document
                  cache.EndAddExternalResource(true, externalResourceDocumentKey, true, ServiceHelper.CacheManager.CreatePolicy(cache), externalResourceRegion);
                  var documentFileName = ServiceHelper.GetFileUri(cache.GetItemExternalResource(externalResourceDocumentKey, externalResourceRegion, false));

                  // If we have *one* output document url (and then maybe the annotations) then set documentName.
                  response.Document = new ConvertItem
                  {
                     Name = externalResourceDocumentKey,
                     Url = CacheController.CreateConversionResultUri(externalResourceRegion, externalResourceDocumentKey),
                     Length = GetFileLength(documentFileName),
                     MimeType = GetMimeType(documentFileName, outputExtension)
                  };

                  // Check for annotations by the set cache key
                  if (externalResourceAnnotationsKey != null)
                  {
                     cache.EndAddExternalResource(true, externalResourceAnnotationsKey, true, ServiceHelper.CacheManager.CreatePolicy(cache), externalResourceRegion);
                     var annotationsFileName = ServiceHelper.GetFileUri(cache.GetItemExternalResource(externalResourceAnnotationsKey, externalResourceRegion, false));

                     response.Annotations = new ConvertItem
                     {
                        Name = externalResourceAnnotationsKey,
                        Url = CacheController.CreateConversionResultUri(externalResourceRegion, externalResourceAnnotationsKey),
                        Length = GetFileLength(annotationsFileName),
                        MimeType = "text/xml"
                     };
                  }
               }
               externalResourceSaveSuccessful = true;
            }

            bool canSaveToCache = CanSaveToCache(output, outputExtension, job);
            if (canSaveToCache)
            {
               var mimeType = GetMimeType(serviceJobData.DocumentFormat);

               LoadDocumentOptions loadOptions = new LoadDocumentOptions
               {
                  Cache = cache,
                  UseCache = cache != null,
                  CachePolicy = ServiceHelper.CacheManager.CreatePolicy(cache),
                  Name = documentNameWithExtension,
                  UserToken = userToken,
                  MimeType = mimeType
               };

               // We may have already defined these response objects in external resource saving, but go ahead again using the output files/streams

               response.DocumentId = null;
               LEADDocument newDocument = null;

               // Try to load the document, fail gracefully if this did not work
               try
               {
                  if (useStreams)
                  {
                     newDocument = DocumentFactory.LoadFromStream(jobData.OutputDocumentStream, loadOptions);
                     response.DocumentId = newDocument.DocumentId;
                  }
                  else
                  {
                     // Temp files or external resource paths
                     var outputDocumentFileName = jobData.OutputDocumentFileName;
                     if (outputDocumentFileName != null)
                     {
                        newDocument = DocumentFactory.LoadFromFile(outputDocumentFileName, loadOptions);
                        response.DocumentId = newDocument.DocumentId;
                     }
                  }

                  if (newDocument != null)
                  {
                     newDocument.AutoDeleteFromCache = false;
                     newDocument.AutoDisposeDocuments = true;
                     newDocument.AutoSaveToCache = false;
                     newDocument.SaveToCache();
                     newDocument.Dispose();
                     newDocument = null;
                  }
               }
               catch { }
               finally
               {
                  if (newDocument != null)
                     newDocument.Dispose();
               }
            }

            return response;
         }
         catch (Exception ex)
         {
            Trace.WriteLine(string.Format("Convert - Error:{1}{0}documentId:{2}", Environment.NewLine, ex.Message, documentId), "Error");
            throw;
         }
         finally
         {
            if (converter != null)
               converter.Dispose();
            if (sourceDocument != null)
               sourceDocument.Dispose();

            if (cacheHasExternalResources && !externalResourceSaveSuccessful)
            {
               try
               {
                  // Finish adding to the cache
                  if (externalResourceDocumentKey != null)
                     cache.EndAddExternalResource(false, externalResourceDocumentKey, false, null, externalResourceRegion);
                  if (externalResourceAnnotationsKey != null)
                     cache.EndAddExternalResource(false, externalResourceAnnotationsKey, false, null, externalResourceRegion);

                  // Delete region if we did not convert the document successfully
                  if (externalResourceRegion != null)
                     cache.DeleteRegion(externalResourceRegion);
               }
               catch (Exception ex)
               {
                  Trace.WriteLine(string.Format("Removing External Resources - Error:{1}{0}documentId:{2}", Environment.NewLine, ex.Message, documentId), "Warning");
               }
            }

            // Delete temporary files, if they exist
            ServiceHelper.SafeDeleteFile(outputDocumentTempFileName);
            ServiceHelper.SafeDeleteFile(outputAnnotationsTempFileName);
         }
      }

      private static void CheckCapabilities(bool useExternalProcess, bool useStreamsOrJob, bool cacheHasExternalResources, ServiceDocumentConverterJobData serviceJobData)
      {
         if (useStreamsOrJob)
         {
            if (serviceJobData.DocumentFormat == DocumentFormat.Svg || serviceJobData.DocumentFormat == DocumentFormat.Html)
               throw new ArgumentException("DocumentFormat cannot be SVG or HTML; only multi-page, single-file output formats are allowed", "serviceJobData");
         }
      }

      private static ConversionOutput RunExternalDocumentConversion(DocumentConverter converter, DocumentConverterJobData jobData, ObjectCache cache, string externalConverterPath, string rasterCodecsOptionsFilePath)
      {
         // For the external document conversion, we move everything into a serializable DocumentConverterPreferences.

         var preferences = new DocumentConverterPreferences();
         // Create the preferences file
         string preferencesTempFileName = RasterDefaults.GetTemporaryFileName();

         string documentWriterOptionsTempFileName = null;

         try
         {
            // Match up as many of DocumentConverterPreferences props with DocumentConverterJobData props as possible
            preferences.JobName = jobData.JobName;

            // Cache
            var fileCache = cache as FileCache;
            if (fileCache == null)
               throw new InvalidOperationException("Cache must be FileCache for this operation");

            preferences.CacheDirectory = fileCache.ResolveDirectory(fileCache.CacheDirectory);
            preferences.CacheDataSerializationMode = fileCache.DataSerializationMode;
            preferences.CachePolicySerializationMode = fileCache.PolicySerializationMode;

            // Document
            preferences.DocumentId = jobData.Document.DocumentId;
            preferences.DocumentUserToken = jobData.Document.UserToken;
            preferences.InputDocumentFileName = null; // Not needed, documentId is used
            preferences.InputFirstPage = jobData.InputDocumentFirstPageNumber;
            preferences.InputLastPage = jobData.InputDocumentLastPageNumber;
            // Set the maximum number of pages to convert. We can do this here or by modifying InputDocumentFirstPageNumber/InputDocumentLastPageNumber (See other #REF_MaximumPages)
            //preferences.InputMaximumPages = ;

            preferences.OutputDocumentFileName = jobData.OutputDocumentFileName;
            preferences.OutputAnnotationsFileName = jobData.OutputAnnotationsFileName;

            // Format
            preferences.DocumentFormat = jobData.DocumentFormat;
            preferences.RasterImageFormat = jobData.RasterImageFormat;
            preferences.RasterImageBitsPerPixel = jobData.RasterImageBitsPerPixel;

            // Delete all files generated if an error occur
            preferences.PurgeOutputFilesOnError = true;

            // Annotations
            preferences.InputAnnotationsFileName = jobData.InputAnnotationsFileName;
            preferences.LoadEmbeddedAnnotation = (jobData.AnnotationsMode != DocumentConverterAnnotationsMode.None);
            preferences.OutputAnnotationsMode = jobData.AnnotationsMode;

            // Set the options
            preferences.PageNumberingTemplate = converter.Options.PageNumberingTemplate;
            preferences.EnableSvgConversion = converter.Options.EnableSvgConversion;
            preferences.SvgImagesRecognitionMode = converter.Options.SvgImagesRecognitionMode;
            preferences.EmptyPageMode = converter.Options.EmptyPageMode;
            preferences.PreprocessingDeskew = converter.Preprocessor.Deskew;
            preferences.PreprocessingOrient = converter.Preprocessor.Orient;
            preferences.PreprocessingInvert = converter.Preprocessor.Invert;
            preferences.ErrorMode = converter.Options.JobErrorMode;
            preferences.EnableTrace = false;
            preferences.OpenOutputDocument = false;

            // OCR - set the engine type only
            var engineTypeString = ServiceHelper.GetSettingValue(ServiceHelper.Key_Ocr_EngineType);
            if (!string.IsNullOrEmpty(engineTypeString))
            {
               try
               {
                  if (engineTypeString.Equals("lead", StringComparison.OrdinalIgnoreCase))
                     preferences.OCREngineType = OcrEngineType.LEAD;
                  else if (engineTypeString.Equals("omnipage", StringComparison.OrdinalIgnoreCase))
                     preferences.OCREngineType = OcrEngineType.LEAD;
                  else if (engineTypeString.Equals("omnipagearabic", StringComparison.OrdinalIgnoreCase))
                     preferences.OCREngineType = OcrEngineType.LEAD;
               }
               catch { }
            }

            // Check for a location of the OCR Runtime
            var runtimeDirectory = ServiceHelper.GetSettingValue(ServiceHelper.Key_Ocr_RuntimeDirectory);
            runtimeDirectory = ServiceHelper.GetAbsolutePath(runtimeDirectory);
            if (string.IsNullOrEmpty(runtimeDirectory))
               runtimeDirectory = ServiceHelper.CheckOCRRuntimeDirectory();
            preferences.OCREngineRuntimePath = runtimeDirectory;

            // RasterCodecs options
            if (!string.IsNullOrEmpty(rasterCodecsOptionsFilePath))
               preferences.RasterCodecsOptionsPath = rasterCodecsOptionsFilePath;

            if (jobData.DocumentFormat != DocumentFormat.User)
            {
               DocumentWriter docWriter = converter.DocumentWriterInstance;
               documentWriterOptionsTempFileName = RasterDefaults.GetTemporaryFileName();
               preferences.DocumentWriterOptionsPath = documentWriterOptionsTempFileName;
               docWriter.SaveOptions(documentWriterOptionsTempFileName);
            }

            // Save the preferences
            preferences.Save(preferencesTempFileName);

            bool debug = Debugger.IsAttached;

            // Run the converter
            int exitCode = 1;
            using (var process = new Process())
            {
               process.StartInfo.FileName = externalConverterPath;
               process.StartInfo.Arguments = string.Format("\"{0}\"", preferencesTempFileName);
               process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

               // Debug
               if (debug)
               {
                  DataReceivedEventHandler output = (sender, args) =>
                  {
                     Trace.WriteLine(string.Format("Converter: {0}", args.Data));
                  };
                  process.StartInfo.UseShellExecute = false;
                  process.StartInfo.RedirectStandardOutput = true;
                  process.OutputDataReceived += output;
                  process.StartInfo.RedirectStandardError = true;
                  process.ErrorDataReceived += output;
               }

               process.Start();

               // Debug
               if (debug)
               {
                  process.BeginOutputReadLine();
                  process.BeginErrorReadLine();
               }

               process.WaitForExit();

               // Debug
               if (debug)
               {
                  process.CancelOutputRead();
                  process.CancelErrorRead();
               }
               exitCode = process.ExitCode;
               process.Dispose();
            }

            // Re-load the preferences so we get the results
            preferences = DocumentConverterPreferences.Load(preferencesTempFileName).Clone();

            if (exitCode == 0)
            {
               // Note, we may fail later if the external converter was using the old DocumentConverterPreferences,
               // which has no support for the Document/Extra/Annotations arrays
               return new ConversionOutput()
               {
                  Files = preferences.OutputFiles,
                  DocumentFiles = preferences.OutputDocumentFiles,
                  DocumentExtraFiles = preferences.OutputDocumentExtraFiles,
                  AnnotationFiles = preferences.OutputAnnotationFiles,
                  DocumentStream = null,
                  AnnotationsStream = null
               };
            }

            // Else there was an error, get the error message
            throw new InvalidOperationException(preferences.ErrorMessage ?? "Unspecified error in external conversion");
         }
         finally
         {
            // Delete the temporary files we created
            ServiceHelper.SafeDeleteFile(preferencesTempFileName);
            ServiceHelper.SafeDeleteFile(documentWriterOptionsTempFileName);
         }
      }

#if LEADTOOLS_V22_OR_LATER
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
      public static ConvertResponse ConvertEditableContent(string editableDocumentName, string documentId, string userToken, string editableContent, Stream editableContentStream, EditableDocumentConverterOptions options)
      {
         // Verify input - we must have a document ID and service job data (from the client).
         if (string.IsNullOrEmpty(editableContent) && editableContentStream == null)
            throw new ArgumentNullException(nameof(editableContent));
         if (options == null)
            throw new ArgumentNullException(nameof(options));


         ObjectCache cache = ServiceHelper.CacheManager.DefaultCache;
         if (cache == null)
            throw new InvalidOperationException("Cache not found.");

         // See if the cache supports storing as an external resource (so we don't have to use temp files)
         bool cacheHasExternalResources = (cache.DefaultCacheCapabilities & DefaultCacheCapabilities.ExternalResources) == DefaultCacheCapabilities.ExternalResources;

         // If we're not converting externally, we have the option to use streams instead of external resources or temporary files.
         // This will disallow the user from downloading the result by default, since we won't be using external resources in the cache.
         bool useStreams = (!cacheHasExternalResources || ServiceHelper.GetSettingBoolean(ServiceHelper.Key_DocumentConverter_ForceStreaming));

         /*
          * From now on, we will set properties that will be useful for both in-process and external-process conversion.
          */

         // Get the output file extension to use
         string outputExtension = DocumentWriter.GetFormatFileExtension(options.DocumentFormat);

         string documentName = !string.IsNullOrWhiteSpace(editableDocumentName) ? editableDocumentName : "NewDocument";
         string documentNameWithExtension = documentName;
         if (!string.IsNullOrEmpty(outputExtension))
            documentNameWithExtension += "." + outputExtension;

         EditableDocumentConverter converter = null;

         // Used with external resources
         bool externalResourceSaveSuccessful = false;
         string externalResourceDocumentKey = null;
         // Conversion will be stored in a new location in the cache
         string externalResourceRegion = Guid.NewGuid().ToString("N");

         // If we don't use external resources or streams, we can use temp files.
         string outputDocumentTempFileName = null;

         try
         {
            Stream outputDocumentStream = null;
            string outputDocumentFileName = null;
            // Set up our output document/annotations.
            if (useStreams)
            {
               // Streams will be used to get the output document
               // However, we won't be able to "host" the converted document in the cache (unless we explicitly do so after).
               outputDocumentStream = new MemoryStream();

            }
            else if (cacheHasExternalResources)
            {
               // We will create an external-resource entry in the cache for the conversion result, cached by the region we made and the document name.
               externalResourceDocumentKey = documentNameWithExtension;
               outputDocumentFileName = ServiceHelper.GetFileUri(cache.BeginAddExternalResource(externalResourceDocumentKey, externalResourceRegion, true));
            }
            else
            {
               // Use system temp files for this operation. Like with streams, we can't "host" the result.
               outputDocumentTempFileName = RasterDefaults.GetTemporaryFileName();
               outputDocumentFileName = outputDocumentTempFileName;
            }

            /*
            *  At this point, DocumentConverterJobData has all required properties.
            */

            // Create the document convert instance.
            converter = new EditableDocumentConverter();

            ConversionOutput output = null;

            // Run the conversion in-process.
            {
               // In-process
               // OCR

               // Run the conversion
               try
               {
                  output = new ConversionOutput();
                  if (outputDocumentStream != null)
                  {
                     if (editableContentStream == null)
                     {
                        converter.ConvertEditableContentData(editableContent, outputDocumentStream, options);
                     }
                     else
                     {
                        converter.ConvertEditableContent(editableContentStream, outputDocumentStream, options);
                     }
                  }
                  else
                  {
                     if (editableContentStream == null)
                     {
                        converter.ConvertEditableContentData(editableContent, outputDocumentFileName, options);
                     }
                     else
                     {
                        using (var fs = new FileStream(outputDocumentFileName, FileMode.OpenOrCreate))
                        {
                           converter.ConvertEditableContent(editableContentStream, fs, options);
                        }
                     }
                  }

                  output.Files = new string[] { outputDocumentFileName };
                  output.DocumentStream = outputDocumentStream;
               }
               catch
               {
                  throw;
               }
            }

            // Converter was successful.
            var response = new ConvertResponse();

            if (!useStreams && cacheHasExternalResources)
            {
               // Check if we have need to archive the results in a ZIP file before returning them to the user
               bool archiveRequired = IsArchiveRequired(output, outputExtension);
               if (archiveRequired)
               {
                  var archiveName = documentName + ".zip";
                  // Yes, create a new cache item and add the results
                  ArchiveResults(archiveName, cache, string.Empty, output.Files, externalResourceRegion, response);

                  // And remove the old items (not needed anymore)
                  if (externalResourceDocumentKey != null)
                     cache.EndAddExternalResource(false, externalResourceDocumentKey, false, null, externalResourceRegion);
               }
               else
               {
                  // No archiving, so save the original annotations and document
                  cache.EndAddExternalResource(true, externalResourceDocumentKey, true, ServiceHelper.CacheManager.CreatePolicy(cache), externalResourceRegion);
                  var documentFileName = ServiceHelper.GetFileUri(cache.GetItemExternalResource(externalResourceDocumentKey, externalResourceRegion, false));

                  // If we have *one* output document url (and then maybe the annotations) then set documentName.
                  response.Document = new ConvertItem
                  {
                     Name = externalResourceDocumentKey,
                     Url = CacheController.CreateConversionResultUri(externalResourceRegion, externalResourceDocumentKey),
                     Length = GetFileLength(documentFileName),
                     MimeType = GetMimeType(documentFileName, outputExtension)
                  };

               }
               externalResourceSaveSuccessful = true;
            }

            bool canSaveToCache = CanSaveToCache(output, outputExtension, null);
            if (canSaveToCache)
            {
               var mimeType = GetMimeType(options.DocumentFormat);

               LoadDocumentOptions loadOptions = new LoadDocumentOptions
               {
                  Cache = cache,
                  UseCache = cache != null,
                  CachePolicy = ServiceHelper.CacheManager.CreatePolicy(cache),
                  Name = documentNameWithExtension,
                  UserToken = userToken,
                  MimeType = mimeType
               };

               // We may have already defined these response objects in external resource saving, but go ahead again using the output files/streams

               response.DocumentId = null;
               LEADDocument newDocument = null;

               // Try to load the document, fail gracefully if this did not work
               try
               {
                  if (useStreams)
                  {
                     newDocument = DocumentFactory.LoadFromStream(outputDocumentStream, loadOptions);
                     response.DocumentId = newDocument.DocumentId;
                  }
                  else
                  {
                     // Temp files or external resource paths
                     if (outputDocumentFileName != null)
                     {
                        newDocument = DocumentFactory.LoadFromFile(outputDocumentFileName, loadOptions);
                        response.DocumentId = newDocument.DocumentId;
                     }
                  }

                  if (newDocument != null)
                  {
                     newDocument.AutoDeleteFromCache = false;
                     newDocument.AutoDisposeDocuments = true;
                     newDocument.AutoSaveToCache = false;
                     newDocument.SaveToCache();
                     newDocument.Dispose();
                     newDocument = null;
                  }
               }
               catch (Exception ex) { Console.WriteLine(ex.Message); }
               finally
               {
                  if (newDocument != null)
                     newDocument.Dispose();
               }
            }

            return response;
         }
         catch (Exception ex)
         {
            Trace.WriteLine(string.Format("Convert - Error:{1}{0}documentId:{2}", Environment.NewLine, ex.Message, documentId), "Error");
            throw;
         }
         finally
         {
            if (converter != null)
               converter.Dispose();

            if (cacheHasExternalResources && !externalResourceSaveSuccessful)
            {
               try
               {
                  // Finish adding to the cache
                  if (externalResourceDocumentKey != null)
                     cache.EndAddExternalResource(false, externalResourceDocumentKey, false, null, externalResourceRegion);

                  // Delete region if we did not convert the document successfully
                  if (externalResourceRegion != null)
                     cache.DeleteRegion(externalResourceRegion);
               }
               catch (Exception ex)
               {
                  Trace.WriteLine(string.Format("Removing External Resources - Error:{1}{0}documentId:{2}", Environment.NewLine, ex.Message, documentId), "Warning");
               }
            }

            // Delete temporary files, if they exist
            ServiceHelper.SafeDeleteFile(outputDocumentTempFileName);
         }
      }
#endif // #if LEADTOOLS_V22_OR_LATER
      private static string CheckSetupDocumentConverterExePath()
      {
         // Check if we are running on a machine that has LEADTOOLS installed, try to get the path automatically
         var appPath = Directory.GetCurrentDirectory();

         string[] setupBinDirs =
         {
            @"..\..\..\..\..\..\Bin\DotNet4\Win32",
            @"..\..\..\..\..\..\Bin\DotNet4\x64",
            $"..\\..\\..\\..\\..\\..\\Bin{ServiceHelper.LTVersion}\\DotNet4\\Win32",
            $"..\\..\\..\\..\\..\\..\\Bin{ServiceHelper.LTVersion}\\DotNet4\\x64"
         };

         const string setupExeName = @"DocumentConverterDemo_original.exe";
         foreach (var setupBinDir in setupBinDirs)
         {
            var dir = Path.GetFullPath(Path.Combine(appPath, setupBinDir));
            if (Directory.Exists(dir))
            {
               var exePath = Path.Combine(dir, setupExeName);
               if (File.Exists(exePath))
                  return exePath;
            }
         }

         // Check if we are the LEADTOOLS ZIP file using LEADTOOLS NuGet packages and the DocumentConverterDemo has been built in its default location.
         string[] zipBinDirs =
         {
            @"..\..\..\..\dotnet-framework\DocumentConverterDemo\bin\Debug",
            @"..\..\..\..\dotnet-framework\DocumentConverterDemo\bin\Release",
            @"..\..\..\..\dotnet-framework\DocumentConverterDemo\bin\x64\Debug",
            @"..\..\..\..\dotnet-framework\DocumentConverterDemo\bin\x64\Release",
            @"..\..\..\..\dotnet-framework\DocumentConverterDemo\bin\x86\Debug",
            @"..\..\..\..\dotnet-framework\DocumentConverterDemo\bin\x86\Release",
            @"..\..\..\..\..\DotNet\CS\DocumentConverterDemo\bin\Debug",
            @"..\..\..\..\..\DotNet\CS\DocumentConverterDemo\bin\Release",
            @"..\..\..\..\..\DotNet\CS\DocumentConverterDemo\bin\x64\Debug",
            @"..\..\..\..\..\DotNet\CS\DocumentConverterDemo\bin\x64\Release",
            @"..\..\..\..\..\DotNet\CS\DocumentConverterDemo\bin\x86\Debug",
            @"..\..\..\..\..\DotNet\CS\DocumentConverterDemo\bin\x86\Release"
         };

         const string zipExeName = @"DocumentConverterDemo.exe";
         foreach (var zipBinDir in zipBinDirs)
         {
            var dir = Path.GetFullPath(Path.Combine(appPath, zipBinDir));
            if (Directory.Exists(dir))
            {
               var exePath = Path.Combine(dir, zipExeName);
               if (File.Exists(exePath))
                  return exePath;
            }
         }

         // Not found
         return null;
      }

      private static DocumentConverterJobData CopyCommonDocumentConverterJobData(ServiceDocumentConverterJobData serviceJobData, string userToken, string outputDocumentName, int documentPagesCount)
      {
         // Set the most common properties from ServiceDocumentConverterJobData to DocumentConverterJobData
         var documentJobData = new DocumentConverterJobData();

         // We are either converting to a Document format or Raster format.
         bool isConvertingToDocumentFormat = serviceJobData.DocumentFormat != DocumentFormat.User;
         if (isConvertingToDocumentFormat)
         {
            documentJobData.DocumentFormat = serviceJobData.DocumentFormat;
            documentJobData.RasterImageFormat = RasterImageFormat.Unknown;
         }
         else
         {
            documentJobData.RasterImageFormat = serviceJobData.RasterImageFormat;
            documentJobData.RasterImageBitsPerPixel = serviceJobData.RasterImageBitsPerPixel;
            documentJobData.DocumentFormat = DocumentFormat.User;
         }

         documentJobData.JobName = string.IsNullOrWhiteSpace(serviceJobData.JobName) ? "DocumentConversion" : serviceJobData.JobName;
         documentJobData.AnnotationsMode = serviceJobData.AnnotationsMode;

         /* 
          * For demonstration purposes, we limit the maximum number of pages for the conversion.
          * This value can be changed in local.config.
          * If only using external conversion, we could simply set a single property (see other #REF_MaximumPages)
          * But since we may be doing in-process work, we do the manual calculations here.
          * 
          * If the first page is 0, conversion will change it to page 1.
          * If the last page is 0 or -1, conversion will change it to the last page.
          */

         // Default to "0" if none is set in the config file
         int maximumPages = ServiceHelper.GetSettingInteger(ServiceHelper.Key_DocumentConverter_MaximumPages, 0);

         var firstPageNumber = serviceJobData.InputDocumentFirstPageNumber;
         var lastPageNumber = serviceJobData.InputDocumentLastPageNumber;

         // Not required for conversion - just for the math below.
         if (firstPageNumber == 0)
            firstPageNumber = 1;

         if (lastPageNumber < 1)
            lastPageNumber = documentPagesCount;
         if (maximumPages > 0)
            lastPageNumber = Math.Min(lastPageNumber, firstPageNumber + maximumPages - 1);

         // If we're converting to a raster format and don't support multi-page, ensure we're only converting one page.
         if (!isConvertingToDocumentFormat && !RasterCodecs.FormatSupportsMultipageSave(documentJobData.RasterImageFormat))
         {

            int pageCount = lastPageNumber - firstPageNumber + 1;
            if (pageCount != 1)
               throw new Exception("Chosen raster format does not support multiple pages");
         }

         documentJobData.InputDocumentFirstPageNumber = firstPageNumber;
         documentJobData.InputDocumentLastPageNumber = lastPageNumber;
         documentJobData.UserToken = userToken;

         UploadDocumentOptions uploadOptions = new UploadDocumentOptions();
         uploadOptions.DocumentId = serviceJobData.OutputDocumentId;
         uploadOptions.UserToken = userToken;
         uploadOptions.Name = outputDocumentName;
         documentJobData.UploadDocumentOptions = uploadOptions;


         /* Remaining to set:
          * - Document - to convert from
          * 
          * if using external resources or temp files:
          * - OutputDocumentFileName
          * - OutputAnnotationsFileName
          * 
          * if using streaming:
          * - OutputDocumentStream
          * - OutputAnnotationsStream
          * 
          * Not Needed:
          * - InputAnnotationsFileName (using Document)
          * - InputDocumentFileName (using Document)
          * - UserData
          */
         return documentJobData;
      }


      private static bool IsArchiveRequired(ConversionOutput output, string documentExtension)
      {
         // Check if we need to archive the result (put in a ZIP file)
         if (output.Files == null || output.Files.Length < 1)
            throw new Exception("No output files");

         // If we have more than 1 file
         if (output.Files.Length > 1)
            return true;

         // If it's not one of the known extensions
         switch (documentExtension)
         {
            // These raster formats are OK
            case "gif":
            case "jpg":
            case "tif":
            case "bmp":

            // These document formats are OK
            case "pdf":
            case "png":
            case "doc":
            case "docx":
            case "xls":
            case "xlsx":
            case "ppt":
            case "pptx":
            case "svg":
            case "txt":
               return false;

            default:
               return true;
         }
      }

      private static void ArchiveResults(string archiveName, ObjectCache cache, string documentId, string[] outputFiles, string cacheRegion, ConvertResponse response)
      {
         string cacheKey = null;
         var beginAdd = false;

         try
         {
            cacheKey = archiveName;
            // Create a new cache item to archive the file(s)
            var archiveFileName = ServiceHelper.GetFileUri(cache.BeginAddExternalResource(cacheKey, cacheRegion, true));
            beginAdd = true;

            // Create an archive and add the files
            ZipTools.ZipFiles(Path.GetDirectoryName(archiveFileName), outputFiles, archiveFileName);

            cache.EndAddExternalResource(true, cacheKey, true, ServiceHelper.CacheManager.CreatePolicy(cache), cacheRegion);
            beginAdd = false;

            var url = CacheController.CreateConversionResultUri(cacheRegion, cacheKey);
            var fileName = ServiceHelper.GetFileUri(cache.GetItemExternalResource(cacheKey, cacheRegion, false));
            response.Archive = new ConvertItem
            {
               Name = cacheKey,
               Url = url,
               MimeType = "multipart/x-zip",
               Length = GetFileLength(fileName)
            };
         }
         finally
         {
            try
            {
               // Delete cache item if we haven't committed yet
               if (beginAdd)
                  cache.EndAddExternalResource(false, cacheKey, false, ServiceHelper.CacheManager.CreatePolicy(cache), cacheRegion);
            }
            catch (Exception ex)
            {
               Trace.WriteLine(string.Format("EndAdd - Error:{1}{0}documentId:{2}", Environment.NewLine, ex.Message, documentId), "Warning");
            }
         }
      }

      private static bool CanSaveToCache(ConversionOutput output, string extension, DocumentConverterJob job)
      {
         if (output.DocumentFiles != null && output.DocumentFiles.Length > 1)
            return false;
         if (output.AnnotationFiles != null && output.AnnotationFiles.Length > 1)
            return false;
         if (output.DocumentExtraFiles != null && output.DocumentExtraFiles.Length > 0)
            return false;
         if (job != null)
         {
            var jobData = job.JobData;
            if (jobData.DocumentFormat == DocumentFormat.Html || jobData.RasterImageFormat == RasterImageFormat.Htm)
               return false;
         }

         return true;
      }

      private static long GetFileLength(string fileName)
      {
         return new FileInfo(fileName).Length;
      }

      private static string GetMimeType(string fileName, string extension)
      {
         // Check the extension if it is a TXT file - must faster than doing Info
         if (extension != null && extension.ToLower() == "txt")
            return "text/plain";

         try
         {
            using (var rasterCodecs = new RasterCodecs())
            {
               var format = rasterCodecs.GetFormat(fileName);
               return RasterCodecs.GetMimeType(format);
            }
         }
         catch
         {
            return "application/octet-stream";
         }
      }

      private static string GetMimeType(DocumentFormat format)
      {
         if (format == DocumentFormat.User)
            return null;

         var extension = DocumentWriter.GetFormatFileExtension(format);
         return RasterCodecs.GetExtensionMimeType(extension);
      }

      public static DocumentOptions GetDocumentOptions(DocumentFormat documentFormat, JObject options)
      {
         // Get the type
         if (options == null)
            return null;

         Type documentOptionsType = null;
         switch (documentFormat)
         {
            case DocumentFormat.Ltd:
               documentOptionsType = typeof(LtdDocumentOptions);
               break;

            case DocumentFormat.Pdf:
               documentOptionsType = typeof(PdfDocumentOptions);
               break;

            case DocumentFormat.Doc:
               documentOptionsType = typeof(DocDocumentOptions);
               break;

            case DocumentFormat.Rtf:
               documentOptionsType = typeof(RtfDocumentOptions);
               break;

            case DocumentFormat.Html:
               documentOptionsType = typeof(HtmlDocumentOptions);
               break;

            case DocumentFormat.Text:
               documentOptionsType = typeof(TextDocumentOptions);
               break;

            case DocumentFormat.Emf:
               documentOptionsType = typeof(EmfDocumentOptions);
               break;

            case DocumentFormat.Xps:
               documentOptionsType = typeof(XpsDocumentOptions);
               break;

            case DocumentFormat.Docx:
               documentOptionsType = typeof(DocxDocumentOptions);
               break;

            case DocumentFormat.Xls:
               documentOptionsType = typeof(XlsDocumentOptions);
               break;

            case DocumentFormat.Pub:
               documentOptionsType = typeof(PubDocumentOptions);
               break;

            case DocumentFormat.Mob:
               documentOptionsType = typeof(MobDocumentOptions);
               break;

            case DocumentFormat.Svg:
               documentOptionsType = typeof(SvgDocumentOptions);
               break;

            case DocumentFormat.AltoXml:
               documentOptionsType = typeof(AltoXmlDocumentOptions);
               break;

            case DocumentFormat.User:
            default:
               break;
         }

         if (documentOptionsType != null)
         {
            return options.ToObject(documentOptionsType) as DocumentOptions;
         }
         else
            return null;
      }

      public static RunConvertJobResponse RunConvertJob(string documentId, IHeaderDictionary headers, ServiceDocumentConverterJobData serviceJobData)
      {
         // Verify input - we must have a document ID and service job data (from the client).
         if (string.IsNullOrEmpty(documentId))
            throw new ArgumentNullException("documentId");
         if (serviceJobData == null)
            throw new ArgumentNullException("serviceJobData");

         // Check the format. DocumentFormat.User and RasterImageFormat.Unknown are provided for custom implementation.
         // One (but not both) must be a standard value - that's what we'll convert to.
         bool isConvertingToDocumentFormat = serviceJobData.DocumentFormat != DocumentFormat.User;
         bool isConvertingToRasterFormat = serviceJobData.RasterImageFormat != RasterImageFormat.Unknown;
         if (!isConvertingToDocumentFormat && !isConvertingToRasterFormat)
            throw new ArgumentException("Either DocumentFormat must not be User or RasterImageFormat must not be Unknown", "serviceJobData");
         if (isConvertingToDocumentFormat && isConvertingToRasterFormat)
            throw new ArgumentException("Either DocumentFormat must be User or RasterImageFormat must be Unknown", "serviceJobData");

         ObjectCache documentCache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(documentId);
         if (documentCache == null)
            throw new InvalidOperationException("Document cache not found.");

         ObjectCache statusCache = ServiceHelper.CacheManager.DefaultCache;
         if (statusCache == null)
            throw new InvalidOperationException("Default cache not found.");

         bool useStreams = ServiceHelper.GetSettingBoolean(ServiceHelper.Key_DocumentConverter_ForceStreaming);

         // Verify that we can continue with our current arguments.
         CheckCapabilities(false, true, false, serviceJobData);

         DocumentConverterJobData documentJobData;

         string documentName = !string.IsNullOrWhiteSpace(serviceJobData.DocumentName) ? serviceJobData.DocumentName : "file";

         // Load the source document from the provided ID.
         string userToken = ServiceHelper.GetUserToken(headers, null);
         var loadFromCacheOptions = new LoadFromCacheOptions
         {
            Cache = documentCache,
            DocumentId = documentId,
            UserToken = userToken
         };
         using (LEADDocument sourceDocument = DocumentFactory.LoadFromCache(loadFromCacheOptions))
         {
            // Fill in some of the simple properties between the serviceJobData and DocumentJobData
            documentJobData = CopyCommonDocumentConverterJobData(serviceJobData, userToken, documentName, sourceDocument.Pages.Count);
            documentJobData.DocumentId = sourceDocument.DocumentId;
         }

         StatusJobData jobData = StatusJobDataRunner.ToStatusJobData(documentJobData);

         DocumentWriter documentWriter = null;

         // If converting to a document format, get the conversion options for this format and add them to the converter.
         if (isConvertingToDocumentFormat)
         {
            // Document Writer
            documentWriter = new DocumentWriter();
            // Convert the Document Options
            var documentOptions = ConverterHelper.GetDocumentOptions(documentJobData.DocumentFormat, serviceJobData.DocumentOptions);
            if (documentOptions != null)
            {
               documentWriter.SetOptions(documentJobData.DocumentFormat, documentOptions);
               ServiceHelper.SetDocumentWriterOptions(documentWriter, documentJobData.DocumentFormat);
            }
         }

         // OCR
         IOcrEngine ocrEngine = ServiceHelper.GetOCREngine();

         var converter = new DocumentConverter();
         converter.Options.PageNumberingTemplate = serviceJobData.PageNumberingTemplate;
         converter.Options.EnableSvgConversion = serviceJobData.EnableSvgConversion;
         converter.Options.SvgImagesRecognitionMode = serviceJobData.SvgImagesRecognitionMode;
         converter.Options.EmptyPageMode = serviceJobData.EmptyPageMode;
         converter.Options.JobErrorMode = serviceJobData.JobErrorMode;
         converter.Preprocessor.Deskew = serviceJobData.PreprocessorDeskew;
         converter.Preprocessor.Orient = serviceJobData.PreprocessorOrient;
         converter.Preprocessor.Invert = serviceJobData.PreprocessorInvert;
         converter.Diagnostics.EnableTrace = Debugger.IsAttached;

         converter.SetAnnRenderingEngineInstance(ServiceHelper.GetAnnRenderingEngine());
         converter.SetDocumentWriterInstance(documentWriter);
         converter.SetOcrEngineInstance(ocrEngine, false);
         jobData.DocumentConverter = converter;

         jobData.UserToken = DocumentFactory.NewCacheId();
         jobData.JobToken = DocumentFactory.NewCacheId();
         var policy = ServiceHelper.CacheManager.CreatePolicy(statusCache);
         jobData.StatusCacheItemPolicy = policy;
         // Only one of input, status, and output cache needs to be set.
         jobData.InputCache = documentCache;
         jobData.StatusCache = statusCache;
         // We will use the document cache for now and redirect it to the correct cache based on the UploadDocumentOptions if we need to
         jobData.OutputCache = documentCache;
         jobData.OutputCacheItemPolicy = ServiceHelper.CacheManager.CreatePolicy(jobData.OutputCache);

         jobData.OutputDocumentName = documentName;

         // Run the conversion
         var runner = new StatusJobDataRunner();

         try
         {
            runner.Prepare(jobData);
         }
         catch (Exception ex)
         {
            Trace.WriteLine(string.Format("RunConvertJob - Error:{1}{0}documentId:{2}", Environment.NewLine, ex.Message, documentId), "Error");
            throw;
         }

         ThreadPool.QueueUserWorkItem((object state) =>
         {
            EventHandler<DocumentConverterJobEventArgs> jobOperation = (sender, e) =>
            {
               // See if we need to redirect the output cache
               if (!e.IsPostOperation && e.Operation == DocumentConverterJobOperation.BeginUploadDocument)
               {
                  UploadDocumentOptions uploadDocumentOptions = e.Job.JobData.UploadDocumentOptions;
                  ObjectCache outCache = ServiceHelper.CacheManager.GetCacheForBeginUpload(uploadDocumentOptions);
                  if (outCache != null)
                  {
                     uploadDocumentOptions.Cache = outCache;
                     uploadDocumentOptions.CachePolicy = ServiceHelper.CacheManager.CreatePolicy(outCache);
                  }
               }
            };

            DocumentConverter runnerDocumentConverter = runner.DocumentConverter;
            runnerDocumentConverter.Jobs.JobOperation += jobOperation;
            try
            {
               runner.Run();
            }
            finally
            {

               runner.Dispose();
               if (runnerDocumentConverter != null)
               {
                  runnerDocumentConverter.Jobs.JobOperation -= jobOperation;
                  runnerDocumentConverter.Dispose();
               }
            }
         });

         return new RunConvertJobResponse
         {
            UserToken = jobData.UserToken,
            JobToken = jobData.JobToken
         };
      }
   }
}
