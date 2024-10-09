// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using Leadtools.Services.Models.PreCache;
using Leadtools;
using Leadtools.Caching;
using Leadtools.Codecs;
using Leadtools.Document;
using Leadtools.Services.Tools.Exceptions;
using Leadtools.Ocr;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml.Linq;

namespace Leadtools.Services.Tools.Helpers
{
   public static class PreCacheHelper
   {
      /*
       * The PreCacheHelper is a static utility class that holds a secondary LEADTOOLS Cache.
       * This Cache does not hold LEADDocument objects like the Document Library Cache does; instead, it
       * holds a URI => Document ID mapping for documents that have been loaded into the Documents cache.
       * The pre-cache can thus be used to return the same cached document ID for a URL, which then is used
       * to load the same Document from the Documents Cache. All requests for a given URI will respond with the same
       * object instead of creating new cache entries.
       * 
       * Note that the PreCacheHelper uses lock() for basic thread safety. The internal Cache code is always thread and application safe,
       * whereas this basic utility class is not.
       */

      private const string PreCacheLegendName = "Legend";
      private static ObjectCache _preCache = null;
      public static bool PreCacheExists
      {
         get
         {
            return _preCache != null;
         }
      }

      // Lock for when we need to synchronously update the legend cache item
      private static readonly object LegendLock = new object();
      private static readonly object ReadingLock = new object();

      public static int[] DefaultSizes = new int[] { 4096, 2048 };

      private static Dictionary<string, string> GetLegend(bool createIfNotExist)
      {
         var legend = _preCache.Get<Dictionary<string, string>>(PreCacheLegendName, null);
         if (legend == null && createIfNotExist)
            legend = new Dictionary<string, string>();
         return legend;
      }

      private static string GetRegionName(Uri requestUri)
      {
         // our uri will be the region name, but we obviously can't have certain characters.
         // so create a hash (32 characters).

         string uri = requestUri.ToString().ToLower();
         byte[] bytes = Encoding.ASCII.GetBytes(uri);
         byte[] hash = null;
         using (var md5 = MD5.Create())
         {
            hash = md5.ComputeHash(bytes);
         }

         // change it back to a string
         StringBuilder result = new StringBuilder(hash.Length * 2);
         for (int i = 0; i < hash.Length; i++)
            result.Append(hash[i].ToString("X2"));

         return result.ToString();
      }
      private static string GetKeyName(int size)
      {
         return "size_" + size;
      }

      public static void CreatePreCache()
      {
         // check first, so not everyone here will get caught by the lock
         if (_preCache != null)
            return;

         var preCacheDirectory = ServiceHelper.GetSettingValue(ServiceHelper.Key_PreCache_Directory);
         if (string.IsNullOrEmpty(preCacheDirectory))
            return;

         preCacheDirectory = ServiceHelper.GetAbsolutePath(preCacheDirectory);
         if (string.IsNullOrEmpty(preCacheDirectory))
         {
            // No setting, pre-caching is disabled
            return;
         }

         try
         {
            var cache = new FileCache();
            cache.CacheDirectory = preCacheDirectory;
            if (!Directory.Exists(preCacheDirectory))
               Directory.CreateDirectory(preCacheDirectory);

            // Choose how we want to serialize the data. We choose JSON for human-readability.
            cache.DataSerializationMode = CacheSerializationMode.Json;
            cache.PolicySerializationMode = CacheSerializationMode.Json;

            _preCache = cache;
         }
         catch (Exception e)
         {
            throw new InvalidOperationException("Could not create pre-cache", e);
         }
      }

