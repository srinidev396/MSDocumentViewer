// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
#if LEADTOOLS_V22_OR_LATER
using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;

using Leadtools.Services.Tools.Helpers;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Text;
using Leadtools.Caching;
using Leadtools.Services.Models.Document;
using Leadtools.Services.Tools.Exceptions;
using Leadtools.Document;
using Leadtools.Document.Converter;
using Leadtools.Document.Writer;
using System.Threading.Tasks;

namespace Leadtools.DocumentViewer.Controllers
{
   /// <summary>
   /// Used with the LEADDocument class of the LEADTOOLS Document JavaScript library for Document Editing.
   /// </summary>
   public class DocumentEditController : Controller
   {
      public DocumentEditController()
      {
         ServiceHelper.InitializeController();
      }

      internal static LEADDocument GetDocument(string documentId, string userToken)
      {
         if (documentId == null)
            throw new ArgumentException("documentId must not be null");

         ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(documentId);
         var loadFromCacheOptions = new LoadFromCacheOptions
         {
            Cache = cache,
            DocumentId = documentId,
            UserToken = userToken
         };
         var document = DocumentFactory.LoadFromCache(loadFromCacheOptions);
         return document;
      }

      /// <summary>
      ///  Loads the specified document from the cache, if possible. and returns the document editable content. Errors if the document is not in the cache.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The document's editable content could not be loaded")]
      [HttpGet("api/[controller]/[action]"), AlwaysCorsFilter]
      public Task<System.Net.Http.HttpResponseMessage> GetEditableContent([FromQuery] GetDocumentEditableContentRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         Response.ContentType = "application/json";

         var userToken = ServiceHelper.GetUserToken(Request.Headers, request);

#if NET
         return DocumentEditableContents.PushStreamResponse(userToken, request, Response.BodyWriter.AsStream());
#else
         return DocumentEditableContents.PushStreamResponse(userToken, request);
#endif
      }

      /// <summary>
      ///  Sets the document editable content. Errors if the document is not in the cache.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The document editable content could not be set")]
      [HttpPost("api/[controller]/[action]")]
      public SetDocumentEditableContentResponse SetEditableContent(SetDocumentEditableContentRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         using (var document = GetDocument(request.DocumentId, ServiceHelper.GetUserToken(this.Request.Headers, request)))
         {
            document.SetEditableContent(Encoding.UTF8.GetBytes(request.EditableContent));
            SetDocumentEditableContentResponse response = new SetDocumentEditableContentResponse();
            return response;
         }
      }

      const int MAX_FILE_SIZE = 500 * 1024 * 1024;

      private static Stream GetDownloadedFileStream(string documentId, string userToken)
      {
         if (documentId == null)
            throw new ArgumentException("documentId must not be null");

         ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(documentId);
         var loadFromCacheOptions = new LoadFromCacheOptions
         {
            Cache = cache,
            DocumentId = documentId,
            UserToken = userToken
         };
         var stm = DocumentFactory.LoadStreamFromCache(loadFromCacheOptions);
         return stm;
      }

      /// <summary>
      ///  Converts the document editable content to the target Document format.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The document editable content could not be set")]
      [HttpPost("api/[controller]/[action]")]
      public async System.Threading.Tasks.Task<ConvertResponse> ExportEditableDocument(ConvertDocumentEditableContentRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         var userToken = ServiceHelper.GetUserToken(this.Request.Headers, request);
         using (var EditableContent = GetDownloadedFileStream(request.EditableContentUri, userToken))
         {

            return await System.Threading.Tasks.Task.Factory.StartNew(() =>
               ConverterHelper.ConvertEditableContent(request.DocumentName, request.DocumentId, userToken, null, EditableContent, new EditableDocumentConverterOptions() { DocumentFormat = (DocumentFormat)request.DocumentFormat }));
         }
      }
   }

   //editable contents streamer class
   static class DocumentEditableContents
   {
#if NET
      static public Task<HttpResponseMessage> PushStreamResponse(string userToken, GetDocumentEditableContentRequest request, Stream bodyStream)
      {
         return Task.Factory.StartNew(() =>
         {
            var result = new HttpResponseMessage(HttpStatusCode.OK);
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            try
            {
               using (var document = DocumentEditController.GetDocument(request.DocumentId, userToken))
               {
                  using (var ps = new DocumentPushStream(bodyStream))
                  {
                     document.GetEditableContent(ps, new DocumentPushStreamOptions());
                  }
               }
            }
            catch (Exception ex)
            {
               var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
               {
                  Content = new StringContent(ex.Message),
                  ReasonPhrase = HttpStatusCode.InternalServerError.ToString()
               };

               return response;
            }
            finally
            {
               bodyStream.Close();
            }
            return result;
         });
      }
#else
      static public Task<HttpResponseMessage> PushStreamResponse(string userToken, GetDocumentEditableContentRequest request)
      {
         try
         {
            var result = new HttpResponseMessage(HttpStatusCode.OK);
            result.Content = new PushStreamContent((stm, ctx, transport) =>
            {
               try
               {
                  using (var document = DocumentEditController.GetDocument(request.DocumentId, userToken))
                  {
                     using (var ps = new DocumentPushStream(stm))
                     {
                        document.GetEditableContent(ps, new DocumentPushStreamOptions());
                     }
                  }
               }
               finally
               {
                  stm.Close();
               }

            }, "application/json");

            return Task.FromResult(result);
         }
         catch (Exception ex)
         {
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
               Content = new StringContent(ex.Message),
               ReasonPhrase = HttpStatusCode.InternalServerError.ToString()
            };

            return Task.FromResult(response);
         }
      }
#endif
   }

   // DocumentPushStream implementation
   class DocumentPushStream : IDocumentPushStream
   {
      private bool disposedValue;
      private bool _aborted = false;

      private Stream _stm;

      public DocumentPushStream(Stream stm)
      {
         _stm = stm;
      }

      public void Abort()
      {
         _aborted = true;
      }

      public RasterExceptionCode Write(Stream stream)
      {
         return Write(stream, null, 0, 0);
      }

      public RasterExceptionCode Write(byte[] data, int offset, int count)
      {
         return Write(null, data, offset, count);
      }

      private RasterExceptionCode Write(Stream stream, byte[] data, int dataOffset, int dataCount)
      {
         try
         {
            // Aborted? return UserAbort
            if (_aborted)
               return RasterExceptionCode.UserAbort;

            // If empty, just return success
            if ((stream == null || stream.Length == 0) && (data == null || dataCount == 0))
            {
               return RasterExceptionCode.Success;
            }

            // Copy data to the stream
            if (_stm != null)
            {
               if (stream != null)
                  stream.CopyTo(_stm);
               else
                  _stm.Write(data, dataOffset, dataCount);

               return RasterExceptionCode.Success;
            }
            else
            {
               return RasterExceptionCode.Failure;
            }
         }
         catch (IOException)
         {
            return RasterExceptionCode.UserAbort;
         }
         catch
         {
            return RasterExceptionCode.Failure;
         }
      }

      protected virtual void Dispose(bool disposing)
      {
         if (!disposedValue)
         {
            if (disposing)
            {
            }

            disposedValue = true;
         }
      }

      public void Dispose()
      {
         Dispose(disposing: true);
         GC.SuppressFinalize(this);
      }
   }
}

#endif // #if LEADTOOLS_V22_OR_LATER
