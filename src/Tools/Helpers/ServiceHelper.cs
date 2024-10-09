// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Configuration;
using System.Globalization;
using System.Net;
using System.IO;
using System.Web;
using System.Reflection;
using System.Net.Http;
using System.Linq;
using System.Runtime.Serialization;
using System.Net.Mime;
using System.Net.Sockets;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Collections.Concurrent;

using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

using Leadtools.Codecs;
using Leadtools.Caching;
using Leadtools.Barcode;
using Leadtools.Document;
using Leadtools.Ocr;
using Leadtools.Annotations.Engine;
using Newtonsoft.Json;
using Leadtools.Document.Writer;
using Leadtools.Services.Tools.Cache;
using System.Text;
using Leadtools.Services.Models;
using Leadtools.DocumentViewer.Controllers;
using Leadtools.Document.Analytics;
using Leadtools.Document.Unstructured;
using System.Runtime.InteropServices;
using Leadtools.Demos;
#if !NET
using IHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#endif

namespace Leadtools.Services.Tools.Helpers
{
    public enum ServiceVendor
    {
        Dropbox = 0,
        GoogleDrive = 1,
        OneDrive = 2,
        SharePoint = 3
    }
    public enum OcrEngineStatus
    {
        /// <summary>
        ///   The OCR Engine was not set, and thus is not being used.
        /// </summary>
        Unset = 0,

        /// <summary>
        ///   An error occurred with the setup.
        /// </summary>
        Error = 1,

        /// <summary>
        ///   The OCR Engine should be working normally.
        /// </summary>
        Ready = 2
    }
    internal static class ServiceHelper
    {
#if LTV22_CONFIG
        public const int LTVersion = 22;
#elif LTV21_CONFIG
      public const int LTVersion = 21;
#endif

        public const string Key_CORS_Origins = "CORS.Origins";
        public const string Key_CORS_Headers = "CORS.Headers";
        public const string Key_CORS_Methods = "CORS.Methods";
        public const string Key_CORS_MaxAge = "CORS.MaxAge";

        public const string Key_Access_Passcode = "Access.Passcode";

        public const string Key_Application_DrawEngineType = "lt.Application.DrawEngineType";
        public const string Key_Application_ShadowFontMode = "lt.Application.ShadowFontMode";
        public const string Key_Application_ShadowFontsDirectory = "lt.Application.ShadowFontsDirectory";

        public const string Key_Application_AllowTempFilesFromDisk = "lt.Application.AllowTempFilesFromDisk";
        public const string Key_Application_TempDirectory = "lt.Application.TempDirectory";

        public const string Key_Application_ReturnRequestUserData = "lt.Application.ReturnRequestUserData";

        public const string Key_Application_UseDataRanges = "lt.Application.UseDataRanges";
        public const string Key_License_FilePath = "lt.License.FilePath";
        public const string Key_License_DeveloperKey = "lt.License.DeveloperKey";

        public const string Key_Cache_CacheManagerConfigFile = "lt.Cache.CacheManagerConfigFile";
        public const string Key_Cache_ConfigFile = "lt.Cache.ConfigFile";
        public const string Key_Cache_SlidingExpiration = "lt.Cache.SlidingExpiration";

        public const string Key_PreCache_DictionaryXml = "lt.PreCache.DictionaryXml";
        public const string Default_PreCache_DictionaryXml = @".\App_Data\PreCacheDictionary.xml";

        public const string Key_PreCache_Directory = "lt.PreCache.Directory";

        public const string Key_Document_MimeTypesFile = "lt.Document.MimeTypesFile";
        public const string Key_Document_OnlyAllowedMimeTypes = "lt.Document.OnlyAllowedMimeTypes";
        public const string Key_Document_AutoUpdateHistory = "lt.Document.AutoUpdateHistory";

        public const string Key_Document_MemoryCache_IsEnabled = "lt.Document.MemoryCache.IsEnabled";
        public const string Key_Document_MemoryCache_MinimumLoadDuration = "lt.Document.MemoryCache.MinimumLoadDuration";
        public const string Key_Document_MemoryCache_MaxmimumItems = "lt.Document.MemoryCache.MaxmimumItems";
        public const string Key_Document_MemoryCache_SlidingExpiration = "lt.Document.MemoryCache.SlidingExpiration";
        public const string Key_Document_MemoryCache_TimerInterval = "lt.Document.MemoryCache.TimerInterval";

        public const string Key_RasterCodecs_DefaultResolution = "lt.RasterCodecs.DefaultResolution";
        public const string Key_RasterCodecs_TimeoutMilliseconds = "lt.RasterCodecs.TimeoutMilliseconds";
        public const string Key_RasterCodecs_HtmlDomainWhitelistFile = "lt.RasterCodecs.HtmlDomainWhitelistFile";
        public const string Key_RasterCodecs_OptionsFilePath = "lt.RasterCodecs.OptionsFilePath";

        public const string Key_Barcodes_Reader_OptionsFilePath = "lt.Barcodes.Reader.OptionsFilePath";

        public const string Key_Ocr_EngineType = "lt.Ocr.EngineType";
        public const string Key_Ocr_RuntimeDirectory = "lt.Ocr.RuntimeDirectory";

        public const string Key_DocumentConverter_UseExternal = "lt.DocumentConverter.UseExternal";
        public const string Key_DocumentConverter_ExePath = "lt.DocumentConverter.ExePath";
        public const string Key_DocumentConverter_ForceStreaming = "lt.DocumentConverter.ForceStreaming";
        public const string Key_DocumentConverter_MaximumPages = "lt.DocumentConverter.MaximumPages";
        public const string Key_DocumentConverter_SavePDFA = "lt.DocumentConverter.SavePDFA";
        public const string Key_DocumentConverter_SavePDFLinearized = "lt.DocumentConverter.SavePDFLinearized";

        public const string Key_Analytics_Directory = "lt.Analytics.Directory";
        public const string Key_DocumentCompare_MaximumPages = "lt.DocumentCompare.MaximumPages";

        public const string Key_Svg_GZip = "lt.Svg.GZip";