      public static PreCacheDocumentResponse AddExistingDocument(Uri uri, LEADDocument document)
      {
         // Add an existing document to the pre-cache.
         // This is used in LoadFromUri for those who wish to get the same cached document
         // from the same URL all the time.
         // Note we do no special loading of text or images here.

         // Get the safe hash name from the uri
         var regionName = GetRegionName(uri);

         // The PreCacheEntries are what will be cached, based on this map of sizes to documentId values.
         var sizeIdDictionary = new Dictionary<int, string>();

         var maximumImagePixelSizes = DefaultSizes;
         // The PreCacheResponseItems are what we will return as a confirmation.
         var responseItems = new PreCacheResponseSizeItem[maximumImagePixelSizes.Length];

         for (var index = 0; index < maximumImagePixelSizes.Length; index++)
         {
            var size = maximumImagePixelSizes[index];
            // If it's in the cache, delete it (deletes from PreCache also)
            string documentId = InternalCheckDocument(regionName, GetKeyName(size));
            if (documentId != null)
               DocumentHelper.DeleteDocument(documentId, null, false, false);
            else
               documentId = document.DocumentId;

            responseItems[index] = new PreCacheResponseSizeItem()
            {
               Seconds = 0,
               DocumentId = documentId,
               MaximumImagePixelSize = size,
            };

            // add to our dictionary for updating the pre-cache all at once
            sizeIdDictionary.Add(size, documentId);
         }

         // Add all the info to the PreCache
         AddDocumentToPreCache(regionName, uri, sizeIdDictionary);

         return new PreCacheDocumentResponse()
         {
            Item = new PreCacheResponseItem()
            {
               Uri = uri.ToString(),
               RegionHash = regionName,
               Items = responseItems,
            }
         };
      }

      public static PreCacheDocumentResponse AddDocument(bool preCacheDictionaryExists, ObjectCache cache, PreCacheDocumentRequest request)
      {
         var loadOptions = new LoadDocumentOptions();
         loadOptions.Cache = cache;
         loadOptions.UseCache = true;

         // Get the expiry policy
         CacheItemPolicy cachePolicy;

         if (request.ExpiryDate == null)
         {
            cachePolicy = ServiceHelper.CreateForeverPolicy();
         }
         else
         {
            cachePolicy = new CacheItemPolicy()
            {
               AbsoluteExpiration = request.ExpiryDate.Value
            };
         }

         loadOptions.CachePolicy = cachePolicy;
         // Get the maximum pixel size, if the user did not pass one, use the default values of 4096 and 2048 (used by the DocumentViewerDemo)
         var maximumImagePixelSizes = request.MaximumImagePixelSizes;
         if (maximumImagePixelSizes == null || maximumImagePixelSizes.Length == 0)
            maximumImagePixelSizes = DefaultSizes;

         // Sort the maximum image pixel size from largest to smallest
         // We will re-use the values from largest to set the smaller images text and SVG since they do
         // not change
         Array.Sort(
            maximumImagePixelSizes,
            new Comparison<int>((x, y) => y.CompareTo(x)));

         // Get the safe hash name from the uri
         var regionName = GetRegionName(request.Uri);

         // The PreCacheEntries are what will be cached, based on this map of sizes to documentId values.
         var sizeIdDictionary = new Dictionary<int, string>();

         // The PreCacheResponseItems are what we will return as a confirmation.
         var responseItems = new PreCacheResponseSizeItem[maximumImagePixelSizes.Length];

         var ocrEngine = ServiceHelper.GetOCREngine();

         // Largest document (to re-use)
         LEADDocument largestDocument = null;

         try
         {
            // Now load the document and cache it
            for (var index = 0; index < maximumImagePixelSizes.Length; index++)
            {
               // No duplicates
               if (index > 0 && maximumImagePixelSizes[index] == maximumImagePixelSizes[index - 1])
                  continue;

               var size = maximumImagePixelSizes[index];

               string documentId;

               if (preCacheDictionaryExists)
               {
                  // If it's in the cache, delete it (deletes from PreCache also)
                  documentId = InternalCheckDocument(regionName, GetKeyName(size));
                  if (documentId != null)
                     DocumentHelper.DeleteDocument(documentId, null, false, false);
               }

               // keep track for logging purposes
               var start = DateTime.Now;

               // re-use the load options, just change the size
               loadOptions.MaximumImagePixelSize = size;

               // Cache the Document
               var document = AddDocumentToCache(largestDocument, ocrEngine, request.Uri, loadOptions, request.CacheOptions, request.FirstPageNumber, request.LastPageNumber);
               try
               {
                  var stop = DateTime.Now;
                  documentId = document.DocumentId;

                  responseItems[index] = new PreCacheResponseSizeItem()
                  {
                     Seconds = Math.Round((stop - start).TotalSeconds, 4),
                     DocumentId = documentId,
                     MaximumImagePixelSize = size,
                  };

                  // add to our dictionary for updating the pre-cache all at once
                  sizeIdDictionary.Add(size, documentId);
               }
               finally
               {
                  if (largestDocument == null)
                     largestDocument = document;
                  else
                     document.Dispose();
               }
            }
         }
         finally
         {
            if (largestDocument != null)
               largestDocument.Dispose();
         }

         if (preCacheDictionaryExists)
         {
            // Add all the info to the PreCache
            AddDocumentToPreCache(regionName, request.Uri, sizeIdDictionary);
         }

         return new PreCacheDocumentResponse()
         {
            Item = new PreCacheResponseItem()
            {
               Uri = request.Uri.ToString(),
               RegionHash = regionName,
               Items = responseItems,
            }
         };
      }

