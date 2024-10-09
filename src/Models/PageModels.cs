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

namespace Leadtools.DocumentViewer.Models.Page
{
   [DataContract]
   public class GetImageRequest : Request
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
      /// The resolution of the image to use, if different from the default.
      /// Optional; not currently sent by the Leadtools.Document.JavaScript library.
      /// </summary>
      [DataMember(Name = "resolution")]
      public int Resolution { get; set; }

      /// <summary>
      /// The mimetype to load the image as.
      /// Optional; not currently sent by the Leadtools.Document.JavaScript library.
      /// </summary>
      [DataMember(Name = "mimeType")]
      public string MimeType { get; set; }

      /// <summary>
      /// The bits per pixel value to use.
      /// Optional; not currently sent by the Leadtools.Document.JavaScript library.
      /// </summary>
      [DataMember(Name = "bitsPerPixel")]
      public int BitsPerPixel { get; set; }

      /// <summary>
      /// The quality factor used for rendering the image.
      /// Optional; not currently sent by the Leadtools.Document.JavaScript library.
      /// </summary>
      [DataMember(Name = "qualityFactor")]
      public int QualityFactor { get; set; }

      /// <summary>
      /// The width of the image to return.
      /// Optional; not currently sent by the Leadtools.Document.JavaScript library.
      /// </summary>
      [DataMember(Name = "width")]
      public int Width { get; set; }

