// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using Leadtools.Services.Tools.Helpers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Leadtools.Services.Tools.Helpers
{
   /* We already have CORS installed globally, but that won't work for GET requests for images.
    * This filter is applied to the actions that return images that are placed directly into 
    * image elements, so that LEADTOOLS can modify them with canvas without getting errors.
    * See "CORS Enabled Image" https://developer.mozilla.org/en-US/docs/Web/HTML/CORS_enabled_image
    * 
    * Note, we only use this filter for origins and do not specify headers or methods.
    */
   public class AlwaysCorsFilter : ActionFilterAttribute
   {
      public override void OnActionExecuted(ActionExecutedContext actionExecutedContext)
      {
         if (actionExecutedContext != null && actionExecutedContext.HttpContext.Response != null)
         {
            try
            {
               actionExecutedContext.HttpContext.Response.Headers.Remove("Access-Control-Allow-Origin");

               actionExecutedContext.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", ServiceHelper.CORSOrigins);
            }
            catch { }//allow this to fail safely

         }
         base.OnActionExecuted(actionExecutedContext);
      }
   }
}
