// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

using Leadtools.Document;
using Leadtools.Caching;
using Leadtools.Services.Models;

namespace Leadtools.DocumentViewer.Models.Annotations
{
   [DataContract]
   public class SetAnnotationsIBMRequest : Request
   {
      /// <summary>
      /// The ID of the document to set annotations to.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The annotation objects to convert.
      /// </summary>
      [DataMember(Name = "annotations")]
      public IBMAnnotation[] Annotations { get; set; }
   }

   [DataContract]
   public class IBMAnnotation
   {
      /// <summary>
      /// The IBM Annotation as an XML string.
      /// </summary>
      [DataMember(Name = "annotation")]
      public string Annotation { get; set; }

      /// <summary>
      /// The password to lock the object with. If null or empty, it will not be used.
      /// </summary>
      [DataMember(Name = "password")]
      public string Password { get; set; }

      /// <summary>
      /// The user ID to associate with this IBM Annotation.
      /// </summary>
      [DataMember(Name = "userId")]
      public string UserId { get; set; }
   }

   [DataContract]
   public class SetAnnotationsIBMResponse : Response
   {
   }

   [DataContract]
   public class GetAnnotationsIBMRequest : Request
   {
      /// <summary>
      /// The ID of the document to get annotations history from.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }
   }

   [DataContract]
   public class GetAnnotationsIBMResponse : Response
   {
      /// <summary>
      /// Dictionary of added IBM-P8 annotations (where the key is the annotation guid).
      /// </summary>
      [DataMember(Name = "added")]
      public Dictionary<string, string> Added { get; set; }

      /// <summary>
      /// Dictionary of modified IBM-P8 annotations (where the key is the annotation guid).
      /// </summary>
      [DataMember(Name = "modified")]
      public Dictionary<string, string> Modified { get; set; }

      /// <summary>
      /// Dictionary of deleted IBM-P8 annotations (where the key is the annotation guid and value is null).
      /// </summary>
      [DataMember(Name = "deleted")]
      public Dictionary<string, string> Deleted { get; set; }
   }

   [DataContract]
   public class SetAnnotationsLEADRequest : Request
   {
      /// <summary>
      /// The ID of the document to set annotations to.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The annotation objects to convert.
      /// </summary>
      [DataMember(Name = "annotations")]
      public LEADAnnotation[] Annotations { get; set; }
   }

   [DataContract]
   public class LEADAnnotation
   {
      /// <summary>
      /// The LEAD Annotation as an XML string.
      /// </summary>
      [DataMember(Name = "annotation")]
      public string Annotation { get; set; }

      /// <summary>
      /// The password to lock the object with. If null or empty, it will not be used.
      /// </summary>
      [DataMember(Name = "password")]
      public string Password { get; set; }

      /// <summary>
      /// The user ID to associate with this LEAD Annotation.
      /// </summary>
      [DataMember(Name = "userId")]
      public string UserId { get; set; }
   }

   [DataContract]
   public class SetAnnotationsLEADResponse : Response
   {
   }

   [DataContract]
   public class GetAnnotationsLEADRequest : Request
   {
      /// <summary>
      /// The ID of the document to get annotations history from.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }
   }

   [DataContract]
   public class GetAnnotationsLEADResponse : Response
   {
      /// <summary>
      /// Dictionary of added LEAD annotations (where the key is the annotation guid).
      /// </summary>
      [DataMember(Name = "added")]
      public Dictionary<string, string> Added { get; set; }

      /// <summary>
      /// Dictionary of modified LEAD annotations (where the key is the annotation guid).
      /// </summary>
      [DataMember(Name = "modified")]
      public Dictionary<string, string> Modified { get; set; }

      /// <summary>
      /// Dictionary of deleted LEAD annotations (where the key is the annotation guid and value is null).
      /// </summary>
      [DataMember(Name = "deleted")]
      public Dictionary<string, string> Deleted { get; set; }
   }

   [DataContract]
   public class SetAnnotationsRequest : Request
   {
      /// <summary>
      /// The ID of the document to set annotations to.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The annotation objects to convert.
      /// </summary>
      [DataMember(Name = "annotations")]
      public Annotation[] Annotations { get; set; }
   }

   [DataContract]
   public class Annotation
   {
      /// <summary>
      /// Any LEAD compatible annotation as a string.
      /// </summary>
      [DataMember(Name = "data")]
      public string Data { get; set; }

      /// <summary>
      /// The password to lock the object with. If null or empty, it will not be used.
      /// </summary>
      [DataMember(Name = "password")]
      public string Password { get; set; }

      /// <summary>
      /// The user ID to associate with this Annotation.
      /// </summary>
      [DataMember(Name = "userId")]
      public string UserId { get; set; }
   }

   [DataContract]
   public class SetAnnotationsResponse : Response
   {
   }

   [DataContract]
   public class GetAnnotationsRequest : Request
   {
      /// <summary>
      /// The ID of the document to get annotations history from.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }
   }

   [DataContract]
   public class GetAnnotationsResponse : Response
   {
      /// <summary>
      /// Dictionary of added LEAD annotations (where the key is the annotation guid).
      /// </summary>
      [DataMember(Name = "added")]
      public Dictionary<string, string> Added { get; set; }

      /// <summary>
      /// Dictionary of modified LEAD annotations (where the key is the annotation guid).
      /// </summary>
      [DataMember(Name = "modified")]
      public Dictionary<string, string> Modified { get; set; }

      /// <summary>
      /// Dictionary of deleted LEAD annotations (where the key is the annotation guid and value is null).
      /// </summary>
      [DataMember(Name = "deleted")]
      public Dictionary<string, string> Deleted { get; set; }
   }
}