        public const string Key_Multimedia_Cache_ConfigFile = "lt.Multimedia.Cache.ConfigFile";
        public const string Key_Multimedia_MP4ConverterPath = "lt.Multimedia.MP4ConverterPath";
        public const string Key_Multimedia_Cache_CleanupInterval = "lt.Multimedia.Cache.CleanupInterval";
        public const string Key_Samples = "lt.Samples";

        public const string Key_Pdf_SignatureFile = "lt.Pdf.SignatureFile";
        public const string Key_Pdf_SignatureFilePassword = "lt.Pdf.SignatureFilePassword";

        public const string Key_Application_DllDirectory = "lt.Application.DllDirectory";
        public const string Key_license_key = "lt.license.key";
        public const string lt_license_leadtools = "lt.license.leadtools";

        public static string CORSOrigins { get; private set; }
        public static string CORSHeaders { get; private set; }
        public static string CORSMethods { get; private set; }
        public static long CORSMaxAge { get; private set; }
        public const int DefaultResolution = 300;
        public static IConfiguration Configuration { get; set; }

        public static string WebRootPath { get; set; }
        public static string ContentRootPath { get; set; }


        static ServiceHelper()
        {
        }

        public static string GetSettingValue(string key)
        {
            string value = null;
            if (Configuration[key] != null)
                value = Configuration[key];

            return value;
        }

        public static bool GetSettingBoolean(string key)
        {
            var stringVal = ServiceHelper.GetSettingValue(key);
            bool temp = false;
            if (bool.TryParse(stringVal, out temp))
                return temp;
            return false;
        }

        public static int GetSettingInteger(string key, int defaultValue)
        {
            var stringVal = ServiceHelper.GetSettingValue(key);
            int temp = defaultValue;
            if (int.TryParse(stringVal, out temp))
                return temp;
            return defaultValue;
        }

        public static bool IsLicenseChecked
        {
            get;
            set;
        }

        public static bool IsKernelExpired
        {
            get;
            set;
        }

        // If true, we will track changes to the document or annotations
        private static bool _autoUpdateHistory = false;
        public static bool AutoUpdateHistory
        {
            get { return _autoUpdateHistory; }
        }

        public static void InitializeService(IConfiguration config)
        {
            /* This method is called by IHostingEnvironment.Startup of the web service
             * We will initialize the global and static objects used through out the demos and
             * Each controller will be able to use these same objects.
             * Controller-specific initialization is performed in InitializeController
             */

            Configuration = config;

            // Try to set our CORS options from local.config.
            // If nothing's there, use "*" (all) for each of them
            CORSOrigins = GetSettingValue(Key_CORS_Origins);
            if (string.IsNullOrWhiteSpace(CORSOrigins))
                CORSOrigins = "*";
            CORSHeaders = GetSettingValue(Key_CORS_Headers);
            if (string.IsNullOrWhiteSpace(CORSHeaders))
                CORSHeaders = "*";
            CORSMethods = GetSettingValue(Key_CORS_Methods);
            if (string.IsNullOrWhiteSpace(CORSMethods))
                CORSMethods = "*";
            CORSMaxAge = GetSettingInteger(Key_CORS_MaxAge, -1);

            // Set the license, initialize the cache and various objects

            // For the license, the TestController.Ping method is used to check the status of this
            // So save the values here to get them later
            try
            {
                DemosGlobal.InitRuntime(ServiceHelper.GetSettingValue(ServiceHelper.Key_Application_DllDirectory));
                SetLicense();
                IsKernelExpired = RasterSupport.KernelExpired;
            }
            catch
            {
                IsKernelExpired = true;
            }

            if (!IsKernelExpired)
            {
                // The license is OK, continue
                try
                {
                    // This setting disables disk access when creating temp files
                    if (!GetSettingBoolean(Key_Application_AllowTempFilesFromDisk))
                        RasterDefaults.TempFileMode = LeadTempFileMode.Memory;

                    string tempDirectory = GetSettingValue(ServiceHelper.Key_Application_TempDirectory);
                    if (!string.IsNullOrEmpty(tempDirectory))
                    {
                        tempDirectory = ServiceHelper.GetAbsolutePath(tempDirectory);
                        RasterDefaults.TemporaryDirectory = tempDirectory;
                    }

                    SetMultiplatformSupport();

                    CreateCache();
                    PreCacheHelper.CreatePreCache();
                    SetRasterCodecsOptions(DocumentFactory.RasterCodecsTemplate, 0);
                    _loadTimeoutMilliseconds = GetSettingInteger(Key_RasterCodecs_TimeoutMilliseconds, _defaultLoadTimeoutMilliseconds);

                    LoadMimeTypesWhitelist();

                    if (GetSettingBoolean(Key_Document_OnlyAllowedMimeTypes))
                    {
                        /*
                         * If true, all unspecified mimeTypes are automatically considered "denied".
                         * This effectively means that only mimeTypes in the "allowed" list are accepted.
                         */
                        DocumentFactory.MimeTypes.DefaultStatus = DocumentMimeTypeStatus.Denied;
                    }

                    _autoUpdateHistory = GetSettingBoolean(Key_Document_AutoUpdateHistory);
                    _returnRequestUserData = GetSettingBoolean(Key_Application_ReturnRequestUserData);

                    _useDataRanges = GetSettingBoolean(Key_Application_UseDataRanges);
                    // If we have a user token then ensure that the service throws an exception when user is trying to access a document
                    // without the correct token
                    DocumentFactory.InvalidUserTokenException = new RasterException(RasterExceptionCode.FileAccessDenied);

                    SetupDocumentMemoryCache();
                }
                catch
                {
                    // Let this pass, it is checked again in TestController.Ping
                }

                CreateOCREngine();
                CreateAnnRenderingEngine();
                //AnalyticsController.InitializeService();
                TestController.InitializeService();
                MultimediaController.CreateCache();
            }
        }

        public static void CleanupService()
        {
            /* This method is called by IHostingEnvironment.Shutdown of the web service
             * We will clean up and destroy the global static objects created in
             * InitializeService
             */
            MultimediaController.Cleanup();

            DocumentFactory.DocumentMemoryCache.Stop();

            if (ServiceHelper.CacheManager != null)
            {
                ServiceHelper.CacheManager.Cleanup();
            }

            if (_ocrEngine != null)
            {
                _ocrEngine.Dispose();
                _ocrEngine = null;
            }
        }

