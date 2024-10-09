//*************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.
// All Rights Reserved.
//*************************************************************

﻿using Leadtools.DocumentViewer.Models.Structure;
using Leadtools.Services.Models;
using Leadtools.Services.Tools.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;


namespace DocumentServiceCore.Tools
{
   public class RequestResponseFilter : IAsyncActionFilter
   {
      public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
      {
         Request request = null;

         //Check if any argument is the service Request model (that would contain UserData, potentially)
         foreach (var arg in context.ActionArguments.Values.ToArray())
            if (arg is Request)
            {
               request = (Request)arg;
               break;
            }

         var resultContext = await next();
         if (ServiceHelper.ReturnRequestUserData && request != null && !string.IsNullOrWhiteSpace(request.UserData) && !request.UserData.Equals("null", StringComparison.OrdinalIgnoreCase))
         {
            var resultObj = resultContext.Result as ObjectResult;
            if (resultObj == null)
            {
               resultObj = new ObjectResult(
                  new Response()
                  {
                     UserData = request.UserData
                  });
               resultContext.Result = resultObj;
            }
            else if (resultObj != null && resultObj.Value is Response)
            {
               var responseObj = resultObj.Value as Response;
               responseObj.UserData = request.UserData;
            }
#if NET
            context?.HttpContext?.Items?.Add("UserData", request.UserData);
#else
            HttpContext.Current.Items.Add("UserData", request.UserData);
#endif
         }
      }

      public bool AllowMultiple
      {
         get { return true; }
      }
   }
}