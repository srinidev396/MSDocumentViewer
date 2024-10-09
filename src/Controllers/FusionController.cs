using Leadtools.Document;
using Leadtools;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Diagnostics;
using Leadtools.Demos;
using Leadtools.Codecs;
using Leadtools.Caching;
//using Leadtools.WinForms;
using Leadtools.DocumentViewer.Models.Factory;
using Leadtools.Services.Tools.Exceptions;
using Leadtools.Services.Tools.Helpers;
using Leadtools.Services.Models;
using Leadtools.Services.Models.PreCache;
using System.Net.Mime;
using System.IO.Packaging;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.VisualBasic;
//using static Microsoft.AspNetCore.Razor.Language.TagHelperMetadata;
//using Microsoft.SharePoint.Client;
using Leadtools.Drawing;
using System.Drawing;
using DocumentService.Models;
//using Leadtools.WinForms;
using System.Windows.Forms;
using Leadtools.Document.Converter;
using Leadtools.Annotations.Engine;
using Microsoft.AspNetCore.Http.Extensions;
using System.Xml;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Leadtools.Document.Writer;
using Microsoft.SharePoint.Client;
using Leadtools.DocumentViewer.Controllers;



namespace DocumentService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class FusionController : ControllerBase
    {
        private static ObjectCache Cache { get; }
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FusionController> _logger;
        public FusionController(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env,ILogger<FusionController> logger)
        {
            ServiceHelper.InitializeController();
            _httpContextAccessor = httpContextAccessor;
            _env = env;
            _logger = logger;
        }
        [HttpPost]
        [Route("SaveTempFileTocache")]
        public async Task<string> SaveTempFileTocache([FromBody] List<DocumentViewrApiModel> model)
        {

            string documentId = null;
            try
            {
                CreateDocumentOptions createOptions = new CreateDocumentOptions();
                createOptions.Cache = ServiceHelper.CacheManager.DefaultCache;
                createOptions.UseCache = true;

                using (Leadtools.Document.LEADDocument document = DocumentFactory.Create(createOptions))
                {

                    document.Name = "TempDocument";
                    document.AutoDeleteFromCache = false;
                    document.AutoDisposeDocuments = true;

                    foreach (var item in model)
                    {
                        LoadDocumentOptions loadOptions = new LoadDocumentOptions();
                        //var filePath = Path.Combine($@"{_env.WebRootPath}\TempFiles", "annotation.xml");
                        //System.IO.File.WriteAllText(filePath, item.Annotaionxml);

                        //var annUri = new Uri(filePath);//new
                        //loadOptions.AnnotationsUri = annUri; //new

                        loadOptions.Cache = createOptions.Cache;
                        var child = DocumentFactory.LoadFromFile(item.FilePath, loadOptions);
                        child.AutoDeleteFromCache = false;
                        child.AutoDisposeDocuments = true;
                        child.SaveToCache();
                        await Task.WhenAll();
                        // loop through each file and build document
                        foreach (var page in child.Pages)
                            document.Pages.Add(page);
                    }

                    document.SaveToCache();

                    documentId = document.DocumentId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
           

            return documentId;
        }
        [HttpGet]
        [Route("GetStreamFlyOutFirstPage")]
        public async Task<FileStreamResult> GetStreamFlyOutFirstPage(string filePath, string fullPath, bool validAttachment)
        {
            FileStreamResult filestramresult = null;
            try
            {
                bool stampWithMessage = false;

                var bmp = Properties.Resources.NotAvailableLarge;//Smead.RecordsManagement.Imaging.Export.Output.NotAvailableImage();
                if (!validAttachment)
                {
                    if (!string.IsNullOrEmpty(fullPath) & System.IO.File.Exists(fullPath))
                    {
                        var format = Format.Jpg;
                        return await Task.Run(() =>
                        {
                            using (var codec = new RasterCodecs())
                            {
                                using (var img = codec.Load(fullPath, 1))
                                {

                                    var rc = new Rectangle(0, 0, FusionAttachment.FlyoutSize.Width, FusionAttachment.FlyoutSize.Height);

                                    if (img.BitsPerPixel <= 2)
                                        format = Format.Tif;
                                    if (img.Width < FusionAttachment.FlyoutSize.Width || img.Height < FusionAttachment.FlyoutSize.Height)
                                    {
                                        rc.Width = img.Width;
                                        rc.Height = img.Height;
                                    }
                                    //rc = RasterImageList.GetFixedAspectRatioImageRectangle(img.Width, img.Height, rc);
                                    rc.Width = img.Width;
                                    rc.Height = img.Height;

                                    var command = new Leadtools.ImageProcessing.ResizeCommand();
                                    command.Flags = RasterSizeFlags.None;
                                    command.DestinationImage = new RasterImage(RasterMemoryFlags.Conventional, rc.Width, rc.Height, img.BitsPerPixel, img.Order, img.ViewPerspective, img.GetPalette(), IntPtr.Zero, 0L);
                                    command.Run(img);

                                    codec.Save(command.DestinationImage, filePath, FusionAttachment.TranslateToLeadToolsFormat(format, img.BitsPerPixel), FusionAttachment.ConvertBitsPerPixel(format, img.BitsPerPixel));

                                    var reason = RasterImageConverter.TestCompatible(command.DestinationImage, true);
                                    var pf = RasterImageConverter.GetNearestPixelFormat(command.DestinationImage);
                                    if (reason != ImageIncompatibleReason.Compatible)
                                        RasterImageConverter.MakeCompatible(command.DestinationImage, pf, true);

                                    using (Bitmap bmp1 = (Bitmap)RasterImageConverter.ConvertToImage(command.DestinationImage, ConvertToImageOptions.None))
                                    {
                                        using (var stream = new System.IO.MemoryStream())
                                        {
                                            bmp1.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                                            return new FileStreamResult(new System.IO.MemoryStream(stream.ToArray()), "image/jpg");
                                        }
                                    }
                                }
                            }
                        });
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            if (filePath.ToLower().StartsWith(FusionAttachment.FileNotFound.ToLower()))
                            {
                                bmp = Properties.Resources.Invalid;
                                if (stampWithMessage)
                                    FusionAttachment.DrawTextOnErrorImage(bmp, FusionAttachment.FileNotFound);
                            }
                            else
                            {

                                if (bmp is null)
                                    bmp = Properties.Resources.NotAvailableLarge;//Smead.RecordsManagement.Imaging.Export.Output.NotAvailableImage();
                                if (stampWithMessage)
                                    FusionAttachment.DrawTextOnErrorImage(bmp, filePath);
                            }
                        }
                        else
                        {
                            if (bmp is null)
                                bmp = Properties.Resources.NotAvailableLarge;
                            if (stampWithMessage)
                                FusionAttachment.DrawTextOnErrorImage(bmp, "File Not Found");
                        }

                        using (var stream = new System.IO.MemoryStream())
                        {
                            bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                            filestramresult = new FileStreamResult(new System.IO.MemoryStream(stream.ToArray()), "image/jpg");
                        }
                    }
                }
                else
                {
                    return await Task.Run(() =>
                    {
                        using (var codec = new RasterCodecs())
                        {
                            using (var img = codec.Load(filePath, 1))
                            {
                                var reason = RasterImageConverter.TestCompatible(img, true);
                                var pf = RasterImageConverter.GetNearestPixelFormat(img);
                                if (reason != ImageIncompatibleReason.Compatible)
                                    RasterImageConverter.MakeCompatible(img, pf, true);

                                using (Bitmap bmp1 = (Bitmap)RasterImageConverter.ConvertToImage(img, ConvertToImageOptions.None))
                                {
                                    using (var stream = new System.IO.MemoryStream())
                                    {
                                        bmp1.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                                        return new FileStreamResult(new System.IO.MemoryStream(stream.ToArray()), "image/jpg");
                                    }
                                }
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return filestramresult;
        }
        [HttpGet]
        [Route("GetCodecInfoFromFile")]
        //not in use
        public DocumentViewrApiModel GetCodecInfoFromFile(string fileName, string extension)
        {
            var model = new DocumentViewrApiModel();
            try
            {
                var codec = new RasterCodecs();
                var info = codec.GetInformation(fileName, true);
                if (IsAPCFile(info.Format, extension))
                    return null;
                model.TotalPages = info.TotalPages;
                model.SizeDisk = info.SizeDisk;
                model.Width = info.Width;
                model.Height = info.Height;
                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }
        [HttpGet]
        [Route("CheckFileHealth")]
        private bool CheckFileHealth(string filepath)
        {
            bool isFileCorrupted = false;
            try
            {
                using (RasterCodecs codec = new RasterCodecs())
                {
                    using (RasterImage img = codec.Load(filepath, 1))
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message == "Invalid file format")
                    isFileCorrupted = false;
                else
                {
                    isFileCorrupted = true;
                }
                _logger.LogError($"isInvalidFileFormat {isFileCorrupted} (if false then the file is corrupted; Exception: {ex.Message}");
            }
            return isFileCorrupted;
        }

        [HttpPost]
        [Route("GetCodecInfoFromFileList")]
        //not in use
        public List<FusionCodeImageInfo> GetCodecInfoFromFileList([FromBody] List<string> Filepathlist)
        {
            var lst = new List<FusionCodeImageInfo>();
            try
            {
                foreach (string path in Filepathlist)
                {
                    var extension = Path.GetExtension(path).Trim();
                    var codec = new RasterCodecs();
                    var info = codec.GetInformation(path, true);
                    bool IsPcfile = IsAPCFile(info.Format, extension);
                    lst.Add(new FusionCodeImageInfo { FilePath = path, Height = info.Height, Width = info.Width, SizeDisk = info.SizeDisk, TotalPages = info.TotalPages, Ispcfile = IsPcfile});
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
            return lst;
        }

        [HttpGet]
        [Route("GetCacheLocation")]
        public string GetCacheLocation()
        {
            var x = ServiceHelper.GetAbsolutePath("wwwroot\\cache");
            return ServiceHelper.GetAbsolutePath("wwwroot\\cache");
        }
        [HttpPost]
        [Route("SaveTempPDFFileToDisk")]
        //public string SaveTempPDFFileToDisk(string filename, List<string> pathString, string serverPath)
        public string SaveTempPDFFileToDisk([FromBody] DocumentViewrApiModel model)
        {
            string filePath = string.Empty;
            try
            {
                int bitsPerPixel = 1;
                var codec = new RasterCodecs();
                codec.ThrowExceptionsOnInvalidImages = false;
                RasterImage srcImage = default;
                bool firstLoop = true;

                foreach (var img in model.stringPath)
                {
                    if (firstLoop == true)
                    {
                        srcImage = codec.Load(img, 0, CodecsLoadByteOrder.RgbOrGray, 1, -1);
                        if (srcImage.BitsPerPixel >= 2)
                            bitsPerPixel = 24;
                        if (srcImage.BitsPerPixel > 24)
                            bitsPerPixel = 24;
                        firstLoop = false;
                    }
                    else
                    {
                        using (RasterImage addpage = codec.Load(img, 0, CodecsLoadByteOrder.RgbOrGray, 1, -1))
                        {

                            if (addpage.BitsPerPixel >= 2)
                                bitsPerPixel = 24;
                            if (addpage.BitsPerPixel > 24)
                                bitsPerPixel = 24;
                            srcImage.AddPages(addpage, 1, addpage.PageCount);
                        }
                    }
                }

                string file = System.IO.Path.GetFileNameWithoutExtension(model.fileName);
                filePath = model.serverPath + file + ".pdf";

                codec.Save(srcImage, filePath, RasterImageFormat.RasPdf, bitsPerPixel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
            
            return filePath;


        }

        private static bool IsAPCFile(RasterImageFormat Format, string extension)
        {
            switch (Format)
            {
                case RasterImageFormat.RasPdf:
                case RasterImageFormat.RasPdfG31Dim:
                case RasterImageFormat.RasPdfG32Dim:
                case RasterImageFormat.RasPdfG4:
                case RasterImageFormat.RasPdfJbig2:
                case RasterImageFormat.RasPdfJpeg:
                case RasterImageFormat.RasPdfJpeg411:
                case RasterImageFormat.RasPdfJpeg422:
                case RasterImageFormat.RasPdfLzw:
                case RasterImageFormat.PdfLeadMrc:
                case RasterImageFormat.Eps:
                case RasterImageFormat.EpsPostscript:
                case RasterImageFormat.Postscript:
                case RasterImageFormat.RtfRaster:
                case RasterImageFormat.Docx:
                case RasterImageFormat.Doc:
                case RasterImageFormat.Xls:
                case RasterImageFormat.Xlsx:
                case RasterImageFormat.Unknown:
                    {
                        return true;
                    }
            }

            if (extension.StartsWith("."))
                extension = extension.Substring(1);

            switch (extension.ToLower() ?? "")
            {
                case "pdf":
                case "fdf":
                    {
                        return true;
                    }
                case "xps":
                    {
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

    }
}
