// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************

using Leadtools.Caching;
using Leadtools.Document;
using Leadtools.Services.Tools.Helpers;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml.Linq;
#if !NET
using IHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
using Microsoft.Extensions.Hosting;
#endif

namespace Leadtools.Services.Tools.Cache
{
   /// <summary>
   /// An example of a Hybrid Memory/File cache manager
   /// </summary>
   /// <remarks>
   /// <para>Documents with size larger than MemoryCacheMaximumDataLength will go into the SerializableMemoryCache.</para>
   /// <para>Documents with size smaller than MemoryCacheMaximumDataLength will go into FileCache</para>
   /// </remarks>
   public class HybridMemoryFileCacheManager : ICacheManager
   {
      // Our caches
      private IDictionary<string, ObjectCache> _caches;
      private IDictionary<string, CacheItemPolicy> _cacheItemPolicies;

      private bool _isInitialized = false;
      private System.Threading.Timer _fileCacheRemoveExpiredItemsTimer;
      public string WebRootPath { get; set; }

      // Its name
      public const string FILE_CACHE_NAME = "FileCache";
      public const string MEMORY_CACHE_NAME = "MemoryCache";
      public const string DEFAULT_CACHE_NAME = FILE_CACHE_NAME;

      // Allow the user to pass the cache name to upload a document to in UploadDocumentOptions
      // Set this as the key and the cache name in UploadDocumentOptions.PostUploadOperations
      public const string UPLOAD_CACHE_NAME_KEY = "CACHE_NAME";

      // FileCache configuration file
      public string FileCacheConfigurationFile = null;

      // File Cache cleanup time interval. Value of 0 means no timer
      public long FileCacheCleanupIntervalSeconds { get; set; } = 1 * 60 * 60; // Every 1 hour

      // Maximum document data length in bytes for Memory cache. Greater than will go into File cache. Value of 0 means put everything in memory cache
      public long MemoryCacheMaximumDataLength { get; set; } = 1 * 1024 * 1024; // 1MB

      // Expiry policy for the memory cache
      public long MemoryCacheSlidingExpiryPolicySeconds { get; set; } = 1 * 60 * 60; // Expiry after 1 hour

      public HybridMemoryFileCacheManager(string WebRootPath, XElement cacheManagerElement)
      {
         if (string.IsNullOrEmpty(WebRootPath))
            throw new ArgumentNullException(nameof(WebRootPath));

         _isInitialized = false;
         this.WebRootPath = WebRootPath;

         // No caches yet
         _caches = new Dictionary<string, ObjectCache>();
         _cacheItemPolicies = new Dictionary<string, CacheItemPolicy>();

         _caches[FILE_CACHE_NAME] = null;
         _cacheItemPolicies[FILE_CACHE_NAME] = null;

         _caches[MEMORY_CACHE_NAME] = null;
         _cacheItemPolicies[MEMORY_CACHE_NAME] = null;

         // Get the values from the XML document
         ParseXml(cacheManagerElement);
      }

      private void ParseXml(XElement cacheManagerElement)
      {
         if (cacheManagerElement == null)
            return;

         // Get all values/value in the config files
         IDictionary<string, string> items = new Dictionary<string, string>();

         IEnumerable<XElement> valueElements = from valuesElement in cacheManagerElement.Descendants("values")
                                               from valueElement in valuesElement.Descendants("value")
                                               select valueElement;
         foreach (XElement valueElement in valueElements)
         {
            XAttribute keyAttr = valueElement.Attribute("key");
            XAttribute valueAttr = valueElement.Attribute("value");
            if (keyAttr != null && valueAttr != null)
            {
               string key = keyAttr.Value;
               string value = valueAttr.Value;
               if (key != null)
                  items.Add(key, value);
            }
         }

         // Parse what we know
         foreach (KeyValuePair<string, string> entry in items)
         {
            string value = entry.Value;

            switch (entry.Key)
            {
               case "file_cache_configuration_file":
                  this.FileCacheConfigurationFile = value;
                  break;

               case "file_cache_cleanup_interval_seconds":
                  this.FileCacheCleanupIntervalSeconds = long.Parse(value);
                  break;

               case "memory_cache_maximum_data_length":
                  this.MemoryCacheMaximumDataLength = long.Parse(value);
                  break;

               case "memory_cache_sliding_expiry_policy_seconds":
                  this.MemoryCacheSlidingExpiryPolicySeconds = long.Parse(value);
                  break;

               default:
                  break;
            }
         }
      }

      private const string CACHE_MANAGER_NAME = "HybridMemoryFileCacheManager";
      public string Name
      {
         get { return CACHE_MANAGER_NAME; }
      }

      public string[] GetCacheNames()
      {
         return new string[] { FILE_CACHE_NAME, MEMORY_CACHE_NAME };
      }

