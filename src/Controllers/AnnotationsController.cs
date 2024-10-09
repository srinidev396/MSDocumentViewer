// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Diagnostics;

using Leadtools.Document;
using Leadtools.Demos;
using Leadtools.Codecs;
using Leadtools.Caching;

using Leadtools.DocumentViewer.Models.Factory;
using Leadtools.Services.Tools.Exceptions;
using Leadtools.Services.Tools.Helpers;
using Leadtools.Services.Models;
using Leadtools.Services.Models.PreCache;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.IO.Packaging;
using Leadtools.DocumentViewer.Models.Annotations;
using Leadtools.Annotations.Engine;
using Microsoft.AspNetCore.Http;

namespace Leadtools.DocumentViewer.Controllers
{
   /// <summary>
   /// Used for setting/getting annotations with external applications.
   /// </summary>
   public class AnnotationsController : Controller
   {
      public AnnotationsController()
      {
         ServiceHelper.InitializeController();
      }

      /*
       * #REF Annotations_Support
       * 
       * The below endpoints (SetAnnotations, GetAnnotations) are used to upload and get Annotations (LEAD, IBM) XML to LEADTOOLS AnnObject instances
       * for use with the LEADTOOLS DocumentViewer.
       * 
       */

      /// <summary>
      ///  Sets annotations into the document.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The annotations could not be set")]
      [HttpPost("api/[controller]/[action]")]
      public SetAnnotationsResponse SetAnnotations(SetAnnotationsRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         // Must have the documentId you'd like to add annotations to.
         // If you only have the document cache URI, DocumentFactory.LoadFromUri needs to be called.
         if (string.IsNullOrEmpty(request.DocumentId))
            throw new ArgumentNullException("documentId");

         // Check that we have annotations.
         if (request.Annotations == null)
            throw new ArgumentNullException("annotations");

         DoSetAnnotations(this.Request, request);

         SetAnnotationsResponse response = new SetAnnotationsResponse();
         return response;
      }

      /// <summary>
      ///  Gets changed annotations from the document.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The annotations could not be retrieved")]
      [HttpPost("api/[controller]/[action]")]
      public GetAnnotationsResponse GetAnnotations(GetAnnotationsRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         // Must have the documentId you'd like to add annotations to.
         // If you only have the document cache URI, DocumentFactory.LoadFromUri needs to be called.
         if (string.IsNullOrEmpty(request.DocumentId))
            throw new ArgumentNullException("documentId");

         // Use the common DoGetAnnotations
         Dictionary<AnnHistoryItemState, Dictionary<string, string>> annotationItems = DoGetAnnotations(this.Request, request);

         GetAnnotationsResponse response = new GetAnnotationsResponse();
         if (annotationItems != null)
         {
            if (annotationItems.ContainsKey(AnnHistoryItemState.Added))
               response.Added = annotationItems[AnnHistoryItemState.Added];
            if (annotationItems.ContainsKey(AnnHistoryItemState.Modified))
               response.Modified = annotationItems[AnnHistoryItemState.Modified];
            if (annotationItems.ContainsKey(AnnHistoryItemState.Deleted))
               response.Deleted = annotationItems[AnnHistoryItemState.Deleted];
         }

         return response;
      }

      /*
       * #REF LEAD_Annotations_Support
       * 
       * The below endpoints (SetAnnotationsLEAD, GetAnnotationsLEAD) are used to upload and get LEAD annotations XML to LEADTOOLS AnnObject instances
       * for use with the LEADTOOLS DocumentViewer.
       * 
       */

      /// <summary>
      ///  Sets LEAD annotations into the document.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The LEAD annotations could not be set")]
      [HttpPost("api/[controller]/[action]")]
      public SetAnnotationsLEADResponse SetAnnotationsLEAD(SetAnnotationsLEADRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         // Must have the documentId you'd like to add annotations to.
         // If you only have the document cache URI, DocumentFactory.LoadFromUri needs to be called.
         if (string.IsNullOrEmpty(request.DocumentId))
            throw new ArgumentNullException("documentId");

         // Check that we have annotations.
         if (request.Annotations == null)
            throw new ArgumentNullException("annotations");

         // Use the common DoSetAnnotations
         SetAnnotationsRequest setAnnotationsRequest = new SetAnnotationsRequest();
         setAnnotationsRequest.DocumentId = request.DocumentId;
         setAnnotationsRequest.Annotations = new Annotation[request.Annotations.Length];
         for (int i = 0; i < request.Annotations.Length; i++)
         {
            Annotation target = null;
            LEADAnnotation source = request.Annotations[i];
            if (source != null)
            {
               target = new Annotation();
               target.Data = source.Annotation;
               target.Password = source.Password;
               target.UserId = source.UserId;
            }

            setAnnotationsRequest.Annotations[i] = target;
         }

         DoSetAnnotations(this.Request, setAnnotationsRequest);

         SetAnnotationsLEADResponse response = new SetAnnotationsLEADResponse();
         return response;
      }

