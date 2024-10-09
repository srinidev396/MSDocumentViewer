// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Diagnostics;

using Leadtools.Document;
using Leadtools.Demos;
using Leadtools.Codecs;
using Leadtools.Caching;

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
using Microsoft.AspNetCore.Http;
using DocumentService.Security;
using System.Web;
using NLog.Extensions.Logging;

namespace Leadtools.DocumentViewer.Controllers
{
    /// <summary>
    /// Used with the DocumentFactory class of the LEADTOOLS Document JavaScript library.
    /// </summary>
    public class FactoryController : Controller
    {
        private readonly ILogger<FactoryController> _logger;
        public FactoryController(ILogger<FactoryController> logger)
        {
            _logger = logger;
            ServiceHelper.InitializeController();
        }

        /// <summary>
        ///  Loads the specified document from the cache, if possible. Errors if the document is not in the cache.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [ServiceErrorAttribute(Message = "The document could not be loaded from the cache")]
        [HttpPost("api/[controller]/[action]")]
        public LoadFromCacheResponse LoadFromCache(LoadFromCacheRequest request)
        {
            var response = new LoadFromCacheResponse();
            try
            {
                if (request == null)
                    throw new ArgumentNullException("request");

                ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.DocumentId);
                var loadFromCacheOptions = new LoadFromCacheOptions
                {
                    Cache = cache,
                    DocumentId = request.DocumentId,
                    UserToken = ServiceHelper.GetUserToken(this.Request.Headers, request)
                };
                using (var document = DocumentFactory.LoadFromCache(loadFromCacheOptions))
                {
                    // Return null if the document does not exist in the cache
                    // If you want to throw an error then call:
                    // DocumentHelper.CheckLoadFromCache(document);

                    if (document != null && document.CacheStatus == DocumentCacheStatus.NotSynced)
                    {
                        // This means this document was uploaded and never loaded, make sure it does not delete itself after we dispose it and perform the same action as if
                        // a document was loaded from a URI
                        CacheController.TrySetCacheUri(document);

                        if (ServiceHelper.AutoUpdateHistory)
                            document.History.AutoUpdate = true;

                        ServiceHelper.SetRasterCodecsOptions(document.RasterCodecs, (int)document.Pages.DefaultResolution);
                        document.AutoDeleteFromCache = false;
                        document.AutoDisposeDocuments = true;
                        document.AutoSaveToCache = false;
                        document.SaveToCache();
                    }

                    response = new LoadFromCacheResponse { Document = document };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message} - Moti Mashiah Message");
            }
            

            return response;
        }