      public bool IsInitialized
      {
         get { return _isInitialized; }
      }

      public void Initialize()
      {
         Trace.WriteLine("Initializing default cache from configuration");

         if (_isInitialized)
            throw new InvalidOperationException("Cache already initialized");

         // Initialize FileCache
         ObjectCache fileCache = InitializeFileCache();
         fileCache.SetName(FILE_CACHE_NAME);
         if (fileCache.Name != FILE_CACHE_NAME)
            throw new InvalidOperationException($"ObjectCache implementation {fileCache.GetType().FullName} does not override SetName");

         _caches[FILE_CACHE_NAME] = fileCache;
         CacheItemPolicy fileCacheItemPolicy = InitializeFileCachePolicy(fileCache);
         _cacheItemPolicies[FILE_CACHE_NAME] = fileCacheItemPolicy;

         // Initialize MemoryCache
         ObjectCache memoryCache = InitializeMemoryCache();
         memoryCache.SetName(MEMORY_CACHE_NAME);
         if (memoryCache.Name != MEMORY_CACHE_NAME)
            throw new InvalidOperationException($"ObjectCache implementation {memoryCache.GetType().FullName} does not override SetName");

         _caches[MEMORY_CACHE_NAME] = memoryCache;
         CacheItemPolicy memoryCacheItemPolicy = InitializeMemoryCachePolicy(this.MemoryCacheSlidingExpiryPolicySeconds);
         _cacheItemPolicies[MEMORY_CACHE_NAME] = memoryCacheItemPolicy;

         // For loading virtual documents support
         DocumentFactory.LoadDocumentFromCache += LoadDocumentFromCacheHandler;

         // Create a timer for file cache clean up

         if (FileCacheCleanupIntervalSeconds > 0)
         {
            long timerIntervalMilliseconds = this.FileCacheCleanupIntervalSeconds * 1000;
            _fileCacheRemoveExpiredItemsTimer = new System.Threading.Timer(FileCacheRemoveExpiredItems, null, 0L, timerIntervalMilliseconds);
         }

         _isInitialized = true;
      }

      public void Cleanup()
      {
         if (!_isInitialized)
            return;

         if (_fileCacheRemoveExpiredItemsTimer != null)
         {
            _fileCacheRemoveExpiredItemsTimer.Dispose();
            _fileCacheRemoveExpiredItemsTimer = null;
         }

         _caches[FILE_CACHE_NAME] = null;
         _cacheItemPolicies[FILE_CACHE_NAME] = null;
         _caches[MEMORY_CACHE_NAME] = null;
         _cacheItemPolicies[MEMORY_CACHE_NAME] = null;
         DocumentFactory.LoadDocumentFromCache -= LoadDocumentFromCacheHandler;
         _isInitialized = false;
      }

      private void LoadDocumentFromCacheHandler(object sender, ResolveDocumentEventArgs e)
      {
         // Get the cache for the document if we have it
         ObjectCache objectCache = GetCacheForDocument(e.LoadFromCacheOptions.DocumentId);
         if (objectCache != null)
            e.LoadFromCacheOptions.Cache = objectCache;
      }

      private void FileCacheRemoveExpiredItems(object stateInfo)
      {
         this.RemoveExpiredItems(null);
      }

      private ObjectCache InitializeFileCache()
      {
         // Initialize the FileCache object from file cache configuration file.
         if (string.IsNullOrEmpty(FileCacheConfigurationFile))
            throw new InvalidOperationException($"file_cache_configuration_file value must set in the XML configuration file");

         string cacheConfigFile = ServiceHelper.GetAbsolutePath(FileCacheConfigurationFile);

         ObjectCache objectCache = null;

         // Set the base directory of the cache (for resolving any relative paths) to this project's path
         var additional = new Dictionary<string, string>();
         additional.Add(ObjectCache.BASE_DIRECTORY_KEY, WebRootPath);

         try
         {
            using (var cacheConfigStream = File.OpenRead(cacheConfigFile))
               objectCache = ObjectCache.CreateFromConfigurations(cacheConfigStream, additional);
         }
         catch (Exception ex)
         {
            throw new InvalidOperationException($"Cannot load cache configuration from '{cacheConfigFile}'", ex);
         }

         return objectCache;
      }

