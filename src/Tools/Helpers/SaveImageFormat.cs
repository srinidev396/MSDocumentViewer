// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Globalization;

using Leadtools;
using Leadtools.Codecs;

namespace Leadtools.DocumentViewer.Tools.Helpers
{
   // Helper classes for wrapping the image formats
   internal abstract class SaveImageFormat
   {
      protected SaveImageFormat() { }

      // bpp and qf are the input values by the user, with these and the loaded image, find out the
      // BitsPerPixel and QualityFactor to use when creating the new image
      public abstract void PrepareToSave(RasterCodecs codecs, RasterImage image, int bpp, int qf);

      // LEADTOOLS uses 0, 2->255 FOR JPG/CMP and 0->9 for PNG. This method normalizes a value between 0 and 100
      // to the correct value required by RasterCodecs
      internal static int NormalizeQualityFactor(int qf, int min, int max, int def)
      {
         if (qf == 0)
            return def;
         else if (qf == 100)
            return max;
         else
         {
            // Normalize between norm1 and norm2
            return (qf - 1) * (max - min - 1) / 98 + min;
         }
      }

      // Derived classes must set these, these are the format, bits per pixel, order and quality factor to use in RasterCodecs.Save
      public RasterImageFormat ImageFormat { get; set; }
      public int BitsPerPixel { get; set; }
      public abstract string MimeType { get; }