        /// <summary>
        ///  Creates and stores an entry for the image at the URI, returning the appropriate Document data.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [ServiceErrorAttribute(Message = "The uri data could not be loaded")]
        [HttpPost("api/[controller]/[action]"), HttpGet("api/[controller]/[action]")] // Support GET only for testing
        public LoadFromUriResponse LoadFromUri(LoadFromUriRequest request)
        {
            if (request == null)
            {
                var m = new ArgumentNullException("request");
                _logger.LogError(m.Message);
            }
                

            if (request.Uri == null)
            {
                var m = new ArgumentException("uri must be specified");
                _logger.LogError(m.Message);
            }
            else
            {
                if (request.Uri.ToString().StartsWith("á"))
                {
                    var uri = TabFusionSecure.DecryptURLParameters(request.Uri.ToString());
                    request.Uri = new Uri(HttpUtility.UrlDecode(uri));
                }
            }

            //ServiceHelper.CheckUriScheme(request.Uri);
            if (request.Options != null && request.Options.AnnotationsUri != null)
                //ServiceHelper.CheckUriScheme(request.Options.AnnotationsUri);

            if (request.Resolution < 0)
                throw new ArgumentException("Resolution must be a value greater than or equal to zero");

            var loadOptions = new LoadDocumentOptions();
            string userToken = ServiceHelper.GetUserToken(this.Request.Headers, request);
            if (request.Options != null)
            {
                loadOptions.DocumentId = request.Options.DocumentId;
                loadOptions.UserToken = userToken;
                loadOptions.AnnotationsUri = request.Options.AnnotationsUri;
                loadOptions.Name = request.Options.Name;
                loadOptions.Password = request.Options.Password;
                loadOptions.LoadEmbeddedAnnotations = request.Options.LoadEmbeddedAnnotations;
                loadOptions.RenderAnnotations = request.Options.RenderAnnotations;
                loadOptions.LoadAttachmentsMode = request.Options.LoadAttachmentsMode;
                loadOptions.LoadFormFieldsMode = request.Options.LoadFormFieldsMode;
                loadOptions.MaximumImagePixelSize = request.Options.MaximumImagePixelSize;
                loadOptions.FirstPageNumber = request.Options.FirstPageNumber;
                loadOptions.LastPageNumber = request.Options.LastPageNumber;
                loadOptions.MimeType = request.Options.MimeType;

                if (!string.IsNullOrEmpty(request.Options.UserToken))
                    userToken = request.Options.UserToken;

                if (request.Options.RedactionOptions != null)
                    loadOptions.RedactionOptions = request.Options.RedactionOptions;

                loadOptions.TimeoutMilliseconds = request.Options.TimeoutMilliseconds;
            }

            // If the user did not specify a timeout use the service value
            if (loadOptions.TimeoutMilliseconds == 0)
            {
                loadOptions.TimeoutMilliseconds = ServiceHelper.LoadTimeoutMilliseconds;
            }

            loadOptions.UserToken = userToken;
            // Check if the user passed a mime type, use it. Otherwise, try to guess from the document name or URL
            if (string.IsNullOrEmpty(loadOptions.MimeType))
            {
                // Get the document name
                var documentName = request.Uri.ToString();

                // Check if this document was uploaded, then hope the user has set LoadDocumentOptions.Name to the original file name
                if (DocumentFactory.IsUploadDocumentUri(request.Uri) && !string.IsNullOrEmpty(loadOptions.Name))
                {
                    // Use that instead
                    documentName = loadOptions.Name;
                }

                // Most image file formats have a signature that can be used to detect to detect the type of the file.
                // However, some formats supported by LEADTOOLS do not, such as plain text files (TXT) or DXF CAD format or 
                // For these, we detect the MIME type from the file extension if available and set it in the load document options and the
                // document library will use this value if it fails to detect the file format from the data.

                if (!string.IsNullOrEmpty(documentName) && !DocumentFactory.IsLeadCacheScheme(documentName))
                    loadOptions.MimeType = RasterCodecs.GetExtensionMimeType(documentName);
            }

            ObjectCache cache = ServiceHelper.CacheManager.GetCacheForLoadFromUri(request.Uri, loadOptions);
            loadOptions.Cache = cache;
            loadOptions.UseCache = cache != null;
            loadOptions.CachePolicy = ServiceHelper.CacheManager.CreatePolicy(cache);

            LEADDocument document = null;
            var loadfromurlrespose = new LoadFromUriResponse();
            try
            {
                // first, check if this is pre-cached
                if (PreCacheHelper.PreCacheExists)
                {
                    string documentId = PreCacheHelper.CheckDocument(request.Uri, loadOptions.MaximumImagePixelSize);
                    if (documentId != null)
                    {
                        var loadFromCacheOptions = new LoadFromCacheOptions
                        {
                            Cache = cache,
                            DocumentId = documentId
                        };
                        var precachedDocument = DocumentFactory.LoadFromCache(loadFromCacheOptions);
                        if (request.LoadPreCached)
                        {
                            // Means the user specifically asked for us not to clone this document. Most probably
                            // single user system or pre-cached for viewing only.
                            document = precachedDocument;
                        }
                        else
                        {
                            // Instead of returning the same pre-cached document, we'll return a cloned version.
                            // This allows the user to make changes (get/set annotations) without affecting the pre-cached version.
                            document = precachedDocument.Clone(cache, new CloneDocumentOptions()
                            {
                                CachePolicy = ServiceHelper.CacheManager.CreatePolicy(cache)
                            });
                        }
                    }
                }

                // else, load normally
                if (document == null)
                {
                    document = DocumentFactory.LoadFromUri(request.Uri, loadOptions);
                    if (document == null)
                    {
                        // This document was rejected due to its mimeTypes or because it was timed out, check the condition
                        ServiceHelper.CheckDocumentFailedToLoad(request.Uri, loadOptions, -1);
                    }
                }

                CacheController.TrySetCacheUri(document);

                if (ServiceHelper.AutoUpdateHistory)
                    document.History.AutoUpdate = true;

                ServiceHelper.SetRasterCodecsOptions(document.RasterCodecs, request.Resolution);
                document.AutoDeleteFromCache = false;
                document.AutoDisposeDocuments = true;
                document.AutoSaveToCache = false;
                document.SaveToCache();

                /* 
                 * NOTE: Use the line below to add this new document
                      * to the pre-cache. By doing so, everyone loading a document from
                 * that URL will get a copy of the same document from the cache/pre-cache.
                 * 
                 * if (!isInPrecache)
                 *  PreCacheHelper.AddExistingDocument(request.Uri, document);
                 */
                loadfromurlrespose = new LoadFromUriResponse { Document = document };
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                if (document != null)
                    document.Dispose();
            }
            finally
            {
                if (document != null)
                    document.Dispose();
            }
            return loadfromurlrespose;
        }

