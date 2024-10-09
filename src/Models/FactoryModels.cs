// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

using Leadtools.Document;
using Leadtools.Caching;
using Leadtools.Services.Models;

namespace Leadtools.DocumentViewer.Models.Factory
{
   [DataContract]
   public class LoadFromCacheRequest : Request
   {
      /// <summary>
      /// The ID to load from the cache (which must be retrieved from an item after LoadFromUri was called, and saved).
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }
   }

   [DataContract]
   public class LoadFromCacheResponse : Response
   {
      /// <summary>
      /// The serialized LEADDocument instance.
      /// </summary>
      [DataMember(Name = "document")]
      public LEADDocument Document { get; set; }
   }

   [DataContract]
   public class LoadFromUriRequest : Request
   {
      /// <summary>
      /// The options to use when loading this document (serialized Leadtools.Document.LoadDocumentOptions instance).
      /// </summary>
      [DataMember(Name = "options")]
      public LoadDocumentOptions Options { get; set; }

      /// <summary>
      /// The URI to the document to be loaded.
      /// </summary>
      [DataMember(Name = "uri")]
      public Uri Uri { get; set; }

      /// <summary>
      /// The resolution to load the document at. To use the default, pass 0.
      /// </summary>
      [DataMember(Name = "resolution")]
      public int Resolution { get; set; }

      /// <summary>
      /// If this document is pre-cached, load it directly and do not automatically clone it
      /// </summary>
      [DataMember(Name = "loadPreCached")]
      public bool LoadPreCached { get; set; }
   }

   [DataContract]
   public class LoadFromUriResponse : Response
   {
      /// <summary>
      /// The serialized LEADDocument instance.
      /// </summary>
      [DataMember(Name = "document")]
      public LEADDocument Document { get; set; }
   }

   [DataContract]
   public class BeginUploadRequest : Request
   {
      /// <summary>
      /// The ID to use for the new document, or null to create a new random DocumentId.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The options to use for uploading the document.
      /// </summary>
      [DataMember(Name = "options")]
      public UploadDocumentOptions Options { get; set; }
   }

   [DataContract]
   public class BeginUploadResponse : Response
   {
      /// <summary>
      /// The URI of the uploaded document in the cache.
      /// </summary>
      [DataMember(Name = "uploadUri")]
      public Uri UploadUri { get; set; }
   }

   [DataContract]
   public class UploadDocumentRequest : Request
   {
      /// <summary>
      /// The uri, retrieved from BeginUpload, that is used for uploading.
      /// </summary>
      [DataMember(Name = "uri")]
      public Uri Uri { get; set; }

      /// <summary>
      /// The data to upload as Base64 string. If this value is null, then Buffer is used.
      /// </summary>
      [DataMember(Name = "data")]
      public string Data { get; set; }

      /// <summary>
      /// The data to upload. The length of this buffer must be set in BufferLength. If this value is null, then Data is used.
      /// </summary>
      [DataMember(Name = "buffer")]
      public byte[] Buffer { get; set; }

      /// <summary>
      /// The length of Buffer in bytes.
      /// </summary>
      [DataMember(Name = "bufferLength")]
      public int BufferLength { get; set; }
   }

   [DataContract]
   public class UploadDocumentBlobRequest : Request
   {
      /// <summary>
      /// The uri, retrieved from BeginUpload, that is used for uploading.
      /// </summary>
      [DataMember(Name = "uri")]
      public Uri Uri { get; set; }
   }

   [DataContract]
   public class EndUploadRequest : Request
   {
      /// <summary>
      /// The uri, retrieved from BeginUpload, that is used for uploading.
      /// </summary>
      [DataMember(Name = "uri")]
      public Uri Uri { get; set; }
   }

   [DataContract]
   public class AbortUploadDocumentRequest : Request
   {
      /// <summary>
      /// The URI from BeginUpload to stop loading to.
      /// </summary>
      [DataMember(Name = "uri")]
      public Uri Uri { get; set; }
   }

   [DataContract]
   public class GetCacheStatisticsResponse : Response
   {
      /// <summary>
      /// The cache statistics information (serialized Leadtools.Caching.CacheStatistics)
      /// </summary>
      [DataMember(Name = "statistics")]
      public CacheStatistics Statistics { get; set; }
   }

   [DataContract]
   public class SaveToCacheRequest : Request
   {
      /// <summary>
      /// The data to use when creating or saving this document - a serialized instance of Leadtools.Document.DocumentDescriptor.
      /// </summary>
      [DataMember(Name = "descriptor")]
      public DocumentDescriptor Descriptor { get; set; }
   }

   [DataContract]
   public class SaveToCacheResponse : Response
   {
      /// <summary>
      /// The serialized LEADDocument instance.
      /// </summary>
      [DataMember(Name = "document")]
      public LEADDocument Document { get; set; }
   }

   [DataContract]
   public class CloneDocumentRequest : Request
   {
      /// <summary>
      /// The ID of the source document to clone
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The ID of the target cloned document (optional)
      /// </summary>
      [DataMember(Name = "cloneDocumentId")]
      public string CloneDocumentId { get; set; }

      /// <summary>
      /// Delete the source document on success.
      /// If this is true, then this method will perform a move operation. Otherwise, this is a copy operation
      /// </summary>
      [DataMember(Name = "deleteSourceDocument")]
      public bool DeleteSourceDocument { get; set; }

      /// <summary>
      /// Optional target cache name if the service supports multiple cashes
      /// </summary>
      [DataMember(Name = "targetCacheName")]
      public string TargetCacheName { get; set; }
   }

   [DataContract]
   public class CloneDocumentResponse : Response
   {
      /// <summary>
      /// The cloned document
      /// </summary>
      [DataMember(Name = "document")]
      public LEADDocument Document { get; set; }
   }

   [DataContract]
   public class DeleteRequest : Request
   {
      /// <summary>
      /// The document to delete.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// Do not throw an exception if the document does not exist in the cache
      /// </summary>
      [DataMember(Name = "allowNonExisting")]
      public bool AllowNonExisting { get; set; }

      /// <summary>
      /// Delete this document even if it was pre-cached
      /// </summary>
      [DataMember(Name = "deletePreCached")]
      public bool DeletePreCached { get; set; }
   }

   [DataContract]
   public class CheckCacheInfoRequest : Request
   {
      /// <summary>
      /// The URI to the document to verify the mimetype for. This may be a cache URI.
      /// </summary>
      [DataMember(Name = "uri")]
      public Uri Uri { get; set; }
   }

   [DataContract]
   public class CacheInfo
   {
      /// <summary>
      /// Whether or not the document is virtual.
      /// </summary>
      [DataMember(Name = "isVirtual")]
      public bool IsVirtual { get; set; }

      /// <summary>
      /// Whether or not the document is loaded already.
      /// </summary>
      [DataMember(Name = "isLoaded")]
      public bool IsLoaded { get; set; }

      /// <summary>
      /// Whether or not the document has annotations.
      /// </summary>
      [DataMember(Name = "hasAnnotations")]
      public bool HasAnnotations { get; set; }

      /// <summary>
      /// The document name, if one is set.
      /// </summary>
      [DataMember(Name = "name")]
      public string Name { get; set; }

      /// <summary>
      /// The reported mimeType of the document.
      /// </summary>
      [DataMember(Name = "mimeType")]
      public string MimeType { get; set; }

      /// <summary>
      /// Whether or not the mimeType is acceptable.
      /// </summary>
      [DataMember(Name = "isMimeTypeAccepted")]
      public bool IsMimeTypeAccepted { get; set; }

      /// <summary>
      /// The page count of the document.
      /// </summary>
      [DataMember(Name = "pageCount")]
      public int PageCount { get; set; }

      /// <summary>
      /// Document user token.
      /// </summary>
      [DataMember(Name = "userToken")]
      public string UserToken { get; set; }

      /// <summary>
      /// Indicate that the document has a user token
      /// </summary>
      [DataMember(Name = "hasUserToken")]
      public bool HasUserToken { get; set; }

      /// <summary>
      /// Indicate that this document uses the document memory cache
      /// </summary>
      [DataMember(Name = "isUsingMemoryCache")]
      public bool IsUsingMemoryCache { get; set; }

      /// <summary>
      /// The cache name
      /// </summary>
      [DataMember(Name = "cacheName")]
      public string CacheName { get; set; }
   }

   [DataContract]
   public class CheckCacheInfoResponse : Response
   {
      /// <summary>
      /// The cache info for the document. If null, the document does not exist.
      /// </summary>
      [DataMember(Name = "cacheInfo")]
      public CacheInfo CacheInfo { get; set; }
   }

   [DataContract]
   public class DownloadAnnotationsRequest : Request
   {
      /// <summary>
      /// The ID of the annotations in the cache to download. Cannot be used if URI is used.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The URI to the annotations to download. Cannot be used if ID is used.
      /// This may be a cache URI.
      /// </summary>
      [DataMember(Name = "uri")]
      public Uri Uri { get; set; }

      /// <summary>
      /// Content disposition
      /// This is optional, if the value is null or empty, then the response Content-Disposition header will be set to either attachment or inline depending on the mime file type.
      /// Otherwise, it will be set to this value.
      /// </summary>
      [DataMember(Name = "contentDisposition")]
      public string ContentDisposition { get; set; }
   }

   [DataContract]
   public class DownloadDocumentRequest : Request
   {
      /// <summary>
      /// The ID of the document in the cache to download. Cannot be used if URI is used.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The URI to the document to download. Cannot be used if ID is used.
      /// This may be a cache URI.
      /// </summary>
      [DataMember(Name = "uri")]
      public Uri Uri { get; set; }

      /// <summary>
      /// If true, external annotations will be returned as well (the result will be a ZIP).
      /// </summary>
      [DataMember(Name = "includeAnnotations")]
      public bool IncludeAnnotations { get; set; }

      /// <summary>
      /// Content disposition
      /// This is optional, if the value is null or empty, then the response Content-Disposition header will be set to either attachment or inline depending on the mime file type.
      /// Otherwise, it will be set to this value.
      /// For instance, if this document is a PDF, then by default Content-Disposition will be set to inline since the mime type is viewable in the browser. This allows the user to open the
      /// document directly in the browser.
      /// To force downloading, set the value of Content-Disposition to "attachment".
      /// </summary>
      [DataMember(Name = "contentDisposition")]
      public string ContentDisposition { get; set; }

      /// <summary>
      /// Filename
      /// This is optional. If the value is null or empty then downloaded filename will be "LEADDocument.Name.ext", where the extension is obtained form the document mime type.
      /// Otherwise, then downloaded filename will be "fileName.ext", where the extension is obtained form the document mime type.
      /// Notes:
      /// The value must be ASCII otherwise it will not be used.
      /// If the value contains an extension and it does not match the extension obtained from the document mime type, then filename.oldext.ext is used.
      /// </summary>
      [DataMember(Name = "fileName")]
      public string FileName { get; set; }

      /// <summary>
      /// If true, the final pdf document will be digitally signed.
      /// </summary>
      [DataMember(Name = "signDocument")]
      public bool SignDocument { get; set; }
   }

   [DataContract]
   public class DocumentsHeartbeatRequest : Request
   {
      /// <summary>
      /// IDs of document to signal that they are still alive.
      /// Used by document memory cache
      /// </summary>
      [DataMember(Name = "documentIds")]
      public string[] DocumentIds { get; set; }
   }

   [DataContract]
   public class SaveAttachmentToCacheRequest : Request
   {
      /// <summary>
      /// The ID of the attachment in the cache to download. Cannot be used if URI is used.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The options to identity the attachment and its upload options
      /// </summary>
      [DataMember(Name = "options")]
      public SaveAttachmentToCacheOptions Options { get; set; }
   }

   [DataContract]
   public class SaveAttachmentToCacheResponse : Response
   {
      /// <summary>
      /// The URI of the uploaded document in the cache
      /// </summary>
      [DataMember(Name = "uploadUri")]
      public Uri UploadUri { get; set; }
   }


   [DataContract]
   public class LoadDocumentAttachmentRequest : Request
   {
      /// <summary>
      /// The ID of the owner document
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// Options to use when loading the attachment
      /// </summary>
      [DataMember(Name = "options")]
      public LoadAttachmentOptions Options { get; set; }
   }

   [DataContract]
   public class LoadDocumentAttachmentResponse : Response
   {
      /// <summary>
      /// The serialized attachment LEADDocument instance
      /// </summary>
      [DataMember(Name = "document")]
      public object Document;
   }
}
