// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.SharePoint.Client;

using Leadtools.Services.Tools.Exceptions;
using Leadtools.DocumentViewer.Models;
using Leadtools.DocumentViewer.Models.SharePoint;
using Leadtools.Services.Tools.Helpers;
using ServiceSharePointItem = Leadtools.DocumentViewer.Models.SharePoint.SharePointItem;
using System.Threading;
using Microsoft.AspNetCore.Mvc;

namespace Leadtools.DocumentViewer.Controllers
{
   public class SharePointController : Controller
   {
      public SharePointController()
      {
         ServiceHelper.InitializeController();
      }

      private static string GetCombinedPath(ClientContext context, Microsoft.SharePoint.Client.List root, string uri)
      {
         context.Load(root, a => a.ParentWebUrl);
         context.ExecuteQuery();

         string combined = "/" + SharedDocumentsList + "/" + uri;
         string parentWebUrl = root.ParentWebUrl;
         if (!string.IsNullOrEmpty(parentWebUrl) && parentWebUrl != "/")
            combined = parentWebUrl + combined;
         return combined;
      }

      private const string SharedDocumentsList = "Shared Documents";
      /// <summary>
      ///   Returns a list of the items in this SharePoint folder.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "request")]
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "Could not get list item")]
      [HttpPost]
      public GetDocumentsListItemsResponse GetDocumentsListItems(GetDocumentsListItemsRequest request)
      {
         using (ClientContext context = SignIn(request.ServerProperties))
         {
            try
            {
               Microsoft.SharePoint.Client.List root = context.Web.Lists.GetByTitle(SharedDocumentsList);

               CamlQuery camlQuery = new CamlQuery();
               if (request.FolderUri != null && !string.IsNullOrWhiteSpace(request.FolderUri.ToString()))
               {
                  camlQuery.FolderServerRelativeUrl = GetCombinedPath(context, root, request.FolderUri.ToString());
               }

               var queryItems = root.GetItems(camlQuery);
               // We must specify what properties we want to load
               context.Load(queryItems, a => a.Include(
                  b => b.DisplayName,
                  b => b.FileSystemObjectType
               ));
               context.ExecuteQuery();

               SharePointItem[] items = new SharePointItem[queryItems.Count];
               for (int i = 0; i < queryItems.Count; i++)
               {
                  ListItem queryItem = queryItems[i];

                  SharePointItem item = new SharePointItem();

                  item.Type = queryItem.FileSystemObjectType;

                  // Sometimes, the Sharepoint directory may not have a "File" or "Folder" column.
                  try
                  {
                     if (item.Type == FileSystemObjectType.File)
                     {
                        context.Load(queryItem, a => a.File.Name);
                        context.ExecuteQuery();
                        item.Name = queryItem.File.Name;
                     }
                     else if (item.Type == FileSystemObjectType.Folder)
                     {
                        context.Load(queryItem, a => a.File.Name);
                        context.ExecuteQuery();
                        item.Name = queryItem.File.Name;
                     }
                  }
                  catch
                  {
                     // Column must not exist
                  }

                  // If we failed above, use the potentially-less-accurate "Display Name"
                  if (string.IsNullOrEmpty(item.Name))
                     item.Name = queryItem.DisplayName;

                  items[i] = item;
               }

               return new GetDocumentsListItemsResponse
               {
                  Items = items
               };
            }
            catch(MissingMemberException)
            {
#if NET
               throw new Exception("The ASP.Net Core document service has known compatibility issues with Sharepoint servers. Please use the ASP.Net (windows) service.");
#else
               throw new NotSupportedException("This version of SharePoint is not supported. Please use SharePoint Online");
#endif
            }
         }
      }

      /// <summary>
      ///   Streams the requested file.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "request")]
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The item could not be downloaded")]
      [HttpPost]
      public DownloadFileResponse DownloadFile(DownloadFileRequest request)
      {
         using (ClientContext context = SignIn(request.ServerProperties))
         {
            try
            {
               Microsoft.SharePoint.Client.List root = context.Web.Lists.GetByTitle(SharedDocumentsList);
               string downloadUri = GetCombinedPath(context, root, request.FileUri.ToString());

               byte[] data = null;
               using (Microsoft.SharePoint.Client.FileInformation fileInfo = Microsoft.SharePoint.Client.File.OpenBinaryDirect(context, downloadUri))
               {
                  using (var ms = new MemoryStream())
                  {
                     fileInfo.Stream.CopyTo(ms);
                     data = ms.ToArray();
                  }
               }

               string base64 = null;
               if (data != null)
                  base64 = System.Convert.ToBase64String(data);

               return new DownloadFileResponse
               {
                  Data = base64
               };
            }
            catch
            {
               throw;
            }
         }
      }

      /// <summary>
      ///   Uploads the supplied file via URI.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "request")]
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The item could not be upload")]
      [HttpPost]
      public void UploadFile(UploadFileRequest request)
      {
         using (ClientContext context = SignIn(request.ServerProperties))
         {
#pragma warning disable SYSLIB0014 // Type or member is obsolete
            using (WebClient client = new WebClient())
            {
               using (Stream uriStream = client.OpenRead(request.FileUri))
               {
                  try
                  {
                     Microsoft.SharePoint.Client.List root = context.Web.Lists.GetByTitle(SharedDocumentsList);

                     string name = request.Name;
                     if (request.FolderUri != null && !string.IsNullOrWhiteSpace(request.FolderUri.ToString()))
                        name = request.FolderUri.ToString() + "/" + request.Name;

                     string uploadUri = GetCombinedPath(context, root, name);

                     Microsoft.SharePoint.Client.File.SaveBinaryDirect(context, uploadUri, uriStream, true);
                     context.ExecuteQuery();
                  }
                  catch
                  {
                     throw;
                  }
               }
            }
#pragma warning restore SYSLIB0014 // Type or member is obsolete
         }
      }

      private ClientContext SignIn(SharePointServerProperties properties)
      {
         if (properties == null)
            throw new ArgumentException("server properties  must be specified");

         if (properties.Uri == null)
            throw new ArgumentException("server uri  must be specified");

         string uri = properties.Uri.ToString();
         if (!uri.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            uri += "/";

         using (ClientContext context = new ClientContext(uri))
         {

            if (properties.UseCredentials)
               context.Credentials = new NetworkCredential(properties.UserName, properties.Password, properties.Domain);
            else
               context.Credentials = CredentialCache.DefaultCredentials;

            return context;
         }
      }
   }
}
