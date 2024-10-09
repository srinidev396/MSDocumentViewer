//*************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.
// All Rights Reserved.
//*************************************************************

﻿using Leadtools.Services.Models;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
//using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Leadtools.DocumentViewer.Models.Test
{
   /*
    We do not want to use [FromBody] on the endpoints as this retricts the endpoint from receiving anything other than JSON.
    We want endpoints that can retrieve null/JSON/XML for our custom requests
    To handle this, we implement our own ModelBinder
    */

   public class DocumentServiceModelBinder : IModelBinder
   {
      private BodyModelBinder defaultBinder;

      public DocumentServiceModelBinder(IList<IInputFormatter> formatters, IHttpRequestStreamReaderFactory readerFactory)
      {
         defaultBinder = new BodyModelBinder(formatters, readerFactory);
      }

      public async Task BindModelAsync(ModelBindingContext bindingContext)
      {
         await defaultBinder.BindModelAsync(bindingContext);

         if (bindingContext.Result.IsModelSet)
         {
            object data = null;

            if (bindingContext.Result.Model as Request != null)
               data = bindingContext.Result.Model;

            if (data != null)
               bindingContext.Result = ModelBindingResult.Success(data);
         }
         else
         {
            bindingContext.Result = ModelBindingResult.Success(null);
         }
      }
   }
}