      public static CacheItemPolicy InitializeFileCachePolicy(ObjectCache objectCache)
      {
         // If the FileCache has a policy set by the configuration, then use it.
         // Otherwise, check for lt.Cache.SlidingExpiration in the configuration file

         CacheItemPolicy policy;
         FileCache fileCache = objectCache as FileCache;

         // If we have it in the cache, use it
         policy = fileCache.DefaultPolicy;
         if (policy != null && !policy.IsInfinite)
            return policy.Clone();

         // If we have it in the service configuration, use it
         TimeSpan slidingExpiration;
         var value = ServiceHelper.GetSettingValue(ServiceHelper.Key_Cache_SlidingExpiration);
         if (value != null)
            value = value.Trim();

         if (!string.IsNullOrEmpty(value) && TimeSpan.TryParse(value, out slidingExpiration))
         {
            // Its in the configuration, use it
            policy = new CacheItemPolicy();
            policy.SlidingExpiration = slidingExpiration;
         }
         else
         {
            // Use the default policy
            if (fileCache.DefaultPolicy != null)
               policy = fileCache.DefaultPolicy.Clone();
            else
               policy = new CacheItemPolicy();
         }

         return policy;
      }

      private static ObjectCache InitializeMemoryCache()
      {
         ObjectCache objectCache = new SerializableMemoryCache();
         return objectCache;
      }

      public static CacheItemPolicy InitializeMemoryCachePolicy(long memoryCacheSlidingExpiryPolicySeconds)
      {
         var policy = new CacheItemPolicy();
         policy.SlidingExpiration = TimeSpan.FromSeconds(memoryCacheSlidingExpiryPolicySeconds);
         return policy;
      }

      private void VerifyCacheName(string cacheName, bool allowNull)
      {
         if (cacheName == null && allowNull)
            return;

         if (!_caches.ContainsKey(cacheName))
            throw new ArgumentException($"Invalid cache name: {cacheName}", nameof(cacheName));
      }

      public void CheckCacheAccess(string cacheName)
      {
         VerifyCacheName(cacheName, true);

         foreach (KeyValuePair<string, ObjectCache> entry in _caches)
         {
            if (cacheName == null || cacheName == entry.Key)
            {
               CacheItemPolicy policy = CreatePolicy(entry.Key);
               VerifyCacheAccess(entry.Value, policy);
            }
         }
      }

      private static void VerifyCacheAccess(ObjectCache objectCache, CacheItemPolicy policy)
      {
         // Check if the cache options setup by the user in the config file is valid and accessible

         // Do this by loading up the cache, adding a region, an item and deleting it
         // This mimics what the document library will do
         if (objectCache == null)
            throw new InvalidOperationException("Cache has not been setup");

         // If this is the default FileCache then try to add/remove an item
         // This check can be performed with all caches but is extra important with
         // FileCache since forgetting to setup the correct read/write access
         // on the cache directory is a common issue when setting up the service
         string regionName = Guid.NewGuid().ToString("N");
         string key = "key";
         objectCache.Add(key, "data", policy, regionName);
         // Verify
         string data = objectCache.Get(key, regionName) as string;
         if (data == null || string.CompareOrdinal(data, "data") != 0)
            throw new InvalidOperationException("Could not read cache item");

         // Delete
         objectCache.DeleteItem(key, regionName);
      }

      public CacheItemPolicy CreatePolicy(string cacheName)
      {
         VerifyCacheName(cacheName, false);

         CacheItemPolicy policy = _cacheItemPolicies[cacheName].Clone();
         return policy;
      }

      public CacheItemPolicy CreatePolicy(ObjectCache objectCache)
      {
         // Get the name of this cache
         string cacheName = GetCacheName(objectCache);
         if (cacheName == null)
            throw new ArgumentException("Invalid object cache");

         return CreatePolicy(cacheName);
      }

      public CacheStatistics GetCacheStatistics(string cacheName)
      {
         CacheStatistics cacheStatistics = new CacheStatistics();

         foreach (KeyValuePair<string, ObjectCache> entry in _caches)
         {
            if (cacheName == null || cacheName == entry.Key)
            {
               // Only FileCache supports this
               FileCache fileCache = entry.Value as FileCache;
               if (fileCache != null)
               {
                  CacheStatistics thisCacheStatistics = fileCache.GetStatistics();
                  if (thisCacheStatistics != null)
                  {
                     cacheStatistics.Regions += thisCacheStatistics.Regions;
                     cacheStatistics.Items += thisCacheStatistics.Items;
                     cacheStatistics.ExpiredItems += thisCacheStatistics.ExpiredItems;
                  }
               }
            }
         }

         return cacheStatistics;
      }

      public void RemoveExpiredItems(string cacheName)
      {
         foreach (KeyValuePair<string, ObjectCache> entry in _caches)
         {
            if (cacheName == null || cacheName == entry.Key)
            {
               // Only FileCache supports this
               FileCache fileCache = entry.Value as FileCache;
               if (fileCache != null)
               {
                  fileCache.CheckPolicies();
               }
            }
         }
      }

      public ObjectCache DefaultCache
      {
         get { return _caches[DEFAULT_CACHE_NAME]; }
      }

      public ObjectCache GetCacheByName(string cacheName)
      {
         VerifyCacheName(cacheName, false);
         return _caches[cacheName];
      }

