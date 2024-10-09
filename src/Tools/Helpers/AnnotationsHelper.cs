// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using Leadtools.DocumentViewer.Models.Page;
using Leadtools.Annotations.Engine;
using Leadtools.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Leadtools.Services.Models;
using Leadtools.Services.Tools.Helpers;
using Microsoft.AspNetCore.Http;
using Leadtools.Caching;

namespace Leadtools.DocumentViewer.Tools.Helpers
{
   internal static class AnnotationMethods
   {
      public static GetAnnotationsResponse GetAnnotations(GetAnnotationsRequest request, IHeaderDictionary headers)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         if (string.IsNullOrEmpty(request.DocumentId))
            throw new ArgumentException("documentId must not be null");

         if (request.PageNumber < 0)
            throw new ArgumentException("'pageNumber' must be a value greater than or equal to 0");

         // If page number is 0, get all annotations

         // Now load the document
         ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.DocumentId);
         var loadFromCacheOptions = new LoadFromCacheOptions
         {
            Cache = cache,
            DocumentId = request.DocumentId,
            UserToken = ServiceHelper.GetUserToken(headers, request)
         };

         using (var document = DocumentFactory.LoadFromCache(loadFromCacheOptions))
         {
            DocumentHelper.CheckLoadFromCache(document);

            var annCodec = new AnnCodecs();
            string annotations = null;
            if (request.PageNumber == 0)
            {
               var containers = document.Annotations.GetAnnotations(request.CreateEmpty);
               annotations = annCodec.SaveAllToString(containers, AnnFormat.Annotations);
            }
            else
            {
               DocumentHelper.CheckPageNumber(document, request.PageNumber);

               var documentPage = document.Pages[request.PageNumber - 1];

               var container = documentPage.GetAnnotations(request.CreateEmpty);
               if (container != null)
                  annotations = annCodec.SaveToString(container, AnnFormat.Annotations, request.PageNumber);
            }
            return new GetAnnotationsResponse { Annotations = annotations };
         }
      }

      public static Response SetAnnotations(SetAnnotationsRequest request, IHeaderDictionary headers)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         if (string.IsNullOrEmpty(request.DocumentId))
            throw new ArgumentException("documentId must not be null");

         if (request.PageNumber < 0)
            throw new ArgumentException("'pageNumber' must be a value greater than or equal to 0");

         // If pageNumber 0, set for all pages

         var annCodec = new AnnCodecs();
         AnnContainer[] containers = null;

         if (!string.IsNullOrEmpty(request.Annotations))
            containers = annCodec.LoadAllFromString(request.Annotations);

         // Now load the document
         ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.DocumentId);
         var loadFromCacheOptions = new LoadFromCacheOptions
         {
            Cache = cache,
            DocumentId = request.DocumentId,
            UserToken = ServiceHelper.GetUserToken(headers, null)
         };

         using (var document = DocumentFactory.LoadFromCache(loadFromCacheOptions))
         {
            DocumentHelper.CheckLoadFromCache(document);

            // If the document is read-only then below will fail. So, temporarily reset the value
            var wasReadOnly = document.IsReadOnly;
            document.IsReadOnly = false;

            if (request.PageNumber == 0)
            {
               // Set all
               document.Annotations.SetAnnotations(containers);
            }
            else
            {
               DocumentHelper.CheckPageNumber(document, request.PageNumber);

               var documentPage = document.Pages[request.PageNumber - 1];
               AnnContainer container = null;
               if (containers != null)
               {
                  if (containers.Length == 1)
                  {
                     container = containers[0];
                  }
                  else
                  {
                     for (var i = 0; i < containers.Length && container == null; i++)
                     {
                        if (containers[i].PageNumber == request.PageNumber)
                           container = containers[i];
                     }
                  }
               }

               documentPage.SetAnnotations(container);
            }

            // reset the read-only value before saving into the cache
            document.IsReadOnly = wasReadOnly;
            document.SaveToCache();
         }
         return new Response();
      }
   }
}