        public static void InitializeController()
        {
            /* This method is called by the constructor of each controller
             * It is assumed that InitializeService has been called before by
             * Application_Start of the web service
             * 
             * Do any per-request setup here
             */
        }

        private static bool _useDataRanges = false;
        public static bool UseDataRanges
        {
            get { return _useDataRanges; }
        }

        private static bool _returnRequestUserData = false;
        public static bool ReturnRequestUserData
        {
            get { return _returnRequestUserData; }
        }

        private static string _multiplatformSupportStatus = "Not Ready";
        public static string MultiplatformSupportStatus
        {
            get { return _multiplatformSupportStatus; }
        }

        public static void SetMultiplatformSupport()
        {
            try
            {
                // Set the optional multi-platform support
                // Refer to https://www.leadtools.com/help/leadtools/v22/dh/to/leadtools-drawing-engine-and-multi-platform-consideration.html

                // Get the current options
                DrawEngineOptions options = DrawEngine.GetOptions();

                var drawEngineTypeString = ServiceHelper.GetSettingValue(ServiceHelper.Key_Application_DrawEngineType);
                if (!string.IsNullOrEmpty(drawEngineTypeString))
                {
                    options.EngineType = (DrawEngineType)Enum.Parse(typeof(DrawEngineType), drawEngineTypeString, true);
                }

                var shadowFontModeString = ServiceHelper.GetSettingValue(ServiceHelper.Key_Application_ShadowFontMode);
                if (!string.IsNullOrEmpty(shadowFontModeString))
                {
                    options.ShadowFontMode = (DrawShadowFontMode)Enum.Parse(typeof(DrawShadowFontMode), shadowFontModeString, true);
                }

                DrawEngine.SetOptions(options);

                // Set the shadow fonts directory
                string shadowFontsDirectory = GetSettingValue(ServiceHelper.Key_Application_ShadowFontsDirectory);
                shadowFontsDirectory = ServiceHelper.GetAbsolutePath(shadowFontsDirectory);
                if (!string.IsNullOrEmpty(shadowFontsDirectory))
                {
                    if (Directory.Exists(shadowFontsDirectory))
                    {
                        // Set the shadow fonts
                        RasterDefaults.SetResourceDirectory(LEADResourceDirectory.Fonts, shadowFontsDirectory);
                    }
                    else
                    {
                        throw new InvalidOperationException(string.Format("Unable to set shadow fonts because the file {0} does not exist or is not a directory.", shadowFontsDirectory));
                    }
                }

                _multiplatformSupportStatus = "Ready";
            }
            catch
            {
                _multiplatformSupportStatus = "Error";
                throw;
            }
        }

        private static string _currentLicensePath = null;
        private static string _currentLicenseKey = null;

        public static string GetCurrentLicensePath()
        {
            return _currentLicensePath;
        }

        public static string GetCurrentLicenseKey()
        {
            return _currentLicenseKey;
        }


        public static void SetLicense()
        {
            /*
             * Set the license and license key here.
             * While this is called with each call to the service,
             * the lines below with RasterSupport.KernelExpired
             * will exit early to avoid checking repeatedly.
             */
            if (!RasterSupport.KernelExpired)
            {
                IsLicenseChecked = true;
                return;
            }

            // file path may be relative or absolute
            // dev key may be relative, absolute, or the full text
            string licensePath = null;
            string devKey = null;
            bool licenseFileFound = false;

            // Check setup
            string commonLicenseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // Find the Examples part
            commonLicenseDir = GetSettingValue(Key_License_FilePath);
            if (Directory.Exists(commonLicenseDir))
            {
                string commonLicenseFilePath = Path.Combine(commonLicenseDir, "LEADTOOLS.lic");
                string commonDeveloperKeyFilePath = Path.Combine(commonLicenseDir, "LICENSE.lic.key");

                bool commonLicenseFileFound = File.Exists(commonLicenseFilePath);
                bool commonDeveloperKeyFileFound = File.Exists(commonDeveloperKeyFilePath);
                if (commonLicenseFileFound && commonDeveloperKeyFileFound)
                {
                    licensePath = commonLicenseFilePath;
                    devKey = File.ReadAllText(commonDeveloperKeyFilePath);
                    licenseFileFound = true;
                }
            }


            if (licenseFileFound)
            {
                IsLicenseChecked = true;
                _currentLicensePath = licensePath;
                _currentLicenseKey = devKey;
                RasterSupport.SetLicense(licensePath, devKey);
            }
        }