      /// <summary>
      ///  Gets changed LEAD annotations from the document.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The LEAD annotations could not be retrieved")]
      [HttpPost("api/[controller]/[action]")]
      public GetAnnotationsLEADResponse GetAnnotationsLEAD(GetAnnotationsLEADRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         // Must have the documentId you'd like to add annotations to.
         // If you only have the document cache URI, DocumentFactory.LoadFromUri needs to be called.
         if (string.IsNullOrEmpty(request.DocumentId))
            throw new ArgumentNullException("documentId");

         // Use the common DoGetAnnotations
         GetAnnotationsRequest getAnnotationsRequest = new GetAnnotationsRequest();
         getAnnotationsRequest.DocumentId = request.DocumentId;
         Dictionary<AnnHistoryItemState, Dictionary<string, string>> annotationItems = DoGetAnnotations(this.Request, getAnnotationsRequest);

         GetAnnotationsLEADResponse response = new GetAnnotationsLEADResponse();
         if (annotationItems != null)
         {
            if (annotationItems.ContainsKey(AnnHistoryItemState.Added))
               response.Added = annotationItems[AnnHistoryItemState.Added];
            if (annotationItems.ContainsKey(AnnHistoryItemState.Modified))
               response.Modified = annotationItems[AnnHistoryItemState.Modified];
            if (annotationItems.ContainsKey(AnnHistoryItemState.Deleted))
               response.Deleted = annotationItems[AnnHistoryItemState.Deleted];
         }

         return response;
      }

      /*
       * #REF IBM_Annotations_Support
       * 
       * The below endpoints (SetAnnotationsIBM, GetAnnotationsIBM) are used to convert IBMP8 annotations XML to LEADTOOLS AnnObject instances
       * for use with the LEADTOOLS DocumentViewer. Due to differences between the IBMP8 spec and LEADTOOLS, certain LEADTOOLS Annotations
       * objects are not yet supported for conversion to/from IBMP8.
       * 
       * A rough list of support is below, along with the Id (from AnnObject.Id).
       * 
       * See the GetAnnotationsIBM endpoint for information on the response.
       * 
       * Supported:
       *    Line (-2),
       *    Rectangle (-3),
       *    Ellipse (-4),
       *    Polyline (-5),
       *    Polygon (-6),
       *    Pointer (-9),
       *    Freehand (-10),
       *    Hilite (-11),
       *    Text (-12),
       *    Note (-15),
       *    Stamp (-16),
       *    StickyNote (-32),
       * 
       * Partially Supported:
       *    TextRollup (-13) becomes a Text (-12) object during conversion to IBM.
       *    TextPointer (-14) becomes a Text (-12) object during conversion to IBM.
       *    Redaction (-22) becomes an opaque Rectangle (-3) object during conversion to IBM.
       * 
       * Unsupported:
       *    Curve (-7),
       *    ClosedCurve (-8),
       *    RubberStamp (-17),
       *    Hotspot (-18),
       *    FreehandHotspot (-19),
       *    Button (-20),
       *    Point (-21),
       *    Ruler (-23),
       *    PolyRuler (-24),
       *    Protractor (-25),
       *    CrossProduct (-26),
       *    Encrypt (-27),
       *    Audio (-28),
       *    RichText (-29),
       *    Media (-30),
       *    Image (-31),
       *    TextHilite (-33),
       *    TextStrikeout (-34),
       *    TextUnderline (-35),
       *    TextRedaction (-36)
       */

      /// <summary>
      ///  Sets IBM P8 annotations into the document.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The IBM annotations could not be set")]
      [HttpPost("api/[controller]/[action]")]
      public SetAnnotationsIBMResponse SetAnnotationsIBM(SetAnnotationsIBMRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         // Must have the documentId you'd like to add annotations to.
         // If you only have the document cache URI, DocumentFactory.LoadFromUri needs to be called.
         if (string.IsNullOrEmpty(request.DocumentId))
            throw new ArgumentNullException("documentId");

         // Check that we have annotations.
         if (request.Annotations == null)
            throw new ArgumentNullException("annotations");

         // Use the common DoSetAnnotations
         SetAnnotationsRequest setAnnotationsRequest = new SetAnnotationsRequest();
         setAnnotationsRequest.DocumentId = request.DocumentId;
         setAnnotationsRequest.Annotations = new Annotation[request.Annotations.Length];
         for (int i = 0; i < request.Annotations.Length; i++)
         {
            Annotation target = null;
            IBMAnnotation source = request.Annotations[i];
            if (source != null)
            {
               target = new Annotation();
               target.Data = source.Annotation;
               target.Password = source.Password;
               target.UserId = source.UserId;
            }

            setAnnotationsRequest.Annotations[i] = target;
         }

         DoSetAnnotations(this.Request, setAnnotationsRequest);

         SetAnnotationsIBMResponse response = new SetAnnotationsIBMResponse();
         return response;
      }