      // Create one of our derived classes based on the user passed mime type
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
      public static SaveImageFormat GetFromMimeType(string mimeType)
      {
         if (string.IsNullOrEmpty(mimeType))
            return null; // We will figure out later

         switch (mimeType.ToLowerInvariant())
         {
            case "image/jpeg":
               return new JpegImageFormat();

            case "image/png":
               return new PngImageFormat();

            case "image/gif":
               return new GifImageFormat();

            case "image/tiff":
               return new TifImageFormat();

            case "image/x-lead-cmp":
               return new CmpImageFormat();

            case "image/bmp":
               return new BmpImageFormat();

            case "application/pdf":
               return new PdfImageFormat();

            case "image/x-jpeg-2000":
               return new J2kImageFormat();

            case "image/x-lead-cmw":
               return new CmwImageFormat();

            case "image/x-jpeg-xr":
               return new JxrImageFormat();

            case "image/x-xps":
               return new XpsImageFormat();

            default:
               throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "'{0}' is an invalid format", mimeType));
         }
      }
   }

   // For saving JPEG images
   internal class JpegImageFormat : SaveImageFormat
   {
      public JpegImageFormat()
      {
      }

      public override string MimeType
      {
         get { return RasterCodecs.GetMimeType(RasterImageFormat.Jpeg422); }
      }

      public override void PrepareToSave(RasterCodecs codecs, RasterImage image, int bpp, int qf)
      {
         // We will always use JPEG 422. This provides the best ratio between quality and performance and
         // supported by all major browsers
         ImageFormat = RasterImageFormat.Jpeg422;
         int qualityFactor;

         if (bpp == 12 || bpp == 16)
         {
            BitsPerPixel = bpp;
            // Gray scale image. QualityFactor 0 (lossless) is the only one supported. Note, none of the browsers currently
            // in the market supports lossless JPEGs
            qualityFactor = 0;
         }
         else
         {
            // Only other BPP supported is 8 or 24, so if it is not any of those, use 24
            if (bpp != 8 && bpp != 24)
               BitsPerPixel = 24;

            // Set the quality factor
            qualityFactor = NormalizeQualityFactor(qf, 2, 255, 20);
         }

         // And set the quality factor
         codecs.Options.Jpeg.Save.QualityFactor = qualityFactor;
      }
   }

   // For saving LEAD CMP images
   internal class CmpImageFormat : JpegImageFormat
   {
      public override string MimeType
      {
         get { return RasterCodecs.GetMimeType(RasterImageFormat.Cmp); }
      }

      public override void PrepareToSave(RasterCodecs codecs, RasterImage image, int bpp, int qf)
      {
         // Same as JPEG, just set the format to CMP
         base.PrepareToSave(codecs, image, bpp, qf);
         ImageFormat = RasterImageFormat.Cmp;
         codecs.Options.Jpeg.Save.CmpQualityFactorPredefined = CodecsCmpQualityFactorPredefined.Custom;
      }
   }

   // For saving JPEG 2000 images
   internal class J2kImageFormat : JpegImageFormat
   {
      public override string MimeType
      {
         get { return RasterCodecs.GetMimeType(RasterImageFormat.J2k); }
      }

      public override void PrepareToSave(RasterCodecs codecs, RasterImage image, int bpp, int qf)
      {
         // Same as JPEG, just set the format to CMP
         base.PrepareToSave(codecs, image, bpp, qf);
         ImageFormat = RasterImageFormat.J2k;
      }
   }

   // For saving LEAD CMW images
   internal class CmwImageFormat : JpegImageFormat
   {
      public override string MimeType
      {
         get { return RasterCodecs.GetMimeType(RasterImageFormat.Cmw); }
      }

      public override void PrepareToSave(RasterCodecs codecs, RasterImage image, int bpp, int qf)
      {
         // Same as JPEG, just set the format to CMP
         base.PrepareToSave(codecs, image, bpp, qf);
         ImageFormat = RasterImageFormat.Cmw;
      }
   }

   // For saving PNG images
   internal class PngImageFormat : SaveImageFormat
   {
      public PngImageFormat() { }

      public override string MimeType
      {
         get { return RasterCodecs.GetMimeType(RasterImageFormat.Png); }
      }

      public override void PrepareToSave(RasterCodecs codecs, RasterImage image, int bpp, int qf)
      {
         // Set the format
         ImageFormat = RasterImageFormat.Png;
         // And the quality factor
         codecs.Options.Png.Save.QualityFactor = NormalizeQualityFactor(qf, 0, 9, 2);

         if (bpp != 0)
         {
            // User passed an explicit value, check if PNG supports it
            if (bpp == 1 || bpp == 4 || bpp == 8 || bpp == 24 || bpp == 32 || bpp == 48 || bpp == 64)
               BitsPerPixel = bpp;  // Yes
            else
               BitsPerPixel = 32;   // No, use default value
         }
         else
         {
            // User did not pass a value, we need to figure it out from the image to produce an image that does not require more
            // size than the original keeping the quality the same.
            if (image.BitsPerPixel == 1)
               BitsPerPixel = 1;
            else if (image.BitsPerPixel <= 4)
               BitsPerPixel = 4;
            else if (image.BitsPerPixel <= 8)
               BitsPerPixel = 8;
            else if (image.BitsPerPixel <= 24)
               BitsPerPixel = 24;
            else
               BitsPerPixel = image.BitsPerPixel;
         }
      }
   }

   // For saving GIF images
   internal class GifImageFormat : SaveImageFormat
   {
      public GifImageFormat() { }

      public override string MimeType
      {
         get { return RasterCodecs.GetMimeType(RasterImageFormat.Gif); }
      }

      public override void PrepareToSave(RasterCodecs codecs, RasterImage image, int bpp, int qf)
      {
         // Set the format
         ImageFormat = RasterImageFormat.Gif;

         if (bpp != 0)
         {
            // User passed an explicit value, check if GIF supports it
            if (bpp <= 8)
               BitsPerPixel = bpp;  // Yes
            else
               BitsPerPixel = 8;    // No, use default value
         }
         else
         {
            // User did not pass a value, we need to figure it out from the image to produce an image that does not require more
            // size than the original keeping the quality the same.
            if (image.BitsPerPixel == 1)
               BitsPerPixel = 1;
            else if (image.BitsPerPixel <= 4)
               BitsPerPixel = 4;
            else
               BitsPerPixel = 8;
         }
      }
   }

   // For saving BMP images
   internal class BmpImageFormat : SaveImageFormat
   {
      public BmpImageFormat() { }

      public override string MimeType
      {
         get { return RasterCodecs.GetMimeType(RasterImageFormat.Bmp); }
      }

      public override void PrepareToSave(RasterCodecs codecs, RasterImage image, int bpp, int qf)
      {
         // Set the format
         ImageFormat = RasterImageFormat.Bmp;

         if (bpp != 0)
         {
            // User passed an explicit value, check if BMP supports it
            if (bpp == 1 || bpp == 4 || bpp == 8 || bpp == 16 || bpp == 24 || bpp == 32)
               BitsPerPixel = bpp;  // Yes
            else
               BitsPerPixel = 24;   // No, use default BPP
         }
         else
         {
            // User did not pass a value, we need to figure it out from the image to produce an image that does not require more
            // size than the original keeping the quality the same.
            if (image.BitsPerPixel == 1)
               BitsPerPixel = 1;
            else if (image.BitsPerPixel <= 1)
               BitsPerPixel = 4;
            else if (image.BitsPerPixel <= 8)
               BitsPerPixel = 8;
            else if (image.BitsPerPixel <= 16)
               BitsPerPixel = 16;
            else if (image.BitsPerPixel == 32)
               BitsPerPixel = 32;
            else
               BitsPerPixel = 24;
         }
      }
   }

   // For saving PDF images (Raster PDF's)
   internal class PdfImageFormat : SaveImageFormat
   {
      public PdfImageFormat() { }

      public override string MimeType
      {
         get { return RasterCodecs.GetMimeType(RasterImageFormat.RasPdf); }
      }

      public override void PrepareToSave(RasterCodecs codecs, RasterImage image, int bpp, int qf)
      {
         // Set the quality factor, we might use JPEG compression, so use the same QF as JPEG images
         codecs.Options.Jpeg.Save.QualityFactor = NormalizeQualityFactor(qf, 0, 9, 2);

         if (bpp != 0)
         {
            // User passed an explicit value, check if PDF supports it
            if (bpp == 1 || bpp == 4 || bpp == 8 || bpp == 24)
               BitsPerPixel = bpp;  // Yes
            else
               BitsPerPixel = 24;   // No, default BPP
         }
         else
         {
            // User did not pass a value, we need to figure it out from the image to produce an image that does not require more
            // size than the original keeping the quality the same.
            if (image.BitsPerPixel == 1)
               BitsPerPixel = 1;
            else if (image.BitsPerPixel <= 4)
               BitsPerPixel = 4;
            else if (image.BitsPerPixel <= 8)
               BitsPerPixel = 8;
            else
               BitsPerPixel = 24;
         }

         // For 1-bpp images, we will use FAX G4 compression inside the PDF
         // For 4 or 8-bpp, we will use LZW compression
         // For anything else (24-bpp), we will use JPEG 422
         // These values produce the smallest size PDF while keeping the image quality high.
         if (BitsPerPixel == 1)
            ImageFormat = RasterImageFormat.RasPdfG4;
         else if (BitsPerPixel == 4 || BitsPerPixel == 8)
            ImageFormat = RasterImageFormat.RasPdfLzw;
         else
            ImageFormat = RasterImageFormat.RasPdfJpeg422;
      }
   }

   // For saving XPS images
   internal class XpsImageFormat : SaveImageFormat
   {
      public XpsImageFormat() { }

      public override string MimeType
      {
         get { return RasterCodecs.GetMimeType(RasterImageFormat.Xps); }
      }

      public override void PrepareToSave(RasterCodecs codecs, RasterImage image, int bpp, int qf)
      {
         // Set the format
         ImageFormat = RasterImageFormat.Xps;

         if (bpp != 0)
         {
            // User passed an explicit value, check if XPS supports it
            if (bpp == 1 || bpp == 4 || bpp == 8 || bpp == 24 || bpp == 32)
               BitsPerPixel = bpp;  // Yes
            else
               BitsPerPixel = 32;   // No BPP
         }
         else
         {
            // User did not pass a value, we need to figure it out from the image to produce an image that does not require more
            // size than the original keeping the quality the same.
            if (image.BitsPerPixel == 1)
               BitsPerPixel = 1;
            else if (image.BitsPerPixel <= 4)
               BitsPerPixel = 4;
            else if (image.BitsPerPixel <= 8)
               BitsPerPixel = 8;
            else if (image.BitsPerPixel == 24)
               BitsPerPixel = 24;
            else
               BitsPerPixel = 32;
         }
      }
   }

   // For saving JXR (JPEG XR) images
   internal class JxrImageFormat : SaveImageFormat
   {
      public JxrImageFormat() { }

      public override string MimeType
      {
         get { return RasterCodecs.GetMimeType(RasterImageFormat.Jxr); }
      }

      public override void PrepareToSave(RasterCodecs codecs, RasterImage image, int bpp, int qf)
      {
         if (bpp != 0)
         {
            // User passed an explicit value, check if JXR supports it
            if (bpp == 1 || bpp == 8 || bpp == 16 || bpp == 24 || bpp == 32 || bpp == 48 || bpp == 64)
               BitsPerPixel = bpp;  // Yes
            else
               BitsPerPixel = 24;   // No, default BPP
         }
         else
         {
            // User did not pass a value, we need to figure it out from the image to produce an image that does not require more
            // size than the original keeping the quality the same.
            if (image.BitsPerPixel == 1)
               BitsPerPixel = 1;
            else if (image.BitsPerPixel <= 8)
               BitsPerPixel = 8;
            else if (image.BitsPerPixel <= 16)
               BitsPerPixel = 16;
            else
               BitsPerPixel = image.BitsPerPixel;
         }

         // Set the format based on the BPP
         if (BitsPerPixel == 1)
            ImageFormat = RasterImageFormat.Jxr;
         else if (BitsPerPixel == 8 || BitsPerPixel == 16)
            ImageFormat = (image.Order == RasterByteOrder.Gray) ? RasterImageFormat.JxrGray : RasterImageFormat.Jxr422;
         else
            ImageFormat = RasterImageFormat.Jxr422;
      }
   }

   // For saving TIF images
   internal class TifImageFormat : SaveImageFormat
   {
      public TifImageFormat() { }

      public override string MimeType
      {
         get { return RasterCodecs.GetMimeType(RasterImageFormat.Tif); }
      }

      public override void PrepareToSave(RasterCodecs codecs, RasterImage image, int bpp, int qf)
      {
         // Set the quality factor, we might use JPEG compression, so use the same QF as JPEG images
         codecs.Options.Jpeg.Save.QualityFactor = NormalizeQualityFactor(qf, 2, 255, 20);

         if (bpp != 0)
         {
            // User passed an explicit value, check if JXR supports it
            if ((bpp >= 1 && bpp <= 8) || bpp == 12 || bpp == 16 || bpp == 24 || bpp == 32 || bpp == 48 || bpp == 64)
               BitsPerPixel = bpp;  // Yes
            else
               BitsPerPixel = 24;   // No, default BPP
         }
         else
         {
            // User did not pass a value, we need to figure it out from the image to produce an image that does not require more
            // size than the original keeping the quality the same.
            if (image.BitsPerPixel == 1)
               BitsPerPixel = 1;
            else if (image.BitsPerPixel <= 8)
               BitsPerPixel = 8;
            else if (image.BitsPerPixel == 12)
               BitsPerPixel = 12;
            else if (image.BitsPerPixel == 16)
               BitsPerPixel = 16;
            else
               BitsPerPixel = image.BitsPerPixel;
         }

         // Set the format base don the bpp:
         // 1-bpp, use FAX Group 4 compression
         // Up to 16-bpp, use LZW compression
         // Anything else, use JPEG 422
         // These values produce the smallest size TIF while keeping the image quality high.

         if (BitsPerPixel == 1)
            ImageFormat = RasterImageFormat.CcittGroup4;
         else if (BitsPerPixel <= 16)
            ImageFormat = RasterImageFormat.TifLzw;
         else
            ImageFormat = RasterImageFormat.TifJpeg422;
      }
   }
}