      /// <summary>
      /// The height of the image to return.
      /// Optional; not currently sent by the Leadtools.Document.JavaScript library.
      /// </summary>
      [DataMember(Name = "height")]
      public int Height { get; set; }
   }

   [DataContract]
   public class GetSvgBackImageRequest : Request
   {
      /// <summary>
      /// The document from which to load the back image.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The page number to load the back image from.
      /// </summary>
      [DataMember(Name = "pageNumber")]
      public int PageNumber { get; set; }

      /// <summary>
      /// The color to use as the background color, such as "transparent".
      /// </summary>
      [DataMember(Name = "backColor")]
      public string BackColor { get; set; }

      /// <summary>
      /// The resolution to use.
      /// Optional; not currently sent by the Leadtools.Document.JavaScript library.
      /// </summary>
      [DataMember(Name = "resolution")]
      public int Resolution { get; set; }

      /// <summary>
      /// The mimetype to load with.
      /// Optional; not currently sent by the Leadtools.Document.JavaScript library.
      /// </summary>
      [DataMember(Name = "mimeType")]
      public string MimeType { get; set; }

      /// <summary>
      /// The bits per pixel to use.
      /// Optional; not currently sent by the Leadtools.Document.JavaScript library.
      /// </summary>
      [DataMember(Name = "bitsPerPixel")]
      public int BitsPerPixel { get; set; }

      /// <summary>
      /// The quality factor to use.
      /// Optional; not currently sent by the Leadtools.Document.JavaScript library.
      /// </summary>
      [DataMember(Name = "qualityFactor")]
      public int QualityFactor { get; set; }

      /// <summary>
      /// The width to use for the image.
      /// Optional; not currently sent by the Leadtools.Document.JavaScript library.
      /// </summary>
      [DataMember(Name = "width")]
      public int Width { get; set; }

      /// <summary>
      /// The height to use for the image.
      /// Optional; not currently sent by the Leadtools.Document.JavaScript library.
      /// </summary>
      [DataMember(Name = "height")]
      public int Height { get; set; }
   }

   [DataContract]
   public class GetThumbnailRequest : Request
   {
      /// <summary>
      /// The ID of the document to get the thumbnail from.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The page number of the thumbnail.
      /// </summary>
      [DataMember(Name = "pageNumber")]
      public int PageNumber { get; set; }

      /// <summary>
      /// The mimetype to use for the thumbnail.
      /// Optional; not currently sent by the Leadtools.Document.JavaScript library.
      /// </summary>
      [DataMember(Name = "mimeType")]
      public string MimeType { get; set; }

      /// <summary>
      /// The width to return for the thumbnail.
      /// </summary>
      [DataMember(Name = "width")]
      public int Width { get; set; }

      /// <summary>
      /// The height to return for the thumbnail.
      /// </summary>
      [DataMember(Name = "height")]
      public int Height { get; set; }
   }

   /// <summary>
   /// An enumeration used for determining how to return an SVG from the service.
   /// </summary>
   [DataContract]
   [Flags]
   public enum DocumentGetSvgOptions
   {
      None = 0,
      /// <summary>
      /// Should polyline text be allowed?
      /// </summary>
      AllowPolylineText = 1 << 0,
      /// <summary>
      /// The created svg will have the text as paths. Default is the output file may have text as both text spans and path objects depending on the input document. Currently
      /// supported by PDF documents only
      /// </summary>
      ForceTextPath = 1 << 0,
      /// <summary>
      /// Should any images be dropped?
      /// </summary>
      DropImages = 1 << 1,
      /// <summary>
      /// Should any shapes be dropped?
      /// </summary>
      DropShapes = 1 << 2,
      /// <summary>
      /// Should any text be dropped?
      /// </summary>
      DropText = 1 << 3,
      /// <summary>
      /// Is this SVG for conversion?
      /// </summary>
      ForConversion = 1 << 4,
      /// <summary>
      /// Should XML parsing errors be ignored?
      /// </summary>
      IgnoreXmlParsingErrors = 1 << 5,
      /// <summary>
      /// The created svg will have the text as real text spans. Default is the output file may have text as both text spans and path objects depending on the input document.
      /// Currently supported by PDF documents only
      /// </summary>
      ForceRealText = 1 << 6
   }

   [DataContract]
   public class GetSvgRequest : Request
   {
      /// <summary>
      /// The ID of the document to load the SVG from.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The page number from which to load the SVG.
      /// </summary>
      [DataMember(Name = "pageNumber")]
      public int PageNumber { get; set; }

      /// <summary>
      /// The SVG options.
      /// </summary>
      [DataMember(Name = "options")]
      public DocumentGetSvgOptions Options { get; set; }

      /// <summary>
      /// If true, images will be unembedded.
      /// </summary>
      [DataMember(Name = "unembedImages")]
      public bool UnembedImages { get; set; }
   }

   [DataContract]
   public class GetTextRequest : Request
   {
      /// <summary>
      /// The ID of the document to get text for.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The number of the page text should be parsed from.
      /// </summary>
      [DataMember(Name = "pageNumber")]
      public int PageNumber { get; set; }

      /// <summary>
      /// Clipping rectangle, of type Leadtools.LeadRectD. Use LeadRectD.Empty to get the text for the whole page.
      /// </summary>
      [DataMember(Name = "clip")]
      public LeadRectD Clip { get; set; }

      /// <summary>
      /// The type of text extraction to use (serialized Leadtools.Document.DocumentTextExtractionMode)
      /// </summary>
      [DataMember(Name = "textExtractionMode")]
      public DocumentTextExtractionMode TextExtractionMode { get; set; }

      /// <summary>
      /// The type of text extraction to use (serialized Leadtools.Document.DocumentTextExtractionMode)
      /// </summary>
      [DataMember(Name = "imagesRecognitionMode")]
      public DocumentTextImagesRecognitionMode ImagesRecognitionMode { get; set; }

      /// <summary>
      /// Build and populate DocumentPageText.Words in the response PageText
      /// </summary>
      [DataMember(Name = "buildWords")]
      public bool BuildWords { get; set; }

      /// <summary>
      /// Build and populate DocumentPageText.Text in the response PageText
      /// </summary>
      [DataMember(Name = "buildText")]
      public bool BuildText { get; set; }

      /// <summary>
      /// Build and populate DocumentPageText.TextMap in the response PageText
      /// </summary>
      [DataMember(Name = "buildTextWithMap")]
      public bool BuildTextWithMap { get; set; }
   }

   [DataContract]
   public class GetTextResponse : Response
   {
      /// <summary>
      /// The page text that was processed (serialized Leadtools.Document.DocumentPageText).
      /// </summary>
      [DataMember(Name = "pageText")]
      public DocumentPageText PageText { get; set; }
   }

   [DataContract]
   public class ReadBarcodesRequest : Request
   {
      /// <summary>
      /// The ID of the document to read barcodes from.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The page number of the barcodes.
      /// </summary>
      [DataMember(Name = "pageNumber")]
      public int PageNumber { get; set; }

      /// <summary>
      /// The bounds in which to check for barcodes (serialized Leadtools.LeadRectD).
      /// </summary>
      [DataMember(Name = "bounds")]
      public LeadRectD Bounds { get; set; }

      /// <summary>
      /// The maximum number of barcodes to read (0 for all).
      /// </summary>
      [DataMember(Name = "maximumBarcodes")]
      public int MaximumBarcodes { get; set; }

      /// <summary>
      /// The list of symbologies to check against. If null, all known symbologies will be tested (array of serialized Leadtools.Barcode.BarcodeSymbology)
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
      [DataMember(Name = "symbologies")]
      public BarcodeSymbology[] Symbologies { get; set; }
   }

   [DataContract]
   public class ReadBarcodesResponse : Response
   {
      /// <summary>
      /// The found barcodes (array of serialized Leadtools.Barcode.BarcodeData)
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
      [DataMember(Name = "barcodes")]
      public BarcodeData[] Barcodes { get; set; }
   }

   [DataContract]
   public class GetAnnotationsRequest : Request
   {
      /// <summary>
      /// The ID of the document to get annotations from.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The page number of the document.
      /// </summary>
      [DataMember(Name = "pageNumber")]
      public int PageNumber { get; set; }

      /// <summary>
      /// If true, empty annotations will be created if none existed for the page.
      /// </summary>
      [DataMember(Name = "createEmpty")]
      public bool CreateEmpty { get; set; }
   }

   [DataContract]
   public class GetAnnotationsResponse : Response
   {
      /// <summary>
      /// The annotations, as a string.
      /// </summary>
      [DataMember(Name = "annotations")]
      public string Annotations { get; set; }
   }

   [DataContract]
   public class SetAnnotationsRequest : Request
   {
      /// <summary>
      /// The ID of the document to set annotations for.
      /// </summary>
      [DataMember(Name = "documentId")]
      public string DocumentId { get; set; }

      /// <summary>
      /// The page number.
      /// </summary>
      [DataMember(Name = "pageNumber")]
      public int PageNumber { get; set; }

      /// <summary>
      /// The annotations, as a string.
      /// </summary>
      [DataMember(Name = "annotations")]
      public string Annotations { get; set; }
   }

}