        /// <summary>
        ///  Creates a link that a document can be uploaded to for storing in the cache. Meant to be used with UploadDocument.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "request")]
        [ServiceErrorAttribute(Message = "The cache could not create an upload url")]
        [HttpPost("api/[controller]/[action]")]
        public BeginUploadResponse BeginUpload(BeginUploadRequest request)
        {
            var uploadOptions = request?.Options;
            if (uploadOptions == null)
                uploadOptions = new UploadDocumentOptions();

            if (string.IsNullOrEmpty(uploadOptions.DocumentId) && !string.IsNullOrEmpty(request?.DocumentId))
                uploadOptions.DocumentId = request.DocumentId;

            string userToken = ServiceHelper.GetUserToken(this.Request.Headers, request);
            if (string.IsNullOrEmpty(uploadOptions.UserToken) && !string.IsNullOrEmpty(userToken))
                uploadOptions.UserToken = userToken;

            ObjectCache cache = ServiceHelper.CacheManager.GetCacheForBeginUpload(uploadOptions);
            uploadOptions.Cache = cache;
            Uri uploadUri = DocumentFactory.BeginUpload(uploadOptions);
            return new BeginUploadResponse { UploadUri = uploadUri };
        }

        /// <summary>
        /// Uploads a chunk of data to the specified URL in the cache.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [ServiceErrorAttribute(Message = "The document data could not be uploaded")]
        [HttpPost("api/[controller]/[action]")]
        public Response UploadDocument(UploadDocumentRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (request.Uri == null)
                throw new ArgumentException("uri must be specified");

            byte[] byteArray = null;
            int byteArrayLength = 0;

            if (request.Buffer != null)
            {
                byteArray = request.Buffer;
                byteArrayLength = request.BufferLength;
            }
            else if (request.Data != null)
            {
                byteArray = System.Convert.FromBase64String(request.Data);
                byteArrayLength = byteArray.Length;
            }

            ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.Uri);
            DocumentFactory.UploadDocument(cache, request.Uri, byteArray, 0, byteArrayLength);
            return new Response();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [ServiceErrorAttribute(Message = "The document blob data could not be uploaded")]
        [HttpPost("api/[controller]/[action]")]
        public async Task<Response> UploadDocumentBlob([FromQuery] UploadDocumentBlobRequest request, IFormFile file)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (request.Uri == null)
                throw new ArgumentException("uri must be specified");

