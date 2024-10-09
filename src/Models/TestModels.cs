// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Leadtools.Services.Models;

namespace Leadtools.DocumentViewer.Models.Test
{
   [DataContract]
   public class PingResponse : Response
   {
      /// <summary>
      /// A simple message, for testing.
      /// </summary>
      [DataMember(Name = "message")]
      public string Message { get; set; }

      /// <summary>
      /// The current time, so the user may tell if it was cached.
      /// </summary>
      [DataMember(Name = "time")]
      public DateTime Time { get; set; }

      /// <summary>
      /// Whether or not the license was able to be checked.
      /// </summary>
      [DataMember(Name = "isLicenseChecked")]
      public bool IsLicenseChecked { get; set; }

      /// <summary>
      /// Whether or not the license is expired.
      /// </summary>
      [DataMember(Name = "isLicenseExpired")]
      public bool IsLicenseExpired { get; set; }

      /// <summary>
      /// The type of kernel - evaluation, for example.
      /// </summary>
      [DataMember(Name = "kernelType")]
      public string KernelType { get; set; }

      /// <summary>
      /// Whether the cache was accessed successfully.
      /// </summary>
      [DataMember(Name = "isCacheAccessible")]
      public bool IsCacheAccessible { get; set; }

      /// <summary>
      /// Name of the current cache manager
      /// </summary>
      [DataMember(Name = "cacheManagerName")]
      public string CacheManagerName { get; set; }

      /// <summary>
      /// Cache names, separated by comma
      /// </summary>
      [DataMember(Name = "cacheNames")]
      public string CacheNames { get; set; }

      /// <summary>
      /// The value of the OCREngineStatus enumeration indicating the OCR Engine Status.
      /// </summary>
      [DataMember(Name = "ocrEngineStatus")]
      public int OcrEngineStatus { get; set; }

      /// <summary>
      /// Service name
      /// </summary>
      [DataMember(Name = "serviceName")]
      public string ServiceName { get; set; }

      /// <summary>
      /// Service platform
      /// </summary>
      [DataMember(Name = "servicePlatform")]
      public string ServicePlatform { get; set; }

      /// <summary>
      /// Service platform
      /// </summary>
      [DataMember(Name = "serviceOperatingSystem")]
      public string ServiceOperatingSystem { get; set; }

      /// <summary>
      /// The service version.
      /// </summary>
      [DataMember(Name = "serviceVersion")]
      public string ServiceVersion { get; set; }

      /// <summary>
      /// The kernel version.
      /// </summary>
      [DataMember(Name = "kernelVersion")]
      public string KernelVersion { get; set; }

      /// <summary>
      /// The multi-platform support.
      /// </summary>
      [DataMember(Name = "multiplatformSupportStatus")]
      public string MultiplatformSupportStatus { get; set; }


      /// <summary>
      /// Whether the multimedia cache is available.
      /// </summary>
      [DataMember(Name = "isMultimediaCacheAvailable")]
      public bool IsMultimediaCacheAvailable { get; set; }


      /// <summary>
      /// Whether or not the MP4 converter is avalaible.
      /// </summary>
      [DataMember(Name = "isMP4ConverterAvailable")]
      public bool IsMP4ConverterAvailable { get; set; }

      /// <summary>
      /// Whether or not the MP4 converter is licensed.
      /// </summary>
      [DataMember(Name = "isMP4ConverterLicensed")]
      public bool IsMP4ConverterLicensed { get; set; }

      /// <summary>
      /// Whether or not the MP4 converter is licensed.
      /// </summary>
      [DataMember(Name = "isSharepointSupported")]
      public bool IsSharepointSupported { get; set; } =
         #if NET
         false;//known compatibility issues for the .net core version of sharepoint client
         #else
         true;
         #endif
   }

   [DataContract]
   public class HeartbeatResponse : Response
   {
      /// <summary>
      /// The current time, so the user may tell if it was cached.
      /// </summary>
      [DataMember(Name = "time")]
      public DateTime Time { get; set; }
   }

   [DataContract]
   public class SampleFilesResponse : Response
   {
      /// <summary>
      /// The current time, so the user may tell if it was cached.
      /// </summary>
      [DataMember(Name = "files")]
      public IList<string> Files { get; set; }
   }
}
