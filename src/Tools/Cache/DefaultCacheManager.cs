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
using System.Xml.Linq;
#if !NET
using IHostEnvironment=Microsoft.AspNetCore.Hosting.IHostingEnvironment;
using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#endif

namespace Leadtools.Services.Tools.Cache
{
   /// <summary>
   /// Default implementation of ICacheManager that has a single CacheObject from the service config
   /// </summary>
   public class DefaultCacheManager : ICacheManager
   {
      // Our cache
      private ObjectCache _objectCache;
      private CacheItemPolicy _cacheItemPolicy;
      private bool _isInitialized = false;
      public string WebRootPath { get; set; }
      
      // Its name
      public const string CACHE_NAME = "DefaultCache";

      public DefaultCacheManager(string WebRootPath, XElement cacheManagerElement)
      {
         if (string.IsNullOrEmpty(WebRootPath))
            throw new ArgumentNullException(nameof(WebRootPath));

         _isInitialized = false;
         this.WebRootPath = WebRootPath;

         // Get the values from the XML document
         ParseXml(cacheManagerElement);
      }

      public void ParseXml(XElement cacheManagerElement)
      {
         // Nothing to parse
      }

      private const string CACHE_MANAGER_NAME = "DefaultCacheManager";
      public string Name
      {
         get { return CACHE_MANAGER_NAME; }
      }

      public string[] GetCacheNames()
      {
         return new string[] { CACHE_NAME };
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

         _objectCache = InitializeDefaultCache(WebRootPath);
         _objectCache.SetName(CACHE_NAME);
         if (_objectCache.Name != CACHE_NAME)
            throw new InvalidOperationException($"ObjectCache implementation {_objectCache.GetType().FullName} does not override SetName");

         _cacheItemPolicy = InitializePolicy(_objectCache);
         _isInitialized = true;
      }

      public void Cleanup()
      {
         if (!_isInitialized)
            return;

         _objectCache = null;
         _cacheItemPolicy = null;
         _isInitialized = false;
      }

      private static ObjectCache InitializeDefaultCache(string WebRootPath)
      {
         // Called by InitializeService the first time the service is run
         // Initialize the global Cache object

         string cacheConfigFile = ServiceHelper.GetSettingValue(ServiceHelper.Key_Cache_ConfigFile);
         cacheConfigFile = ServiceHelper.GetAbsolutePath(cacheConfigFile);
         if (string.IsNullOrEmpty(cacheConfigFile))
            throw new InvalidOperationException($"The cache configuration file location in '{ServiceHelper.Key_Cache_ConfigFile}' in the configuration file is empty");

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

      public static CacheItemPolicy InitializePolicy(ObjectCache objectCache)
      {
         // If the FileCache has a policy set by the configuration, then use it.
         // Otherwise, check for lt.Cache.SlidingExpiration in the configuration file

         CacheItemPolicy policy = null;

         // If we have it in the cache, use it
         if (objectCache is FileCache)
         {
            FileCache fileCache = objectCache as FileCache;
            if (fileCache.DefaultPolicy != null && !fileCache.DefaultPolicy.IsInfinite)
               policy = fileCache.DefaultPolicy.Clone();
         }

         if (policy == null)
         {
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
               FileCache fileCache = objectCache as FileCache;
               // Use the default policy
               if (fileCache != null && fileCache.DefaultPolicy != null)
                  policy = fileCache.DefaultPolicy.Clone();
               else
                  policy = new CacheItemPolicy();
            }
         }

         return policy;
      }

      private static void VerifyCacheName(string cacheName, bool allowNull)
      {
         if (cacheName == null && allowNull)
            return;

         if (CACHE_NAME != cacheName)
            throw new ArgumentException($"Invalid cache name: {cacheName}", nameof(cacheName));
      }

      public void CheckCacheAccess(string cacheName)
      {
         VerifyCacheName(cacheName, true);

         // Check if the cache directory setup by the user in the config file is valid and accessible

         // Do this by loading up the cache, adding a region, an item and deleting it
         // This mimics what the document library will do
         if (_objectCache == null)
            throw new InvalidOperationException("Cache has not been setup");

         // Try to add/remove an item. This check can be performed with all caches but is extra important with
         // ObjectCache since forgetting to setup the correct read/write access on the cache directory for instance
         // is a common issue when setting up the service
         string regionName = Guid.NewGuid().ToString("N");
         CacheItemPolicy policy = CreatePolicy(CACHE_NAME);
         string key = "key";
         _objectCache.Add(key, "data", policy, regionName);
         // Verify
         string data = _objectCache.Get(key, regionName) as string;
         if (data == null || string.CompareOrdinal(data, "data") != 0)
            throw new InvalidOperationException("Could not read cache item");

         // Delete
         _objectCache.DeleteItem(key, regionName);
      }

      public CacheItemPolicy CreatePolicy(string cacheName)
      {
         VerifyCacheName(cacheName, false);

         return _cacheItemPolicy.Clone();
      }

      public CacheItemPolicy CreatePolicy(ObjectCache objectCache)
      {
         // Get the name of this cache
         string cacheName = GetCacheName(objectCache);
         if (cacheName == null)
            throw new InvalidOperationException("Invalid object cache");

         return CreatePolicy(cacheName);
      }

      public CacheStatistics GetCacheStatistics(string cacheName)
      {
         VerifyCacheName(cacheName, true);

         CacheStatistics cacheStatistics;

         // Only supported by FileCache
         FileCache fileCache = _objectCache as FileCache;
         if (fileCache != null)
         {
            cacheStatistics = _objectCache.GetStatistics();
         }
         else
         {
            cacheStatistics = new CacheStatistics();
         }

         return cacheStatistics;
      }

      public void RemoveExpiredItems(string cacheName)
      {
         VerifyCacheName(cacheName, true);

         // Only supported by FileCache
         FileCache fileCache = _objectCache as FileCache;
         if (fileCache != null)
         {
            fileCache.CheckPolicies();
         }
      }

      public ObjectCache DefaultCache
      {
         get { return _objectCache; }
      }

      public ObjectCache GetCacheByName(string cacheName)
      {
         VerifyCacheName(cacheName, false);
         return _objectCache;
      }

      public string GetCacheName(ObjectCache objectCache)
      {
         if (objectCache == null)
            throw new ArgumentNullException(nameof(objectCache));

         if (objectCache == _objectCache)
            return CACHE_NAME;

         return null;
      }

      public ObjectCache GetCacheForDocument(string documentId)
      {
         if (documentId == null)
            throw new ArgumentNullException(nameof(documentId));

         return _objectCache;
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
         if (!DocumentFactory.IsUploadDocumentUri(documentUri))
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
         return _objectCache;
      }

      public ObjectCache GetCacheForBeginUpload(UploadDocumentOptions uploadDocumentOptions)
      {
         return _objectCache;
      }

      public ObjectCache GetCacheForCreate(CreateDocumentOptions createDocumentOptions)
      {
         return _objectCache;
      }
   }
}
