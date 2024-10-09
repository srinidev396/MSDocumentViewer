// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Net;
using System.Runtime.Serialization;
using Leadtools.Services.Tools.Helpers;

namespace Leadtools.Services.Tools.Exceptions
{
   [AttributeUsage(AttributeTargets.Method)]
   public sealed class ServiceErrorAttribute : Attribute
   {
      // The user-safe message that will eventually be the ServiceError.Message.
      public string Message { get; set; }

      // An alternate MethodName value to return.
      public string MethodName { get; set; }
   }


   [DataContract]
   public class ServiceError
   {
      public ServiceError()
      {
         Message = "An error occurred. Please contact an administrator.";
         StatusCode = HttpStatusCode.InternalServerError;
         Detail = null;
         Code = 0;
         Link = $"https://www.leadtools.com/help/leadtools/v{ServiceHelper.LTVersion}/dh/javascript/to/introduction.html";
         ExceptionType = null;
         MethodName = "Unknown";
         UserData = null;
      }

      /* Always safe to show to an end-user.
       * Will hold the "SafeErrorMessage" attribute of a method
       * or the Exception.Message of an explicit ServiceException.
       */
      [DataMember]
      public string Message { get; set; }

      [DataMember]
      public HttpStatusCode StatusCode { get; set; }

      /* Sometimes null. Not end-user safe.
       * Often contains the actual Exception.Message from an 
       * Exception that wasn't thrown as a ServiceException.
       */
      [DataMember]
      public string Detail { get; set; }

      [DataMember]
      public int Code { get; set; }

      [DataMember]
      public string Link { get; set; }

      [DataMember]
      public string ExceptionType { get; set; }

      [DataMember]
      public string MethodName { get; set; }

      [DataMember]
      public string UserData { get; set; }
   }


   /*
    * For use with all top-level errors.
    * Has special methods to match with GlobalExceptionHandler
    * and make a user-safe error message.
    */
   [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
   [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly"), Serializable]
   public sealed class ServiceException : Exception
   {

      public HttpStatusCode StatusCode { get; private set; }

      public ServiceException()
         : base()
      {
      }

      public ServiceException(string userMessage)
         : base(userMessage)
      {
      }

      public ServiceException(string userMessage, Exception innerException)
         : base(userMessage, innerException)
      {
      }

      public ServiceException(string userMessage, HttpStatusCode statusCode)
         : base(userMessage)
      {
         StatusCode = statusCode;
      }

      public ServiceException(string userMessage, Exception innerException, HttpStatusCode statusCode)
         : base(userMessage, innerException)
      {
         StatusCode = statusCode;
      }
   }
}
