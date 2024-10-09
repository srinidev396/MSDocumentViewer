// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using Leadtools.DocumentViewer.Models.Images;
using Leadtools.Services.Tools.Exceptions;
using Leadtools.DocumentViewer.Tools.Helpers;
using Leadtools.Services.Tools.Helpers;
using Leadtools;
using Leadtools.Document;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Leadtools.Caching;
using Leadtools.Codecs;

namespace Leadtools.DocumentViewer.Controllers
{
   /// <summary>
   /// Used with the DocumentImages class of the LEADTOOLS Document JavaScript library.
   /// </summary>
   public class ImagesController : Controller
   {
      /* These endpoints will not necessarily return objects,
       * since most of the time the returned streams
       * will be set directly to a URL.
       */



      public ImagesController()
      {
         ServiceHelper.InitializeController();
      }

      /// <summary>
      /// Retrieves thumbnails as a grid, instead of as individual images.
      /// The grid is determined by the page number range passed.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The thumbnails could not be retrieved")]
      [HttpGet("api/[controller]/[action]"), AlwaysCorsFilter]
      public IActionResult GetThumbnailsGrid([FromQuery] GetThumbnailsGridRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request", "must not be null");

         if (string.IsNullOrEmpty(request.DocumentId))
            throw new ArgumentException("documentId", "must not be null");

         if (request.FirstPageNumber < 0)
            throw new ArgumentException("'firstPageNumber' must be a value greater than or equal to 0");

         var firstPageNumber = request.FirstPageNumber;
         var lastPageNumber = request.LastPageNumber;

         // Default is page 1 and -1
         if (firstPageNumber == 0)
            firstPageNumber = 1;
         if (lastPageNumber == 0)
            lastPageNumber = -1;

         if (request.Width < 0 || request.Height < 0)
            throw new ArgumentException("'width' and 'height' must be value greater than or equal to 0");
         if (request.MaximumGridWidth < 0)
            throw new ArgumentException("'maximumGridWidth' must be a value greater than or equal to 0");

         // Get the image format
         var saveFormat = SaveImageFormat.GetFromMimeType(request.MimeType);

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

            if (request.Width > 0 && request.Height > 0)
               document.Images.ThumbnailPixelSize = new LeadSize(request.Width, request.Height);

            using (var image = document.Images.GetThumbnailsGrid(firstPageNumber, lastPageNumber, request.MaximumGridWidth))
            {
               Stream stream = ImageSaver.SaveImage(image, document.RasterCodecs, saveFormat, request.MimeType, 0, 0, Response);
               ServiceHelper.UpdateCacheSettings(cache, HttpContext.Response);
               return File(stream, ImageSaver.GetMimeType(saveFormat));
            }
         }
      }

      /// <summary>
      /// Loads an image and returns it in one of the specified formats.
      /// </summary>
      /// <param name="file">The source file.</param>
      /// <returns>A stream containing the image data.</returns>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The file blob data could not be uploaded")]
      [HttpPost("api/[controller]/[action]")]
      public FileResult LoadImageFile(IFormFile file)
      {
         if (file == null)
            throw new ArgumentNullException("file");

         using (Stream stream = file.OpenReadStream())
            return LoadImageStream(stream, file.FileName);
      }

      private FileResult LoadImageStream(Stream stream, string fileName = null, int pageNumber = 1, int resolution = 0, string mimeType = null, int bitsPerPixel = 0, int qualityFactor = 0, int imageWidth = 4000, int imageHeight = 4000)
      {
         if (stream == null)
            throw new ArgumentNullException("stream");

         var page = pageNumber;
         // Default is page 1
         if (page == 0)
            page = 1;

         if (resolution < 0)
            throw new ArgumentOutOfRangeException("resolution", "must be a value greater than or equals to 0");

         // Sanity check on other parameters
         if (qualityFactor < 0 || qualityFactor > 100)
            throw new ArgumentOutOfRangeException("qualityFactor", "must be a value between 0 and 100");

         if (imageWidth < 0)
            throw new ArgumentOutOfRangeException("width", "must be a value greater than or equal to 0");
         if (imageHeight < 0)
            throw new ArgumentOutOfRangeException("height", "must be a value greater than or equal to 0");

         // Get the image format
         SaveImageFormat saveFormat = SaveImageFormat.GetFromMimeType(mimeType);

         using (var codecs = new RasterCodecs())
         {
            ServiceHelper.SetRasterCodecsOptions(codecs, resolution);

            if (!string.IsNullOrEmpty(fileName))
               codecs.Options.Load.Name = fileName;

            using (RasterImage image = codecs.Load(stream, 0, CodecsLoadByteOrder.BgrOrGray, page, page))
            {
               // Resize it (will only resize if both width and height are not 0), will also take care of FAX images (with different resolution)
               ImageResizer.ResizeImage(image, imageWidth, imageHeight);

               // We need to find out the format, bits/pixel and quality factor
               // If the user gave as a format, use it
               if (saveFormat == null)
               {
                  // If the user did not give us a format, use PNG
                  saveFormat = new PngImageFormat();
                  mimeType = "image/png";
               }

               saveFormat.PrepareToSave(codecs, image, bitsPerPixel, qualityFactor);

               using (MemoryStream ms = new MemoryStream())
               {
                  codecs.Save(image, ms, saveFormat.ImageFormat, saveFormat.BitsPerPixel);
                  return File(ms.ToArray(), saveFormat.MimeType);
               }
            }
         }
      }
   }
}
