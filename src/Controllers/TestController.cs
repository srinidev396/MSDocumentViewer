// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;

using Leadtools;

using Leadtools.Services.Tools.Exceptions;
using Leadtools.Services.Models;
using Leadtools.DocumentViewer.Models.Test;
using Leadtools.Services.Tools.Helpers;
using System.Runtime.Serialization;
using System.Reflection;
using Leadtools.Services.Tools.Cache;
using System.IO;

namespace Leadtools.DocumentViewer.Controllers
{
   /// <summary>
   /// Used with the DocumentFactory class of the LEADTOOLS Document JavaScript library.
   /// </summary>
   public class TestController : Controller
   {
      public static List<string> SampleFilesAvailable = new List<string>();

      public static void InitializeService()
      {
         BuildSampleFileList();
      }

      private static void BuildSampleFileList()
      {
         var sampleFolder = ServiceHelper.GetSettingValue(ServiceHelper.Key_Samples);
         if (string.IsNullOrWhiteSpace(sampleFolder))
            return;

         var directory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", sampleFolder);

         foreach(var file in Directory.EnumerateFiles(directory))
         {
            try
            {
               var name = Path.GetFileName(file);
               SampleFilesAvailable.Add(name);
            }
            catch
            {
               Trace.Write($"Failed to get name for file in path {file}");
            }
         }
      }

      /// <summary>
      ///   Pings the service to ensure a connection, returning data about the status of the LEADTOOLS license.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "request")]
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The service is not available", MethodName = "VerifyService")]
      [HttpGet("api/[controller]/GetSampleFiles")]
      public SampleFilesResponse GetSampleFiles([FromQuery] Request request)
      {
         return new SampleFilesResponse()
         {
            Files = SampleFilesAvailable
         };
      }

      /* This Ping() method is used to detect that everything is working fine
       * before a demo begins. Otherwise, errors from loading the initial document
       * may tell the wrong story because the user hasn't set up the service yet.
       */

      /// <summary> 
      ///   Pings the service to ensure a connection, returning data about the status of the LEADTOOLS license.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "request")]
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The service is not available", MethodName = "VerifyService")]
      [HttpPost("api/[controller]/Ping")]
      public PingResponse PostPing(Request request)
      {
         return Ping(request);
      }

      /// <summary>
      ///   Pings the service to ensure a connection, returning data about the status of the LEADTOOLS license.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "request")]
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The service is not available", MethodName = "VerifyService")]
      [HttpGet("api/[controller]/Ping")]
      public PingResponse GetPing([FromQuery] Request request)
      {
         return Ping(request);
      }

      [NonAction]
      public PingResponse Ping(Request request)
      {
         Trace.WriteLine("Leadtools Document Service: Ready");

         var response = new PingResponse();
         response.Time = DateTime.Now;
         bool ready = true;

         // Set the service info (name, version and platform)
         var serviceAssembly = typeof(TestController).Assembly;
         var serviceFileVersionInfo = FileVersionInfo.GetVersionInfo(serviceAssembly.Location);
         response.ServiceName = serviceFileVersionInfo.OriginalFilename;
#if !STD
         response.ServicePlatform = ".NET Framework";
#else
         response.ServicePlatform = ".NET Core";
#endif
         response.ServiceOperatingSystem = ResolveOSDesc(System.Runtime.InteropServices.RuntimeInformation.OSDescription);
         response.ServiceVersion = serviceFileVersionInfo.FileVersion;

         // Get Kernel Version
         try
         {
            var leadAssembly = typeof(RasterImage).Assembly;
            var leadFileVersionInfo = FileVersionInfo.GetVersionInfo(leadAssembly.Location);
            response.KernelVersion = leadFileVersionInfo.FileVersion.Replace(",", ".");
         }
         catch
         {
            ready = false;
         }

         Trace.WriteLine("Getting Toolkit status");
         response.IsLicenseChecked = ServiceHelper.IsLicenseChecked;
         response.IsLicenseExpired = ServiceHelper.IsKernelExpired;
         if (response.IsLicenseChecked)
            response.KernelType = RasterSupport.KernelType.ToString().ToUpper();
         else
            response.KernelType = null;

         if (response.IsLicenseExpired || !response.IsLicenseChecked)
            ready = false;

         try
         {
            ICacheManager cacheManager = ServiceHelper.CacheManager;
            if (cacheManager != null)
            {
               response.CacheManagerName = cacheManager.Name;
               string[] cacheNames = cacheManager.GetCacheNames();
               string sep = string.Empty;
               response.CacheNames = string.Empty;
               foreach (string cacheName in cacheNames)
               {
                  response.CacheNames += sep + cacheName;
                  sep = ",";
               }
               cacheManager.CheckCacheAccess(null);
               response.IsCacheAccessible = true;
            }
         }
         catch (Exception)
         {
            response.IsCacheAccessible = false;
            ready = false;
         }

         // Add OCR Status
         response.OcrEngineStatus = (int)ServiceHelper.OcrEngineStatus;
         response.MultiplatformSupportStatus = ServiceHelper.MultiplatformSupportStatus;

         // Multimedia
         response.IsMultimediaCacheAvailable = MultimediaController.IsCacheAvalable();
         response.IsMP4ConverterAvailable = (MultimediaController.GetMP4ConverterPath() != null);
         response.IsMP4ConverterLicensed = MultimediaController.IsMP4ConverterLicensed();

         response.Message = ready ? "Ready" : "Not Ready";

         return response;
      }

      private string ResolveOSDesc(string desc)
      {
#if FOR_STD
         if(Platform.IsWindows)
         {
            var m = System.Text.RegularExpressions.Regex.Match(desc, @"Microsoft Windows \d+\.\d+\.(?<build>\d+)");
            if(m.Success)
            {               
               var build = m.Groups["build"].Value;
               if(int.TryParse(build, out var buildNumber))
               {
                  if(buildNumber >= 22000)
                  {
                     desc = @"Microsoft Windows 11";
                  }
               }
            }
         }
#endif // #if FOR_STD
         return desc;
      }

      /// <summary>
      ///   Pings the service to ensure a connection.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "request")]
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The service is not available", MethodName = "Heartbeat")]
      [HttpPost("api/[controller]/Heartbeat")] 
      public HeartbeatResponse PostHeartbeat(Request request)
      {
         return new HeartbeatResponse
         {
            Time = DateTime.Now
         };
      }

      /// <summary>
      ///   Pings the service to ensure a connection.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "request")]
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The service is not available", MethodName = "Heartbeat")]
      [HttpGet("api/[controller]/Heartbeat")]
      public HeartbeatResponse GetHeartbeat([FromQuery] Request request)
      {
         return new HeartbeatResponse
         {
            Time = DateTime.Now
         };
      }

      /// <summary>
      /// A test method, not used, to show the use of "userData".
      /// </summary>
      // Modify and return user data
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The user data could not be accessed")]
      [HttpPost("api/[controller]/[action]")]
      public Response CheckUserData(Request request)
      {
         var userData = request.UserData;
         object newUserData = new ReturnUserDataObject()
         {
            Data = userData,
            Message = "Welcome to the Document Service: " + DateTime.Now.ToLongTimeString()
         };
         return new Response
         {
            UserData = Newtonsoft.Json.JsonConvert.SerializeObject(newUserData)
         };
      }

      [DataContract]
      internal class ReturnUserDataObject
      {
         [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
         [DataMember]
         public string Data { get; set; }

         [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
         [DataMember]
         public string Message { get; set; }
      }
   }
}