      /// <summary>
      ///  Gets changed IBM P8 annotations from the document.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The IBM annotations could not be retrieved")]
      [HttpPost("api/[controller]/[action]")]
      public GetAnnotationsIBMResponse GetAnnotationsIBM(GetAnnotationsIBMRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         // Must have the documentId you'd like to add annotations to.
         // If you only have the document cache URI, DocumentFactory.LoadFromUri needs to be called.
         if (string.IsNullOrEmpty(request.DocumentId))
            throw new ArgumentNullException("documentId");

         // The IBM annotations will be stored here (key: GUID, value: XML string).
         Dictionary<AnnHistoryItemState, Dictionary<string, string>> annotationItems = null;

         ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.DocumentId);
         var loadFromCacheOptions = new LoadFromCacheOptions
         {
            Cache = cache,
            DocumentId = request.DocumentId,
            UserToken = ServiceHelper.GetUserToken(this.Request.Headers, request)
         };
         using (var document = DocumentFactory.LoadFromCache(loadFromCacheOptions))
         {
            // Ensure we have the document.
            DocumentHelper.CheckLoadFromCache(document);

            // Use the history to return all added, modified, and deleted IBM annotations.
            annotationItems = GetAnnotationsIBMHistory(document);

         }

         // Here (if defined) we send the converted annotations to disk as an example.
         if (annotationItems != null && _processToDiskRoot != null)
         {
            try
            {
               string dir = _processToDiskRoot + "/" + request.DocumentId;
               ProcessToDisk(dir, annotationItems);
            }
            catch (Exception e)
            {
               Trace.WriteLine(string.Format("Failed to process converted annotations to disk: {0}", e.Message));
            }
         }

         GetAnnotationsIBMResponse response = new GetAnnotationsIBMResponse();
         if (annotationItems != null)
         {
            // See #REF IBM_Annotations_Support
            if (annotationItems.ContainsKey(AnnHistoryItemState.Added))
               response.Added = annotationItems[AnnHistoryItemState.Added];
            if (annotationItems.ContainsKey(AnnHistoryItemState.Modified))
               response.Modified = annotationItems[AnnHistoryItemState.Modified];
            if (annotationItems.ContainsKey(AnnHistoryItemState.Deleted))
               response.Deleted = annotationItems[AnnHistoryItemState.Deleted];
         }

         return response;
      }

      private static void DoSetAnnotations(HttpRequest httpRequest, SetAnnotationsRequest request)
      {
         ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.DocumentId);
         var loadFromCacheOptions = new LoadFromCacheOptions
         {
            Cache = cache,
            DocumentId = request.DocumentId,
            UserToken = ServiceHelper.GetUserToken(httpRequest.Headers, request)
         };

