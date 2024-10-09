// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.
// All Rights Reserved.
// *************************************************************
using System;
using Microsoft.AspNetCore.Mvc;

using Leadtools.Services.Models.Compare;
using Leadtools.Services.Tools.Exceptions;
using Leadtools.Document;
using Leadtools.Document.Compare;
using Leadtools.DocumentViewer.Tools.Helpers;
using Leadtools.Services.Tools.Helpers;
using System.Collections.Generic;
using System.IO;
using Leadtools.Services.Models;
using Leadtools.Caching;
using Leadtools.ImageProcessing;
using Leadtools.Codecs;

namespace Leadtools.DocumentViewer.Controllers
{
   public class CompareController : Controller
   {
      public CompareController()
      {
         ServiceHelper.InitializeController();
      }

      /// <summary>
      /// Returns a report based on a DocumentDifference object.
      /// </summary>
      /// <param name="request">A <see cref="GenerateReportRequest">GenerateReportRequest</see> containing the report information.</param>
      /// <returns>The report in the requested mime type</returns>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "Report generation failed")]
      [HttpPost("api/[controller]/[action]")]
      public IActionResult GenerateReport(GenerateReportRequest request)
      {
         if (request == null) throw new ArgumentNullException(nameof(request));
         if (string.IsNullOrWhiteSpace(request.Mimetype)) throw new ArgumentNullException($"{nameof(request)} Mimetype");
         if (request.Differences == null) throw new ArgumentNullException($"{nameof(request)} Differences");

         var stream = new MemoryStream();
         switch (request.Mimetype)
         {
            case "text/markdown":
               if (request.Options == null) request.Options = new MarkdownReportOptions();

               var markdownOptions = request.Options as MarkdownReportOptions;
               request.Differences.GenerateMarkdownReport(stream, markdownOptions);
               break;
            default:
               throw new ArgumentException("Invalid mimetype provided", nameof(request));
         }

         stream.Position = 0;
         ObjectCache cache = ServiceHelper.CacheManager.DefaultCache;
         ServiceHelper.UpdateCacheSettings(cache, HttpContext.Response);

         return File(stream, request.Mimetype);
      }

      /// <summary>
      /// Compares two string objects.
      /// </summary>
      /// <param name="request">A <see cref="StringCompareRequest">StringCompareRequest</see> containing the string inputs.</param>
      /// <returns>A list of text differences.</returns>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "Compare failed.")]
      [HttpPost("api/[controller]/[action]")]
      public StringCompareResponse StringCompare(StringCompareRequest request)
      {
         if (request == null) throw new ArgumentNullException(nameof(request));
         if(request.Inputs.Length != 2)
            throw new ArgumentException("There must be exactly 2 input strings", "Inputs");

         var comparer = new DocumentComparer();
         var results = comparer.CompareText(request.Inputs);

         return new StringCompareResponse()
         {
            TextDifferences = results
         };
      }

      /// <summary>
      /// Runs an asynchronous compare job
      /// </summary>
      /// <param name="request">A <see cref="RunCompareJobRequest">RunCompareJobRequest</see> containing the job data.</param>
      /// <returns>A <see cref="RunCompareJobResponse">RunCompareJobResponse</see> containing cache information about the job.</returns>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "Compare failed.")]
      [HttpPost("api/[controller]/[action]")]
      public RunCompareJobResponse Run(RunCompareJobRequest request)
      {
         if (request == null) throw new ArgumentNullException(nameof(request));
         return CompareHelper.RunCompareJob(this.Request.Headers, request.JobData);
      }

      /// <summary>
      /// Query the status of a running job
      /// </summary>
      /// <param name="request">A <see cref="QueryCompareJobRequest">QueryCompareJobRequest</see> containing a job's cache information.</param>
      /// <returns>A <see cref="QueryCompareJobResponse">QueryCompareJobResponse</see> containing the current job information. </returns>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "Compare failed.")]
      [HttpPost("api/[controller]/[action]")]
      public QueryCompareJobResponse Query(QueryCompareJobRequest request)
      {
         if (request == null) throw new ArgumentNullException(nameof(request));

         ObjectCache cache = ServiceHelper.CacheManager.DefaultCache;
         var response = new QueryCompareJobResponse()
         {
            JobData = CompareJobRunner.QueryJobStatus(cache, request.UserToken, request.JobToken)
         };

         return response;
      }

      /// <summary>
      /// Deletes all information from the cache about a job
      /// </summary>
      /// <param name="request">A <see cref="DeleteCompareJobRequest">DeleteCompareJobRequest</see> containing a job's cache information.</param>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "Compare failed.")]
      [HttpPost("api/[controller]/[action]")]
      public Response Delete(DeleteCompareJobRequest request)
      {
         if (request == null) throw new ArgumentNullException(nameof(request));

         ObjectCache cache = ServiceHelper.CacheManager.DefaultCache;
         CompareJobRunner.DeleteJob(cache, request.UserToken, request.JobToken);
         return new Response();
      }