      public string GetCacheName(ObjectCache objectCache)
      {
         if (objectCache == null)
            throw new ArgumentNullException(nameof(objectCache));

         foreach (KeyValuePair<string, ObjectCache> entry in _caches)
         {
            if (objectCache == entry.Value)
               return entry.Key;
         }

         return null;
      }

      public ObjectCache GetCacheForDocument(string documentId)
      {
         // Find the cache by checking if the data exists
         foreach (ObjectCache objectCache in _caches.Values)
         {
            if (DocumentFactory.IsDocumentInCache(objectCache, documentId))
            {
               return objectCache;
            }
         }

         // Not found
         return null;
      }

      public ObjectCache GetCacheForDocumentOrDefault(string documentId)
      {
         ObjectCache objectCache = null;

         if (!string.IsNullOrEmpty(documentId))
         {
            objectCache = GetCacheForDocument(documentId);
         }

         if (objectCache == null)
            objectCache = DefaultCache;

         return objectCache;
      }

      public ObjectCache GetCacheForDocument(Uri documentUri)
      {
         if (documentUri == null)
            throw new ArgumentNullException(nameof(documentUri));

         // Get the document ID from the URI and call the other version of this function
         if (!DocumentFactory.IsLeadCacheScheme(documentUri.ToString()))
            throw new ArgumentException($"{documentUri.ToString()} is not a valid LEAD document URI", nameof(documentUri));

         string documentId = DocumentFactory.GetLeadCacheData(documentUri);
         return GetCacheForDocument(documentId);
      }

      public ObjectCache GetCacheForDocumentOrDefault(Uri documentUri)
      {
         ObjectCache objectCache = null;

         if (documentUri != null)
         {
            objectCache = GetCacheForDocument(documentUri);
         }

         if (objectCache == null)
            objectCache = DefaultCache;

         return objectCache;
      }

      public ObjectCache GetCacheForLoadFromUri(Uri uri, LoadDocumentOptions loadDocumentOptions)
      {
         if (uri == null)
            throw new ArgumentNullException(nameof(uri));

         // If this the URI to a LEAD document in the cache, use that instead because this is not an operation
         // that will create a new document in the cache
         if (DocumentFactory.IsUploadDocumentUri(uri))
         {
            return GetCacheForDocument(uri);
         }

         // Find out the document size, get it from the URI
         long dataLength = GetContentLength(uri);
         // Check based on the size
         ObjectCache objectCache = GetCacheForDataLength(dataLength);
         return objectCache;
      }

      private static long GetContentLength(Uri uri)
      {
         try
         {
#pragma warning disable SYSLIB0014 // Type or member is obsolete
            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(uri);
#pragma warning restore SYSLIB0014 // Type or member is obsolete
            webRequest.Method = "HEAD";
            long dataLength;
            using (HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse())
            {
               dataLength = response.ContentLength;
            }

            return dataLength;
         }
         catch
         {
            // Swallow any exceptions
            return 0;
         }
      }

      public ObjectCache GetCacheForBeginUpload(UploadDocumentOptions uploadDocumentOptions)
      {
         if (uploadDocumentOptions == null)
            throw new ArgumentNullException(nameof(uploadDocumentOptions));

         ObjectCache objectCache = null;

         // See if the user specified a cache by name
         IDictionary<string, string> postOperations = uploadDocumentOptions.PostUploadOperations;
         if (postOperations != null && postOperations.ContainsKey(UPLOAD_CACHE_NAME_KEY))
         {
            string cacheName = postOperations[UPLOAD_CACHE_NAME_KEY];
            if (!string.IsNullOrEmpty(cacheName))
            {
               objectCache = GetCacheByName(cacheName);
            }
         }

         if (objectCache == null)
         {
            // Check based on the size
            objectCache = GetCacheForDataLength(uploadDocumentOptions.DocumentDataLength);
         }

         // If this is the file cache, disable streaming since it is not needed and will use more
         // memory than it is required
         if (uploadDocumentOptions.EnableStreaming && objectCache is FileCache) {
            uploadDocumentOptions.EnableStreaming = false;
         }

         return objectCache;
      }

      public ObjectCache GetCacheForCreate(CreateDocumentOptions createDocumentOptions)
      {
         if (createDocumentOptions == null)
            throw new ArgumentNullException(nameof(createDocumentOptions));

         // Store in memory cache
         return GetCacheByName(MEMORY_CACHE_NAME);
      }

      private ObjectCache GetCacheForDataLength(long dataLength)
      {
         ObjectCache objectCache;

         if (MemoryCacheMaximumDataLength > 0 && dataLength > MemoryCacheMaximumDataLength)
            objectCache = _caches[FILE_CACHE_NAME];
         else
            objectCache = _caches[MEMORY_CACHE_NAME];

         return objectCache;
      }
   }
}
