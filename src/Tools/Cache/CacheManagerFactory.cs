// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************

using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
#if !NET
using IHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
using Microsoft.Extensions.Hosting;
#endif

namespace Leadtools.Services.Tools.Cache
{
   public static class CacheManagerFactory
   {
      public const string XML_ROOT_NAME = "leadtools_cache_manager";
      public const string XML_CACHE_MANAGER_ELEMENT_NAME = "cache_manager";
      public const string XML_TYPE_NAME = "type";

      public static ICacheManager CreateFromConfiguration(Stream stream, string WebRootPath)
      {
         // Get the type name from the stream then load it

         if (stream == null)
            throw new ArgumentNullException("stream");

         var xmlDocument = XDocument.Load(stream);
         if (!CheckConfiguration(xmlDocument))
            return null;

         // Find first cache manager element
         XElement cacheManagerElement = (from el in xmlDocument.Root.Elements()
                             where el.Name == XML_CACHE_MANAGER_ELEMENT_NAME
                             select el).FirstOrDefault();
         if (cacheManagerElement == null)
            return null;

         // Get the type name
         XElement typeNameElement = cacheManagerElement.Element(XML_TYPE_NAME);
         if (typeNameElement == null)
            return null;

         // Try to create it
         string typeName = typeNameElement.Value;
         if (string.IsNullOrEmpty(typeName))
            return null;

         Type cacheManagerType = Type.GetType(typeName);
         ICacheManager cacheManager = Activator.CreateInstance(cacheManagerType, new object[] { WebRootPath, cacheManagerElement }) as ICacheManager;
         return cacheManager;
      }

      private static bool CheckConfiguration(XDocument xmlDocument)
      {
         // Check if we have the correct XML file
         if (xmlDocument == null || xmlDocument.Root == null || xmlDocument.Root.Name != XML_ROOT_NAME)
            return false;

         return true;
      }
   }
}