      private static LEADDocument AddDocumentToCache(LEADDocument largestDocument, IOcrEngine ocrEngine, Uri documentUri, LoadDocumentOptions loadOptions, DocumentCacheOptions cacheOptions, int firstPageNumber, int lastPageNumber)
      {
         // Adds the document to the cache. Note that a new cache entry is created for each different maximumImagePixelSize.

         var document = DocumentFactory.LoadFromUri(documentUri, loadOptions);
         try
         {
            if (document == null)
               throw new InvalidOperationException("Failed to load URI: " + documentUri);

            if (firstPageNumber == 0 || firstPageNumber == -1)
               firstPageNumber = 1;
            if (lastPageNumber == 0 || lastPageNumber == -1)
               lastPageNumber = document.Pages.Count;

            // We will modify this document...
            bool wasReadOnly = document.IsReadOnly;
            document.IsReadOnly = false;

            if (document.Text.TextExtractionMode != DocumentTextExtractionMode.OcrOnly && !document.Images.IsSvgSupported && ocrEngine != null)
               document.Text.OcrEngine = ocrEngine;

            // Set in the cache options that we want
            document.CacheOptions = cacheOptions;

            // prepare the document, caching as much as possible.
            if (document.IsStructureSupported && !document.Structure.IsParsed)
               document.Structure.Parse();

            // Need to cache the SVG with and without getting the back image
            var loadSvgOptions = new CodecsLoadSvgOptions();

            foreach (var page in document.Pages)
            {
               int pageNumber = page.PageNumber;
               if (pageNumber < firstPageNumber || pageNumber > lastPageNumber)
                  continue;

               // If we have a previous largest document, use the same
               // SVG and text instead of recreating them (they do not change based on image size)
               DocumentPage largestDocumentPage = null;

               if (largestDocument != null)
                  largestDocumentPage = largestDocument.Pages[pageNumber - 1];

               if (cacheOptions == DocumentCacheOptions.None)
               {
                  // We are done, do not cache the images, svg or text
                  continue;
               }

               // Don't cache page images for raster (non-SVG) documents
               document.CacheOptions = page.IsSvgSupported ? cacheOptions : (cacheOptions & ~DocumentCacheOptions.PageImage);

               if ((cacheOptions & DocumentCacheOptions.PageSvg) == DocumentCacheOptions.PageSvg)
               {
                  // SVG, this does not depend on the image size
                  using (var svg = page.GetSvg(null))
                  {
                  }

                  using (var svg = page.GetSvg(loadSvgOptions))
                  {
                  }
               }

               if ((cacheOptions & DocumentCacheOptions.PageSvgBackImage) == DocumentCacheOptions.PageSvgBackImage)
               {
                  // SVG back image, this is different based on the image size
                  using (var svgBack = page.GetSvgBackImage(RasterColor.FromKnownColor(RasterKnownColor.White)))
                  {
                  }
               }

               if ((cacheOptions & DocumentCacheOptions.PageImage) == DocumentCacheOptions.PageImage)
               {
                  // Image, this is different based on the image size
                  using (var image = page.GetImage())
                  {
                  }
               }

               if ((cacheOptions & DocumentCacheOptions.PageThumbnailImage) == DocumentCacheOptions.PageThumbnailImage)
               {

                  // Thumbnail, this does not depend on the image size but there is no set thumbnail method
                  using (var thumbnailImage = page.GetThumbnailImage())
                  {
                  }
               }

               if ((cacheOptions & DocumentCacheOptions.PageText) == DocumentCacheOptions.PageText)
               {
                  // Text, this does not depend on the image size
                  if (largestDocumentPage == null)
                  {
                     page.GetText();
                  }
                  else
                  {
                     var pageText = largestDocumentPage.GetText();
                     page.SetText(pageText);
                  }
               }
            }

            document.AutoDeleteFromCache = false;
            document.AutoDisposeDocuments = true;
            document.AutoSaveToCache = false;
            // Stop caching
            document.CacheOptions = DocumentCacheOptions.None;
            document.IsReadOnly = wasReadOnly;

            // save it to the regular cache
            document.SaveToCache();

            return document;
         }
         catch (Exception)
         {
            if (document != null)
               document.Dispose();
            throw;
         }
      }

