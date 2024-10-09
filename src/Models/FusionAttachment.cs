using Leadtools;
using Microsoft.SqlServer.Server;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using System;
using Leadtools.Document.Unstructured;
using Leadtools.Codecs;
using System.Collections.Generic;

namespace DocumentService.Models
{
    public class FusionAttachment
    {
        public string filepath { get; set; }
        public CodecsImageInfo Info { get; set; }
        public static Size FlyoutSize =  new Size(450, 590);
        public static String FileNotFound = "File not found";
        public static RasterImageFormat TranslateToLeadToolsFormat(Format format, int bitsPerPixel)
        {
            switch (format)
            {
                case Format.Bmp:
                    {
                        return RasterImageFormat.Bmp;
                    }
                case Format.Gif:
                    {
                        return RasterImageFormat.Gif;
                    }
                case Format.Png:
                    {
                        return RasterImageFormat.Png;
                    }
                case Format.Tif:
                    {
                        if (bitsPerPixel <= 2)
                            return RasterImageFormat.TifxFaxG4;
                        return RasterImageFormat.Tif;
                    }

                default:
                    {
                        return RasterImageFormat.Jpeg;
                    }
            }
        }
        public static void DrawTextOnErrorImage(Bitmap bmp, string message)
        {
            if (String.IsNullOrEmpty(message)) return;
            using (Graphics gfx = Graphics.FromImage(bmp))
            {
                using(Font arial = new Font("Arial", 10))
                {
                    using(StringFormat fmt = StringFormatting(StringAlignment.Center, false))
                    {
                        SizeF fSize = gfx.MeasureString(message, arial, new SizeF(bmp.Width - 6, bmp.Height - 6), fmt);

                        gfx.DrawString(message, arial, Brushes.Black, new RectangleF((bmp.Width - fSize.Width) / 2, 4, fSize.Width, fSize.Height), fmt);
                    }
                }
            }
        }


        private static StringFormat StringFormatting(StringAlignment alignment, Boolean useEllipsis)
        {
        var sf = StringFormat.GenericTypographic;
            sf.Alignment = alignment;
            sf.LineAlignment = StringAlignment.Center;
            sf.Trimming = StringTrimming.None;
            sf.FormatFlags = StringFormatFlags.FitBlackBox;

            if (useEllipsis) 
            {
                sf.Trimming = StringTrimming.EllipsisPath;
            }

            return sf;
        }

        public static int ConvertBitsPerPixel(Format format, int bitsPerPixel)
        {
            switch (format)
            {
                case (Format)OutputFormat.Bmp:
                    {
                        if (bitsPerPixel < 4)
                            return 24;
                        return bitsPerPixel;
                    }

                case (Format)OutputFormat.Gif:
                    {
                        return 8;
                    }

                case (Format)OutputFormat.Png:
                    {
                        return 24;
                    }

                case (Format)OutputFormat.Tif:
                    {
                        return bitsPerPixel;
                    }

                default:
                    {
                        return 24;
                    }
            }
        }
        
    }
    public class FusionCodeImageInfo
    {
        public string FilePath { get; set; }
        public int TotalPages { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public long SizeDisk { get; set; }
        public bool Ispcfile { get; set; }
    }

    public class DocumentViewrApiModel
    {
        public int TotalPages { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public long SizeDisk { get; set; }
        public string FilePath { get; set; }
        public List<string> stringPath { get; set; }
        public string fileName { get; set; }
        public string serverPath { get; set; }
    }


    public enum Format
    {
        Htm = 0x44D,
        Xml = 0x47E,
        Text = 0x514,
        Bmp = 0x5D,
        Tif = 0x5DD,
        Gif = 0x5DF,
        Jpg = 0x5FF,
        Png = 0x626
    }

    public enum OutputFormat
    {
        Htm = 0x44D,
        Xml = 0x47E,
        Text = 0x514,
        Bmp = 0x5D,
        Tif = 0x5DD,
        Gif = 0x5DF,
        Jpg = 0x5FF,
        Png = 0x626
    }

}
