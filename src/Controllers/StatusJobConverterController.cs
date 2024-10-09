// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using Microsoft.AspNetCore.Mvc;

using Leadtools.Services.Tools.Exceptions;
using Leadtools.Services.Tools.Helpers;
using Leadtools.Services.Models;
using Leadtools.Services.Models.Document;
using Leadtools.Document.Converter;
using Leadtools.Services.Models.StatusJobConverter;
using Leadtools.Caching;

namespace Leadtools.DocumentViewer.Controllers
{
   /// <summary>
   /// Used with the StatusJobDataRunner class of the LEADTOOLS Document JavaScript library. 
   /// </summary>
   public class StatusJobConverterController : Controller
   {
      public StatusJobConverterController()
      {
         ServiceHelper.InitializeController();
      }

      /// <summary>
      /// Runs the status job conversion specified by the conversion job data on the document.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The conversion job could not be started")]
      [HttpPost("api/[controller]/[action]")]
      public RunConvertJobResponse Run(RunConvertJobRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request");
         return ConverterHelper.RunConvertJob(request.DocumentId, this.Request.Headers, request.JobData);
      }

      /// <summary>
      /// Queries the status of the conversion job.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The conversion job could not be queried")]
      [HttpPost("api/[controller]/[action]")]
      public QueryConvertJobStatusResponse Query(QueryConvertJobStatusRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         ObjectCache cache = ServiceHelper.CacheManager.DefaultCache;
         return new QueryConvertJobStatusResponse
         {
            jobData = StatusJobDataRunner.QueryJobStatus(cache, request.UserToken, request.JobToken)
         };
      }

      /// <summary>
      /// Deletes the status entry for the conversion job.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The conversion job cache entry could not be deleted")]
      [HttpPost("api/[controller]/[action]")]
      public Response Delete(DeleteConvertJobStatusRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         ObjectCache cache = ServiceHelper.CacheManager.DefaultCache;
         StatusJobDataRunner.DeleteJob(cache, request.UserToken, request.JobToken);
         return new Response();
      }

      /// <summary>
      /// Aborts the conversion job.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The conversion job could not be aborted")]
      [HttpPost("api/[controller]/[action]")]
      public Response Abort(AbortConvertJobRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         ObjectCache cache = ServiceHelper.CacheManager.DefaultCache;
         StatusJobDataRunner.AbortJob(cache, request.UserToken, request.JobToken);
         return new Response();
      }
   }
}