            if (file == null)
                throw new ArgumentNullException("file must be specified");

            ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.Uri);

            using (Stream stream = file.OpenReadStream())
            {
                var buffer = new byte[1024 * 1024];
                int bytes = 0;
                do
                {
                    bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytes > 0)
                        DocumentFactory.UploadDocument(cache, request.Uri, buffer, 0, bytes);
                }
                while (bytes > 0);
            }
            return new Response();
        }

        /// <summary>
        ///  Optional - marks the end of uploading data for the specified URL in the cache. The service can execute commands to do
        ///  additional processing with this fully-uploaded document.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "request")]
        [ServiceErrorAttribute(Message = "The document upload could not be completed")]
        [HttpPost("api/[controller]/[action]")]
        public Response EndUpload(EndUploadRequest request)
        {
            // Note - we cannot always expect this endpoint to get called or be successful.
            if (request != null)
            {
                ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.Uri);
                DocumentFactory.EndUpload(cache, request.Uri);
            }
            return new Response();
        }

        /// <summary>
        /// Aborts the document upload.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [ServiceErrorAttribute(Message = "The document upload could not be aborted")]
        [HttpPost("api/[controller]/[action]")]
        public Response AbortUploadDocument(AbortUploadDocumentRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            // Only useful in cases where routing matches but uri is null (like ".../CancelUpload?uri")

            try
            {
                ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.Uri);
                DocumentFactory.AbortUploadDocument(cache, request.Uri);
            }
            catch
            {
                //ignore any error
            }

            return new Response();
        }

        /// <summary>
        ///  Saves the specified document to the cache. If the document is not in the cache it will be created.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [ServiceErrorAttribute(Message = "The document could not be saved to the cache")]
        [HttpPost("api/[controller]/[action]")]
        public SaveToCacheResponse SaveToCache(SaveToCacheRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            if (request.Descriptor == null)
                throw new ArgumentNullException("Descriptor");

            ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.Descriptor.DocumentId);

            // First try to load it from the cache, if success, update it. Otherwise, assume it is not there and create a new document

            var loadFromCacheOptions = new LoadFromCacheOptions
            {
                Cache = cache,
                DocumentId = request.Descriptor.DocumentId,
                UserToken = ServiceHelper.GetUserToken(this.Request.Headers, request)
            };

            using (var document = DocumentFactory.LoadFromCache(loadFromCacheOptions))
            {
                if (document != null)
                {
                    // Update it
                    document.UpdateFromDocumentDescriptor(request.Descriptor);
                    document.AutoDeleteFromCache = false;
                    document.AutoDisposeDocuments = true;
                    document.AutoSaveToCache = false;
                    document.SaveToCache();
                    return new SaveToCacheResponse { Document = document };
                }
            }

            // Above failed, create a new one.
            var createOptions = new CreateDocumentOptions();
            createOptions.Descriptor = request.Descriptor;
            cache = ServiceHelper.CacheManager.GetCacheForCreate(createOptions);
            createOptions.Cache = cache;
            createOptions.CachePolicy = ServiceHelper.CacheManager.CreatePolicy(cache);
            createOptions.UseCache = cache != null;
            using (var document = DocumentFactory.Create(createOptions))
            {
                if (document == null)
                    throw new InvalidOperationException("Failed to create document");

                CacheController.TrySetCacheUri(document);

                if (ServiceHelper.AutoUpdateHistory)
                    document.History.AutoUpdate = true;

                document.AutoDeleteFromCache = false;
                document.AutoDisposeDocuments = true;
                document.AutoSaveToCache = false;
                document.SaveToCache();
                return new SaveToCacheResponse { Document = document };
            }
        }

        /// <summary>
        ///  Saves the specified document to the cache. If the document is not in the cache it will be created.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [ServiceErrorAttribute(Message = "The document could not be cloned")]
        [HttpPost("api/[controller]/[action]")]
        public CloneDocumentResponse CloneDocument(CloneDocumentRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            string userToken = ServiceHelper.GetUserToken(this.Request.Headers, request);

            // Get the source cache
            ObjectCache sourceCache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.DocumentId);

            // Get the target cache
            ObjectCache targetCache = sourceCache;
            if (!string.IsNullOrEmpty(request.TargetCacheName))
            {
                targetCache = ServiceHelper.CacheManager.GetCacheByName(request.TargetCacheName);
            }

            // Clone options
            var cloneDocumentOptions = new CloneDocumentOptions
            {
                DocumentId = request.CloneDocumentId,
                CachePolicy = ServiceHelper.CacheManager.CreatePolicy(targetCache),
                UserToken = userToken
            };

            // Clone it
            string targetDocumentId = DocumentFactory.CloneDocument(sourceCache, targetCache, request.DocumentId, cloneDocumentOptions);

            // See if the user asked us to delete the source
            if (request.DeleteSourceDocument)
            {
                var deleteFromCacheOptions = new LoadFromCacheOptions
                {
                    Cache = sourceCache,
                    DocumentId = request.DocumentId,
                    UserToken = userToken
                };
                DocumentFactory.DeleteFromCache(deleteFromCacheOptions);
            }

            // Load the target document and return it
            var loadFromCacheOptions = new LoadFromCacheOptions
            {
                Cache = targetCache,
                DocumentId = targetDocumentId,
                UserToken = userToken
            };
            using (LEADDocument document = DocumentFactory.LoadFromCache(loadFromCacheOptions))
            {
                document.AutoSaveToCache = false;
                document.AutoDeleteFromCache = false;
                document.SaveToCache();

                return new CloneDocumentResponse
                {
                    Document = document
                };
            }
        }

        /* Deletes the document immediately from the cache.
         * Usually changing AutoDeleteFromCache to true
         * would only delete the document when the cache is purged,
         * but it also deletes it the next time the document is cleaned
         * up - which happens to be right after we are finished changing
         * the autoDeleteFromCache property. So it's immediate.
         */
        /// <summary>
        /// Deletes the document from the cache.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [ServiceErrorAttribute(Message = "The document could not be deleted", MethodName = "DeleteFromCache")]
        [HttpPost("api/[controller]/[action]")]
        public Response Delete(DeleteRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            DocumentHelper.DeleteDocument(request.DocumentId, this.Request.Headers, !request.DeletePreCached, !request.AllowNonExisting);

            return new Response();
        }

        /// <summary>
        /// Purges the cache of all outdated items. Requires a passcode set on the service's configuration.
        /// </summary>
        // used to check the policies and remove outstanding cache items.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "request")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [ServiceErrorAttribute(Message = "The cache could not be purged")]
        [HttpPost("api/[controller]/[action]"), HttpGet("api/[controller]/[action]")]
        public Response PurgeCache(string passcode = null)
        {
            string passToCheck = ServiceHelper.GetSettingValue(ServiceHelper.Key_Access_Passcode);
            if (!string.IsNullOrWhiteSpace(passToCheck) && passcode != passToCheck)
                throw new ServiceException("Cache cannot be purged - passcode is incorrect", HttpStatusCode.Unauthorized);

            ServiceHelper.CacheManager.RemoveExpiredItems(null);

            return new Response();
        }

        /// <summary>
        /// Checks the policies of the cache items and returns statistics, without deleting expired items.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "request")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [ServiceErrorAttribute(Message = "The cache statistics could not be retrieved")]
        [HttpGet("api/[controller]/[action]")]
        public GetCacheStatisticsResponse GetCacheStatistics(string passcode = null)
        {
            string passToCheck = ServiceHelper.GetSettingValue(ServiceHelper.Key_Access_Passcode);
            if (!string.IsNullOrWhiteSpace(passToCheck) && passcode != passToCheck)
                throw new ServiceException("Cache statistics cannot be retrieved - passcode is incorrect", HttpStatusCode.Unauthorized);

            Caching.CacheStatistics statistics = ServiceHelper.CacheManager.GetCacheStatistics(null);
            return new GetCacheStatisticsResponse()
            {
                Statistics = statistics
            };
        }

        /// <summary>
        /// Adds the specified document to the cache with an unlimited expiration and all document data. Future calls to LoadFromUri may return
        /// this document (matched by URI).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "request")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [ServiceErrorAttribute(Message = "The document could not be pre-cached")]
        [HttpPost("api/[controller]/[action]")]
        public PreCacheDocumentResponse PreCacheDocument(PreCacheDocumentRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            string passToCheck = ServiceHelper.GetSettingValue(ServiceHelper.Key_Access_Passcode);
            if (!string.IsNullOrWhiteSpace(passToCheck) && request.Passcode != passToCheck)
                throw new ServiceException("Document cannot be pre-cached - passcode is incorrect", HttpStatusCode.Unauthorized);

            if (request.Uri == null)
                throw new ArgumentException("uri must be specified");

            ServiceHelper.CheckUriScheme(request.Uri);

            // Check if we setup pre-caching system, otherwise, pre-cache existing document in the cache
            bool preCacheDictionaryExists = PreCacheHelper.PreCacheExists;

            ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.Uri.ToString());

            // Get the cache options, if none, use All (means if the user did not pass a value, we will cache everything in the document)
            if (request.CacheOptions == DocumentCacheOptions.None)
                request.CacheOptions = DocumentCacheOptions.All;

            return PreCacheHelper.AddDocument(preCacheDictionaryExists, cache, request);
        }

        /// <summary>
        /// Returns all the entries in the pre-cache.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "request")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [ServiceErrorAttribute(Message = "The pre-cache dictionary could not be returned")]
        [HttpGet("api/[controller]/[action]")]
        public ReportPreCacheResponse ReportPreCache([FromQuery] ReportPreCacheRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            string passToCheck = ServiceHelper.GetSettingValue(ServiceHelper.Key_Access_Passcode);
            if (!string.IsNullOrWhiteSpace(passToCheck) && request.Passcode != passToCheck)
                throw new ServiceException("Pre-cache cannot be reported - passcode is incorrect", HttpStatusCode.Unauthorized);

            if (!PreCacheHelper.PreCacheExists)
            {
                // Return an empty report
                return new ReportPreCacheResponse();
            }

            return PreCacheHelper.ReportDocuments(request.Clear, request.Clean);
        }

        /// <summary>
        /// Checks that the mimetype for an uploaded document is acceptable to load.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [ServiceErrorAttribute(Message = "The document cache info could not be retrieved")]
        [HttpPost("api/[controller]/[action]")]
        public CheckCacheInfoResponse CheckCacheInfo(CheckCacheInfoRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.Uri.ToString());
            DocumentCacheInfo cacheInfo = DocumentFactory.GetDocumentCacheInfo(cache, request.Uri.ToString());

            if (cacheInfo == null)
                return new CheckCacheInfoResponse();

            var documentMimeType = cacheInfo.MimeType;
            var status = DocumentFactory.MimeTypes.GetStatus(documentMimeType);
            var isAccepted = status != DocumentMimeTypeStatus.Denied;

            var serviceCacheInfo = new CacheInfo
            {
                IsVirtual = cacheInfo.IsVirtual,
                IsLoaded = cacheInfo.IsLoaded,
                HasAnnotations = cacheInfo.HasAnnotations,
                Name = cacheInfo.Name,
                MimeType = documentMimeType,
                IsMimeTypeAccepted = isAccepted,
                PageCount = cacheInfo.PageCount,
                UserToken = cacheInfo.UserToken,
                HasUserToken = cacheInfo.HasUserToken,
                IsUsingMemoryCache = cacheInfo.IsUsingMemoryCache,
                CacheName = cacheInfo.CacheName
            };

            return new CheckCacheInfoResponse
            {
                CacheInfo = serviceCacheInfo
            };
        }

        /// <summary>
        /// Downloads the annotations of a document for external use.
        /// If no annotations exist, an empty XML file is returned.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "request")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [ServiceErrorAttribute(Message = "The annotations could not be downloaded")]
        [HttpGet("api/[controller]/[action]")]
        public IActionResult DownloadAnnotations([FromQuery] DownloadAnnotationsRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if ((request.DocumentId == null && request.Uri == null) || (request.DocumentId != null && request.Uri != null))
                throw new InvalidOperationException("DocumentId or Uri must not be null, but not both");

            ObjectCache cache;
            DocumentCacheInfo cacheInfo;

            if (request.Uri != null)
            {
                cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.Uri);
                cacheInfo = DocumentFactory.GetDocumentCacheInfo(cache, request.Uri);
            }
            else
            {
                cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.DocumentId);
                cacheInfo = DocumentFactory.GetDocumentCacheInfo(cache, request.DocumentId);
            }
            DocumentHelper.CheckCacheInfo(cacheInfo);

            string name = "download";
            if (!string.IsNullOrEmpty(cacheInfo.Name))
            {
                string documentMimeType = cacheInfo.MimeType;
                string documentExtension = RasterCodecs.GetMimeTypeExtension(documentMimeType);
                string cacheInfoName = ServiceHelper.RemoveExtension(cacheInfo.Name, documentExtension);
                // We will response this ContentDisposition.FileName, and this has to be ASCII
                if (ServiceHelper.IsASCII(cacheInfoName))
                    name = cacheInfoName;
            }

            string annotationsName = string.Format("{0}_ann.xml", name);

            string responseFileName = annotationsName;
            string responseContentType = "application/xml";

            string documentId = cacheInfo.DocumentId;

            var response = HttpContext.Response;

            // For "Save to Google Drive" access, we must have the appropriate CORS headers.
            // See https://developers.google.com/drive/v3/web/savetodrive
            response.Headers.Remove("Access-Control-Allow-Headers");
            response.Headers.Add("Access-Control-Allow-Headers", "Range");
            response.Headers.Remove("Access-Control-Expose-Headers");
            response.Headers.Add("Access-Control-Expose-Headers", "Cache-Control, Content-Encoding, Content-Range");

            var outputStream = new MemoryStream();
            string userToken = ServiceHelper.GetUserToken(this.Request.Headers, request);
            var downloadAnnotationsOptions = new DownloadDocumentOptions
            {
                Cache = cache,
                DocumentId = documentId,
                UserToken = userToken,
                Offset = 0,
                Length = -1,
                Stream = outputStream
            };
            DocumentFactory.DownloadAnnotations(downloadAnnotationsOptions);
            ServiceHelper.SetResponseViewFileName(response, responseFileName, responseContentType, request.ContentDisposition);
            outputStream.Position = 0;
            return File(outputStream, responseContentType);

        }

        /// <summary>
        /// Downloads a document (and possibly annotations) for external use.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "request")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [ServiceErrorAttribute(Message = "The item could not be downloaded")]
        [HttpGet("api/[controller]/[action]")]
        public IActionResult DownloadDocument([FromQuery] DownloadDocumentRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if ((request.DocumentId == null && request.Uri == null) || (request.DocumentId != null && request.Uri != null))
                throw new InvalidOperationException("DocumentId or Uri must not be null, but not both");

            ObjectCache cache;
            DocumentCacheInfo cacheInfo;

            if (request.Uri != null)
            {
                cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.Uri);
                cacheInfo = DocumentFactory.GetDocumentCacheInfo(cache, request.Uri);
            }
            else
            {
                cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.DocumentId);
                cacheInfo = DocumentFactory.GetDocumentCacheInfo(cache, request.DocumentId);
            }
            DocumentHelper.CheckCacheInfo(cacheInfo);

            // Need to find the mime type of this item we are downloading.
            // See if we have a mime type, if it fails, check the document name.
            // If this fails as well, use a generic mime type
            string documentExtension = null;
            string documentMimeType = cacheInfo.MimeType;
            const string defaultMimeType = "application/octet-stream";

            // Get it from the mime type
            if (string.IsNullOrEmpty(documentMimeType) && !string.IsNullOrEmpty(cacheInfo.Name))
            {
                documentMimeType = RasterCodecs.GetExtensionMimeType(cacheInfo.Name);
                if (string.IsNullOrEmpty(documentMimeType))
                {
                    documentMimeType = defaultMimeType;

                    try
                    {
                        documentExtension = Path.GetExtension(cacheInfo.Name);
                        if (string.IsNullOrEmpty(documentExtension))
                            documentExtension = null;
                        if (documentExtension != null && documentExtension.StartsWith("."))
                            documentExtension = documentExtension.Substring(1);
                    }
                    catch { }
                }
            }

            if (documentExtension == null)
            {
                if (!string.IsNullOrEmpty(documentMimeType))
                {
                    documentExtension = RasterCodecs.GetMimeTypeExtension(documentMimeType);
                }
                else
                {
                    documentMimeType = defaultMimeType;
                }
            }

            if (string.IsNullOrEmpty(documentExtension))
            {
                documentExtension = "data";
            }

            string name = "download";

            // If the user passed a file name with extension, use it
            if (!string.IsNullOrEmpty(request.FileName) && ServiceHelper.IsASCII(request.FileName))
            {
                name = ServiceHelper.RemoveExtension(request.FileName, documentExtension);
            }
            else if (!string.IsNullOrEmpty(cacheInfo.Name))
            {
                string cacheInfoName = ServiceHelper.RemoveExtension(cacheInfo.Name, documentExtension);
                // We will response this ContentDisposition.FileName, and this has to be ASCII
                if (ServiceHelper.IsASCII(cacheInfoName))
                    name = cacheInfoName;

                name = name.Replace(' ', '-');
            }

            string documentName = string.Format("{0}.{1}", name, documentExtension);
            string annotationsName = string.Format("{0}_ann.xml", name);

            string responseFileName = documentName;
            string responseContentType = documentMimeType;

            bool zipAnnotations = request.IncludeAnnotations && cacheInfo.HasAnnotations;
            if (zipAnnotations)
            {
                // We will create a ZIP file
                responseFileName = string.Format("{0}.zip", name);
                responseContentType = MediaTypeNames.Application.Zip;
            }

            string documentId = cacheInfo.DocumentId;
            string userToken = ServiceHelper.GetUserToken(this.Request.Headers, request);

            // For "Save to Google Drive" access, we must have the appropriate CORS headers.
            // See https://developers.google.com/drive/v3/web/savetodrive
            var response = HttpContext.Response;
            response.Headers.Remove("Access-Control-Allow-Headers");
            response.Headers.Add("Access-Control-Allow-Headers", "Range");
            response.Headers.Remove("Access-Control-Expose-Headers");
            response.Headers.Add("Access-Control-Expose-Headers", "Cache-Control, Content-Encoding, Content-Range");

            MemoryStream outputStream = new MemoryStream();
            if (zipAnnotations)
            {
                // We must create a new memory stream because Package.Open tries to request position
                using (var zipStream = new MemoryStream())
                {
                    using (var package = Package.Open(zipStream, FileMode.CreateNew))
                    {
                        var documentPart = package.CreatePart(new Uri(string.Format("/{0}", documentName), UriKind.Relative), documentMimeType);
                        Stream documentStream = documentPart.GetStream();

                        var downloadDocumentOptions = new DownloadDocumentOptions
                        {
                            Cache = cache,
                            DocumentId = documentId,
                            UserToken = userToken,
                            Offset = 0,
                            Length = -1,
                            Stream = documentStream
                        };

                        DocumentFactory.DownloadDocument(downloadDocumentOptions);
                        if (request.SignDocument)
                            DocumentHelper.SignDocument(documentStream, documentMimeType);
                        documentStream.Close();

                        var annotationsPart = package.CreatePart(new Uri(string.Format("/{0}", annotationsName), UriKind.Relative), "text/xml");
                        Stream annotationsStream = annotationsPart.GetStream();

                        var downloadAnnotationsOptions = new DownloadDocumentOptions
                        {
                            Cache = cache,
                            DocumentId = documentId,
                            UserToken = userToken,
                            Offset = 0,
                            Length = -1,
                            Stream = annotationsStream
                        };
                        DocumentFactory.DownloadAnnotations(downloadAnnotationsOptions);
                        annotationsStream.Close();
                    }

                    zipStream.Position = 0;
                    ServiceHelper.CopyStream(zipStream, outputStream);

                }
            }
            else
            {
                // Just the document
                var downloadDocumentOptions = new DownloadDocumentOptions();
                downloadDocumentOptions.Cache = cache;
                downloadDocumentOptions.DocumentId = documentId;
                downloadDocumentOptions.UserToken = userToken;
                downloadDocumentOptions.Offset = 0;
                downloadDocumentOptions.Length = -1;
                downloadDocumentOptions.Stream = outputStream;
                DocumentFactory.DownloadDocument(downloadDocumentOptions);
                if (request.SignDocument)
                    DocumentHelper.SignDocument(outputStream, documentMimeType);
            }

            ServiceHelper.SetResponseViewFileName(response, responseFileName, responseContentType, request.ContentDisposition);
            outputStream.Position = 0;
            return File(outputStream, responseContentType);
        }

        /// <summary>
        ///  Sends a heartbeat signal to the documents.
        ///  Used to update the memory cache value for these documents
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [ServiceErrorAttribute(Message = "Document heartbeats could not performed")]
        [HttpPost("api/[controller]/[action]")]
        public Response DocumentsHeartbeat(DocumentsHeartbeatRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (request.DocumentIds == null || request.DocumentIds.Length == 0)
                return new Response();

            if (DocumentFactory.DocumentMemoryCache.IsStarted)
            {
                foreach (string documentId in request.DocumentIds)
                    DocumentFactory.DocumentMemoryCache.HasDocument(documentId, true);
            }

            return new Response();
        }

        /// <summary>
        ///  Saves an attachment to the cache
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [ServiceErrorAttribute(Message = "The attachment could not be saved")]
        [HttpPost("api/[controller]/[action]")]
        public SaveAttachmentToCacheResponse SaveAttachmentToCache(SaveAttachmentToCacheRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            if (request.DocumentId == null)
                throw new InvalidOperationException("DocumentId must not be null");

            if (request.Options == null)
                throw new ArgumentNullException("Options");

            // Load the source document
            string userToken = ServiceHelper.GetUserToken(this.Request.Headers, request);
            ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.DocumentId);
            var loadFromCacheOptions = new LoadFromCacheOptions
            {
                Cache = cache,
                DocumentId = request.DocumentId,
                UserToken = userToken
            };
            using (var document = DocumentFactory.LoadFromCache(loadFromCacheOptions))
            {
                DocumentHelper.CheckLoadFromCache(document);

                // Save the attachment into the cache
                UploadDocumentOptions uploadDocumentOptions = request.Options.UploadDocumentOptions;
                if (uploadDocumentOptions == null)
                {
                    uploadDocumentOptions = new UploadDocumentOptions();
                    request.Options.UploadDocumentOptions = uploadDocumentOptions;
                }

                ObjectCache targetCache = ServiceHelper.CacheManager.GetCacheForBeginUpload(uploadDocumentOptions);
                uploadDocumentOptions.Cache = targetCache;
                uploadDocumentOptions.CachePolicy = ServiceHelper.CacheManager.CreatePolicy(targetCache);
                uploadDocumentOptions.UserToken = userToken;

                request.Options.UpdateAttachmentDocumentId = true;
                string attachmentDocumentId = document.SaveAttachmentToCache(request.Options);

                // Done
                return new SaveAttachmentToCacheResponse
                {
                    UploadUri = DocumentFactory.MakeLeadCacheUri(attachmentDocumentId)
                };
            }
        }

        /// <summary>
        ///  Loads an attachment document
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [ServiceErrorAttribute(Message = "The attachment could not be loaded")]
        [HttpPost("api/[controller]/[action]")]
        public LoadDocumentAttachmentResponse LoadDocumentAttachment(LoadDocumentAttachmentRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            if (request.DocumentId == null)
                throw new InvalidOperationException("DocumentId must not be null");
            if (request.Options == null)
                throw new ArgumentNullException("Options");

            // Load the source document
            string userToken = ServiceHelper.GetUserToken(this.Request.Headers, request);
            ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.DocumentId);
            var loadFromCacheOptions = new LoadFromCacheOptions
            {
                Cache = cache,
                DocumentId = request.DocumentId,
                UserToken = userToken
            };
            using (var document = DocumentFactory.LoadFromCache(loadFromCacheOptions))
            {
                DocumentHelper.CheckLoadFromCache(document);

                // Load the attachment
                request.Options.UpdateAttachmentDocumentId = true;

                // If the user did not specify a document ID set one, so we can check against it 
                // if the document was rejected due to its mime type
                if (request.Options.LoadDocumentOptions != null && string.IsNullOrEmpty(request.Options.LoadDocumentOptions.DocumentId))
                {
                    request.Options.LoadDocumentOptions.DocumentId = DocumentFactory.NewCacheId();
                }

                using (var attachmentDocument = document.LoadDocumentAttachment(request.Options))
                {
                    if (attachmentDocument == null)
                    {
                        // This document was rejected due to its mimeTypes or because it was timed out, check the condition
                        ServiceHelper.CheckDocumentFailedToLoad(null, request.Options.LoadDocumentOptions, request.Options.AttachmentNumber);
                    }

                    CacheController.TrySetCacheUri(attachmentDocument);

                    if (ServiceHelper.AutoUpdateHistory)
                        attachmentDocument.History.AutoUpdate = true;

                    attachmentDocument.AutoDeleteFromCache = false;
                    attachmentDocument.AutoDisposeDocuments = true;
                    attachmentDocument.AutoSaveToCache = false;
                    attachmentDocument.SaveToCache();

                    return new LoadDocumentAttachmentResponse { Document = attachmentDocument };
                }
            }
        }
    }
}
