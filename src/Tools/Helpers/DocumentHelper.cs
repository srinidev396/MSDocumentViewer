// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using Leadtools;
using Leadtools.Caching;
using Leadtools.Document;
using Leadtools.Document.Converter;
using Leadtools.Document.Writer;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;

namespace Leadtools.Services.Tools.Helpers
{
   internal static class DocumentHelper
   {
      public static void CheckLoadFromCache(LEADDocument document)
      {
         if (document == null)
            throw new InvalidOperationException("Document was not found in the cache");
      }

      public static void CheckCacheInfo(DocumentCacheInfo cacheInfo)
      {
         if (cacheInfo == null)
            throw new InvalidOperationException("Document was not found in the cache");
      }

      public static void CheckPageNumber(LEADDocument document, int pageNumber)
      {
         if (pageNumber > document.Pages.Count)
            throw new ArgumentOutOfRangeException("pageNumber", pageNumber, "Must be a value between 1 and " + document.Pages.Count);
      }

      public static void DeleteDocument(string documentId, IHeaderDictionary headers, bool preventIfPreCached, bool throwIfNull)
      {
         ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(documentId);
         var loadFromCacheOptions = new LoadFromCacheOptions
         {
            Cache = cache,
            DocumentId = documentId,
            UserToken = ServiceHelper.GetUserToken(headers, null)
         };
         using (var document = DocumentFactory.LoadFromCache(loadFromCacheOptions))
         {
            if (throwIfNull)
               DocumentHelper.CheckLoadFromCache(document);

            if (document != null)
            {
               // Check if it's one of our pre-cached documents. 
               // If it is, don't remove it from the cache.
               if (PreCacheHelper.PreCacheExists && document.Uri != null)
               {
                  if (preventIfPreCached && PreCacheHelper.CheckDocument(document.Uri, document.Images.MaximumImagePixelSize) != null)
                  {
                     return;
                  }
                  else
                  {
                     PreCacheHelper.RemoveDocument(document.Uri, null);
                  }
               }

               document.AutoDeleteFromCache = true;
               // But not the children documents (if any)
               foreach (var child in document.Documents)
               {
                  child.AutoDeleteFromCache = false;
               }
               document.AutoDisposeDocuments = true;
            }
         }
      }

      public static DocumentFormFieldResources ToFormFieldResources(string resources)
      {
         if (resources == null || resources.Length == 0)
            return null;

         DocumentFormFieldResources result = JsonConvert.DeserializeObject<DocumentFormFieldResources>(resources);
         return result;
      }

      public static List<DocumentFormFieldsContainer> ToFormFields(string fieldsContainers)
      {
         if (fieldsContainers == null || fieldsContainers.Length == 0)
            return null;

         var containers = new List<DocumentFormFieldsContainer>();
         var jsonFieldsContainersArray = JArray.Parse(fieldsContainers);
         foreach (var item in jsonFieldsContainersArray)
         {
            if (item == null)
               continue;

            var containerValue = item["container"];
            if (containerValue != null)
            {
               var container = new DocumentFormFieldsContainer();
               container.PageNumber = containerValue["pageNumber"].Value<int>();
               var formsFields = JArray.Parse(containerValue["children"].Value<string>());

               foreach (dynamic fieldObj in formsFields)
               {
                  DocumentFormField documentFormField = null;
                  if (fieldObj.type == "DocumentTextFormField")
                     documentFormField = fieldObj.ToObject<DocumentTextFormField>();
                  else if (fieldObj.type == "DocumentButtonFormField")
                     documentFormField = fieldObj.ToObject<DocumentButtonFormField>();
                  else if (fieldObj.type == "DocumentChoiceFormField")
                     documentFormField = fieldObj.ToObject<DocumentChoiceFormField>();
                  else if (fieldObj.type == "DocumentSignatureFormField")
                     documentFormField = fieldObj.ToObject<DocumentSignatureFormField>();

                  if (documentFormField != null)
                  {
                     documentFormField.IsModified = true;
                     container.Children.Add(documentFormField);
                  }
               }

               if (container != null)
                  containers.Add(container);
            }
         }

         return containers;
      }

      public static void SignDocument(Stream stream, string mimeType)
      {
         if (mimeType != "application/pdf") return;

         var pdfSignatureFile = ServiceHelper.GetSettingValue(ServiceHelper.Key_Pdf_SignatureFile);
         if (!string.IsNullOrWhiteSpace(pdfSignatureFile))
         {
            stream.Seek(0, SeekOrigin.Begin);
            var pdfSignatureFilePassword = ServiceHelper.GetSettingValue(ServiceHelper.Key_Pdf_SignatureFilePassword);
            Leadtools.Pdf.PDFFile.SignDocument(stream, null, pdfSignatureFile, pdfSignatureFilePassword);
         }
      }

   }

   [Serializable]
   [DataContract]
   public class ServiceDocumentConverterJobData
   {
      public ServiceDocumentConverterJobData()
      {
         JobErrorMode = DocumentConverterJobErrorMode.Continue;
         PageNumberingTemplate = "##name##_Page(##page##).##extension##";
         EnableSvgConversion = true;
         SvgImagesRecognitionMode = DocumentConverterSvgImagesRecognitionMode.Auto;
      }

      [DataMember(Name = "jobErrorMode")]
      public DocumentConverterJobErrorMode JobErrorMode { get; set; }

      [DataMember(Name = "pageNumberingTemplate")]
      public string PageNumberingTemplate { get; set; }

      [DataMember(Name = "enableSvgConversion")]
      public bool EnableSvgConversion { get; set; }

      [DataMember(Name = "svgImagesRecognitionMode")]
      public DocumentConverterSvgImagesRecognitionMode SvgImagesRecognitionMode { get; set; }

      [DataMember(Name = "emptyPageMode")]
      public DocumentConverterEmptyPageMode EmptyPageMode { get; set; }

      [DataMember(Name = "preprocessorDeskew")]
      public bool PreprocessorDeskew { get; set; }

      [DataMember(Name = "preprocessorOrient")]
      public bool PreprocessorOrient { get; set; }

      [DataMember(Name = "preprocessorInvert")]
      public bool PreprocessorInvert { get; set; }

      [DataMember(Name = "inputDocumentFirstPageNumber")]
      public int InputDocumentFirstPageNumber { get; set; }

      [DataMember(Name = "inputDocumentLastPageNumber")]
      public int InputDocumentLastPageNumber { get; set; }

      [DataMember(Name = "documentFormat")]
      public DocumentFormat DocumentFormat { get; set; }

      [DataMember(Name = "rasterImageFormat")]
      public RasterImageFormat RasterImageFormat { get; set; }

      [DataMember(Name = "rasterImageBitsPerPixel")]
      public int RasterImageBitsPerPixel { get; set; }

      // We deserialize this field manually from JSON Object to a specific DocumentOptions type
      [DataMember(Name = "documentOptions")]
      public JObject DocumentOptions { get; set; }

      [DataMember(Name = "jobName")]
      public string JobName { get; set; }

      [DataMember(Name = "annotationsMode")]
      public DocumentConverterAnnotationsMode AnnotationsMode { get; set; }

      [DataMember(Name = "documentName")]
      public string DocumentName { get; set; }

      [DataMember(Name = "outputDocumentId")]
      public string OutputDocumentId { get; set; }

      [DataMember(Name = "annotations")]
      public string Annotations { get; set; }
   }
}
