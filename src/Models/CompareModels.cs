// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.IO;
using Leadtools.Services.Tools.Helpers;
using Leadtools.Services.Models;
using Leadtools.Document;
using Leadtools.Document.Compare;
using Leadtools.Services.Models.StatusJobConverter;
using Leadtools.DocumentViewer.Tools.Helpers;

namespace Leadtools.Services.Models.Compare
{
   [DataContract]
   public class GenerateReportRequest : Request
   {
      /// <summary>
      /// The mimetype for the report. 
      /// </summary>
      [DataMember(Name = "mimetype")]
      public string Mimetype { get; set; }

      /// <summary>
      /// The options to use when generating the report
      /// </summary>
      [DataMember(Name = "options")]
      public ReportOptions Options { get; set; }

      /// <summary>
      /// The DocumentDifference object to generate the report based off of.
      /// </summary>
      [DataMember(Name = "differences")]
      public DocumentDifference Differences { get; set; }
   }

   [DataContract]
   public class CompareRequest : Request
   {
      /// <summary>
      /// The IDs of the documents to compare.
      /// </summary>
      [DataMember(Name = "documentIds")]
      public string[] DocumentIds { get; set; }

      /// <summary>
      /// The options to use when comparing
      /// </summary>
      [DataMember(Name = "options")]
      public DocumentCompareOptions Options { get; set; }
   }

   [DataContract]
   public class CompareResponse : Response
   {
      /// <summary>
      /// The IDs of the documents to compare.
      /// </summary>
      [DataMember(Name = "documentDifference")]
      public DocumentDifference DocumentDifference { get; set; }
   }

   [DataContract]
   public class StringCompareRequest : Request
   {
      /// <summary>
      /// The IDs of the documents to compare.
      /// </summary>
      [DataMember(Name = "inputs")]
      public string[] Inputs { get; set; }

      /// <summary>
      /// The options to use when comparing
      /// </summary>
      [DataMember(Name = "options")]
      public DocumentCompareOptions Options { get; set; }
   }

   [DataContract]
   public class StringCompareResponse : Response
   {
      /// <summary>
      /// The IDs of the documents to compare.
      /// </summary>
      [DataMember(Name = "textDifferences")]
      public IList<TextDifference> TextDifferences { get; set; }
   }

   [DataContract]
   public class RunCompareJobRequest : Request
   {
      /// <summary>
      /// The IDs of the documents to compare.
      /// </summary>
      [DataMember(Name = "jobData")]
      public ServiceCompareJobData JobData { get; set; }
   }

   [DataContract]
   public class RunCompareJobResponse : RunConvertJobResponse { }

   [DataContract]
   public class QueryCompareJobRequest : QueryConvertJobStatusRequest { }

   [DataContract]
   public class DeleteCompareJobRequest : DeleteConvertJobStatusRequest { }

   [DataContract]
   public class AbortCompareJobRequest : AbortConvertJobRequest { }

   [DataContract]
   public class QueryCompareJobResponse : Response
   {
      /// <summary>
      /// The IDs of the documents to compare.
      /// </summary>
      [DataMember(Name = "jobData")]
      public CompareJobData JobData { get; set; }
   }


   [DataContract]
   public class RasterCompareRequest : Request
   {
      /// <summary>
      /// The document ID for the original document.
      /// </summary>
      [DataMember(Name = "originalDocumentId")]
      public string OriginalDocumentId { get; set; }

      /// <summary>
      /// The page number in the original document to compare.
      /// </summary>
      [DataMember(Name = "originalPageNumber")]
      public int OriginalPageNumber { get; set; }

      /// <summary>
      /// The Offset for the original document page
      /// </summary>
      [DataMember(Name = "originalOffset")]
      public LeadPointD OriginalOffset { get; set; }

      /// <summary>
      /// The angle to rotate the page.
      /// </summary>
      [DataMember(Name = "originalRotationAngle")]
      public int OriginalRotationAngle { get; set; }

      /// <summary>
      /// The document ID for the Modified document.
      /// </summary>
      [DataMember(Name = "modifiedDocumentId")]
      public string ModifiedDocumentId { get; set; }

      /// <summary>
      /// The page number in the Modified document to compare.
      /// </summary>
      [DataMember(Name = "modifiedPageNumber")]
      public int ModifiedPageNumber { get; set; }

      /// <summary>
      /// The Offset for the Modified document page
      /// </summary>
      [DataMember(Name = "modifiedOffset")]
      public LeadPointD ModifiedOffset { get; set; }

      /// <summary>
      /// The angle to rotate the page.
      /// </summary>
      [DataMember(Name = "modifiedRotationAngle")]
      public int ModifiedRotationAngle { get; set; }

      /// <summary>
      /// Threshold value to use when comparing pixel color values.
      /// </summary>
      [DataMember(Name = "threshold")]
      public int Threshold { get; set; }

      /// <summary>
      /// The expected background color in the original image
      /// </summary>
      [DataMember(Name = "originalBackground")]
      public string OriginalBackground { get; set; }

      /// <summary>
      /// The expected foreground color in the original image
      /// </summary>
      [DataMember(Name = "originalForeground")]
      public string OriginalForeground { get; set; }

      /// <summary>
      /// The background color in the modified image.
      /// </summary>
      [DataMember(Name = "modifiedBackground")]
      public string ModifiedBackground { get; set; }

      /// <summary>
      /// The forground color in the modified image
      /// </summary>
      [DataMember(Name = "modifiedForeground")]
      public string ModifiedForeground { get; set; }

      /// <summary>
      /// The color for external sections in the output image
      /// </summary>
      [DataMember(Name = "outputExternal")]
      public string OutputExternal { get; set; }

      /// <summary>
      /// The color for the background in the output image
      /// </summary>
      [DataMember(Name = "outputBackground")]
      public string OutputBackground { get; set; }

      /// <summary>
      /// The color for matching sections in the output image.
      /// </summary>
      [DataMember(Name = "outputMatch")]
      public string OutputMatch { get; set; }

      /// <summary>
      /// The color for added sections in the output image
      /// </summary>
      [DataMember(Name = "outputAddition")]
      public string OutputAddition { get; set; }

      /// <summary>
      /// The color for deleted sections in the output image
      /// </summary>
      [DataMember(Name = "outputDeletion")]
      public string OutputDeletion { get; set; }

      /// <summary>
      /// The color for changed sections in the output image
      /// </summary>
      [DataMember(Name = "outputChange")]
      public string OutputChange { get; set; }

      /// <summary>
      /// Optional: The document ID for the output document.
      /// </summary>
      [DataMember(Name = "outputDocumentId")]
      public string OutputDocumentId { get; set; }

      /// <summary>
      /// Optional: The output document name
      /// </summary>
      [DataMember(Name = "outputDocumentName")]
      public string OutputDocumentName { get; set; }
   }

   [DataContract]
   public class RasterCompareResponse : Response
   {
      /// <summary>
      /// ID to the output document generated.
      /// </summary>
      [DataMember(Name = "outputDocumentId")]
      public string OutputDocumentId { get; set; }
   }

   [DataContract]
   public class GetCompareImageRequest : Request
   {
      /// <summary>
      /// The document to get the image from.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The page from which to get the image.
      /// </summary>
      [DataMember(Name = "pageNumber")]
      public int PageNumber { get; set; }


      /// <summary>
      /// Background color to set.
      /// </summary>
      [DataMember(Name = "backgroundColor")]
      public string BackgroundColor { get; set; }

      /// <summary>
      /// Foreground color to set.
      /// </summary>
      [DataMember(Name = "foregroundColor")]
      public string ForegroundColor { get; set; }
   }
}
