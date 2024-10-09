// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using Leadtools.Services.Models;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Leadtools.DocumentViewer.Models.FormFields
{
   [DataContract]
   public class SetFormFieldsRequest : Request
   {
      /// <summary>
      /// The ID of the document to edit.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The Document form fields containers
      /// </summary>
      [DataMember(Name = "formFieldsContainers")]
      public string FormFieldsContainers { get; set; }
   }

   [DataContract]
   public class SetFormFieldsResponse : Response
   {
   }

   [DataContract]
   public class SetFormFieldResourcesRequest : Request
   {
      /// <summary>
      /// The ID of the document to edit.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The Document form fields containers
      /// </summary>
      [DataMember(Name = "formFieldResources")]
      public string FormFieldResources { get; set; }
   }

   [DataContract]
   public class SetFormFieldResourcesResponse : Response
   {
   }
}
