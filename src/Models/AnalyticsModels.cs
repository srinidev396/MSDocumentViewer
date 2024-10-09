// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using Leadtools.Document;
using Leadtools.Document.Analytics;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Leadtools.Services.Models.Analytics
{
   public delegate ActionElement ActionGenerator();

   public class ActionSet
   {
      [DataMember(Name = "id")]
      public string Id { get; set; }

      [DataMember(Name = "title")]
      public string Title { get; set; }
      [DataMember(Name = "description")]
      public string Description { get; set; }

      [IgnoreDataMember]
      public ActionGenerator generator { get; set; }
   }

   [DataContract]
   public class GetActionSetsRequest : Request
   {
   }

   [DataContract]
   public class GetActionSetsResponse : Response
   {
      /// <summary>
      /// The forms found on the server
      /// </summary>
      [DataMember(Name = "actionSets")]
      public ActionSet[] ActionSets { get; set; }
   }

   [DataContract]
   public class GetRuleSetsRequest : Request
   {
   }


   [DataContract]
   public class GetRuleSetsResponse : Response
   {
      /// <summary>
      /// The forms found on the server
      /// </summary>
      [DataMember(Name = "ruleSets")]
      public RuleSet[] RuleSets { get; set; }
   }

   [DataContract]
   public class RuleSet
   {
      public string File { get; set; }

      [DataMember(Name = "id")]
      public string Id { get; set; }

      [DataMember(Name = "title")]
      public string Title { get; set; }
   }

   public class AnalysisResult
   {
      [DataMember(Name = "elementName")]
      public string ElementName { get; set; }

      [DataMember(Name = "bounds")]
      public LeadRectD[] Bounds { get; set; }

      [DataMember(Name = "value")]
      public string Value { get; set; }

      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      [DataMember(Name = "pageNumber")]
      public int PageNumber { get; set; }

      [DataMember(Name = "isFromOcr")]
      public bool IsFromOcr { get; set; }

      [DataMember(Name = "confidence")]
      public int Confidence { get; set; }
   }

   public class TextAnalysisResult : AnalysisResult
   {
      [DataMember(Name = "documentCharacters")]
      public DocumentCharacter[] DocumentCharacters { get; set; }
   }

   [DataContract]
   public class RunAnalysisRequest: Request
   {
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      [DataMember(Name = "elements")]
      public string Elements { get; set; }

      [DataMember(Name = "ruleSetIds")]
      public string[] RuleSetIds { get; set; }

      [DataMember(Name = "firstPageNumber")]
      public int FirstPageNumber { get; set; }

      [DataMember(Name = "lastPageNumber")]
      public int LastPageNumber { get; set; }

      // Return the results in RunResponse.Results
      [DataMember(Name = "returnResults")]
      public bool ReturnResults { get; set; }

      [DataMember(Name = "actionIds")]
      public string[] ActionIds { get; set; }
   }

   [DataContract]
   public class RunAnalysisResponse : Response
   {
      [DataMember(Name = "results")]
      public AnalysisResult[] Results { get; set; }
   }

   public class ApplyActionsRequest : Request
   {
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      [DataMember(Name = "actionIds")]
      public string[] ActionIds { get; set; }

      [DataMember(Name = "results")]
      public TextAnalysisResult[] Results { get; set; }
   }
}
