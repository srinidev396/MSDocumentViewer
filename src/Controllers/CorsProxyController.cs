// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using Leadtools.Services.Tools.Exceptions;
using Leadtools.Services.Tools.Helpers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Leadtools.DocumentViewer.Controllers
{
   /// <summary>
   /// Used with local load document mode.
   /// </summary>
   public class CorsProxyController : Controller
   {
      private static HttpClient _httpClient = new HttpClient();

      public CorsProxyController()
      {
         ServiceHelper.InitializeController();
      }
      private class HttpResponseMessageResult : IActionResult
      {
         private readonly HttpResponseMessage _responseMessage;

         public HttpResponseMessageResult(HttpResponseMessage responseMessage)
         {
            _responseMessage = responseMessage; // could add throw if null
         }

         public async Task ExecuteResultAsync(ActionContext context)
         {
            context.HttpContext.Response.StatusCode = (int)_responseMessage.StatusCode;

            foreach (var header in _responseMessage.Content.Headers)
            {
               try
               {
                  context.HttpContext.Response.Headers.Add(header.Key, header.Value.ToArray());
               }
               catch (Exception)
               {
               }
            }
            foreach (var header in _responseMessage.Headers)
            {
               if (!header.Key.Equals("X-Powered-By") && !header.Key.Equals("Server"))
               {
                  try
                  {
                     context.HttpContext.Response.Headers.Add(header.Key, header.Value.ToArray());
                  }
                  catch (Exception)
                  {
                  }
               }
            }
            using (var stream = await _responseMessage.Content.ReadAsStreamAsync())
            {
               await stream.CopyToAsync(context.HttpContext.Response.Body);
               await context.HttpContext.Response.Body.FlushAsync();
            }
         }
      }
      private static void CopyFromOriginalRequestContentAndHeaders(HttpContext context, HttpRequestMessage requestMessage)
      {
         string requestMethod = context.Request.Method;
         if (!HttpMethods.IsGet(requestMethod) &&
            !HttpMethods.IsHead(requestMethod) &&
            !HttpMethods.IsDelete(requestMethod) &&
            !HttpMethods.IsTrace(requestMethod))
         {
            var streamContent = new StreamContent(context.Request.Body);
            requestMessage.Content = streamContent;
         }

         foreach (var header in context.Request.Headers)
         {
            if (header.Key.Length > 8 && header.Key.Substring(0, 8).Equals("Content-"))
            {
               if (requestMessage.Content != null)
                  requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
            else if (header.Key.Equals("Accept-Encoding"))
            {
               string[] headerValue = header.Value.ToArray();
               if (headerValue.Length > 0)
               {
                  List<string> list = headerValue[0].Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                  int i = list.IndexOf("br");
                  if (i >= 0)
                     list.RemoveAt(i);
                  headerValue[0] = String.Join(", ", list);
               }
               requestMessage.Headers.TryAddWithoutValidation(header.Key, headerValue);
            }
            else
            {
               requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
         }
      }

      private static HttpMethod GetMethod(string method)
      {
         if (HttpMethods.IsDelete(method)) return HttpMethod.Delete;
         if (HttpMethods.IsGet(method)) return HttpMethod.Get;
         if (HttpMethods.IsHead(method)) return HttpMethod.Head;
         if (HttpMethods.IsOptions(method)) return HttpMethod.Options;
         if (HttpMethods.IsPost(method)) return HttpMethod.Post;
         if (HttpMethods.IsPut(method)) return HttpMethod.Put;
         if (HttpMethods.IsTrace(method)) return HttpMethod.Trace;
         return new HttpMethod(method);
      }


      private static HttpRequestMessage CreateTargetMessage(HttpContext context, Uri uri)
      {
         var requestMessage = new HttpRequestMessage();
         CopyFromOriginalRequestContentAndHeaders(context, requestMessage);
         requestMessage.RequestUri = uri;
         requestMessage.Headers.Host = uri.Host;
         requestMessage.Method = GetMethod(context.Request.Method);
         return requestMessage;
      }

      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "Proxy failed")]
      [HttpHead("api/[controller]/[action]"), HttpGet("api/[controller]/[action]")]
      public async Task<IActionResult> Proxy(string url)
      {
         var context = this.HttpContext;
         Uri uri = new Uri(url);
         using (HttpRequestMessage targetRequstMessage = CreateTargetMessage(context, uri))
         {
            HttpResponseMessage responseMessage = await _httpClient.SendAsync(targetRequstMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
            context.Response.RegisterForDispose(responseMessage);
            return new HttpResponseMessageResult(responseMessage);
         }
      }


   }
}