      /// <summary>
      /// Aborts a currently running job
      /// </summary>
      /// <param name="request">A <see cref="AbortCompareJobRequest">AbortCompareJobRequest</see> containing a job's cache information.</param>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "Compare failed.")]
      [HttpPost("api/[controller]/[action]")]
      public Response Abort(AbortCompareJobRequest request)
      {
         if (request == null) throw new ArgumentNullException(nameof(request));

         ObjectCache cache = ServiceHelper.CacheManager.DefaultCache;
         CompareJobRunner.AbortJob(cache, request.UserToken, request.JobToken);
         return new Response();
      }

      /// <summary>
      /// Compares two DocumentPage images.
      /// </summary>
      /// <param name="request">A <see cref="RasterCompareRequest">RasterCompareRequest</see> containing raster compare information.</param>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "Compare failed.")]
      [HttpPost("api/[controller]/[action]")]
      public RasterCompareResponse CompareRasterPage(RasterCompareRequest request)
      {
         if (request == null || string.IsNullOrWhiteSpace(request.ModifiedDocumentId) || string.IsNullOrWhiteSpace(request.OriginalDocumentId))
            throw new ArgumentNullException(nameof(request));

         string userToken = ServiceHelper.GetUserToken(this.Request.Headers, request);

         ObjectCache origCache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.OriginalDocumentId);
         var origCacheOptions = new LoadFromCacheOptions()
         {
            DocumentId = request.OriginalDocumentId,
            Cache = origCache,
            UserToken = userToken
         };

         ObjectCache modCache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.ModifiedDocumentId);
         var modCacheOptions = new LoadFromCacheOptions()
         {
            DocumentId = request.ModifiedDocumentId,
            Cache = modCache,
            UserToken = userToken
         };

         RasterImage outputImage = null;

