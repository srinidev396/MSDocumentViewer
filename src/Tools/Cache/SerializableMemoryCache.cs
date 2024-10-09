// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************

using Leadtools.Caching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Leadtools.Services.Tools.Cache
{
   // Sample implementation of ObjectCache that stores the items in a in-memory concurrent dictionary
   // Supports serialization
   // This cache does not support eviction and will be cleared when the application is restarted
   // Add implementation to save the cache dictionaries to disk in between sessions
   public class SerializableMemoryCache : ObjectCache
   {
      // Options

      // Can be used to turn region support on and off
      public bool IsRegionsSupported { get; set; } = true;
      // If this value is set, then we have support for external resources (meaning, we can save cache items to disk)
      public string ResourcesDirectory { get; set; }

      // The cache backend
      // Each region is a dictionary of keys and values
      // The cache is a dictionary of regions (document IDs)
      private ConcurrentDictionary<string, ConcurrentDictionary<string, byte[]>> _cache = new ConcurrentDictionary<string, ConcurrentDictionary<string, byte[]>>();

      [Conditional("LOG")]
      public static void Log(string method, string key, string regionName, bool value)
      {
         Trace.WriteLine(string.Format("{0} {1}.{2} {3}", method, key, regionName, value), "MyMemoryCache");
      }

      // Abstract methods/properties that must be implemented:

      // The cache name
      private string _name = "SerializableMemoryCache";
      public override string Name
      {
         get { return _name; }
      }

      public override void SetName(string value)
      {
         _name = value;
      }

      // We only support binary serialization
      public override CacheSerializationMode DataSerializationMode
      {
         get { return CacheSerializationMode.Binary; }
         set { throw new NotSupportedException(); }
      }

      public override CacheSerializationMode PolicySerializationMode
      {
         get { return CacheSerializationMode.Binary; }
         set { throw new NotSupportedException(); }
      }

      // Return the cache capabilities we have. We can support regions and external resources depending on the options used to create us
      public override DefaultCacheCapabilities DefaultCacheCapabilities
      {
         get
         {
            var caps = DefaultCacheCapabilities.None;
            caps |= DefaultCacheCapabilities.Serialization;
            if (IsRegionsSupported)
               caps |= DefaultCacheCapabilities.CacheRegions;
            if (ResourcesDirectory != null)
               caps |= DefaultCacheCapabilities.ExternalResources;
            return caps;
         }
      }

      //
      // The following methods and properties must be implemented
      //

      // Check if the cache contains the specified item
      public override bool Contains(string key, string regionName)
      {
         bool ret = false;

         if (_cache.ContainsKey(regionName))
         {
            var regionCache = _cache[regionName];
            if (regionCache.ContainsKey(key))
               ret = true;
         }

         Log("Contains", key, regionName, ret);

         return ret;
      }

      // Add a new item to the cache. If an old item exists, return it
      public override CacheItem<T> AddOrGetExisting<T>(CacheItem<T> item, CacheItemPolicy policy)
      {
         if (item == null)
            throw new ArgumentNullException("item");

         CacheItem<T> oldItem = null;

         if (_cache.ContainsKey(item.RegionName))
         {
            var regionCache = _cache[item.RegionName];
            if (regionCache.ContainsKey(item.Key))
            {
               try
               {
                  var oldPayload = ReadPayload<T>(item.Key, item.RegionName);
                  oldItem = new CacheItem<T>(item.Key, oldPayload, item.RegionName);
               }
               catch (Exception)
               {
                  oldItem = null;
               }
            }
         }

         // Save new data
         WritePayload(item.Key, item.Value, item.RegionName);

         Log("AddOrGetExisting", item.Key, item.RegionName, oldItem != null);

         // Return old item
         return oldItem;
      }

      // Get the cache item, or null if it does not exist
      public override CacheItem<T> GetCacheItem<T>(string key, string regionName)
      {
         CacheItem<T> item = null;
         var payload = ReadPayload<T>(key, regionName);
         if (payload != null)
         {
            item = new CacheItem<T>(key);
            item.Value = payload;
            item.RegionName = regionName;
         }

         Log("GetCacheItem", key, regionName, item != null);

         return item;
      }

      // Update the item with new value (if exists). Return status
      public override bool UpdateCacheItem<T>(CacheItem<T> item)
      {
         if (item == null)
            throw new ArgumentNullException("item");

         var payload = ReadPayload<T>(item.Key, item.RegionName);
         if (payload != null)
         {
            WritePayload(item.Key, item.Value, item.RegionName);
         }

         Log("UpdateCacheItem", item.Key, item.RegionName, payload != null);

         return payload != null;
      }

      // Update the policy of an item
      public override void UpdatePolicy(string key, CacheItemPolicy policy, string regionName)
      {
         // We do not support per item policies, so nothing to do
         Log("UpdatePolicy", key, regionName, this.Contains(key, regionName));
      }

      // Remove the cache item. If exists, return the existing value
      public override T Remove<T>(string key, string regionName)
      {
         // Per MS design, we need to return the object
         T value = default(T);

         // Check if we have it
         var exists = Contains(key, regionName);
         if (exists)
         {
            // Get it
            value = Get<T>(key, regionName);

            DoDeleteItem(key, regionName);
         }

         Log("Remove", key, regionName, exists);

         return value;
      }

      // Delete the item if exists
      public override void DeleteItem(string key, string regionName)
      {
         // Check if we have it
         var exists = Contains(key, regionName);
         if (exists)
            DoDeleteItem(key, regionName);

         Log("DeleteItem", key, regionName, exists);
      }

      // Delete a whole region
      public override void DeleteRegion(string regionName)
      {
         // This is called when a document is deleted from the cache if we specified DefaultCacheCapabilities.CacheRegions
         // Otherwise, DeleteAll will be called
         if (string.IsNullOrEmpty(regionName))
            return;

         var exists = _cache.ContainsKey(regionName);
         if (exists)
         {
            var regionCache = _cache[regionName];

            foreach (var value in regionCache)
               DoDeleteItem(value.Key, regionName);

            ConcurrentDictionary<string, byte[]> values;
            _cache.TryRemove(regionName, out values);
         }

         Log("DeleteRegion", string.Empty, regionName, exists);
      }

      // Try adding new external resources
      public override Uri BeginAddExternalResource(string key, string regionName, bool readWrite)
      {
         // This is called when if DefaultCacheCapabilities.ExternalResources is specified. Otherwise, the data is stored
         // in the items directly

         Uri uri = GetItemExternalResource(key, regionName, readWrite);
         Log("BeginAddExternalResources", key, regionName, uri != null);
         return uri;
      }

      // Finish adding new external resources
      public override void EndAddExternalResource<T>(bool commit, string key, T value, CacheItemPolicy policy, string regionName)
      {
         // This is called when if DefaultCacheCapabilities.ExternalResources is specified. Otherwise, the data is stored
         // in the items directly

         // Save the policy only
         if (commit)
         {
            var item = new CacheItem<T>(key, value, regionName);
            Add<T>(item, policy);
         }
         else
         {
            RemoveItemExternalResource(key, regionName);
            Remove(key, regionName);
         }

         Log("BeginAddExternalResources", key, regionName, commit);
      }

      // Get the external resource associated with an item
      public override Uri GetItemExternalResource(string key, string regionName, bool readWrite)
      {
         // This is called when if DefaultCacheCapabilities.ExternalResources is specified

         if ((this.DefaultCacheCapabilities & DefaultCacheCapabilities.ExternalResources) != DefaultCacheCapabilities.ExternalResources)
            throw new InvalidOperationException("This cache does not support external resources");

         var filePath = GetKeyFileName(key, regionName, true);
         var uri = new Uri(filePath);

         Log("GetItemExternalResource", key, regionName, uri != null);
         return uri;
      }

      // Remove the external resource associated with an item
      public override void RemoveItemExternalResource(string key, string regionName)
      {
         // This is called when if DefaultCacheCapabilities.ExternalResources is specified

         if ((this.DefaultCacheCapabilities & DefaultCacheCapabilities.ExternalResources) != DefaultCacheCapabilities.ExternalResources)
            throw new InvalidOperationException("This cache does not support external resources");

         var filePath = GetKeyFileName(key, regionName, false);
         var exists = File.Exists(filePath);
         if (exists)
            File.Delete(filePath);

         Log("RemoveItemExternalResource", key, regionName, exists);
      }

      //
      // The following methods and properties must be implemented but are never called from the Documents service
      //

      // Default regions support
      public override object this[string key]
      {
         // This is never called by LEADTOOLS Documents service.
         get
         {
            throw new NotSupportedException();
         }
         set
         {
            throw new NotSupportedException();
         }
      }

      // Enumerate the cache keys
      public override void EnumerateKeys(string region, EnumerateCacheEntriesCallback callback)
      {
         // This is never called by the Documents toolkit.
         throw new NotSupportedException();
      }

      // Enumerate the cache regions
      public override void EnumerateRegions(EnumerateCacheEntriesCallback callback)
      {
         // This is never called by the Documents toolkit.
         throw new NotSupportedException();
      }

      // Get the number of items in the region
      public override long GetCount(string regionName)
      {
         // This is never called by the Documents toolkit.
         throw new NotSupportedException();
      }

      // Get HTTP URL access to the data in a cache URL
      public override Uri GetItemVirtualDirectoryUrl(string key, string regionName)
      {
         // Only called if DefaultCacheCapabilities.VirtualDirectory is specified, which we do not
         throw new NotSupportedException();
      }

      // Get statistics about the cache
      public override CacheStatistics GetStatistics()
      {
         // This is never called by the Documents toolkit.
         throw new NotSupportedException();
      }

      // Get statistics about a cache region
      public override CacheStatistics GetStatistics(string key, string regionName)
      {
         // This is never called by the Documents toolkit.
         throw new NotSupportedException();
      }

      // Get all the values from the cache
      public override IDictionary<string, object> GetValues(IEnumerable<string> keys, string regionName)
      {
         // This is never called by the Documents toolkit.
         throw new NotSupportedException();
      }

      // Get an enumerator to the cache item
      protected override IEnumerator<KeyValuePair<string, object>> GetEnumerator()
      {
         // This is never called by the Documents toolkit.
         throw new NotSupportedException();
      }

      //
      // Implementation
      //

      private void DoDeleteItem(string key, string regionName)
      {
         // Delete the item
         if (_cache.ContainsKey(regionName))
         {
            var regionCache = _cache[regionName];
            if (regionCache.ContainsKey(key))
            {
               byte[] value;
               regionCache.TryRemove(key, out value);

               if ((this.DefaultCacheCapabilities & DefaultCacheCapabilities.ExternalResources) == DefaultCacheCapabilities.ExternalResources)
               {
                  RemoveItemExternalResource(key, regionName);
               }

               CheckEmptyRegion(regionName);
            }
         }
      }

      private void CheckEmptyRegion(string regionName)
      {
         if (!string.IsNullOrEmpty(regionName))
         {
            if (_cache.ContainsKey(regionName))
            {
               var regionCache = _cache[regionName];
               if (regionCache.Count == 0)
               {
                  ConcurrentDictionary<string, byte[]> values;
                  _cache.TryRemove(regionName, out values);
               }
            }
         }
      }

      private string GetKeyFileName(string key, string regionName, bool createDirectory)
      {
         string extension = (key != null && key.Contains(".")) ? null : ".data";

         regionName = regionName == null ? string.Empty : regionName;

         var directory = Path.Combine(this.ResourcesDirectory, regionName);
         var fileName = key;
         if (extension != null)
            fileName += extension;
         var filePath = Path.Combine(directory, fileName);

         if (createDirectory && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

         return filePath;
      }

      private ConcurrentDictionary<string, byte[]> DoGetRegionCache(string regionName, bool createRegion)
      {
         if (!_cache.ContainsKey(regionName))
         {
            if (createRegion)
               _cache.TryAdd(regionName, new ConcurrentDictionary<string, byte[]>());
         }

         var regionCache = _cache[regionName];
         return regionCache;
      }

      private void WritePayload<T>(string key, T value, string regionName)
      {
         if (value == null)
            throw new InvalidOperationException("Value of null is not supported by this cache implementation. Type is " + typeof(T).FullName);

         // Write the data
         var regionCache = DoGetRegionCache(regionName, true);
         byte[] data;
         var formatter = new BinaryFormatter();
         using (var ms = new MemoryStream())
         {
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            formatter.Serialize(ms, value);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            data = ms.ToArray();
         }
         regionCache[key] = data;
      }

      // Read the content of a cache item
      private T ReadPayload<T>(string key, string regionName)
      {
         var regionCache = DoGetRegionCache(regionName, true);
         T value = default(T);

         if (regionCache != null && regionCache.ContainsKey(key))
         {
            byte[] data = regionCache[key];
            var formatter = new BinaryFormatter();
            using (var ms = new MemoryStream(data))
            {
#pragma warning disable SYSLIB0011 // Type or member is obsolete
               value = (T)formatter.Deserialize(ms);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            }
         }

         return value;
      }
   }
}