      private static void AddDocumentToPreCache(string hashRegion, Uri uri, Dictionary<int, string> sizeItems)
      {
         // keep it in the pre-cache until deleted.
         CacheItemPolicy cachePolicy = ServiceHelper.CreateForeverPolicy();

         foreach (KeyValuePair<int, string> dictionaryEntry in sizeItems)
         {
            // create our object to place in the cache
            var cacheEntry = new PreCacheEntry()
            {
               MaximumImagePixelSize = dictionaryEntry.Key,
               DocumentId = dictionaryEntry.Value,
               Reads = 0
            };

            // use the regionHash as our region name, the size as our key, and the entry as our value.
            var cacheItem = new CacheItem<PreCacheEntry>(GetKeyName(cacheEntry.MaximumImagePixelSize), cacheEntry, hashRegion);
            _preCache.Add(cacheItem, cachePolicy);
         }

         lock (LegendLock)
         {
            // Also add this to our "Legend" file that will just hold all the regionHash <==> uri mappings in one place.
            var legendMappings = GetLegend(true);
            if (legendMappings.ContainsKey(hashRegion))
               legendMappings.Remove(hashRegion);
            legendMappings.Add(hashRegion, uri.ToString());

            // Add it back to the cache
            _preCache.Add(new CacheItem<Dictionary<string, string>>(PreCacheLegendName, legendMappings), ServiceHelper.CreateForeverPolicy());
         }
      }

      public static String CheckDocument(Uri documentUri, int maximumImagePixelSize)
      {
         return InternalCheckDocument(GetRegionName(documentUri), GetKeyName(maximumImagePixelSize));
      }

      private static String InternalCheckDocument(string regionName, string keyName)
      {
         // Add a lock to the read, since we're changing the cache item and re-saving it.
         lock (ReadingLock)
         {
            var entry = _preCache.Get<PreCacheEntry>(keyName, regionName);
            if (entry != null)
            {
               // Update the "reads" property
               entry.Reads++;
               _preCache.Add(new CacheItem<PreCacheEntry>(keyName, entry, regionName), ServiceHelper.CreateForeverPolicy());
               return entry.DocumentId;
            }
            else
            {
               return null;
            }
         }
      }

