using System;
using System.IO;
using System.Net;
using System.Web;
using Leadtools.Codecs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using DocumentService;
using Microsoft.SharePoint.Client.Utilities;
using Leadtools.Services.Tools.Helpers;
using Leadtools.Ocr;
using Leadtools.DocumentViewer.Tools.Helpers;
using System.Drawing;
using System.Windows;
using System.Net.Http;
using Leadtools;
using Leadtools.Document.Writer;

namespace DocumentService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class OcrController : ControllerBase
    {
        [HttpGet]
        [Route("GetText")]
        public string GetText(Uri uri, int imageWidth, int imageHeight, int pageNumber = 1, int left = 0, int top = 0, int right = 0, int bottom = 0)
        {
            if (uri is null)
                throw new ArgumentNullException("uri");
            if (pageNumber < 0)
                throw new ArgumentOutOfRangeException("pageNumber", "must be a value greater than or equal to 0");
            int page = pageNumber;
            if (page == 0)
                page = 1;
            if (imageWidth < 0)
                throw new ArgumentOutOfRangeException("imageWidth", "must be a value greater than or equal to 0");
            if (imageHeight < 0)
                throw new ArgumentOutOfRangeException("imageHeight", "must be a value greater than or equal to 0");
            if (left < 0)
                throw new ArgumentOutOfRangeException("left", "must be a value greater than or equal to 0");
            if (top < 0)
                throw new ArgumentOutOfRangeException("top", "must be a value greater than or equal to 0");
            if (right < 0)
                throw new ArgumentOutOfRangeException("right", "must be a value greater than or equal to 0");
            if (bottom < 0)
                throw new ArgumentOutOfRangeException("bottom", "must be a value greater than or equal to 0");
            string tempFile = Path.GetTempFileName();
            try
            {
                if (uri.ToString().StartsWith(Conversions.ToString("á")))
                {
                    string Urid = Security.TabFusionSecure.DecryptURLParameters(uri.ToString());
                    uri = new Uri(Microsoft.SharePoint.Client.Utilities.HttpUtility.UrlKeyValueDecode(Urid));
                }
                using (var client = new WebClient())
                {
                    client.DownloadFile(uri, tempFile);
                }
               
                using (var codecs = new RasterCodecs())
                {
                    ServiceHelper.InitCodecs(codecs, ServiceHelper.DefaultResolution);

                    using (var ocrEngine = ServiceHelper.CreateOCREngine(codecs))
                    {
                        var rasterImage = codecs.Load(tempFile, pageNumber);

                        using (var ocrPage = ocrEngine.CreatePage(rasterImage,OcrImageSharingMode.None))
                        {

                            if (right != 0 && bottom != 0)
                            {
                                var bounds = LeadRect.FromLTRB(left, top, right, bottom);
                                ImageResizer.ResizeImage(rasterImage, ocrPage.Width, ocrPage.Height);
                                var resizer = ImageResizer.ImageResizerOcr(ocrPage.Width, ocrPage.Height, imageWidth, imageHeight);

                                if (resizer.IsNeeded)
                                   bounds = ImageResizer.ToImage(bounds, resizer);

                                var zone = new OcrZone();
                                zone.ZoneType = OcrZoneType.Text;
                                zone.Bounds = bounds;
                                ocrPage.Zones.Add(zone);
                            }
                            else
                            {
                                ocrPage.AutoPreprocess(OcrAutoPreprocessPageCommand.Invert, null);
                            }

                            ocrPage.Recognize(null);
                            string text = ocrPage.GetText(-1);
                            var currentContext = HttpContext;

                            if (currentContext != null)
                            {
                                currentContext.Response.ContentType = "text/plain";
                                currentContext.Response.Headers.Add("ContentLength", (text.Length * 2).ToString());
                            }

                            return text;
                        }
                    }
                }
            }

            finally
            {

                if (System.IO.File.Exists(tempFile))
                {
                    try
                    {
                        System.IO.File.Delete(tempFile);
                    }
                    catch
                    {
                    }
                }
            }
        }




    }
}