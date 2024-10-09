// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using Leadtools.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Leadtools.Services.Models.PreCache
{
   [DataContract]
   public class PreCacheDocumentRequest : Request
   {
      /// <summary>
      /// The URI to the document to be pre-cached.
      /// </summary>
      [DataMember(Name = "uri")]
      public Uri Uri { get; set; }

      /// <summary>
      /// The date this document will be expired. If null, then the document will never expire.
      /// </summary>
      [DataMember(Name = "expiryDate")]
      public DateTime? ExpiryDate { get; set; }

      /// <summary>
      /// The parts of the document to pre-cache. If None (default value), then DocumentCacheOptions.All is used.
      /// Array of serialized Leadtools.Document.DocumentCacheOptions.
      /// </summary>
      [DataMember(Name = "cacheOptions")]
      public DocumentCacheOptions CacheOptions { get; set; }

      /// <summary>
      /// The maximum pixel size values to use when pre-caching images. If null, then 4096 and 2048 are used.
      /// </summary>
      [DataMember(Name = "maximumImagePixelSizes")]
      public int[] MaximumImagePixelSizes { get; set; }

      /// <summary>
      /// Page number where pre-caching starts. 0 to start at the first page
      /// </summary>
      [DataMember(Name = "firstPageNumber")]
      public int FirstPageNumber { get; set; }

      /// <summary>
      /// Page number where pre-caching stops. 0 or -1 to stop at the last page
      /// </summary>
      [DataMember(Name = "lastPageNumber")]
      public int LastPageNumber { get; set; }

      /// <summary>
      /// A simple passcode to restrict others from pre-caching. For production, use a more advanced authorization system.
      /// </summary>
      [DataMember(Name = "passcode")]
      public string Passcode { get; set; }
   }

   [DataContract]
   public class PreCacheDocumentResponse : Response
   {
      /// <summary>
      /// The pre-cache response item.
      /// </summary>
      [DataMember(Name = "item")]
      public PreCacheResponseItem Item { get; set; }
   }

   [DataContract]
   public class ReportPreCacheRequest : Request
   {
      /// <summary>
      /// Choose whether to delete all pre-cache entries.
      /// </summary>
      [DataMember(Name = "clear")]
      public bool Clear { get; set; }

      /// <summary>
      /// Choose whether to remove pre-cache entries that don't have a matching cache entry.
      /// </summary>
      [DataMember(Name = "clean")]
      public bool Clean { get; set; }

      /// <summary>
      /// A simple passcode to restrict others from receiving a pre-cache report. For production, use a more advanced authorization system.
      /// </summary>
      [DataMember(Name = "passcode")]
      public string Passcode { get; set; }
   }

   [DataContract]
   public class ReportPreCacheResponse : Response
   {
      /// <summary>
      /// An array of the pre-cache entries stored in the pre-cache.
      /// </summary>
      [DataMember(Name = "entries")]
      public PreCacheResponseItem[] Entries { get; set; }

      /// <summary>
      /// An array of the pre-cache document items that were removed.
      /// </summary>
      [DataMember(Name = "removed")]
      public PreCacheResponseItem[] Removed { get; set; }
   }

   [DataContract]
   public class PreCacheResponseItem
   {
      /// <summary>
      /// The value of the URI for the pre-cache document, as a string.
      /// </summary>
      [DataMember(Name = "uri")]
      public string Uri { get; set; }

      /// <summary>
      /// The pre-cached items.
      /// </summary>
      [DataMember(Name = "items")]
      public PreCacheResponseSizeItem[] Items { get; set; }

      /// <summary>
      /// The mapped hashkey of the pre-cache entry from the request URI.
      /// </summary>
      [DataMember(Name = "hashKey")]
      public string RegionHash { get; set; }
   }

   [DataContract]
   public class PreCacheResponseSizeItem
   {
      /// <summary>
      /// The mapped documentId of the cached document.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The value of MaximumImagePixelSize
      /// </summary>
      [DataMember(Name = "maximumImagePixelSize")]
      public int MaximumImagePixelSize { get; set; }

      /// <summary>
      /// The time it took to pre-cache the document, if relevant to the operation.
      /// Else it will be zero.
      /// </summary>
      [DataMember(Name = "seconds")]
      public double Seconds { get; set; }

      /// <summary>
      /// The times this item has been accessed, if relevant to the operation.
      /// Else it will be zero.
      /// </summary>
      [DataMember(Name = "reads")]
      public int Reads { get; set; }
   }

   [Serializable]
   [DataContract]
   public class PreCacheEntry
   {
      /// <summary>
      ///  The stored identifier for the document.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The maximum pixel size of pages for this cached document instance.
      /// </summary>
      [DataMember(Name = "maximumImagePixelSize")]
      public int MaximumImagePixelSize { get; set; }

      /// <summary>
      /// The number of times this cache item has been read with the intention of loading.
      /// </summary>
      [DataMember(Name = "reads")]
      public int Reads { get; set; }
   }
}