      public static ReportPreCacheResponse ReportDocuments(bool clear, bool syncWithCache)
      {
         var responseEntries = new List<PreCacheResponseItem>();
         var responseRemoved = new List<PreCacheResponseItem>();

         Dictionary<string, string> legendMappings = null;
         lock (LegendLock)
         {
            // we'll need the legend file to get the URI back from the regionName.
            legendMappings = GetLegend(false);
         }

         // Note: since we have a set structure, we won't need to check case regionName == null.
         _preCache.EnumerateRegions(delegate (string regionName)
         {
            string uri = null;
            if (legendMappings.ContainsKey(regionName))
               uri = legendMappings[regionName];

            var entriesSizeItems = new List<PreCacheResponseSizeItem>();
            var removedSizeItems = new List<PreCacheResponseSizeItem>();

            // within each region, check all entries.
            _preCache.EnumerateKeys(regionName, delegate (string keyName)
            {
               var entry = _preCache.Get<PreCacheEntry>(keyName, regionName);
               var sizeItem = new PreCacheResponseSizeItem()
               {
                  DocumentId = entry.DocumentId,
                  MaximumImagePixelSize = entry.MaximumImagePixelSize,
                  Reads = entry.Reads
               };

               if (clear)
               {
                  removedSizeItems.Add(sizeItem);
               }
               else if (syncWithCache)
               {
                  // if the cache entry did not exist, add it to the removed items.
                  var loadFromCacheOptions = new LoadFromCacheOptions
                  {
                     Cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(entry.DocumentId),
                     DocumentId = entry.DocumentId
                  };
                  using (var document = DocumentFactory.LoadFromCache(loadFromCacheOptions))
                  {
                     if (document == null)
                     {
                        removedSizeItems.Add(sizeItem);
                        return;
                     }
                  }
               }

               if (!clear)
               {
                  // if !syncWithCache or it was not null
                  entriesSizeItems.Add(sizeItem);
               }
            });

            if (entriesSizeItems.Count > 0)
            {
               var entriesItem = new PreCacheResponseItem()
               {
                  RegionHash = regionName,
                  Uri = uri,
                  Items = entriesSizeItems.ToArray<PreCacheResponseSizeItem>()
               };
               responseEntries.Add(entriesItem);
            }

            if (removedSizeItems.Count > 0)
            {
               var removedItem = new PreCacheResponseItem()
               {
                  RegionHash = regionName,
                  Uri = uri,
                  Items = removedSizeItems.ToArray<PreCacheResponseSizeItem>()
               };
               responseRemoved.Add(removedItem);
            }
         });

         // do the deletion
         if (responseRemoved.Count > 0)
         {
            foreach (var responseItem in responseRemoved)
            {
               foreach (var sizeItem in responseItem.Items)
               {
                  _preCache.DeleteItem(GetKeyName(sizeItem.MaximumImagePixelSize), responseItem.RegionHash);
               }
            }
            _preCache.EnumerateRegions(delegate (string regionName)
            {
               CheckDeleteRegion(regionName);
            });
         }

         return new ReportPreCacheResponse()
         {
            Entries = responseEntries.ToArray<PreCacheResponseItem>(),
            Removed = responseRemoved.ToArray<PreCacheResponseItem>()
         };
      }

      public static void RemoveDocument(Uri uri, int[] maximumImagePixelSizes)
      {
         string regionName = GetRegionName(uri);
         // If maximumImagePixelSizes is null, delete all of the sizes for the document.
         if (maximumImagePixelSizes == null)
         {
            _preCache.DeleteRegion(regionName);
         }
         else
         {
            foreach (int size in maximumImagePixelSizes)
               _preCache.DeleteItem(GetKeyName(size), regionName);
         }
         CheckDeleteRegion(regionName);
      }

      private static void CheckDeleteRegion(string regionName)
      {
         // check if the region is now empty. If so,
         // 1) delete it
         // 2) remove it from the "Table of Contents"

         bool isEmpty = true;
         _preCache.EnumerateKeys(regionName, delegate (string keyName)
         {
            isEmpty = false;
         });
         if (isEmpty)
         {
            _preCache.DeleteRegion(regionName);

            // lock here since we're updating the legend item
            lock (LegendLock)
            {
               // Also delete this from our "Legend" file that holds all the uri <==> regionHash mappings in one place.
               var legendMappings = GetLegend(false);
               if (legendMappings == null)
                  return;
               if (legendMappings.ContainsKey(regionName.ToString()))
                  legendMappings.Remove(regionName.ToString());

               // Add it back to the cache
               _preCache.Add(new CacheItem<Dictionary<string, string>>(PreCacheLegendName, legendMappings), ServiceHelper.CreateForeverPolicy());
            }
         }
      }
   }
}