         using (LEADDocument document = DocumentFactory.LoadFromCache(loadFromCacheOptions))
         {
            // Ensure we have the document.
            DocumentHelper.CheckLoadFromCache(document);

            // Get the annotations from the request.
            Annotation[] annotationItems = request.Annotations;
            if (annotationItems != null && annotationItems.Length > 0)
            {
               SetAnnotationsHistory(document, annotationItems);
            }

            // Enable history tracking from this point forward so that calls to retrieve the Annotations will have a history from this set operation.
            document.History.AutoUpdate = true;

            // Clear any old history.
            AnnHistory history = document.Annotations.GetHistory();
            if (history != null)
            {
               history.Clear();
               document.Annotations.SetHistory(history);
            }
            document.SaveToCache();
         }
      }

      private Dictionary<AnnHistoryItemState, Dictionary<string, string>> DoGetAnnotations(HttpRequest httpRequest, GetAnnotationsRequest request)
      {
         // The annotations will be stored here (key: GUID, value: XML string).
         Dictionary<AnnHistoryItemState, Dictionary<string, string>> annotationItems = null;

         ObjectCache cache =  ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.DocumentId);
         var loadFromCacheOptions = new LoadFromCacheOptions
         {
            Cache = cache,
            DocumentId = request.DocumentId,
            UserToken = ServiceHelper.GetUserToken(httpRequest.Headers, request)
         };
         using (var document = DocumentFactory.LoadFromCache(loadFromCacheOptions))
         {
            // Ensure we have the document.
            DocumentHelper.CheckLoadFromCache(document);

            // Use the history to return all added, modified, and deleted annotations.
            annotationItems = GetAnnotationsLEADHistory(document);

         }

         // Here (if defined) we send the converted annotations to disk as an example.
         if (annotationItems != null && _processToDiskRoot != null)
         {
            try
            {
               string dir = _processToDiskRoot + "/" + request.DocumentId;
               ProcessToDisk(dir, annotationItems);
            }
            catch (Exception e)
            {
               Trace.WriteLine(string.Format("Failed to process converted annotations to disk: {0}", e.Message));
            }
         }

         return annotationItems;
      }

      // For debugging - if true, annotations history will be logged to the console
      private static bool _logAnnotationsHistory = false;

      // For processing - as an example, write to disk.
      private static string _processToDiskRoot = null;

      private static void SetAnnotationsHistory(LEADDocument document, Annotation[] annotationItems)
      {
         // If the document is read-only then we won't be able to modify its settings. Temporarily change this state.
         bool wasReadOnly = document.IsReadOnly;
         document.IsReadOnly = false;

         // We have all the objects loaded; now, add them to their respective page containers.
         Dictionary<int, AnnContainer> modifiedContainers = new Dictionary<int, AnnContainer>();

         AnnRenderingEngine annRenderingEngine = null;

         for (int annotationItemIndex = 0; annotationItemIndex < annotationItems.Length; annotationItemIndex++)
         {
            // Any Get this item
            Annotation annotationItem = annotationItems[annotationItemIndex];
            if (annotationItem == null || string.IsNullOrEmpty(annotationItem.Data))
               continue;

            try
            {
               // Check the format
               AnnotationFormat format = GetAnnotationFormat(annotationItem);
               if (format == AnnotationFormat.LEAD)
               {
                  AddFromLEADAnnotation(annotationItem, annotationItemIndex, document, modifiedContainers);
               }
               else if (format == AnnotationFormat.IBMP8)
               {
                  if (annRenderingEngine == null)
                     annRenderingEngine = ServiceHelper.GetAnnRenderingEngine();
                  AddFromIBMP8Annotation(annRenderingEngine, annotationItem, annotationItemIndex, document, modifiedContainers);
               }
               else
               {
                  Trace.WriteLine(string.Format("Failed to get the format of Annotation at index {0}", annotationItemIndex));
               }
            }
            catch (Exception e)
            {
               Trace.WriteLine(string.Format("Failed to convert Annotation at index {0}: {1}", annotationItemIndex, e.Message));
            }
         }

         // Set the modified containers in the document.
         AnnContainer[] containers = modifiedContainers.Values.ToArray();
         document.Annotations.SetAnnotations(containers);

         // Reset the read-only value from above before saving into the cache.
         document.IsReadOnly = wasReadOnly;
      }

      private static void AddFromLEADAnnotation(Annotation annotationItem, int annotationItemIndex, LEADDocument document, Dictionary<int, AnnContainer> modifiedContainers)
      {
         // Load it
         var annCodecs = new AnnCodecs();

         AnnContainer[] annContainers = annCodecs.LoadAllFromString(annotationItem.Data);
         if (annContainers == null || annContainers.Length == 0)
            return;

         foreach (AnnContainer annContainer in annContainers)
         {
            // Before adding, get the target page number.
            int pageNumber = annContainer.PageNumber;

            // Make sure the page exists.
            // If zero, set to page 1; if outside of the document range, disregard it.
            if (pageNumber == 0)
            {
               pageNumber = 1;
            }
            else if (pageNumber > document.Pages.Count)
            {
               return;
            }

            // Get its container (one per page) and add the object to it.
            DocumentPage documentPage = document.Pages[pageNumber - 1];

            AnnContainer container = null;
            if (modifiedContainers.ContainsKey(pageNumber))
            {
               container = modifiedContainers[pageNumber];
            }
            else
            {
               container = documentPage.GetAnnotations(true);
               modifiedContainers.Add(pageNumber, container);
            }

            // Add the objects
            foreach (AnnObject annObj in annContainer.Children)
            {
               // Add the supplied properties
               if (!string.IsNullOrEmpty(annotationItem.Password))
               {
                  annObj.Lock(annotationItem.Password);
               }
               if (!string.IsNullOrEmpty(annotationItem.UserId))
               {
                  annObj.UserId = annotationItem.UserId;
                  Dictionary<string, string> metadata = annObj.Metadata;
                  string key = AnnObject.AuthorMetadataKey;
                  if (metadata.ContainsKey(key))
                  {
                     if (string.IsNullOrEmpty(metadata[key]))
                        metadata[key] = annotationItem.UserId;
                  }
                  else
                  {
                     metadata.Add(key, annotationItem.UserId);
                  }
               }
               container.Children.Add(annObj);
            }
         }
      }

      private static void AddFromIBMP8Annotation(AnnRenderingEngine annRenderingEngine, Annotation annotationItem, int annotationItemIndex, LEADDocument document, Dictionary<int, AnnContainer> modifiedContainers)
      {
         // Before converting, get the target page number.
         string ibmPageNumberValue = AnnCodecs.ReadIBMP8PropDescAttr(annotationItem.Data, AnnCodecs.IBM_PAGENUMBER);
         int pageNumber = 1;
         if (!string.IsNullOrEmpty(ibmPageNumberValue))
            int.TryParse(ibmPageNumberValue, out pageNumber);

         // Make sure the page exists.
         // If zero, set to page 1; if outside of the document range, disregard it.
         if (pageNumber == 0)
         {
            pageNumber = 1;
         }
         else if (pageNumber > document.Pages.Count)
         {
            return;
         }

         // Get its container (one per page) and add the object to it.
         DocumentPage documentPage = document.Pages[pageNumber - 1];

         AnnContainer container = null;
         if (modifiedContainers.ContainsKey(pageNumber))
         {
            container = modifiedContainers[pageNumber];
         }
         else
         {
            container = documentPage.GetAnnotations(true);
            modifiedContainers.Add(pageNumber, container);
         }

         var ibmP8ReadOptions = new IBMP8ReadOptions();
         // Start converting. We need a rendering engine instance to help with measuring font sizes.
         ibmP8ReadOptions.RenderingEngine = annRenderingEngine;
         ibmP8ReadOptions.Mapper = container.Mapper;

         // Convert to a LEADTOOLS AnnObject.
         // See "#REF IBM_Annotations_Support" for support info
         AnnObject annObj = AnnCodecs.ConvertFromIBMP8(annotationItem.Data, ibmP8ReadOptions);
         if (annObj == null)
         {
            Trace.WriteLine("Conversion from IBM Annotation not supported for item at index " + annotationItemIndex);
            return;
         }

         // Add the supplied properties
         if (!string.IsNullOrEmpty(annotationItem.Password))
         {
            annObj.Lock(annotationItem.Password);
         }
         if (!string.IsNullOrEmpty(annotationItem.UserId))
         {
            annObj.UserId = annotationItem.UserId;
            Dictionary<string, string> metadata = annObj.Metadata;
            string key = AnnObject.AuthorMetadataKey;
            if (metadata.ContainsKey(key))
            {
               if (string.IsNullOrEmpty(metadata[key]))
                  metadata[key] = annotationItem.UserId;
            }
            else
            {
               metadata.Add(key, annotationItem.UserId);
            }
         }
         container.Children.Add(annObj);
      }

      private enum AnnotationFormat
      {
         Unknown,
         LEAD,
         IBMP8
      }

      private static AnnotationFormat GetAnnotationFormat(Annotation annotation)
      {
         if (annotation == null || string.IsNullOrEmpty(annotation.Data))
            return AnnotationFormat.Unknown;

         string data = annotation.Data;

         int index = 0;
         while (index < data.Length && char.IsWhiteSpace(data[index]))
            index++;

         if (index >= data.Length)
            return AnnotationFormat.Unknown;

         if (data.StartsWith("<FnAnno"))
            return AnnotationFormat.IBMP8;
         else
            return AnnotationFormat.LEAD;
      }

      /*
       * See #REF IBM_Annotations_Support.
       * If true, unsupported types will have their response value set as the constant.
       */
      public static string UNSUPPORTED_VALUE = "UNSUPPORTED";
      public static bool ReturnUnsupported = true;

      private static Dictionary<AnnHistoryItemState, Dictionary<string, string>> GetAnnotationsLEADHistory(LEADDocument document)
      {
         // Get the history (which is updated each time SetAnnotations/SaveToCache is called).
         AnnHistory history = document.Annotations.GetHistory();
         if (history == null)
            return null;

         // Condense the history to get all the changes since the last time the history was cleared in one list.
         history.Condense();
         if (history.Items.Count == 0)
            return null;

         string documentId = document.DocumentId;

         bool logging = _logAnnotationsHistory;
         if (logging)
         {
            Trace.WriteLine(string.Format("Logging: LEADTOOLS annotations to LEADTOOLS annotations for document '{0}'", documentId));
            Trace.WriteLine("  Condensed history of changes since last save/set:");
            var items = (List<AnnHistoryItem>)history.Items;
            foreach (AnnHistoryItem item in items)
            {
               // You also have access to the user information here.
               Trace.WriteLine(string.Format("    {0} {1} {2}", item.Guid, item.State, item.Timestamp));
            }
         }

         // Get all annotation containers (and the objects within them).
         AnnContainer[] containers = document.Annotations.GetAnnotations(false);

         // Process all the added objects. LEAD tracks both "Added" and "AddedAndModified" (cases
         // where annotations were added and then modified from the default). Here, we combine them
         // for a simple "ADDED" list.
         Dictionary<string, string> addedObjects = new Dictionary<string, string>();

         AnnCodecs annCodecs = new AnnCodecs();

         // Get the guids for the added objects from our history object.
         string[] guids = history.GetGuidForState(AnnHistoryItemState.Added);
         ProcessLEADObjects(annCodecs, addedObjects, documentId, guids, AnnHistoryItemState.Added, containers);
         guids = history.GetGuidForState(AnnHistoryItemState.AddedAndModified);
         ProcessLEADObjects(annCodecs, addedObjects, documentId, guids, AnnHistoryItemState.Added, containers);

         // Process all modified objects
         Dictionary<string, string> modifiedObjects = new Dictionary<string, string>();
         string[] modifiedGuids = history.GetGuidForState(AnnHistoryItemState.Modified);
         ProcessLEADObjects(annCodecs, modifiedObjects, documentId, modifiedGuids, AnnHistoryItemState.Modified, containers);

         // Process all deleted objects (note, we only get the guids for them; no LEAD objects will be here)
         Dictionary<string, string> deletedObjects = new Dictionary<string, string>();
         guids = history.GetGuidForState(AnnHistoryItemState.Deleted);
         ProcessLEADObjects(annCodecs, deletedObjects, documentId, guids, AnnHistoryItemState.Deleted, containers);

         // Clear the history since we updated everything.
         history.Clear();
         // Set the history again (so it will be saved in the cache for this document).
         document.Annotations.SetHistory(history);

         // List of converted objects that has been modified
         Dictionary<AnnHistoryItemState, Dictionary<string, string>> annotationItems = new Dictionary<AnnHistoryItemState, Dictionary<string, string>>();
         annotationItems.Add(AnnHistoryItemState.Added, addedObjects);
         annotationItems.Add(AnnHistoryItemState.Modified, modifiedObjects);
         annotationItems.Add(AnnHistoryItemState.Deleted, deletedObjects);

         return annotationItems;
      }

      private static void ProcessLEADObjects(AnnCodecs annCodecs, Dictionary<string, string> annotationItems, string documentId, string[] guids, AnnHistoryItemState state, AnnContainer[] containers)
      {
         if (guids.Length == 0)
            return;

         bool logging = _logAnnotationsHistory;
         if (logging)
         {
            Trace.WriteLine(string.Format("Processing '{0}' objects", state));
         }

         // Deleted objects are a special case - we won't have their annotations to convert! We will only have the guid.
         if (state == AnnHistoryItemState.Deleted)
         {
            foreach (string guid in guids)
            {
               if (logging)
                  Trace.WriteLine(string.Format("     {0}", guid));
               annotationItems.Add(guid, null);
            }
         }
         else
         {
            // Get the LEADTOOLS annotation objects for each container from the guids.
            IDictionary<int, IList<AnnObject>> objects = AnnContainer.FindObjectsByGuid(containers, guids);

            foreach (int containerIndex in objects.Keys)
            {
               AnnContainer container = containers[containerIndex];
               IList<AnnObject> containerObjects = objects[containerIndex];
               // This container will be used for saving.
               AnnContainer pageContainer = new AnnContainer();
               pageContainer.PageNumber = container.PageNumber;
               pageContainer.Size = container.Size;
               pageContainer.Mapper.MapResolutions(container.Mapper.SourceDpiX, container.Mapper.SourceDpiY, container.Mapper.TargetDpiX, container.Mapper.TargetDpiY);

               if (logging)
               {
                  Trace.WriteLine(string.Format("  Page {0}", container.PageNumber));
               }
               // Convert each AnnObject back to an IBM annotation XML string.
               foreach (AnnObject annObj in containerObjects)
               {
                  string guid = annObj.Guid;

                  string outputLogMessage = null;
                  if (logging)
                     outputLogMessage = string.Format("     {0} ({1}) ({2})", annObj.FriendlyName, annObj.Id, guid);

                  // The GUID for added objects are auto-generated by LEAD to random values.
                  // You may modify them (or any attribute) like this:
                  //annObj.Guid = "mod - " + guid;

                  // Convert this object into a container.
                  pageContainer.Children.Clear();
                  pageContainer.Children.Add(annObj);
                  string annObjString = annCodecs.SaveToString(pageContainer, AnnFormat.Annotations, pageContainer.PageNumber);
                  // If null, the LEAD AnnObject is not supported (could be a user object).
                  if (annObjString == null)
                  {
                     if (logging)
                        Trace.WriteLine(string.Format("{0} UNSUPPORTED", outputLogMessage));
                     if (ReturnUnsupported)
                     {
                        // Still add it to the response
                        annotationItems.Add(guid, UNSUPPORTED_VALUE);
                     }
                     continue;
                  }
                  else
                  {
                     if (logging)
                        Trace.WriteLine(outputLogMessage);
                  }

                  // Update the guid for our response, too
                  annotationItems.Add(guid, annObjString);
               }
            }
         }
      }

      private static Dictionary<AnnHistoryItemState, Dictionary<string, string>> GetAnnotationsIBMHistory(LEADDocument document)
      {
         // Get the history (which is updated each time SetAnnotations/SaveToCache is called).
         AnnHistory history = document.Annotations.GetHistory();
         if (history == null)
            return null;

         // Condense the history to get all the changes since the last time the history was cleared in one list.
         history.Condense();
         if (history.Items.Count == 0)
            return null;

         string documentId = document.DocumentId;

         bool logging = _logAnnotationsHistory;
         if (logging)
         {
            Trace.WriteLine(string.Format("Logging: LEADTOOLS annotations to IBM annotations for document '{0}'", documentId));
            Trace.WriteLine("  Condensed history of changes since last save/set:");
            var items = (List<AnnHistoryItem>)history.Items;
            foreach (AnnHistoryItem item in items)
            {
               // You also have access to the user information here.
               Trace.WriteLine(string.Format("    {0} {1} {2}", item.Guid, item.State, item.Timestamp));
            }
         }

         // Get all annotation containers (and the objects within them).
         AnnContainer[] containers = document.Annotations.GetAnnotations(false);

         // Process all the added objects. LEAD tracks both "Added" and "AddedAndModified" (cases
         // where annotations were added and then modified from the default). Here, we combine them
         // for a simple "ADDED" list.
         Dictionary<string, string> addedObjects = new Dictionary<string, string>();

         // Get the guids for the added objects from our history object.
         string[] guids = history.GetGuidForState(AnnHistoryItemState.Added);
         ProcessIBMObjects(addedObjects, documentId, guids, AnnHistoryItemState.Added, containers);
         guids = history.GetGuidForState(AnnHistoryItemState.AddedAndModified);
         ProcessIBMObjects(addedObjects, documentId, guids, AnnHistoryItemState.Added, containers);

         // Process all modified objects
         Dictionary<string, string> modifiedObjects = new Dictionary<string, string>();
         string[] modifiedGuids = history.GetGuidForState(AnnHistoryItemState.Modified);
         ProcessIBMObjects(modifiedObjects, documentId, modifiedGuids, AnnHistoryItemState.Modified, containers);

         // Process all deleted objects (note, we only get the guids for them; no IBM objects will be here)
         Dictionary<string, string> deletedObjects = new Dictionary<string, string>();
         guids = history.GetGuidForState(AnnHistoryItemState.Deleted);
         ProcessIBMObjects(deletedObjects, documentId, guids, AnnHistoryItemState.Deleted, containers);

         // Clear the history since we updated everything.
         history.Clear();
         // Set the history again (so it will be saved in the cache for this document).
         document.Annotations.SetHistory(history);

         // List of converted objects that has been modified
         Dictionary<AnnHistoryItemState, Dictionary<string, string>> annotationItems = new Dictionary<AnnHistoryItemState, Dictionary<string, string>>();
         annotationItems.Add(AnnHistoryItemState.Added, addedObjects);
         annotationItems.Add(AnnHistoryItemState.Modified, modifiedObjects);
         annotationItems.Add(AnnHistoryItemState.Deleted, deletedObjects);

         return annotationItems;
      }

      private static void ProcessIBMObjects(Dictionary<string, string> annotationItems, string documentId, string[] guids, AnnHistoryItemState state, AnnContainer[] containers)
      {
         if (guids.Length == 0)
            return;

         bool logging = _logAnnotationsHistory;
         if (logging)
         {
            Trace.WriteLine(string.Format("Processing '{0}' objects", state));
         }

         // Use the write options to customize the conversion.
         IBMP8WriteOptions writeOptions = new IBMP8WriteOptions();
         // We know the destination page number from the container, so we don't need to infer it.
         writeOptions.InferPageNumberFromMetadata = false;
         writeOptions.InferForMultiPageTiffFromMetadata = false;

         // Deleted objects are a special case - we won't have their annotations to convert! We will only have the guid.
         if (state == AnnHistoryItemState.Deleted)
         {
            foreach (string guid in guids)
            {
               if (logging)
                  Trace.WriteLine(string.Format("     {0}", guid));
               annotationItems.Add(guid, null);
            }
         }
         else
         {
            // Get the LEADTOOLS annotation objects for each container from the guids.
            IDictionary<int, IList<AnnObject>> objects = AnnContainer.FindObjectsByGuid(containers, guids);

            foreach (int containerIndex in objects.Keys)
            {
               AnnContainer container = containers[containerIndex];
               IList<AnnObject> containerObjects = objects[containerIndex];

               // Customize the write options for this specific container.
               writeOptions.PageNumber = container.PageNumber;
               writeOptions.Mapper = container.Mapper;

               if (logging)
               {
                  Trace.WriteLine(string.Format("  Page {0}", container.PageNumber));
               }
               // Convert each AnnObject back to an IBM annotation XML string.
               foreach (AnnObject annObj in containerObjects)
               {
                  string guid = annObj.Guid;

                  string outputLogMessage = null;
                  if (logging)
                     outputLogMessage = string.Format("     {0} ({1}) ({2})", annObj.FriendlyName, annObj.Id, guid);

                  // See "#REF IBM_Annotations_Support" for support info
                  string ibmObjString = AnnCodecs.ConvertToIBMP8(annObj, writeOptions);
                  // If null, the LEAD AnnObject is not supported in the IBMP8 spec.
                  if (ibmObjString == null)
                  {
                     if (logging)
                        Trace.WriteLine(string.Format("{0} UNSUPPORTED", outputLogMessage));
                     if (ReturnUnsupported)
                     {
                        // Still add it to the response
                        annotationItems.Add(guid, UNSUPPORTED_VALUE);
                     }
                     continue;
                  }
                  else
                  {
                     if (logging)
                        Trace.WriteLine(outputLogMessage);
                  }

                  // The GUID for added objects are auto-generated by LEAD to random values.
                  // You may modify them (or any attribute) here:
                  guid = "mod-" + annObj.Guid;
                  // Change the guid in the XML
                  ibmObjString = AnnCodecs.WriteIBMP8PropDescAttr(ibmObjString, AnnCodecs.IBM_ID, guid, writeOptions);

                  // Update the guid for our response, too
                  annotationItems.Add(guid, ibmObjString);
               }
            }
         }
      }

      private static void ProcessToDisk(string dir, Dictionary<AnnHistoryItemState, Dictionary<string, string>> annotationItems)
      {

         // Also separate by time, since saving can occur multiple times
         string nowTime = DateTime.Now.ToString("MMddyy_Hmmss");
         dir = dir + "/" + nowTime;

         if (annotationItems.ContainsKey(AnnHistoryItemState.Added))
            DumpObjects(dir, "added", annotationItems[AnnHistoryItemState.Added]);
         if (annotationItems.ContainsKey(AnnHistoryItemState.Modified))
            DumpObjects(dir, "modified", annotationItems[AnnHistoryItemState.Modified]);
         if (annotationItems.ContainsKey(AnnHistoryItemState.Deleted))
            DumpObjects(dir, "deleted", annotationItems[AnnHistoryItemState.Deleted]);
      }

      // A helper method for dumping the converted annotations back to disk.
      private static void DumpObjects(string dir, string subDir, Dictionary<string, string> annotationItems)
      {
         try
         {
            string directory = Path.Combine(dir, subDir);
            Directory.CreateDirectory(directory);

            string allAsText = "";
            foreach (KeyValuePair<string, string> annotationItem in annotationItems)
            {
               string guid = annotationItem.Key;
               string xml = annotationItem.Value;
               allAsText += string.Format("// {0}\n{1}\n", guid, xml);
               System.IO.File.WriteAllText(Path.Combine(directory, guid) + ".xml", xml);
            }
            if (allAsText.Length > 0)
               System.IO.File.WriteAllText(Path.Combine(directory, "all.txt"), allAsText);
         }
         catch (Exception ex)
         {
            throw new InvalidOperationException("Error writing converted file to disk", ex);
         }
      }
   }
}
