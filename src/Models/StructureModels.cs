// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Runtime.Serialization;

using Leadtools.Document;
using Leadtools.Services.Models;
using System.Collections.Generic;

namespace Leadtools.DocumentViewer.Models.Structure
{
   [DataContract]
   public class ParseStructureRequest : Request
   {
      /// <summary>
      /// The document to parse.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// Whether or not to parse bookmarks from the document.
      /// </summary>
      [DataMember(Name = "parseBookmarks")]
      public bool ParseBookmarks { get; set; }

      /// <summary>
      /// Whether or not to parse bookmarks from the document.
      /// </summary>
      [DataMember(Name = "parsePageLinks")]
      public bool ParsePageLinks { get; set; }
   }

   [DataContract]
   public class ParseStructureResponse : Response
   {
      /// <summary>
      /// The bookmarks that exist for the document (serialized array of Leadtools.Document.DocumentBookmark).
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
      [DataMember(Name = "bookmarks")]
      public DocumentBookmark[] Bookmarks { get; set; }

      /// <summary>
      /// The page links that exist on the document (serialized array of array of Leadtools.Document.DocumentLink).
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
      [DataMember(Name = "pageLinks")]
      public DocumentLink[][] PageLinks { get; set; }
   }
}
