// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************

using Leadtools.Caching;
using Leadtools.Document;
using Leadtools.DocumentViewer.Models.FormFields;
using Leadtools.Services.Tools.Exceptions;
using Leadtools.Services.Tools.Helpers;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Leadtools.DocumentViewer.Controllers
{
   /// <summary>
   /// Used for setting/getting form fields with external applications.
   /// </summary>
   public class FormFieldsController : Controller
   {
      public FormFieldsController()
      {
         ServiceHelper.InitializeController();
      }

      /// <summary>
      ///  Sets FormFields into the document.
      /// </summary>
      [ServiceErrorAttribute(Message = "The form fields could not be set")]
      [HttpPost("api/[controller]/[action]")]
      public SetFormFieldsResponse SetFormFields(SetFormFieldsRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         // Must have the documentId you'd like to add annotations to.
         // If you only have the document cache URI, DocumentFactory.LoadFromUri needs to be called.
         if (string.IsNullOrEmpty(request.DocumentId))
            throw new ArgumentNullException("documentId");

         DoSetFormFields(request);

         SetFormFieldsResponse response = new SetFormFieldsResponse();
         return response;
      }

      private void DoSetFormFields(SetFormFieldsRequest request)
      {
         var formFields = DocumentHelper.ToFormFields(request.FormFieldsContainers);

         ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.DocumentId);
         var loadFromCacheOptions = new LoadFromCacheOptions
         {
            Cache = cache,
            DocumentId = request.DocumentId,
            UserToken = ServiceHelper.GetUserToken(this.Request.Headers, null)
         };
         using (var document = DocumentFactory.LoadFromCache(loadFromCacheOptions))
         {
            DocumentHelper.CheckLoadFromCache(document);

            // If the document is read-only then we won't be able to modify its settings. Temporarily change this state.
            bool wasReadOnly = document.IsReadOnly;
            document.IsReadOnly = false;

            if (formFields != null && formFields.Count > 0)
            {
               document.FormFields.SetFormFields(formFields.ToArray());
            }
            else
            {
               document.FormFields.SetFormFields(null);
            }

            document.IsReadOnly = wasReadOnly;
            document.SaveToCache();
         }
      }

      /// <summary>
      ///  Sets FormFields into the document.
      /// </summary>
      [ServiceErrorAttribute(Message = "The form fields could not be set")]
      [HttpPost("api/[controller]/[action]")]
      public SetFormFieldsResponse SetFormFieldResources(SetFormFieldResourcesRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         // Must have the documentId you'd like to add annotations to.
         // If you only have the document cache URI, DocumentFactory.LoadFromUri needs to be called.
         if (string.IsNullOrEmpty(request.DocumentId))
            throw new ArgumentNullException("documentId");

         ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.DocumentId);
         var loadFromCacheOptions = new LoadFromCacheOptions
         {
            Cache = cache,
            DocumentId = request.DocumentId,
            UserToken = ServiceHelper.GetUserToken(this.Request.Headers, null)
         };
         using (var document = DocumentFactory.LoadFromCache(loadFromCacheOptions))
         {
            DocumentHelper.CheckLoadFromCache(document);

            // If the document is read-only then we won't be able to modify its settings. Temporarily change this state.
            bool wasReadOnly = document.IsReadOnly;
            document.IsReadOnly = false;
            try
            {
               var resources = DocumentHelper.ToFormFieldResources(request.FormFieldResources);
               document.FormFields.SetResources(resources);
            }
            finally
            {
               document.IsReadOnly = wasReadOnly;
            }
            document.SaveToCache();
         }

         SetFormFieldsResponse response = new SetFormFieldsResponse();
         return response;
      }
   }
}