         try
         {
            using (LEADDocument original = DocumentFactory.LoadFromCache(origCacheOptions))
            using (LEADDocument modified = DocumentFactory.LoadFromCache(modCacheOptions))
            {
               DocumentHelper.CheckLoadFromCache(original);
               DocumentHelper.CheckLoadFromCache(modified);

               if (request.OriginalPageNumber < 1 || request.OriginalPageNumber > original.Pages.Count)
                  throw new ArgumentOutOfRangeException(nameof(request.OriginalPageNumber));

               if (request.ModifiedPageNumber < 1 || request.ModifiedPageNumber > modified.Pages.Count)
                  throw new ArgumentOutOfRangeException(nameof(request.ModifiedPageNumber));

               DocumentPage originalPage = original.Pages[request.OriginalPageNumber - 1];
               DocumentPage modifiedPage = modified.Pages[request.ModifiedPageNumber - 1];

               var compareOptions = new RasterCompareOptions()
               {
                  OriginalOffset = request.OriginalOffset,
                  OriginalRotationAngle = request.OriginalRotationAngle,
                  ModifiedOffset = request.ModifiedOffset,
                  ModifiedRotationAngle = request.ModifiedRotationAngle,
                  Threshold = request.Threshold,
               };

               if (!string.IsNullOrWhiteSpace(request.OriginalBackground))
                  compareOptions.OriginalBackground = RasterColor.FromHtml(request.OriginalBackground);

               if (!string.IsNullOrWhiteSpace(request.OriginalForeground))
                  compareOptions.OriginalForeground = RasterColor.FromHtml(request.OriginalForeground);

               if (!string.IsNullOrWhiteSpace(request.ModifiedBackground))
                  compareOptions.ModifiedBackground = RasterColor.FromHtml(request.ModifiedBackground);

               if (!string.IsNullOrWhiteSpace(request.ModifiedForeground))
                  compareOptions.ModifiedForeground = RasterColor.FromHtml(request.ModifiedForeground);

               if (!string.IsNullOrWhiteSpace(request.OutputExternal))
                  compareOptions.OutputExternal = RasterColor.FromHtml(request.OutputExternal);

               if (!string.IsNullOrWhiteSpace(request.OutputBackground))
                  compareOptions.OutputBackground = RasterColor.FromHtml(request.OutputBackground);

               if (!string.IsNullOrWhiteSpace(request.OutputMatch))
                  compareOptions.OutputMatch = RasterColor.FromHtml(request.OutputMatch);

               if (!string.IsNullOrWhiteSpace(request.OutputAddition))
                  compareOptions.OutputAddition = RasterColor.FromHtml(request.OutputAddition);

               if (!string.IsNullOrWhiteSpace(request.OutputDeletion))
                  compareOptions.OutputDeletion = RasterColor.FromHtml(request.OutputDeletion);

               if (!string.IsNullOrWhiteSpace(request.OutputChange))
                  compareOptions.OutputChange = RasterColor.FromHtml(request.OutputChange);

               outputImage = new DocumentComparer().CompareRasterPage(new List<DocumentPage>() { originalPage, modifiedPage }, compareOptions);
            }

            string uploadedDocumentId = UploadOutputImage(outputImage, request, userToken);
            return new RasterCompareResponse()
            {
               OutputDocumentId = uploadedDocumentId
            };
         }
         finally
         {
            if (outputImage != null)
               outputImage.Dispose();
         }
      }

      private static string UploadOutputImage(RasterImage outputImage, RasterCompareRequest request, string userToken)
      {
         using (var outputImageStream = new MemoryStream())
         {
            using (var rasterCodecs = new RasterCodecs())
            {
               rasterCodecs.Save(outputImage, outputImageStream, RasterImageFormat.Png, 0);
            }

            outputImageStream.Position = 0;
            long dataLength = outputImageStream.Length;

            UploadDocumentOptions uploadDocumentOptions = new UploadDocumentOptions();
            uploadDocumentOptions.DocumentId = request.OutputDocumentId;
            uploadDocumentOptions.DocumentDataLength = dataLength;
            uploadDocumentOptions.PageCount = 1;
            uploadDocumentOptions.EnableStreaming = true;
            uploadDocumentOptions.UserToken = userToken;
            uploadDocumentOptions.MimeType = RasterCodecs.GetMimeType(RasterImageFormat.Png);
            ObjectCache outCache = ServiceHelper.CacheManager.GetCacheForBeginUpload(uploadDocumentOptions);
            uploadDocumentOptions.Cache = outCache;
            uploadDocumentOptions.CachePolicy = ServiceHelper.CacheManager.CreatePolicy(outCache);

            Uri documentUri = DocumentFactory.BeginUpload(uploadDocumentOptions);
            var buffer = new byte[1024 * 1024];
            int bytes = 0;
            do
            {
               bytes = outputImageStream.Read(buffer, 0, buffer.Length);
               if (bytes > 0)
                  DocumentFactory.UploadDocument(outCache, documentUri, buffer, 0, bytes);
            }
            while (bytes > 0);

            DocumentFactory.EndUpload(outCache, documentUri);

            return DocumentFactory.GetLeadCacheData(documentUri);
         }
      }

      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "Failed to generate image")]
      [HttpGet("api/[controller]/[action]")]
      public IActionResult GetColorPage([FromQuery] GetCompareImageRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         var pageNumber = request.PageNumber;

         if (string.IsNullOrEmpty(request.DocumentId))
            throw new ArgumentException("documentId must not be null");

         if (pageNumber < 0)
            throw new ArgumentException("'pageNumber' must be a value greater than or equal to 0");

         // Default is page 1
         if (pageNumber == 0)
            pageNumber = 1;

         // Now load the document
         ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.DocumentId);
         var loadFromCacheOptions = new LoadFromCacheOptions
         {
            Cache = cache,
            DocumentId = request.DocumentId,
            UserToken = ServiceHelper.GetUserToken(this.Request.Headers, request)
         };
         using (var document = DocumentFactory.LoadFromCache(loadFromCacheOptions))
         {
            DocumentHelper.CheckLoadFromCache(document);
            DocumentHelper.CheckPageNumber(document, pageNumber);

            var documentPage = document.Pages[pageNumber - 1];
            using (var image = documentPage.GetImage())
            {
               ColorResolutionCommand cmd = new ColorResolutionCommand()
               {
                  Mode = ColorResolutionCommandMode.CreateDestinationImage,
                  BitsPerPixel = 1,
                  Order = RasterByteOrder.Rgb,
                  DitheringMethod = RasterDitheringMethod.None,
                  PaletteFlags = ColorResolutionCommandPaletteFlags.UsePalette,
               };
               cmd.SetPalette(new RasterColor[] {
                  RasterColor.White,
                  RasterColor.Black,
               });
               cmd.Run(image);

               cmd.DestinationImage.SetPalette(new RasterColor[] {
                  RasterColor.FromHtml(request.BackgroundColor),
                  RasterColor.FromHtml(request.ForegroundColor),
               }, 0, 2);

               var stream = ImageSaver.SaveImage(cmd.DestinationImage, document.RasterCodecs, null, null, 1, 0, Response);
               return File(stream, ImageSaver.GetMimeType(null));
            }
         }
      }
   }
}
