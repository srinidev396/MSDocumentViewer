//*************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.
// All Rights Reserved.
//*************************************************************

﻿using Leadtools.DocumentViewer.Models.Factory;
using Leadtools.DocumentViewer.Models.Page;
using Leadtools.DocumentViewer.Models.Structure;
using Leadtools.Services.Models;
using Leadtools.Services.Models.Document;
using Leadtools.Services.Models.PreCache;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
//using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Leadtools.DocumentViewer.Models.Test
{
   public class DocumentServiceModelBinderProvider : IModelBinderProvider
   {
      private readonly IList<IInputFormatter> formatters;
      private readonly IHttpRequestStreamReaderFactory readerFactory;

      public DocumentServiceModelBinderProvider(IList<IInputFormatter> formatters, IHttpRequestStreamReaderFactory readerFactory)
      {
         this.formatters = formatters;
         this.readerFactory = readerFactory;
      }

      /*
       We do not want to use [FromBody] on the endpoints as this retricts the endpoint from receiving anything other than JSON.
       We want endpoints that can retrieve null/JSON/XML for our custom requests
       So we want our provider to only handle our Requests if the BindingSource is Body/Form/null
       */
      public IModelBinder GetBinder(ModelBinderProviderContext context)
      {
         if ((context.Metadata.ModelType == typeof(Request) || context.Metadata.ModelType.IsSubclassOf(typeof(Request))) && 
            (context.BindingInfo.BindingSource == Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource.Body || context.BindingInfo.BindingSource == Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource.Form || context.BindingInfo.BindingSource == null))
            return new DocumentServiceModelBinder(formatters, readerFactory);

         return null;
      }
   }
}
