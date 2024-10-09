// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************

using Leadtools.Caching;
using Leadtools.Document;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Leadtools.Services.Tools.Cache
{
   /// <summary>
   /// All cache operations are redirected to an implementation of this class
   /// Supports multiple caches
   /// </summary>
   public interface ICacheManager
   {
      /// <summary>
      /// Get the name of this multi-cache manager system
      /// </summary>
      string Name { get; }

      /// <summary>
      /// Return all the cache names supported by this manager
      /// </summary>
      /// <returns></returns>
      string[] GetCacheNames();

      /// <summary>
      /// Checks whether this cache manager is initialized correctly and ready to be used
      /// </summary>
      bool IsInitialized { get; }

      /// <summary>
      /// Initializes the ICacheManager from a configuration file, creating as many ObjectCache implementations as the configurations indicates
      /// This is called from service startup
      /// </summary>
      void Initialize();

      /// <summary>
      /// Cleanup the ICacheManager. Called when
      /// This is called from service shutdown
      /// </summary>
      void Cleanup();

      /// <summary>
      /// Checks that all configured caches are accessible
      /// </summary>
      /// <param name="cacheName">Cache name to check. If NULL, then all caches.</param>
      void CheckCacheAccess(string cacheName);

      /// <summary>
      /// Creates a cache item policy for the specified cache name
      /// </summary>
      /// <param name="cacheName">Cache name to use. Cannot be NULL.</param>
      /// <returns></returns>
      CacheItemPolicy CreatePolicy(string cacheName);

      /// <summary>
      /// Creates a cache item policy for the specified cache
      /// </summary>
      /// <param name="objectCache">Cache to use. Cannot be NULL.</param>
      /// <returns></returns>
      CacheItemPolicy CreatePolicy(ObjectCache objectCache);

      /// <summary>
      /// Gets the cache statistics
      /// </summary>
      /// <param name="cacheName"></param>
      /// <returns></returns>
      CacheStatistics GetCacheStatistics(string cacheName);

      /// <summary>
      /// Cleans the cache items
      /// </summary>
      /// <param name="cacheName"></param>
      void RemoveExpiredItems(string cacheName);

      /// <summary>
      /// Return the default cache.
      /// </summary>
      ObjectCache DefaultCache { get; }

      /// <summary>
      /// Gets a cache by name.
      /// </summary>
      /// <param name="cacheName"></param>
      /// <returns>ObjectCache</returns>
      ObjectCache GetCacheByName(string cacheName);

      /// <summary>
      /// Gets the name of this cache.
      /// </summary>
      /// <param name="objectCache">ObjectCache</param>
      /// <returns></returns>
      string GetCacheName(ObjectCache objectCache);

      /// <summary>
      /// Get the cache where this document is stored.
      /// </summary>
      /// <param name="documentId"></param>
      /// <returns>ObjectCache or null if the document is not found in any of the caches.</returns>
      ObjectCache GetCacheForDocument(string documentId);

      /// <summary>
      /// Get the cache where this document is stored. If not found, return the default cache
      /// </summary>
      /// <param name="documentId"></param>
      /// <returns>ObjectCache or the default cache if the document is not found in any of the caches.</returns>
      ObjectCache GetCacheForDocumentOrDefault(string documentId);

      /// <summary>
      /// Get the cache where this document is stored.
      /// </summary>
      /// <param name="documentUri">leadcache:// document.</param>
      /// <returns>ObjectCache or null if the document is not found in any of the caches.</returns>
      ObjectCache GetCacheForDocument(Uri documentUri);

      /// <summary>
      /// Get the cache where this document is stored.. If not found, return the default cache
      /// </summary>
      /// <param name="documentUri">leadcache:// document.</param>
      /// <returns>ObjectCache or the default cache if the document is not found in any of the caches.</returns>
      ObjectCache GetCacheForDocumentOrDefault(Uri documentUri);

      /// <summary>
      /// Get the cache to store a new document, called by LoadFromUri
      /// </summary>
      /// <param name="uri"></param>
      /// <param name="loadDocumentOptions"></param>
      /// <returns>ObjectCache</returns>
      ObjectCache GetCacheForLoadFromUri(Uri uri, LoadDocumentOptions loadDocumentOptions);

      /// <summary>
      /// Get the cache to store a new document, called by BeginUpload and Convert
      /// </summary>
      /// <param name="uploadDocumentOptions"></param>
      /// <returns>ObjectCache</returns>
      ObjectCache GetCacheForBeginUpload(UploadDocumentOptions uploadDocumentOptions);

      /// <summary>
      /// Get the cache to store new virtual document. Called by Create
      /// </summary>
      /// <param name="createDocumentOptions">Options</param>
      /// <returns>ObjectCache</returns>
      ObjectCache GetCacheForCreate(CreateDocumentOptions createDocumentOptions);
   }
}
