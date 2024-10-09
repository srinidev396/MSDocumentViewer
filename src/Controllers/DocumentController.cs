// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

using Leadtools.Services.Models.Document;
using Leadtools.Services.Tools.Exceptions;
using Leadtools.Document;
using Leadtools.Document.Compare;
using Leadtools.Services.Tools.Helpers;
using Leadtools.Caching;

namespace Leadtools.DocumentViewer.Controllers
{
   /// <summary>
   /// Used with the LEADDocument class of the LEADTOOLS Document JavaScript library.
   /// </summary>
   public class DocumentController : Controller
   {
      public DocumentController()
      {
         ServiceHelper.InitializeController();
      }

      /// <summary>
      /// Returns a decrypted version of the document when passed the correct password, or throws an exception.
      /// </summary>
      /// <param name="request">A <see cref="DecryptRequest">DecryptRequest</see> containing an identifier and password.</param>
      /// <returns>A <see cref="DecryptResponse">DecryptResponse</see> containing all new document information.</returns>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The document decryption failed")]
      [HttpPost("api/[controller]/[action]")]
      public DecryptResponse Decrypt(DecryptRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         if (request.DocumentId == null)
            throw new ArgumentException("documentId must not be null");

         ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.DocumentId);
         var loadFromCacheOptions = new LoadFromCacheOptions
         {
            Cache = cache,
            DocumentId = request.DocumentId,
            UserToken = ServiceHelper.GetUserToken(this.Request.Headers, null)
         };
         using (var document = DocumentFactory.LoadFromCache(loadFromCacheOptions))
         {
            DocumentHelper.CheckLoadFromCache(document);

            if (!document.Decrypt(request.Password))
               throw new ServiceException("Incorrect Password", HttpStatusCode.Forbidden);

            document.SaveToCache();
            return new DecryptResponse { Document = document };
         }
      }

      /// <summary>
      /// Runs the conversion specified by the conversion job data on the document, and stores the result to the cache.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The document could not be converted")]
      [HttpPost("api/[controller]/[action]")]
      public ConvertResponse Convert(ConvertRequest request)
      {
         return ConverterHelper.Convert(request.DocumentId, this.Request.Headers, request.JobData);
      }

      // For debugging - if true, document history will be logged to the console
      private static bool _loggingDocumentHistory = false;

      /// <summary>
      /// Retrieves changes to the document since the history was last cleared.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The document history could not be retrieved")]
      [HttpPost("api/[controller]/[action]")]
      public GetHistoryResponse GetHistory(GetHistoryRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         // Must have the documentId you'd like to add annotations to.
         // If you only have the document cache URI, DocumentFactory.LoadFromUri needs to be called.
         if (string.IsNullOrEmpty(request.DocumentId))
            throw new ArgumentNullException("documentId");

         IList<DocumentHistoryItem> items = null;

         ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.DocumentId);
         var loadFromCacheOptions = new LoadFromCacheOptions
         {
            Cache = cache,
            DocumentId = request.DocumentId,
            UserToken = ServiceHelper.GetUserToken(this.Request.Headers, request)
         };
         using (var document = DocumentFactory.LoadFromCache(loadFromCacheOptions))
         {
            // Ensure we have the document.
            DocumentHelper.CheckLoadFromCache(document);

            DocumentHistory history = document.History;
            items = history.GetItems();

            if (items != null && request.ClearHistory)
            {
               history.Clear();
               document.SaveToCache();
            }
         }

         if (_loggingDocumentHistory)
            ShowHistory(request.DocumentId, items);

         DocumentHistoryItem[] itemsArray = new DocumentHistoryItem[items.Count];
         items.CopyTo(itemsArray, 0);
         GetHistoryResponse response = new GetHistoryResponse
         {
            Items = itemsArray
         };
         return response;
      }

      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The documents could not be compared")]
      [HttpPost("api/[controller]/[action]")]
      public CompareResponse Compare(CompareRequest request)
      {
         if (request == null)
            throw new ArgumentNullException(nameof(request));

         if (request.DocumentIds == null)
            throw new ArgumentNullException("DocumentIds");

         if (request.DocumentIds.Length != 2)
            throw new ArgumentException("There must be exactly 2 Document Id's provided", "DocumentIds");

         ObjectCache cache1 = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.DocumentIds[0]);
         var loadFromCacheOptions1 = new LoadFromCacheOptions
         {
            Cache = cache1,
            DocumentId = request.DocumentIds[0],
            UserToken = ServiceHelper.GetUserToken(this.Request.Headers, null)
         };

         ObjectCache cache2 = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.DocumentIds[1]);
         var loadFromCacheOptions2 = new LoadFromCacheOptions
         {
            Cache = cache1,
            DocumentId = request.DocumentIds[1],
            UserToken = ServiceHelper.GetUserToken(this.Request.Headers, null)
         };

         using (var firstDocument = DocumentFactory.LoadFromCache(loadFromCacheOptions1))
         using (var secondDocument = DocumentFactory.LoadFromCache(loadFromCacheOptions2))
         {
            DocumentHelper.CheckLoadFromCache(firstDocument);
            DocumentHelper.CheckLoadFromCache(secondDocument);
            var ocrEngine = ServiceHelper.GetOCREngine();
            if (ocrEngine != null)
            {
               firstDocument.Text.OcrEngine = ocrEngine;
               secondDocument.Text.OcrEngine = ocrEngine;
            }
               

            var comparer = new DocumentComparer();
            var documentDifference = comparer.CompareDocument(new List<LEADDocument>() { firstDocument, secondDocument });

            return new CompareResponse()
            {
               DocumentDifference = documentDifference
            };
         }
      }

      private static void ShowHistory(string documentId, IList<DocumentHistoryItem> items)
      {
         if (items == null || items.Count == 0)
            return;

         Trace.WriteLine(string.Format("History for document '{0}'", documentId));
         foreach (DocumentHistoryItem item in items)
         {
            Trace.WriteLine(string.Format("   User: '{0}' Timestamp: {1} Comment: '{2}' Change: '{3}' PageNumber: {4}",
               item.UserId != null ? item.UserId : "[null]",
               item.Timestamp,
               item.Comment != null ? item.Comment : "[null]",
               GetName(item.ModifyType),
               item.PageNumber));
         }
      }

      private static string GetName(DocumentHistoryModifyType value)
      {
         switch (value)
         {
            case DocumentHistoryModifyType.Created: return "Created";
            case DocumentHistoryModifyType.Decrypted: return "Decrypted";
            case DocumentHistoryModifyType.Pages: return "Pages";
            case DocumentHistoryModifyType.PageViewPerspective: return "Page ViewPerspective";
            case DocumentHistoryModifyType.PageAnnotations: return "Page Annotations";
            case DocumentHistoryModifyType.PageMarkDeleted: return "Page MarkDeleted";
            case DocumentHistoryModifyType.PageImage: return "Page Image";
            case DocumentHistoryModifyType.PageSvgBackImage: return "Page SvgBackImage";
            case DocumentHistoryModifyType.PageSvg: return "Page Svg";
            case DocumentHistoryModifyType.PageText: return "Page Text";
            case DocumentHistoryModifyType.PageLinks: return "Page Links";
            default: return "Unknown";
         }
      }
   }
}
