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
using Leadtools.Document.Compare;
using Leadtools.Document.Writer;

namespace Leadtools.Services.Models.Document
{
   [DataContract]
   public class DecryptRequest : Request
   {
      /// <summary>
      /// The document's identification number.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The attempted password for the document.
      /// </summary>
      [DataMember(Name = "password")]
      public string Password { get; set; }
   }

   [DataContract]
   public class DecryptResponse : Response
   {
      /// <summary>
      /// The decrypted document's information.
      /// </summary>
      [DataMember(Name = "document")]
      public LEADDocument Document { get; set; }
   }

   [DataContract]
   public class ConvertRequest : Request
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
   public class ConvertItem
   {
      /// <summary>
      /// The user-friendly name of the item being returned, for downloading.
      /// </summary>
      [DataMember(Name = "name")]
      public string Name { get; set; }

      /// <summary>
      /// The URL of the item being returned, for downloading.
      /// </summary>
      [DataMember(Name = "url")]
      public Uri Url { get; set; }

      /// <summary>
      /// The mimetype of the converted item.
      /// </summary>
      [DataMember(Name = "mimeType")]
      public string MimeType { get; set; }

      /// <summary>
      /// The file length, in bytes, of the converted item.
      /// </summary>
      [DataMember(Name = "length")]
      public long Length { get; set; }
   }

   [DataContract]
   public class ConvertResponse : Response
   {
      /// <summary>
      /// The cache ID of the item (only used for documents)
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// If the item had to be served as one archived file, it exists here.
      /// </summary>
      [DataMember(Name = "archive")]
      public ConvertItem Archive { get; set; }

      /// <summary>
      /// The newly converted document, if it was not archived.
      /// </summary>
      [DataMember(Name = "document")]
      public ConvertItem Document { get; set; }

      /// <summary>
      /// The converted annotations, if not archived.
      /// </summary>
      [DataMember(Name = "annotations")]
      public ConvertItem Annotations { get; set; }
   }

   [DataContract]
   public class GetHistoryRequest : Request
   {
      /// <summary>
      /// The ID of the document to get the history of.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// Whether or not to clear the history after retrieving it.
      /// </summary>
      [DataMember(Name = "clearHistory")]
      public bool ClearHistory { get; set; }
   }

   [DataContract]
   public class GetHistoryResponse : Response
   {
      /// <summary>
      /// The document history items.
      /// </summary>
      [DataMember(Name = "items")]
      public DocumentHistoryItem[] Items { get; set; }
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

#if LEADTOOLS_V22_OR_LATER
   [DataContract]
   public class GetDocumentEditableContentRequest : Request
   {
      /// <summary>
      /// The ID of the document to edit.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }
   }

   [DataContract]
   public class SetDocumentEditableContentRequest : Request
   {
      /// <summary>
      /// The ID of the document to edit.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The editable content for the document editor.
      /// </summary>
      [DataMember(Name = "editableContent")]
      public string EditableContent { get; set; }
   }

   [DataContract]
   public class SetDocumentEditableContentResponse : Response
   {
   }

   [DataContract]
   public class ConvertDocumentEditableContentRequest : Request
   {
      /// <summary>
      /// The ID of the document to edit.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The updated editable content.
      /// </summary>
      [DataMember(Name = "editableContentUri")]
      public string EditableContentUri { get; set; }

      /// <summary>
      /// The document name.
      /// </summary>
      [DataMember(Name = "documentName")]
      public string DocumentName { get; set; }

      /// <summary>
      /// The Document format to use
      /// </summary>
      [DataMember(Name = "documentFormat")]
      public int DocumentFormat { get; set; }
   }
#endif // #if LEADTOOLS_V22_OR_LATER
}
