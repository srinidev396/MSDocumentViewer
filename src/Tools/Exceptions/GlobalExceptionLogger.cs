// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Diagnostics;
using System.Web.Http.ExceptionHandling;

namespace Leadtools.Services.Tools.Exceptions
{
   public class GlobalExceptionLogger : ExceptionLogger
   {
      /* The order of execution is ExceptionLogger, ExceptionFilter, ExceptionHandler.
       * Loggers receive all exceptions, Filter receives a subset of them, and Handler
       * receives only exceptions for which an HttpResponse can be sent.
       * See the comments for the other two classes.
       * More info: Web Api Error Handling
       * (http://www.asp.net/web-api/overview/error-handling)
       * 
       * A Logger is good to use because it can log every Exception.
       * Only a logger can handle these four things:
       * - Exceptions thrown from controller constructors.
       * - Exceptions thrown from message handlers.
       * - Exceptions thrown during routing.
       * - Exceptions thrown during response content serialization.
       *
       */

      public override void Log(ExceptionLoggerContext context)
      {
         if (context == null)
            throw new ArgumentNullException("context");

         bool callsHandler = context.CallsHandler;
         // If it will be handled by the Handler as well
         string handleType = callsHandler ? "Handled" : "Unhandled";
         string path = context.Request.RequestUri.PathAndQuery;
         Trace.WriteLine(String.Format("{1} Exception Logged at '{2}':{0}   {3}: {4}", Environment.NewLine, handleType, path, context.Exception.GetType(), context.Exception.Message));
      }
   }
}
