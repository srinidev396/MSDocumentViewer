// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;

namespace Leadtools.Services.Tools.Exceptions
{
   /* By default, if there is an exception thrown in a Controller's constructor
    * we get a non-descriptive default error, something like
    * "Please ensure the controller has a parameterless constructor".
    * This new Activator (which should be added in WebApiConfig.cs) throws the
    * innerException for more information.
    */

   public class ExceptionHandlingControllerActivator : IHttpControllerActivator
   {
      private IHttpControllerActivator _underlyingActivator;

      public ExceptionHandlingControllerActivator(IHttpControllerActivator concreteActivator)
      {
         _underlyingActivator = concreteActivator;
      }

      public IHttpController Create(HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor, Type controllerType)
      {
         try
         {
            return _underlyingActivator.Create(request, controllerDescriptor, controllerType);
         }
         catch (Exception e)
         {
            // The inner exception is the important one
            throw e.InnerException;
         }
      }
   }
}
