// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Leadtools.Services.Tools.Helpers;
using Leadtools.Services.Models;
using Leadtools.Document;
using Leadtools.Document.Converter;

namespace Leadtools.Services.Models.StatusJobConverter
{
   [DataContract]
   public class RunConvertJobRequest : Request
   {
      /// <summary>
      /// The ID of the document to convert.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The relevant job data that will be used to understand the conversion needed.
      /// </summary>
      [DataMember(Name = "jobData")]
      public ServiceDocumentConverterJobData JobData { get; set; }
   }

   [DataContract]
   public class RunConvertJobResponse : Response
   {
      /// <summary>
      /// The user token (region) for the job.
      /// </summary>
      [DataMember(Name = "userToken")]
      public string UserToken { get; set; }

      /// <summary>
      /// The job token (key) for the job.
      /// </summary>
      [DataMember(Name = "jobToken")]
      public string JobToken { get; set; }
   }

   [DataContract]
   public class QueryConvertJobStatusRequest : Request
   {
      /// <summary>
      /// The user token (region) for the job.
      /// </summary>
      [DataMember(Name = "userToken")]
      public string UserToken { get; set; }

      /// <summary>
      /// The job token (key) for the job.
      /// </summary>
      [DataMember(Name = "jobToken")]
      public string JobToken { get; set; }
   }

   [DataContract]
   public class QueryConvertJobStatusResponse : Response
   {
      /// <summary>
      /// All data depicting the status of the job.
      /// </summary>
      [DataMember(Name = "jobData")]
      public StatusJobData jobData { get; set; }
   }

   public class DeleteConvertJobStatusRequest : Request
   {
      /// <summary>
      /// The user token (region) for the job.
      /// </summary>
      [DataMember(Name = "userToken")]
      public string UserToken { get; set; }

      /// <summary>
      /// The job token (key) for the job.
      /// </summary>
      [DataMember(Name = "jobToken")]
      public string JobToken { get; set; }
   }

   [DataContract]
   public class AbortConvertJobRequest : Request
   {
      /// <summary>
      /// The user token (region) for the job.
      /// </summary>
      [DataMember(Name = "userToken")]
      public string UserToken { get; set; }

      /// <summary>
      /// The job token (key) for the job.
      /// </summary>
      [DataMember(Name = "jobToken")]
      public string JobToken { get; set; }
   }
}
