// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Microsoft.SharePoint.Client;
using Leadtools.Services.Models;

namespace Leadtools.DocumentViewer.Models.SharePoint
{
   [DataContract]
   public class GetDocumentsListItemsRequest : Request
   {
      /// <summary>
      /// The SharePoint server properties.
      /// </summary>
      [DataMember(Name = "serverProperties")]
      public SharePointServerProperties ServerProperties { get; set; }

      /// <summary>
      /// The folder to retrieve a list of items for.
      /// </summary>
      [DataMember(Name = "folderUri")]
      public Uri FolderUri { get; set; }
   }

   [DataContract]
   public class GetDocumentsListItemsResponse : Response
   {
      /// <summary>
      /// The SharePoint server directory items.
      /// </summary>
      [DataMember(Name = "items")]
      public SharePointItem[] Items { get; set; }
   }

   [DataContract]
   public class DownloadFileRequest : Request
   {
      /// <summary>
      /// The SharePoint server properties.
      /// </summary>
      [DataMember(Name = "serverProperties")]
      public SharePointServerProperties ServerProperties { get; set; }

      /// <summary>
      /// The uri of the file to download.
      /// </summary>
      [DataMember(Name = "fileUri")]
      public Uri FileUri { get; set; }
   }

   [DataContract]
   public class DownloadFileResponse : Response
   {
      /// <summary>
      /// The file as a base64 string
      /// </summary>
      [DataMember(Name = "data")]
      public string Data { get; set; }
   }

   [DataContract]
   public class UploadFileRequest : Request
   {
      /// <summary>
      /// The SharePoint server properties.
      /// </summary>
      [DataMember(Name = "serverProperties")]
      public SharePointServerProperties ServerProperties { get; set; }

      /// <summary>
      /// The URI of the file to upload.
      /// </summary>
      [DataMember(Name = "fileUri")]
      public Uri FileUri { get; set; }

      /// <summary>
      /// The name to use with the uploaded file.
      /// </summary>
      [DataMember(Name = "name")]
      public string Name { get; set; }

      /// <summary>
      /// The destination of the uploaded file.
      /// </summary>
      [DataMember(Name = "folderUri")]
      public Uri FolderUri { get; set; }
   }

   [DataContract]
   public class SharePointServerProperties
   {
      public SharePointServerProperties() { }
      /// <summary>
      /// The server URI passed by the user.
      /// </summary>
      [DataMember(Name = "uri")]
      public Uri Uri { get; set; }

      /// <summary>
      /// Whether to use network credentials when connecting to the server.
      /// </summary>
      [DataMember(Name = "useCredentials")]
      public bool UseCredentials { get; set; }

      /// <summary>
      /// The credentials user name (if using credentials).
      /// </summary>
      [DataMember(Name = "userName")]
      public string UserName { get; set; }

      /// <summary>
      /// The credentials password (if using credentials).
      /// </summary>
      [DataMember(Name = "password")]
      public string Password { get; set; }

      /// <summary>
      /// The credentials domain (if using credentials).
      /// </summary>
      [DataMember(Name = "domain")]
      public string Domain { get; set; }
   }

   [DataContract]
   public class SharePointItem
   {
      /// <summary>
      /// The name of the file/folder.
      /// </summary>
      [DataMember(Name = "name")]
      public string Name { get; set; }

      /// <summary>
      /// The type of the item (from Microsoft.SharePoint.Client.FileSystemObjectType - Invalid,File,Folder,Web)
      /// </summary>
      [DataMember(Name = "type")]
      public FileSystemObjectType Type { get; set; }
   }
}
