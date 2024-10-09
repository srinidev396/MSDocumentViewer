// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System.IO;
using System.Web;

using Leadtools;
using Leadtools.Codecs;

namespace Leadtools.DocumentViewer.Tools.Helpers
{
   internal static class ImageSaver
   {
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      public static Stream SaveImage(RasterImage image, RasterCodecs rasterCodecs, SaveImageFormat saveFormat, string mimeType, int bitsPerPixel, int qualityFactor, object Response)
      {
         // We need to find out the format, bits/pixel and quality factor
         // If the user gave as a format, use it
         if (saveFormat == null)
         {
            // If the user did not give us a format, use PNG
            saveFormat = new PngImageFormat();
            mimeType = "image/png";
         }

         saveFormat.PrepareToSave(rasterCodecs, image, bitsPerPixel, qualityFactor);

         // Save it to a memory stream
         var ms = new MemoryStream();
         rasterCodecs.Save(image, ms, saveFormat.ImageFormat, saveFormat.BitsPerPixel);
         return PrepareStream(ms, mimeType, Response);
      }

      public static Stream PrepareStream(Stream stream, string mimeType, object Response=null)
      {
         stream.Position = 0;

         // Set the MIME type and Content-Type if there is a valid web context
#if NET
         var reponse = Response as Microsoft.AspNetCore.Http.HttpResponse;
#else
         var reponse = HttpContext.Current?.Response;
#endif
         if (reponse != null)
         {
            reponse.ContentType = mimeType;
            reponse.Headers.Add("ContentLength", stream.Length.ToString());
         }
         return stream;
      }

         public static string GetMimeType(SaveImageFormat saveFormat)
      {
         // If the user gave as a format, use it
         if (saveFormat == null)
            // If the user did not give us a format, use PNG
            saveFormat = new PngImageFormat();

         string mimeType = saveFormat.MimeType;
         return mimeType;
      }
   }
}
