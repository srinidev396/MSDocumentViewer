// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Leadtools.DocumentViewer.Tools.Helpers;
using Leadtools.Services.Models;
using Leadtools;
using Leadtools.Barcode;
using Leadtools.Document;

namespace Leadtools.DocumentViewer.Models.Multimedia
{
   [DataContract]
   public class GetVideoRequest : Request
   {
      /// <summary>
      /// The url to retrieve the video.
      /// </summary>
      [DataMember(Name = "url")]
      public string url { get; set; }

      /// <summary>
      /// The preferred video mime type.
      /// </summary>
      [DataMember(Name = "mime")]
      public string mime { get; set; }


   }

   [DataContract]
   public class ConvertVideoRequest : Request
   {
      /// <summary>
      /// The url to retrieve the video.
      /// </summary>
      [DataMember(Name = "url")]
      public string url { get; set; }

      /// <summary>
      /// The target video mime type.
      /// </summary>
      [DataMember(Name = "mime")]
      public string mime { get; set; }


      /// <summary>
      /// Specifies the operation to perform. THis is used to upload the video in chunks.
      /// </summary>
      [DataMember(Name = "operation")]
      public string operation { get; set; }
   }

   [DataContract]
   public class ConvertVideoResponse : Response
   {
      /// <summary>
      /// The cache url to retrieve the video.
      /// </summary>
      [DataMember(Name = "url")]
      public string url { get; set; }


   }

   [DataContract]
   public class DeleteCachedVideoRequest : Request
   {
      /// <summary>
      /// The cached video url to delete.
      /// </summary>
      [DataMember(Name = "url")]
      public string url { get; set; }

      /// <summary>
      /// The mime type of the video to remove. If not specifiied, then all cached mime types for this video will be removed.
      /// </summary>
      [DataMember(Name = "mime")]
      public string mime { get; set; }

   }

   [DataContract]
   public class DeleteCachedVideoResponse : Response
   {

   }


}