        public static string GetAbsolutePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath) || relativePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                // Not a legal path
                return relativePath;
            }

            relativePath = relativePath.Trim();
            if (!Path.IsPathRooted(relativePath))
                relativePath = Path.Combine(ContentRootPath, relativePath);
            return relativePath;
        }

        public static string GetAbsoluteWebPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath) || relativePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                // Not a legal path
                return relativePath;
            }

            relativePath = relativePath.Trim();
            if (!Path.IsPathRooted(relativePath))
                relativePath = Path.Combine(WebRootPath, relativePath);
            return relativePath;
        }


        public static bool IsAbsolutePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath) || relativePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                // Not a legal path
                return false;
            }

            return Path.IsPathRooted(relativePath.Trim());
        }

        public static void CreateCache()
        {
            // Called by InitializeService the first time the service is run
            // Initialize the global ICacheManager object

            ICacheManager cacheManager = null;

            // See if we have a CacheManager configuration file
            string cacheManagerConfigFile = GetSettingValue(Key_Cache_CacheManagerConfigFile);
            cacheManagerConfigFile = GetAbsolutePath(cacheManagerConfigFile);
            if (!string.IsNullOrEmpty(cacheManagerConfigFile))
            {
                using (var stream = File.OpenRead(cacheManagerConfigFile))
                    cacheManager = CacheManagerFactory.CreateFromConfiguration(stream, WebRootPath);
            }
            else
            {
                // Try to create the default ICacheManager directly (backward compatibility)
                cacheManager = new DefaultCacheManager(WebRootPath, null);
            }

            if (cacheManager == null)
                throw new InvalidOperationException("Could not find a valid LEADTOOLS cache system configuration.");

            cacheManager.Initialize();
            _cacheManager = cacheManager;
        }

        public static void UpdateCacheSettings(ObjectCache cache, HttpResponse response)
        {
            TimeSpan slidingExpiration = TimeSpan.Zero;
            CacheItemPolicy policy = ServiceHelper.CacheManager.CreatePolicy(cache);
            if (policy != null)
                slidingExpiration = policy.SlidingExpiration;

            if (slidingExpiration == ObjectCache.NoSlidingExpiration)
                slidingExpiration = TimeSpan.FromMinutes(60);

            response.Headers.Append("Cache-Control", $"public, max-age={slidingExpiration}");
        }

        public static CacheItemPolicy CreateForeverPolicy()
        {
            // Creates a 3-year policy (for pre-cached items)
            var policy = new CacheItemPolicy();
            policy.AbsoluteExpiration = DateTime.MaxValue;
            return policy;
        }

        // Default load timeout.
        private const int _defaultLoadTimeoutMilliseconds = 0;
        private static int _loadTimeoutMilliseconds = _defaultLoadTimeoutMilliseconds;
        public static int LoadTimeoutMilliseconds
        {
            get { return _loadTimeoutMilliseconds; }
        }

        // LEADTOOLS Document library uses 300 as the default DPI, so we use the same
        private const int _defaultResolution = 300;

        public static void SetRasterCodecsOptions(RasterCodecs rasterCodecs, int resolution)
        {
            // Set up any extra options to use here
            if (resolution == 0)
                resolution = GetSettingInteger(Key_RasterCodecs_DefaultResolution, _defaultResolution);

            // Set the load resolution
            rasterCodecs.Options.Wmf.Load.XResolution = resolution;
            rasterCodecs.Options.Wmf.Load.YResolution = resolution;
            rasterCodecs.Options.RasterizeDocument.Load.XResolution = resolution;
            rasterCodecs.Options.RasterizeDocument.Load.YResolution = resolution;

            // Overwrite PDF save options if we have it in the setting
            bool savePdfA = GetSettingBoolean(Key_DocumentConverter_SavePDFA);
            bool savePdfLinearized = GetSettingBoolean(Key_DocumentConverter_SavePDFLinearized);

            if (savePdfA || savePdfLinearized)
            {
                CodecsPdfSaveOptions pdfSaveOptions = rasterCodecs.Options.Pdf.Save;

                if (savePdfA)
                    pdfSaveOptions.Version = CodecsRasterPdfVersion.PdfA;
                if (savePdfLinearized)
                    pdfSaveOptions.Linearized = true;
            }

            String domainWhitelist = LoadHtmlDomainWhitelist();
            if (!string.IsNullOrEmpty(domainWhitelist))
            {
                CodecsHtmlLoadOptions htmlLoadOptions = rasterCodecs.Options.Html.Load;
                htmlLoadOptions.DomainWhitelist = domainWhitelist;
            }

            // See if we have an options file in the config
            var value = GetSettingValue(Key_RasterCodecs_OptionsFilePath);
            value = GetAbsolutePath(value);
            if (!string.IsNullOrEmpty(value))
                rasterCodecs.LoadOptions(value);

            /* In Web API, resources are pulled from a Temp folder.
             * So rastercodecs needs to be given an initial path that corresponds
             * to the /bin folder.
             * There is an after-build target that copies these files from the proper /Bin<ver> folder.
             */
            var binPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (Directory.Exists(binPath) && File.Exists(Path.Combine(binPath, @"Leadtools.Pdf.Utilities.dll")))
            {
                rasterCodecs.Options.Pdf.InitialPath = binPath;
            }
        }

        public static void SetDocumentWriterOptions(DocumentWriter documentWriter, DocumentFormat documentFormat)
        {
            if (documentFormat == DocumentFormat.Pdf)
            {
                // Overwrite PDF save options if we have it in the setting
                bool savePdfA = GetSettingBoolean(Key_DocumentConverter_SavePDFA);
                bool savePdfLinearized = GetSettingBoolean(Key_DocumentConverter_SavePDFLinearized);

                if (savePdfA || savePdfLinearized)
                {
                    PdfDocumentOptions pdfOptions = documentWriter.GetOptions(documentFormat) as PdfDocumentOptions;

                    if (savePdfA)
                        pdfOptions.DocumentType = PdfDocumentType.PdfA;

                    if (savePdfLinearized)
                        pdfOptions.Linearized = true;

                    documentWriter.SetOptions(documentFormat, pdfOptions);
                }
            }
        }

        public static void LoadMimeTypesWhitelist()
        {
            string mimeTypesFileName = GetSettingValue(Key_Document_MimeTypesFile);
            mimeTypesFileName = GetAbsolutePath(mimeTypesFileName);
            if (string.IsNullOrEmpty(mimeTypesFileName))
                return;

            MimeTypesConfig mimeTypesConfig = null;

            try
            {
                using (StreamReader sr = new StreamReader(mimeTypesFileName))
                    mimeTypesConfig = JsonConvert.DeserializeObject<MimeTypesConfig>(sr.ReadToEnd());
            }
            catch (Exception e)
            {
                Trace.WriteLine(string.Format("MimeTypes list not loaded: {0}", e.Message));
                return;
            }

            if (mimeTypesConfig == null)
                return;
            var entries = DocumentFactory.MimeTypes.Entries;

            string[] allowed = mimeTypesConfig.Allowed;
            if (allowed != null && allowed.Length > 0)
            {
                foreach (string allowedMimeType in allowed)
                    entries.Add(allowedMimeType, DocumentMimeTypeStatus.Allowed);
            }

            string[] denied = mimeTypesConfig.Denied;
            if (denied != null && denied.Length > 0)
            {
                foreach (string allowedMimeType in denied)
                    entries.Add(allowedMimeType, DocumentMimeTypeStatus.Denied);
            }

            if (entries.Count > 0)
            {
                // We have entries, hook up a callback to get the mime type of denied URLs for better user messages
                // See FactoryController.LoadFromUri
                _rejectedMimeTypes = new ConcurrentDictionary<string, string>();
                DocumentFactory.MimeTypes.UserGetDocumentStatus = GetMimeTypesDocumentStatusHandler;
            }
        }

        private static ConcurrentDictionary<string, string> _rejectedMimeTypes;

        // This callback is invoked by DocumentFactory whenever LoadFromUri/LoadDocumentAttachment is called to check if the mime type of the document
        // is allowed. We will use it to extract the actual mime type of a URL/attachment if rejected for better error message in
        // FactoryController.LoadFromUri/LoadDocumentAttachment
        private static DocumentMimeTypeStatus GetMimeTypesDocumentStatusHandler(Uri uri, LoadDocumentOptions options, DocumentMimeTypeSource source, string mimeType)
        {
            // Call default implementation
            DocumentMimeTypeStatus status = DocumentFactory.MimeTypes.GetDocumentStatus(uri, options, source, mimeType);
            // Check if the mime type of this URL was rejected, if so, record it
            if (status == DocumentMimeTypeStatus.Denied)
            {
                // If we have a real mime type, add it to our list
                if (!string.IsNullOrEmpty(mimeType))
                {
                    string documentId = options != null ? options.DocumentId : null;
                    if (uri != null)
                        _rejectedMimeTypes.TryAdd(uri.ToString(), mimeType);
                    else if (documentId != null)
                        _rejectedMimeTypes.TryAdd(documentId, mimeType);
                }
            }

            return status;
        }

        public static string TryGetRejectedMimeType(Uri uri, string documentId)
        {
            // See if we have a URI/DocumentId and if we are actually listening to mime types
            if (_rejectedMimeTypes == null)
            {
                return null;
            }

            // Try to get and remove it from the list
            string mimeType = null;
            if (uri != null)
                _rejectedMimeTypes.TryRemove(uri.ToString(), out mimeType);
            else if (documentId != null)
                _rejectedMimeTypes.TryRemove(documentId, out mimeType);

            return mimeType;
        }

        // Checks the reason FactoryController.LoadFromUri or LoadDocumentAttachment failed
        public static void CheckDocumentFailedToLoad(Uri uri, LoadDocumentOptions loadOptions, int attachmentNumber)
        {
            // Get the mime type, first what the user passed
            string mimeType = loadOptions.MimeType;
            // Next, see if it was rejected by the toolkit and we recorded it
            string rejectedMimeType = TryGetRejectedMimeType(uri, loadOptions.DocumentId);
            if (rejectedMimeType != null)
                mimeType = rejectedMimeType;

            var errorMessage = new StringBuilder();

            if (uri != null)
                errorMessage.AppendFormat("Document at URI '{0}' ", uri);
            else if (attachmentNumber != -1)
                errorMessage.AppendFormat("Document attachment number '{0}' ", attachmentNumber);
            else if (loadOptions.DocumentId != null)
                errorMessage.AppendFormat("Document '{0}' ", loadOptions.DocumentId);
            else
                errorMessage.Append("Document ");

            if (rejectedMimeType == null && loadOptions.TimeoutMilliseconds > 0)
            {
                // Most probably it was timed out
                errorMessage.Append("was timed out");
            }
            else
            {
                // Its mime type was rejected
                errorMessage.Append("uses a blocked mimeType");

                if (mimeType != null)
                    errorMessage.AppendFormat(" '{0}'", mimeType);
            }

            throw new InvalidOperationException(errorMessage.ToString());
        }

        public static string LoadHtmlDomainWhitelist()
        {
            string domainWhitelistFileName = GetSettingValue(Key_RasterCodecs_HtmlDomainWhitelistFile);
            domainWhitelistFileName = GetAbsolutePath(domainWhitelistFileName);
            if (string.IsNullOrEmpty(domainWhitelistFileName))
                return null;

            HtmlDomainWhitelistConfig domainWhitelistConfig = null;

            try
            {
                using (StreamReader sr = new StreamReader(domainWhitelistFileName))
                    domainWhitelistConfig = JsonConvert.DeserializeObject<HtmlDomainWhitelistConfig>(sr.ReadToEnd());
            }
            catch (Exception e)
            {
                Trace.WriteLine(string.Format("Html Domain Whitelist not loaded: {0}", e.Message));
                return null;
            }

            if (domainWhitelistConfig == null)
                return null;

            string config = "";
            string[] whitelisted = domainWhitelistConfig.Whitelisted;
            if (whitelisted != null && whitelisted.Length > 0)
            {
                for (int i = 0; i < whitelisted.Length; i++)
                {
                    config += whitelisted[i];
                    if (i != whitelisted.Length - 1)
                        config += "|";
                }
            }
            return config;
        }

        private static void SetupDocumentMemoryCache()
        {
            // Setup memory cache support
            if (!GetSettingBoolean(Key_Document_MemoryCache_IsEnabled))
                return;

            var documentMemoryCacheStartOptions = new DocumentMemoryCacheStartOptions();

            documentMemoryCacheStartOptions.MaximumItems = GetSettingInteger(Key_Document_MemoryCache_MaxmimumItems, documentMemoryCacheStartOptions.MaximumItems);

            int defaultTimeSpan = (int)documentMemoryCacheStartOptions.MinimumLoadDuration.TotalMilliseconds;
            documentMemoryCacheStartOptions.MinimumLoadDuration = TimeSpan.FromMilliseconds(GetSettingInteger(Key_Document_MemoryCache_MinimumLoadDuration, defaultTimeSpan));
            defaultTimeSpan = (int)documentMemoryCacheStartOptions.SlidingExpiration.TotalMilliseconds;
            documentMemoryCacheStartOptions.SlidingExpiration = TimeSpan.FromMilliseconds(GetSettingInteger(Key_Document_MemoryCache_SlidingExpiration, defaultTimeSpan));
            defaultTimeSpan = (int)documentMemoryCacheStartOptions.TimerInterval.TotalMilliseconds;
            documentMemoryCacheStartOptions.TimerInterval = TimeSpan.FromMilliseconds(GetSettingInteger(Key_Document_MemoryCache_TimerInterval, defaultTimeSpan));

            DocumentFactory.DocumentMemoryCache.Start(documentMemoryCacheStartOptions);
        }

        public static bool SetBarcodeReadOptions(BarcodeReader reader)
        {
            // See if we have an options file in the config
            var value = GetSettingValue(Key_Barcodes_Reader_OptionsFilePath);
            value = GetAbsolutePath(value);
            if (string.IsNullOrEmpty(value))
            {
                // Return false to indicate that the user did not set any barcode options.
                // We will try different options ourselves.
                return false;
            }

            reader.LoadOptions(value);
            return true;
        }

        public static void InitBarcodeReader(BarcodeReader reader, bool doublePass)
        {
            // Default options to read most barcodes
            reader.ImageType = BarcodeImageType.Unknown;

            // Both directions for 1D
            OneDBarcodeReadOptions oneDOptions = reader.GetDefaultOptions(BarcodeSymbology.UPCA) as OneDBarcodeReadOptions;
            oneDOptions.SearchDirection = BarcodeSearchDirection.HorizontalAndVertical;

            GS1DatabarStackedBarcodeReadOptions gs1Options = reader.GetDefaultOptions(BarcodeSymbology.GS1DatabarStacked) as GS1DatabarStackedBarcodeReadOptions;
            gs1Options.SearchDirection = BarcodeSearchDirection.HorizontalAndVertical;

            FourStateBarcodeReadOptions fourStateOptions = reader.GetDefaultOptions(BarcodeSymbology.USPS4State) as FourStateBarcodeReadOptions;
            fourStateOptions.SearchDirection = BarcodeSearchDirection.HorizontalAndVertical;

            PatchCodeBarcodeReadOptions patchCodeOptions = reader.GetDefaultOptions(BarcodeSymbology.PatchCode) as PatchCodeBarcodeReadOptions;
            patchCodeOptions.SearchDirection = BarcodeSearchDirection.HorizontalAndVertical;

            PostNetPlanetBarcodeReadOptions postNetOptions = reader.GetDefaultOptions(BarcodeSymbology.PostNet) as PostNetPlanetBarcodeReadOptions;
            postNetOptions.SearchDirection = BarcodeSearchDirection.HorizontalAndVertical;

            PharmaCodeBarcodeReadOptions pharmaCodeOptions = reader.GetDefaultOptions(BarcodeSymbology.PharmaCode) as PharmaCodeBarcodeReadOptions;
            pharmaCodeOptions.SearchDirection = BarcodeSearchDirection.HorizontalAndVertical;

            // Double pass
            oneDOptions.EnableDoublePass = doublePass;

            DatamatrixBarcodeReadOptions dataMatrixOptions = reader.GetDefaultOptions(BarcodeSymbology.Datamatrix) as DatamatrixBarcodeReadOptions;
            dataMatrixOptions.EnableDoublePass = doublePass;

            PDF417BarcodeReadOptions pdf417Options = reader.GetDefaultOptions(BarcodeSymbology.PDF417) as PDF417BarcodeReadOptions;
            pdf417Options.EnableDoublePass = doublePass;

            MicroPDF417BarcodeReadOptions microPdf4127Options = reader.GetDefaultOptions(BarcodeSymbology.MicroPDF417) as MicroPDF417BarcodeReadOptions;
            microPdf4127Options.EnableDoublePass = doublePass;

            QRBarcodeReadOptions qrOptions = reader.GetDefaultOptions(BarcodeSymbology.QR) as QRBarcodeReadOptions;
            qrOptions.EnableDoublePass = doublePass;

            reader.ImageType = BarcodeImageType.Unknown;
        }

        public static void SafeDeleteFile(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                try
                {
                    File.Delete(fileName);
                }
                catch { }
            }
        }

        // Global Cache Manager object used by all controllers.
        // This object is created during service initialization
        private static ICacheManager _cacheManager = null;
        public static ICacheManager CacheManager
        {
            get { return _cacheManager; }
        }

        private static OcrEngineStatus _OcrEngineStatus = OcrEngineStatus.Unset;
        public static OcrEngineStatus OcrEngineStatus
        {
            get { return ServiceHelper._OcrEngineStatus; }
        }

        // Global IOcrEngine instance used by all the controllers
        // This object is created during service initialization
        private static IOcrEngine _ocrEngine;

        public static IOcrEngine GetOCREngine()
        {
            return _ocrEngine;
        }

        private static string _ocrEngineRuntimeDirectory;
        public static string OcrEngineRuntimeDirectory
        {
            get { return _ocrEngineRuntimeDirectory; }
        }

        public static string CheckOCRRuntimeDirectory()
        {
            // Check if we are running on a machine that has LEADTOOLS installed, try to get the path automatically
            var appPath = WebRootPath;
            string runtimeDir = DemosGlobal.OCRLeadRuntimeFolder;

            if (String.IsNullOrEmpty(runtimeDir))
                return null;

            var dir = Path.GetFullPath(Path.Combine(appPath, runtimeDir));
            if (Directory.Exists(dir) && Directory.EnumerateFileSystemEntries(dir).Any())
                return dir;

            // Not found
            return null;
        }
        public static void InitCodecs(RasterCodecs codecs, int resolution)
        {
            if (resolution == 0)
                resolution = DefaultResolution;
            codecs.Options.Wmf.Load.XResolution = resolution;
            codecs.Options.Wmf.Load.YResolution = resolution;
            codecs.Options.RasterizeDocument.Load.XResolution = resolution;
            codecs.Options.RasterizeDocument.Load.YResolution = resolution;
            string rasterCodecsOptionsFilePath = GetSettingValue(Key_RasterCodecs_OptionsFilePath);
            if (!string.IsNullOrEmpty(rasterCodecsOptionsFilePath) && File.Exists(rasterCodecsOptionsFilePath))
                codecs.LoadOptions(rasterCodecsOptionsFilePath);
        }
        public static IOcrEngine CreateOCREngine(RasterCodecs codecs)
        {

            var runtimeDirectory = ServiceHelper.GetSettingValue(ServiceHelper.Key_Ocr_RuntimeDirectory);
            runtimeDirectory = ServiceHelper.GetAbsolutePath(runtimeDirectory);
            if (string.IsNullOrEmpty(runtimeDirectory))
            {
                throw new ArgumentException("OCR Engine directory not set");
            }
            return CreateOCREngine(runtimeDirectory, codecs);
        }
        private static IOcrEngine CreateOCREngine(string runtimeDirectory, RasterCodecs codecs)
        {

            IOcrEngine ocrEngine = OcrEngineManager.CreateEngine(OcrEngineType.LEAD);
            ocrEngine.Startup(codecs, null, null, runtimeDirectory);
            try
            {
                //var pathDir = @"C:\inetpub\wwwroot\TAB\Web Access\OcrAdvantageRuntime";
                ocrEngine.Startup(codecs, null, null, runtimeDirectory);
                return ocrEngine;
            }
            catch (Exception ex)
            {
                if (ocrEngine != null) ocrEngine.Dispose();
                System.Diagnostics.Trace.WriteLine($"{ex.Message} + The OCR Engine could not be started. This application will continue to run, but without OCR functionality.");
            }
            return ocrEngine;
        }
        public static void CreateOCREngine()
        {
            if (_ocrEngine != null)
                _ocrEngine.Dispose();

            // Reset the OCR Engine Status
            _OcrEngineStatus = OcrEngineStatus.Unset;

            var engineTypeString = ServiceHelper.GetSettingValue(ServiceHelper.Key_Ocr_EngineType);
            if (string.IsNullOrEmpty(engineTypeString))
                return;

            var engineType = OcrEngineType.LEAD;
            try
            {
                // not necessary since we set to LEAD OCR above, but here as an example.
                if (engineTypeString.Equals("lead", StringComparison.OrdinalIgnoreCase))
                    engineType = OcrEngineType.LEAD;
                else if (engineTypeString.Equals("omnipage", StringComparison.OrdinalIgnoreCase))
                    engineType = OcrEngineType.OmniPage;
            }
            catch
            {
                // Error with engine type
                _OcrEngineStatus = OcrEngineStatus.Error;
                return;
            }

            // Check for a location of the OCR Runtime
            var runtimeDirectory = ServiceHelper.GetSettingValue(ServiceHelper.Key_Ocr_RuntimeDirectory);
            runtimeDirectory = ServiceHelper.GetAbsolutePath(runtimeDirectory);
#if !FOR_NUGET
         if (string.IsNullOrEmpty(runtimeDirectory))
            runtimeDirectory = CheckOCRRuntimeDirectory();
#endif

            // Use LEAD OCR engine
#if LEADTOOLS_V21_OR_LATER
            var ocrEngine = OcrEngineManager.CreateEngine(engineType);
#else
         var ocrEngine = OcrEngineManager.CreateEngine(engineType, true);
#endif // #if LEADTOOLS_V21_OR_LATER

            try
            {
                if (string.IsNullOrEmpty(runtimeDirectory))
                {
                    string userProfileVariableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "userprofile" : "HOME"; // Handle Linux and macOS operating systems paths and environment variables differences
                    string nugetsDir = string.Format(@".nuget{0}packages{0}leadtools.ocr.languages.main.net{0}", Path.DirectorySeparatorChar);

                    var dirs = Directory.GetDirectories(Environment.GetEnvironmentVariable(userProfileVariableName), nugetsDir);
                    int maxverMajor = 0;
                    int maxverMinor = 0;
                    foreach (var dir in dirs)
                    {
                        var versionDir = Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar);
                        versionDir = versionDir.Split(Path.DirectorySeparatorChar).Last();
                        var major = int.Parse(versionDir.Split('.').First());
                        if (major > maxverMajor)
                            maxverMajor = major;
                    }

                    foreach (var dir in dirs)
                    {
                        var versionDir = Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar);
                        versionDir = versionDir.Split(Path.DirectorySeparatorChar).Last();

                        var major = int.Parse(versionDir.Split('.').First());
                        var minor = int.Parse(versionDir.Split('.').Last());
                        if (major == maxverMajor)
                            if (minor > maxverMinor)
                                maxverMinor = minor;
                    }

                    runtimeDirectory = Path.Combine(Environment.GetEnvironmentVariable(userProfileVariableName), nugetsDir, $@"{maxverMajor}.0.0.{maxverMinor}\content\OcrLEADRuntime");
                    if (!string.IsNullOrWhiteSpace(runtimeDirectory) && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        runtimeDirectory = runtimeDirectory.Replace('\\', '/');

                    if (!Directory.Exists(runtimeDirectory))
                        runtimeDirectory = null;
                }

                // Start it up
                ocrEngine.Startup(null, null, null, runtimeDirectory);
                _ocrEngine = ocrEngine;
                _OcrEngineStatus = OcrEngineStatus.Ready;
                _ocrEngineRuntimeDirectory = runtimeDirectory;
            }
            catch
            {
                ocrEngine.Dispose();
                _OcrEngineStatus = OcrEngineStatus.Error;
                System.Diagnostics.Trace.WriteLine("The OCR Engine could not be started. This application will continue to run, but without OCR functionality.");
            }
        }

        public static int GetMaxComparePages()
        {
            return GetSettingInteger(Key_DocumentCompare_MaximumPages, 0);
        }

        public static string GetFileUri(Uri uri)
        {
            if (!uri.IsAbsoluteUri)
                return uri.ToString();
            if (uri.Scheme == Uri.UriSchemeFile)
                return uri.LocalPath;
            else
                return null;
        }

        public static void CopyStream(Stream source, Stream target)
        {
            const int bufferSize = 1024 * 64;
            var buffer = new byte[bufferSize];
            int bytesRead = 0;
            do
            {
                bytesRead = source.Read(buffer, 0, bufferSize);
                if (bytesRead > 0)
                    target.Write(buffer, 0, bytesRead);
            }
            while (bytesRead > 0);
        }

        private static bool CanViewInBrowser(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;
            switch (contentType)
            {
                case "application/pdf":
                //case "application/xml": Uncomment to allow XML to be viewed in browsers that support it
                case "application/json":
                case "text/plain":
                case "text/html":
                case "text/css":
                case "image/png":
                case "image/jpeg":
                case "image/gif":
                    return true;

                default:
                    return false;
            }
        }

        public static void SetResponseViewFileName(HttpResponse response, string fileName, string browserViewContentType, string contentDisposition)
        {
            string dispositionType = contentDisposition;
            if (string.IsNullOrEmpty(dispositionType))
            {
                bool tryViewInBrowser = CanViewInBrowser(browserViewContentType);
                // "Content-Disposition: Inline" specifies that the document should be opened with the browser's viewer if possible and gives a name to the downloaded item.
                dispositionType = tryViewInBrowser ? DispositionTypeNames.Inline : DispositionTypeNames.Attachment;
            }

            // "Content-Disposition: Inline" specifies that the document should be opened with the browser's viewer if possible and gives a name to the downloaded item.
            var disposition = new ContentDisposition
            {
                DispositionType = dispositionType,
                FileName = fileName
            };
            response.Headers.Add("Content-Disposition", disposition.ToString());
        }

        public static string RemoveExtension(string filename, string extension)
        {
            if (string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(extension))
                return filename;

            if (!extension.StartsWith("."))
                extension = "." + extension;

            if (filename.EndsWith(extension))
                return filename.Substring(0, filename.LastIndexOf(extension));

            return filename;
        }

        public static bool IsASCII(string str)
        {
            // Cannot use none ASCII file names
            if (str == null || str.Length == 0)
                return true;

            foreach (char c in str)
            {
                if (c < 32 || c > 127)
                    return false;
            }

            return true;
        }

        // Global AnnRenderingEngine instance used by all the controllers
        // This object is created during service initialization
        private static AnnRenderingEngine _renderingEngine;
        public static AnnRenderingEngine GetAnnRenderingEngine()
        {
            return _renderingEngine;
        }

        public static void CreateAnnRenderingEngine()
        {
            if (_renderingEngine != null)
                return;

            const string annotationsEngineAssemblyName = "Leadtools.Annotations.Engine";
#if !FOR_STD
         const string annotationsRenderingAssemblyName = "Leadtools.Annotations.Rendering.WinForms";
         const string renderingEngineTypeFullName = "Leadtools.Annotations.Rendering.AnnWinFormsRenderingEngine";
         const string platformLibName = "Leadtools.Annotations.Winforms";
         const string platformToolsTypeName = "Leadtools.Annotations.WinForms.Tools";
#else
            const string annotationsRenderingAssemblyName = "Leadtools.Annotations.Rendering";
            const string renderingEngineTypeFullName = "Leadtools.Annotations.Rendering.AnnDrawRenderingEngine";
            const string platformLibName = "Leadtools.Annotations.Rendering";
            const string platformToolsTypeName = "Leadtools.Annotations.Rendering.Tools";
#endif

            Assembly annotationsRenderingAssembly = null;
            try
            {
                string fullName = typeof(AnnRenderingEngine).Assembly.FullName;
                fullName = fullName.Replace(annotationsEngineAssemblyName, annotationsRenderingAssemblyName);

                AssemblyName assemblyName = new AssemblyName(annotationsRenderingAssemblyName);
                annotationsRenderingAssembly = Assembly.Load(assemblyName);
                _renderingEngine = annotationsRenderingAssembly.CreateInstance(renderingEngineTypeFullName) as AnnRenderingEngine;
            }
            catch
            {
                // We will not be able to support annotations overlay. But everything else is supported
            }

            // Load the annotations resources dynamically
            if (_renderingEngine != null)
            {
                try
                {
                    AssemblyName assemblyName = annotationsRenderingAssembly.GetName();
                    var platformAssemblyName = new AssemblyName();
                    platformAssemblyName.Name = platformLibName;
                    platformAssemblyName.Version = assemblyName.Version;
                    string platformFullName = platformAssemblyName.ToString();
#if !FOR_STD
               Assembly annotationsPlatformAssembly = Assembly.Load(platformFullName);
#else
                    Assembly annotationsPlatformAssembly = Assembly.Load(new AssemblyName(platformFullName));
#endif

                    if (annotationsPlatformAssembly != null)
                    {
                        Type annotationsPlatformTools = annotationsPlatformAssembly.GetType(platformToolsTypeName);
#if !FOR_STD
                  _renderingEngine.Resources = annotationsPlatformTools.GetMethod("LoadResources").Invoke(null, null) as Leadtools.Annotations.Engine.AnnResources;
#else
                        _renderingEngine.Resources = annotationsPlatformTools.GetTypeInfo().GetDeclaredMethod("LoadResources").Invoke(null, null) as Leadtools.Annotations.Engine.AnnResources;
#endif
                    }
                }
                catch
                {
                    // This one is OK if we don not have it, only for resources
                }
            }
        }

        // We should not let the user use file scheme (unsafe and security issue) to upload files.
        public static void CheckUriScheme(Uri uri)
        {
            bool isFileScheme = false;
            try
            {
                if (uri.IsFile || uri.IsUnc || uri.Scheme == Uri.UriSchemeFile)
                    isFileScheme = true;
            }
            catch { }

            if (isFileScheme)
                throw new ArgumentException("uri scheme not supported by this implementation");
        }

        // Set this value to any other than null to force clients to send a user token
        // to the service for all calls.
        //public static string REQUEST_USER_TOKEN = null;
        public static string REQUEST_USER_TOKEN = "user-token";
        public static string GetUserToken(IHeaderDictionary headers, Request request)
        {
            if (REQUEST_USER_TOKEN == null)
                return null;

            if (headers != null && headers.Keys.Contains(REQUEST_USER_TOKEN))
                return headers[REQUEST_USER_TOKEN];

            if (request != null && request.DocumentUserToken != null)
                return request.DocumentUserToken;

            return null;
        }

        public static string GetUserToken(HttpRequestHeaders headers, Request request)
        {
            if (REQUEST_USER_TOKEN == null)
                return null;

            IEnumerable<string> tokenValue = new List<string>();
            if (headers != null && headers.TryGetValues(REQUEST_USER_TOKEN, out tokenValue) && tokenValue.Count() > 0)
                return tokenValue.First();

            if (request != null && request.DocumentUserToken != null)
                return request.DocumentUserToken;

            return null;
        }
    }
}

class MimeTypesConfig
{
    [DataMember(Name = "allowed")]
    public string[] Allowed { get; set; }

    [DataMember(Name = "denied")]
    public string[] Denied { get; set; }
}

class HtmlDomainWhitelistConfig
{
    [DataMember(Name = "whitelisted")]
    public string[] Whitelisted { get; set; }
}
