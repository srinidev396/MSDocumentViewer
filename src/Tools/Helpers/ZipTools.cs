// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Web;

namespace Leadtools.Services.Tools.Helpers
{
   internal static class ZipTools
   {
      public static void ZipFiles(string baseDirectory, string[] files, string zipFileName)
      {
         if (baseDirectory == null) throw new ArgumentNullException("baseDirectory");
         if (zipFileName == null) throw new ArgumentNullException("zipFileName");

         if (files == null || files.Length == 0)
            return;

         baseDirectory = baseDirectory.ToLower(CultureInfo.CurrentCulture);
         var baseDirectoryLength = baseDirectory.Length;

         using (var package = Package.Open(zipFileName, FileMode.Create))
         {
            foreach (var file in files)
            {
               if (string.IsNullOrEmpty(file) ||
                  !File.Exists(file) ||
                  !file.ToLower(CultureInfo.CurrentCulture).StartsWith(baseDirectory, StringComparison.OrdinalIgnoreCase))
                  continue;

               // Add the part, relative to the base directory in the /path/file.ext format
               var uri = file.Substring(baseDirectoryLength);
               if (string.IsNullOrEmpty(uri))
                  continue;

               // Replace \ with /
               uri = uri.Replace('\\', '/');
               // Spaces is not supported
               uri = uri.Replace(" ", "_");
               if (!uri.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                  uri = "/" + uri;

               var partUri = new Uri(uri, UriKind.Relative);
               var part = package.CreatePart(
                  partUri,
                  System.Net.Mime.MediaTypeNames.Application.Zip,
                  CompressionOption.Normal);

               using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
               {
                  ServiceHelper.CopyStream(fileStream, part.GetStream());
               }
            }
         }
      }
   }
}
