// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;

using Leadtools.Caching;
using Leadtools.Document;
using Leadtools.Document.Compare;
using Leadtools.Services.Models.Compare;
using Leadtools.Services.Tools.Helpers;
using Microsoft.AspNetCore.Http;

namespace Leadtools.DocumentViewer.Tools.Helpers
{
   internal static class CompareHelper
   {
      public static RunCompareJobResponse RunCompareJob(IHeaderDictionary headers, ServiceCompareJobData serviceJobData)
      {
         if (serviceJobData == null || serviceJobData.DocumentIds == null || serviceJobData.DocumentIds.Count == 0) throw new ArgumentNullException(nameof(serviceJobData));
         if (serviceJobData.DocumentIds.Count != 2) throw new ArgumentOutOfRangeException(nameof(serviceJobData), "Exactly two Document Ids must be provided.");
         if (string.IsNullOrWhiteSpace(serviceJobData.OutputMimetype)) throw new ArgumentNullException(nameof(serviceJobData), "Output mimetypes must not be null");

         var userToken = ServiceHelper.GetUserToken(headers, null);

         var maxPages = ServiceHelper.GetMaxComparePages();

         IList<LEADDocument> documents = null;
         try
         {
            documents = (maxPages > 0) ?
               LoadCappedPages(serviceJobData.DocumentIds, maxPages, userToken) :
               LoadAllPages(serviceJobData.DocumentIds, userToken);
         }
         catch (Exception)
         {
            DisposeAllDocuments(documents);
            throw;
         }

         var jobData = new CompareJobData();
         jobData.OutputDocumentId = string.IsNullOrEmpty(serviceJobData.OutputDocumentId) ? DocumentFactory.NewCacheId() : serviceJobData.OutputDocumentId;
         jobData.OutputMimetype = serviceJobData.OutputMimetype;
         jobData.Documents = documents;
         jobData.StatusCache = ServiceHelper.CacheManager.DefaultCache;
         jobData.StatusPolicy = ServiceHelper.CacheManager.CreatePolicy(jobData.StatusCache);
         jobData.OutputCache = ServiceHelper.CacheManager.DefaultCache;
         jobData.OutputPolicy = ServiceHelper.CacheManager.CreatePolicy(jobData.OutputCache);
         jobData.UserToken = DocumentFactory.NewCacheId();
         jobData.JobToken = DocumentFactory.NewCacheId();
         jobData.OutputDocumentName = $"{documents[0].Name} - {documents[1].Name} report";

         var runner = new CompareJobRunner();
         try
         {
            runner.PrepareJob(jobData);
         }
         catch (Exception ex)
         {
            DisposeAllDocuments(documents);
            Trace.WriteLine(string.Format("RunCompareError - Error:{1}{0}documentIds:{2}", Environment.NewLine, ex.Message, serviceJobData.DocumentIds.ToString()), "Error");
            throw;
         }

         ThreadPool.QueueUserWorkItem((object state) =>
         {
            EventHandler<CompareJobRunnerEventArgs> operation = (sender, e) =>
            {
               if (!e.IsPostOperation && e.Operation == CompareJobRunnerOperation.BeginUpload)
               {
                  UploadDocumentOptions uploadDocumentOptions = e.Data as UploadDocumentOptions;
                  uploadDocumentOptions.UserToken = userToken;
                  ObjectCache outCache = ServiceHelper.CacheManager.GetCacheForBeginUpload(uploadDocumentOptions);
                  if (outCache != null)
                  {
                     e.CompareJobData.OutputCache = outCache;
                     uploadDocumentOptions.CachePolicy = ServiceHelper.CacheManager.CreatePolicy(outCache);
                  }
               }
            };

            runner.Operation += operation;

            try
            {
               runner.RunJob(jobData);
            }
            finally
            {
               runner.Operation -= operation;
               DisposeAllDocuments(jobData.Documents);
            }
         });

         return new RunCompareJobResponse()
         {
            JobToken = jobData.JobToken,
            UserToken = jobData.UserToken
         };
      }

      private static List<LEADDocument> LoadAllPages(IList<string> documentIds, string userToken)
      {
         var documents = new List<LEADDocument>();

         try
         {
            foreach (var id in documentIds)
            {
               var options = new LoadFromCacheOptions()
               {
                  DocumentId = id,
                  Cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(id),
                  UserToken = userToken
               };

               var document = DocumentFactory.LoadFromCache(options);
               if (document == null) throw new ArgumentNullException(nameof(documentIds), $"Document with id: {id} does not exist in the cache");

               documents.Add(document);
            }
         }
         catch
         {
            DisposeAllDocuments(documents);
            throw;
         }

         return documents;
      }

      private static List<LEADDocument> LoadCappedPages(IList<string> documentIds, int maxPages, string userToken)
      {
         var documents = new List<LEADDocument>();
         var templateUrl = "leadcache://";

         try
         {
            foreach (var id in documentIds)
            {
               var url = $"{templateUrl}/{id}";
               var options = new LoadDocumentOptions()
               {
                  LastPageNumber = maxPages,
                  Cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(id),
                  UserToken = userToken
               };
               var document = DocumentFactory.LoadFromUri(new Uri(url), options);
               if (document == null) throw new ArgumentNullException(nameof(documentIds), $"Document with id: {id} does not exist in the cache");
               documents.Add(document);
            }
         }
         catch
         {
            DisposeAllDocuments(documents);
            throw;
         }

         return documents;
      }

      private static void DisposeAllDocuments(IEnumerable<LEADDocument> documents)
      {
         if (documents != null)
         {
            foreach (LEADDocument document in documents)
            {
               if (document != null)
                  document.Dispose();
            }
         }
      }
   }

   public class ServiceCompareJobData
   {
      [DataMember(Name = "documentIds")]
      public IList<string> DocumentIds { get; set; }

      [DataMember(Name = "outputDocumentId")]
      public string OutputDocumentId { get; set; }

      [DataMember(Name = "outputMimetype")]
      public string OutputMimetype { get; set; }
   }
}
