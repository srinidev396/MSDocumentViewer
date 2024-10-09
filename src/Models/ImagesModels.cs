// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Leadtools.Services.Models;

namespace Leadtools.DocumentViewer.Models.Images
{
   [DataContract]
   public class GetThumbnailsGridRequest : Request
   {
      /// <summary>
      /// The document to get thumbnails from.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The first page number to use. If unspecified, 1 is used.
      /// </summary>
      [DataMember(Name = "firstPageNumber")]
      public int FirstPageNumber { get; set; }

      /// <summary>
      /// The last page number to use. If unspecified, the last page is used.
      /// </summary>
      [DataMember(Name = "lastPageNumber")]
      public int LastPageNumber { get; set; }

      /// <summary>
      /// Optional - the mimetype to use for the images.
      /// Not currently sent by the Leadtools.Document.JavaScript library.
      /// </summary>
      [DataMember(Name = "mimeType")]
      public string MimeType { get; set; }

      /// <summary>
      /// The maximum width for the grid.
      /// The number of columns is determined by dividing this value by the width.
      /// </summary>
      [DataMember(Name = "maximumGridWidth")]
      public int MaximumGridWidth { get; set; }

      /// <summary>
      /// The width of an individual image of the grid. Images are resized to fit
      /// within the width and height provided.
      /// The width is used along with maximumGirdWidth to determine the columns.
      /// </summary>
      [DataMember(Name = "width")]
      public int Width { get; set; }

      /// <summary>
      /// The height of an individual image of the grid. Images are resized to fit
      /// within the width and height provided.
      /// </summary>
      [DataMember(Name = "height")]
      public int Height { get; set; }
   }
}
