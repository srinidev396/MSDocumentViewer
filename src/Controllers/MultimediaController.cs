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
using System.Text;
using System.IO;
using System.Security.Cryptography;
using Leadtools.Caching;
using Leadtools.DocumentViewer.Models.Multimedia;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace Leadtools.DocumentViewer.Controllers
{
   public class MultimediaController : Controller
   {

      /// <summary>
      /// Used to download, convert, and delete multimedia files.
      /// </summary>


      private static HttpClient _httpClient = new HttpClient();
      private static FileCache _cache = null;
      private static CacheItemPolicy _cacheItemPolicy = null;
      private static readonly object _cacheLock = new object();
      private static System.Threading.Timer _cleanupTimer = null;
      private const string _leadMMCacheScheme = @"leadmmcache";
      private static Dictionary<string, string> _contentTypeDictionary = new Dictionary<string, string>()
      {
         { ".webm", "video/webm" },
         { ".mp4", "video/mp4" },
         { ".mov", "video/quicktime" },
         { ".dv", "video/x-dv" },
         { ".avi", "video/x-msvideo" },
         { ".mp2v", "video/mpeg" },
         { ".mod", "video/mpeg" },
         { ".mk3d", "video/x-matroska-3d" },
         { ".mts", "video/vnd.dlna.mpeg-tts" },
         { ".mp2", "video/mpeg" },
         { ".m2ts", "video/vnd.dlna.mpeg-tts" },
         { ".wm", "video/x-ms-wm" },
         { ".mpg", "video/mpeg" },
         { ".lsx", "video/x-la-asf" },
         { ".3gp2", "video/3gpp2" },
         { ".m1v", "video/mpeg" },
         { ".axv", "video/annodex" },
         { ".mpe", "video/mpeg" },
         { ".m2t", "video/vnd.dlna.mpeg-tts" },
         { ".wvx", "video/x-ms-wvx" },
         { ".wmp", "video/x-ms-wmp" },
         { ".mpv2", "video/mpeg" },
         { ".m2v", "video/mpeg" },
         { ".asx", "video/x-ms-asf" },
         { ".mpeg", "video/mpeg" },
         { ".flv", "video/x-flv" },
         { ".3g2", "video/3gpp2" },
         { ".wmv", "video/x-ms-wmv" },
         { ".mpa", "video/mpeg" },
         { ".ivf", "video/x-ivf" },
         { ".3gpp", "video/3gpp" },
         { ".asf", "video/x-ms-asf" },
         { ".3gp", "video/3gpp" },
         { ".nsc", "video/x-ms-asf" },
         { ".m4v", "video/x-m4v" },
         { ".dif", "video/x-dv" },
         { ".ts", "video/vnd.dlna.mpeg-tts" },
         { ".ogv", "video/ogg" },
         { ".ogg", "video/ogg" },
         { ".ogm", "video/ogg" },
         { ".movie", "video/x-sgi-movie" },
         { ".divx", "video/divx" },
         { ".tts", "video/vnd.dlna.mpeg-tts" },
         { ".mqv", "video/quicktime" },
         { ".asr", "video/x-ms-asf" },
         { ".mkv", "video/x-matroska" },
         { ".lsf", "video/x-la-asf" },
         { ".f4v", "video/mp4" },
         { ".wmx", "video/x-ms-wmx" },
         { ".vbk", "video/mpeg" },
         { ".qt", "video/quicktime" },
         { ".mp4v", "video/mp4" }
      };

      private static Dictionary<string, string> _extensionDictionary = new Dictionary<string, string>()
      {
         { "video/webm", ".webm" },
         { "video/x-ivf", ".ivf" },
         { "video/x-ms-asf", ".asf" },
         { "video/x-matroska-3d", ".mk3d" },
         { "video/x-la-asf", ".lsf" },
         { "video/x-matroska", ".mkv" },
         { "video/mpeg", ".mpg" },
         { "video/3gpp", ".3gp" },
         { "video/x-ms-wvx", ".wvx" },
         { "video/x-msvideo", ".avi" },
         { "video/avi", ".avi" },
         { "video/x-sgi-movie", ".movie" },
         { "video/quicktime", ".mov" },
         { "video/x-dv", ".dif" },
         { "video/x-m4v", ".m4v" },
         { "video/divx", ".divx" },
         { "video/3gpp2", ".3gp" },
         { "video/mp4", ".mp4" },
         { "video/annodex", ".axv" },
         { "video/x-ms-wmx", ".wmx" },
         { "video/x-ms-wmv", ".wmv" },
         { "video/x-ms-wmp", ".wmp" },
         { "video/x-ms-wm", ".wm" },
         { "video/vnd.dlna.mpeg-tts", ".ts" },
         { "video/ogg", ".ogg" },
         { "video/x-flv", ".flv" }
      };


      // private methods

      private class ReadWriteLock : IDisposable
      {
         private bool _disposed = false;

         private SemaphoreSlim _in = new SemaphoreSlim(1, 1);
         private SemaphoreSlim _out = new SemaphoreSlim(1, 1);
         private SemaphoreSlim _wrt = new SemaphoreSlim(0, 1);
         private UInt32 _ctrin = 0;
         private UInt32 _ctrout = 0;
         private bool _wait;

         public async Task LockRead()
         {
            await _in.WaitAsync();
            _ctrin++;
            _in.Release();
         }
         public async Task UnlockRead()
         {
            await _out.WaitAsync();
            _ctrout++;
            if (_wait && _ctrin == _ctrout)
               _wrt.Release();
            _out.Release();

         }

         public async Task LockWrite()
         {
            await _in.WaitAsync();
            await _out.WaitAsync();
            if (_ctrin == _ctrout)
            {
               _out.Release();
            }
            else
            {
               _wait = true;
               _out.Release();
               await _wrt.WaitAsync();
               _wait = false;
            }
         }
         public void UnlockWrite()
         {
            _in.Release();
         }

         public void WriteToRead()
         {
            _ctrin++;
            _in.Release();
         }

         public async Task ReadToWrite()
         {
            await _in.WaitAsync();
            await _out.WaitAsync();
            _ctrout++;
            if (_ctrin == _ctrout)
            {
               _out.Release();
            }
            else
            {
               _wait = true;
               _out.Release();
               await _wrt.WaitAsync();
               _wait = false;
            }
         }

         public void Dispose()
         {
            Dispose(true);
         }
         protected virtual void Dispose(bool disposing)
         {
            if (_disposed)
            {
               return;
            }

            if (disposing)
            {
               _in?.Dispose();
               _out?.Dispose();
               _wrt?.Dispose();
            }

            _disposed = true;
         }
      }

      private class AutoReadWriteLock : IDisposable
      {
         private bool _disposed = false;
         private ReadWriteLock _rwlock = null;
         private enum LockState
         {
            None,
            Read,
            Write
         };
         private LockState _state = LockState.None;
         public AutoReadWriteLock(ReadWriteLock rwlock)
         {
            _rwlock = rwlock;
         }
         public async Task LockRead()
         {
            if (_state == LockState.Read)
               return;
            else if (_state == LockState.Write)
               _rwlock.WriteToRead();
            else
               await _rwlock.LockRead();
            _state = LockState.Read;
         }
         public async Task LockWrite()
         {
            if (_state == LockState.Write)
               return;
            else if (_state == LockState.Read)
               await _rwlock.ReadToWrite();
            else
               await _rwlock.LockWrite();
            _state = LockState.Write;
         }

         public async Task Unlock()
         {
            if (_state == LockState.None)
               return;
            else if (_state == LockState.Read)
               await _rwlock.UnlockRead();
            else if (_state == LockState.Write)
               _rwlock.UnlockWrite();
            _state = LockState.None;
         }

         public void Dispose()
         {
            Dispose(true);
         }
         protected virtual void Dispose(bool disposing)
         {
            if (_disposed)
            {
               return;
            }

            if (disposing)
            {
               Unlock().Wait();
            }

            _disposed = true;
         }
      }


      private static ConcurrentDictionary<string, ReadWriteLock> _regionLockMap = new ConcurrentDictionary<string, ReadWriteLock>();
      private static ReadWriteLock GetRegionLock(string region)
      {
         return _regionLockMap.GetOrAdd(region, x => new ReadWriteLock());
      }

      private static async Task<AutoReadWriteLock> GetRegionReadLock(string region)
      {
         ReadWriteLock rwlock = GetRegionLock(region);
         AutoReadWriteLock alock = new AutoReadWriteLock(rwlock);
         await alock.LockRead();
         return alock;
      }

      private static async Task<AutoReadWriteLock> GetRegionWriteLock(string region)
      {
         ReadWriteLock rwlock = GetRegionLock(region);
         AutoReadWriteLock alock = new AutoReadWriteLock(rwlock);
         await alock.LockWrite();
         return alock;
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


      private bool IsCacheUrl(string url)
      {
         return !String.IsNullOrEmpty(url) && url.Trim().ToLower().StartsWith(_leadMMCacheScheme + "://");
      }


      private string CreateCacheUrl()
      {
         return _leadMMCacheScheme + @"://" + Guid.NewGuid().ToString().Replace("-", "").ToLowerInvariant();
      }

      private string ValidateCacheUrl(string url, bool canCreate)
      {
         if (string.IsNullOrEmpty(url))
         {
            if (canCreate)
               return CreateCacheUrl();
            else
               return null;
         }
         else
         {
            if (!IsCacheUrl(url))
               return null;
            return url;

         }

      }

      private string GetRegionName(string url, string token)
      {
         string lc = url.ToLower();
         if (!string.IsNullOrEmpty(token))
            lc += @"#token=" + token.ToLower();
         using (var algorithm = MD5.Create())
         {
            var hashedBytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(lc));

            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
         }
      }

      private HttpRequestMessage CreateTargetMessage(HttpContext context, Uri uri)
      {
         var requestMessage = new HttpRequestMessage();
         CopyFromOriginalRequestContentAndHeaders(context, requestMessage);
         requestMessage.RequestUri = uri;
         requestMessage.Headers.Host = uri.Host;
         requestMessage.Method = GetMethod(context.Request.Method);
         return requestMessage;
      }

      private void CopyFromOriginalRequestContentAndHeaders(HttpContext context, HttpRequestMessage requestMessage)
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
            else
            {
               requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
         }
      }

      private HttpMethod GetMethod(string method)
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

      private static CacheItemPolicy CreatePolicy(ObjectCache objectCache)
      {
         // If the FileCache has a policy set by the configuration, then use it.
         // Otherwise, check for lt.Cache.SlidingExpiration in the configuration file

         CacheItemPolicy policy = null;

         // If we have it in the cache, use it
         if (objectCache is FileCache)
         {
            FileCache fileCache = objectCache as FileCache;
            if (fileCache.DefaultPolicy != null && !fileCache.DefaultPolicy.IsInfinite)
               policy = fileCache.DefaultPolicy.Clone();
         }

         if (policy == null)
         {
            // If we have it in the service configuration, use it
            TimeSpan slidingExpiration;
            var value = ServiceHelper.GetSettingValue(ServiceHelper.Key_Cache_SlidingExpiration);
            if (value != null)
               value = value.Trim();

            if (!string.IsNullOrEmpty(value) && TimeSpan.TryParse(value, out slidingExpiration))
            {
               // Its in the configuration, use it
               policy = new CacheItemPolicy();
               policy.SlidingExpiration = slidingExpiration;
            }
            else
            {
               FileCache fileCache = objectCache as FileCache;
               // Use the default policy
               if (fileCache != null && fileCache.DefaultPolicy != null)
                  policy = fileCache.DefaultPolicy.Clone();
               else
                  policy = new CacheItemPolicy();
            }
         }
         return policy;
      }


      public static FileCache GetCache()
      {
         lock (_cacheLock)
         {
            if (_cache == null)
               return null;

            return _cache as FileCache;
         }
      }

      private static async Task RemoveExpiredItem(FileCache cache, string key, string region)
      {
         using (AutoReadWriteLock rwlock = await GetRegionWriteLock(region))
            CheckCachePolicy(cache, key, region);

      }

      private static SemaphoreSlim _removeExpired = new SemaphoreSlim(1, 1);
      private static void RemoveExpiredItems()
      {
         if (!_removeExpired.Wait(0))
            return;

         try
         {
            FileCache cache = GetCache();
            if (cache == null)
               return;

            cache.EnumerateRegions(delegate (string region)
            {
               // enumerate the keys
               cache.EnumerateKeys(region, delegate (string key)
               {
                  RemoveExpiredItem(cache, key, region).Wait();
               });
            });
         }
         catch (Exception)
         {

         }
         finally
         {
            _removeExpired.Release();
         }
      }

      private static void DeleteCacheItem(FileCache cache, string key, string region)
      {
         try
         {
            cache.DeleteItem(key, region);

            Uri fileUri = cache.GetItemExternalResource(key, region, false);
            if (fileUri != null)
               TryDeleteFile(fileUri.LocalPath);
            string cacheDir = cache.ResolveDirectory(cache.CacheDirectory);
            string directory = Path.Combine(cacheDir, region);
            TryDeleteEmptyDirectory(directory);
         }
         catch (Exception)
         {


         }
      }

      private static void DeleteCacheRegion(FileCache cache, string region)
      {
         try
         {
            string cacheDir = cache.ResolveDirectory(cache.CacheDirectory);
            string directory = Path.Combine(cacheDir, region);
            foreach (string filePath in Directory.GetFiles(directory, "*" + GetCachePolicyExt(cache)))
               TryDeleteFile(filePath);

            foreach (string filePath in Directory.GetFiles(directory, "video.*"))
               TryDeleteFile(filePath);
            TryDeleteEmptyDirectory(directory);
         }
         catch (Exception)
         {
         }


      }

      private static void CheckCachePolicy(FileCache cache, string key, string region)
      {
         try
         {
            if (!cache.CheckPolicy(key, region))
               DeleteCacheItem(cache, key, region);
         }
         catch (Exception)
         {


         }
      }

      private static void TimerCleanup(object stateInfo)
      {
         RemoveExpiredItems();
      }

      private static CacheItemPolicy GetPolicy()
      {
         lock (_cacheLock)
         {
            if (_cacheItemPolicy == null)
               return ServiceHelper.CreateForeverPolicy();
            return _cacheItemPolicy.Clone();
         }
      }


      private async Task<bool> DownloadAsync(string url, String filePath, CancellationToken requestAborted)
      {

         try
         {
            Uri uri = new Uri(url, UriKind.RelativeOrAbsolute);

            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
               using (var responseMessage = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, requestAborted))
               {
                  if (responseMessage.StatusCode != System.Net.HttpStatusCode.OK)
                     return false;

                  using (
                     Stream contentStream = await responseMessage.Content.ReadAsStreamAsync(),
                     stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                  {
                     await contentStream.CopyToAsync(stream);
                     return true;
                  }
               }
            }
         }
         catch (OperationCanceledException)
         {
            throw;
         }
         catch (Exception)
         {
            return false;
         }
      }

      private Task<int> RunProcessAsync(Process process, CancellationToken requestAborted)
      {
         var tcs = new TaskCompletionSource<int>();
         bool started = false;

         requestAborted.ThrowIfCancellationRequested();

         CancellationTokenRegistration token = requestAborted.Register(() =>
         {
            if (started)
               process.Kill();
         });


         process.StartInfo.UseShellExecute = false;
         process.StartInfo.CreateNoWindow = true;
         process.EnableRaisingEvents = true;
         process.StartInfo.RedirectStandardOutput = true;
         process.StartInfo.RedirectStandardError = true;


         process.Exited += (s, ea) =>
         {
            token.Dispose();

            if (requestAborted.IsCancellationRequested)
               tcs.SetException(new OperationCanceledException(requestAborted));
            else
               tcs.SetResult(process.ExitCode);
         };
         process.OutputDataReceived += (s, ea) => Console.WriteLine(ea.Data);
         process.ErrorDataReceived += (s, ea) => Console.WriteLine("ERR: " + ea.Data);

         started = process.Start();
         if (!started)
         {
            token.Dispose();
            throw new InvalidOperationException("Could not start process: " + process);
         }
         process.BeginOutputReadLine();
         process.BeginErrorReadLine();

         return tcs.Task;
      }


      private async Task<bool> ConvertToMP4(string source, string target, CancellationToken requestAborted)
      {

         var exePath = GetMP4ConverterPath();
         if (string.IsNullOrEmpty(exePath))
            return false;
         try
         {
            // Run the converter
            using (var process = new Process())
            {
               process.StartInfo.FileName = exePath;
               string licfile = ServiceHelper.GetCurrentLicensePath();
               string lickey = ServiceHelper.GetCurrentLicenseKey();
               if (String.IsNullOrEmpty(licfile) || String.IsNullOrEmpty(lickey))
                  process.StartInfo.Arguments = string.Format("/silent-mode \"{0}\" \"{1}\"", source, target);
               else
                  process.StartInfo.Arguments = string.Format("/silent-mode /license-file \"{0}\" /license-key \"{1}\" \"{2}\" \"{3}\"", licfile, lickey, source, target);



               UInt32 exitCode = (UInt32)await RunProcessAsync(process, requestAborted).ConfigureAwait(false);
               return (exitCode == 0);
            }
         }
         catch (OperationCanceledException)
         {
            throw;
         }
         catch (Exception)
         {
            return false;
         }
      }


      private static bool IsDirectoryEmpty(string path)
      {
         return Directory.GetFiles(path).Length == 0;
      }
      private static bool CheckEmptyDirectory(string directory)
      {
         try
         {
            return Directory.Exists(directory) && IsDirectoryEmpty(directory);
         }
         catch { return false; }
      }

      private static bool TryDeleteEmptyDirectory(string directory)
      {
         try
         {
            if (!CheckEmptyDirectory(directory))
               return false;
            Directory.Delete(directory);
            return true;
         }
         catch { return false; }
      }

      private static void TryDeleteFile(string path)
      {
         try
         {
            System.IO.File.Delete(path);

         }
         catch (Exception)
         {


         }
      }


      private string GetContentType(string path)
      {
         if (String.IsNullOrEmpty(path))
            return null;


         int i = path.IndexOf('#');
         if (i >= 0)
            path = path.Substring(0, i);

         i = path.IndexOf('?');
         if (i >= 0)
            path = path.Substring(0, i);

         i = path.LastIndexOf('/');
         if (i >= 0)
            path = path.Substring(i + 1);

         i = path.LastIndexOf('\\');
         if (i >= 0)
            path = path.Substring(i + 1);

         i = path.LastIndexOf(':');
         if (i >= 0)
            path = path.Substring(i + 1);

         i = path.LastIndexOf('.');
         if (i >= 0)
            path = path.Substring(i);
         else
            return null;

         path = path.Trim().ToLower();

         string value;

         if (!_contentTypeDictionary.TryGetValue(path, out value))
            return null;
         return value;
      }

      private string GetExtension(string contentType)
      {
         if (String.IsNullOrEmpty(contentType))
            return null;

         string value;

         if (!_extensionDictionary.TryGetValue(contentType.Trim().ToLower(), out value))
            return null;
         return value;
      }

      private static string GetCachePolicyExt(FileCache cache)
      {
         switch (cache.PolicySerializationMode)
         {
            case CacheSerializationMode.Xml:
               return ".policy.xml";
            case CacheSerializationMode.Json:
               return ".policy.json";

            case CacheSerializationMode.Binary:
            default:
               return ".policy";
         }

      }

      private string IsCached(string key, string region)
      {
         FileCache cache = GetCache();
         if (cache == null)
            return null;


         Uri fileUri = cache.GetItemExternalResource(key, region, false);
         if (fileUri == null)
            return null;
         String filePath = fileUri.LocalPath;
         if (String.IsNullOrEmpty(filePath))
            return null;
         if (!System.IO.File.Exists(filePath + GetCachePolicyExt(cache)))
            return null;
         if (!System.IO.File.Exists(filePath))
            return null;
         return filePath;
      }

      private async Task<String> DownloadToCache(string region, string source, string contentType, CancellationToken requestAborted)
      {
         FileCache cache = GetCache();
         if (cache == null)
            return null;


         try
         {



            string ext = GetExtension(contentType);
            if (String.IsNullOrEmpty(ext))
               return null;

            string key = "video" + ext;

            string filePath = IsCached(key, region);
            if (!String.IsNullOrEmpty(filePath))
               return filePath;

            Uri fileUri = cache.BeginAddExternalResource(key, region, true);
            if (fileUri == null)
               return null;
            filePath = fileUri.LocalPath;
            if (String.IsNullOrEmpty(filePath))
               return null;

            bool result = false;

            try
            {
               result = await DownloadAsync(source, filePath, requestAborted);
            }
            catch (OperationCanceledException)
            {
               throw;
            }
            catch (Exception)
            {
            }
            finally
            {
               if (!result)
                  TryDeleteFile(filePath);
               cache.EndAddExternalResource(result, key, null, GetPolicy(), region);
            }
            if (!result)
               return null;
            return filePath;
         }
         finally
         {

         }
      }

      private String GetCachedMP4(string region)
      {

         FileCache cache = GetCache();
         if (cache == null)
            return null;

         string key = "video.mp4";

         return IsCached(key, region);
      }


      private async Task<String> ConvertToCachedMP4(string region, string source, CancellationToken requestAborted)
      {
         FileCache cache = GetCache();
         if (cache == null)
            return null;

         try
         {


            string key = "video.mp4";


            string filePath = IsCached(key, region);
            if (!String.IsNullOrEmpty(filePath))
               return filePath;

            Uri fileUri = cache.BeginAddExternalResource(key, region, true);
            if (fileUri == null)
               return null;
            filePath = fileUri.LocalPath;
            if (String.IsNullOrEmpty(filePath))
               return null;

            bool result = false;

            try
            {
               result = await ConvertToMP4(source, filePath, requestAborted);
            }
            catch (OperationCanceledException)
            {
               throw;
            }
            finally
            {
               if (!result)
                  TryDeleteFile(filePath);
               cache.EndAddExternalResource(result, key, null, GetPolicy(), region);
            }
            if (!result)
               return null;
            return filePath;

         }
         finally
         {
         }
      }
      private async Task<String> UploadToCache(string region, IFormFile source, string contentType, CancellationToken requestAborted, bool append = false)
      {
         FileCache cache = GetCache();
         if (cache == null)
            return null;


         try
         {


            string ext = GetExtension(contentType);
            if (String.IsNullOrEmpty(ext))
               return null;



            string key = "video" + ext;

            if (!append)
            {
               DeleteCacheItem(cache, key, region);
            }

            Uri fileUri = cache.BeginAddExternalResource(key, region, true);
            if (fileUri == null)
               return null;
            String filePath = fileUri.LocalPath;
            if (String.IsNullOrEmpty(filePath))
               return null;

            bool result = false;

            try
            {
               if (append)
               {
                  using (FileStream stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None))
                  {
                     await source.CopyToAsync(stream, requestAborted);
                     result = true;
                  }

               }
               else
               {
                  using (FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                  {
                     await source.CopyToAsync(stream, requestAborted);
                     result = true;
                  }
               }
            }
            catch (OperationCanceledException)
            {
               throw;
            }
            catch (Exception)
            {
            }
            finally
            {
               if (!result)
                  TryDeleteFile(filePath);
               cache.EndAddExternalResource(result, key, null, GetPolicy(), region);
            }
            if (!result)
               return null;
            return filePath;
         }
         finally
         {

         }
      }


      private void DeleteFromCache(string region, string contentType)
      {
         FileCache cache = GetCache();
         if (cache == null)
            return;

         try
         {

            if (String.IsNullOrEmpty(contentType))
            {
               DeleteCacheRegion(cache, region);
            }
            else
            {
               string ext = GetExtension(contentType);
               if (String.IsNullOrEmpty(ext))
                  return;
               string key = "video" + ext;
               DeleteCacheItem(cache, key, region);

            }
         }
         catch (OperationCanceledException)
         {
            throw;
         }
         catch (Exception)
         {
         }
      }

      private bool IsVideo(string mime)
      {
         return !string.IsNullOrEmpty(mime) && mime.ToLower().StartsWith("video/");
      }

      private bool IsMP4Video(string mime)
      {
         return !string.IsNullOrEmpty(mime) && mime.ToLower().Equals("video/mp4");
      }

      private bool ShouldConvert(string sourceMime, string targetMime)
      {
         return IsVideo(sourceMime) && (!string.IsNullOrEmpty(targetMime) && !sourceMime.ToLower().Equals(targetMime.ToLower()));
      }

      private bool CanConvert(string sourceMime, string targetMime)
      {
         return IsVideo(sourceMime) && IsMP4Video(targetMime) && !string.IsNullOrEmpty(GetMP4ConverterPath()) && IsMP4ConverterLicensed();
      }
      private bool ShouldStore(string sourceMime, string targetMime)
      {
         return IsMP4Video(sourceMime) && IsMP4Video(targetMime);
      }


      // public methods

      public MultimediaController()
      {
         ServiceHelper.InitializeController();
      }

      [NonAction]
      public static string GetMP4ConverterPath()
      {
         var exePath = ServiceHelper.GetSettingValue(ServiceHelper.Key_Multimedia_MP4ConverterPath);
         if (string.IsNullOrEmpty(exePath))
            return null;
         exePath = ServiceHelper.GetAbsolutePath(exePath);
         if (!System.IO.File.Exists(exePath))
            return null;
         return exePath;
      }
      [NonAction]
      public static bool IsMP4ConverterLicensed()
      {
         if (RasterSupport.IsLocked(RasterSupportType.MultimediaVideoStreaming) && RasterSupport.IsLocked(RasterSupportType.MultimediaMpeg2Transport))
            return false;
         return true;
      }

      [NonAction]
      public static bool IsCacheAvalable()
      {
         lock (_cacheLock)
         {
            return (_cache != null);
         }
      }

      [NonAction]
      public static FileCache CreateCache()
      {
         try
         {

            lock (_cacheLock)
            {
               if (_cache != null)
                  return _cache as FileCache;

               ObjectCache objectCache = null;
               CacheItemPolicy policy = null;
               try
               {
                  string cacheConfigFile = ServiceHelper.GetSettingValue(ServiceHelper.Key_Multimedia_Cache_ConfigFile);
                  cacheConfigFile = ServiceHelper.GetAbsolutePath(cacheConfigFile);
                  if (string.IsNullOrEmpty(cacheConfigFile))
                     return null;


                  // Set the base directory of the cache (for resolving any relative paths) to this project's path
                  var additional = new Dictionary<string, string>();
                  additional.Add(ObjectCache.BASE_DIRECTORY_KEY, ServiceHelper.WebRootPath);

                  using (var cacheConfigStream = System.IO.File.OpenRead(cacheConfigFile))
                     objectCache = ObjectCache.CreateFromConfigurations(cacheConfigStream, additional);

                  if (objectCache == null)
                     return null;

                  policy = CreatePolicy(objectCache);
                  if (policy == null)
                     return null;
               }
               catch (Exception)
               {
                  return null;
               }
               if (objectCache == null || policy == null)
                  return null;

               if (!(objectCache is FileCache))
                  return null;

               _cache = objectCache as FileCache;
               _cacheItemPolicy = policy;

               long cleanupInterval = (60 * 60); // 1 hour default

               try
               {
                  cleanupInterval = long.Parse(ServiceHelper.GetSettingValue(ServiceHelper.Key_Multimedia_Cache_CleanupInterval));
               }
               catch (Exception)
               {
               }
               _cleanupTimer = new System.Threading.Timer(TimerCleanup, null, 0L, cleanupInterval * 1000);

               return _cache as FileCache;
            }
         }
         catch (Exception)
         {
            return null;
         }
      }

      [NonAction]
      public static void Cleanup()
      {
         try
         {

            lock (_cacheLock)
            {
               if (_cleanupTimer != null)
               {
                  _cleanupTimer.Dispose();
                  _cleanupTimer = null;
               }
               _cacheItemPolicy = null;
               _cache = null;

            }
         }
         catch (Exception)
         {

         }
      }

      /// <summary>
      /// Returns a video file.
      /// If the mime parameter specifies video/mp4. the controller will attempt to convert the video if it does not match.
      /// </summary>
      private async Task<IActionResult> GetVideoHelper(HttpContext context, GetVideoRequest request, string mime)
      {
         AutoReadWriteLock regionLock = null;
         try
         {

            if (request == null)
               return new BadRequestResult();

            if (string.IsNullOrEmpty(request.url))
               return new BadRequestResult();

            if (!String.IsNullOrEmpty(request.mime))
               mime = request.mime;


            string url = Uri.UnescapeDataString(request.url);
            if (string.IsNullOrEmpty(url))
               return new BadRequestResult();

            Uri uri = new Uri(url, UriKind.RelativeOrAbsolute);

            string region = GetRegionName(url, ServiceHelper.GetUserToken(this.Request.Headers, request));

            string contentType = GetContentType(url);
            if (String.IsNullOrEmpty(contentType))
               contentType = "application/octet-stream";


            if (IsCacheUrl(url))
            {
               regionLock = await GetRegionReadLock(region);
               String cachePath = GetCachedMP4(region);
               if (cachePath == null)
                  return new NotFoundResult();
               PhysicalFileResult result = new PhysicalFileResult(cachePath, "video/mp4");
               result.EnableRangeProcessing = true;
               context.Response.RegisterForDispose(regionLock);
               regionLock = null;
               return result;
            }
            else if (!uri.IsAbsoluteUri)
            {


               string path = ServiceHelper.GetAbsoluteWebPath(url);
               if (!System.IO.File.Exists(path))
                  return new NotFoundResult();

               if (ShouldConvert(contentType, mime))
               {

                  if (!CanConvert(contentType, mime))
                     return new NotFoundResult();

                  regionLock = await GetRegionReadLock(region);
                  String cachePath = GetCachedMP4(region);
                  if (cachePath == null)
                  {
                     await regionLock.LockWrite();
                     cachePath = await ConvertToCachedMP4(region, path, context.RequestAborted);
                     await regionLock.LockRead();
                  }
                  if (cachePath != null)
                  {
                     PhysicalFileResult result = new PhysicalFileResult(cachePath, "video/mp4");
                     result.EnableRangeProcessing = true;
                     context.Response.RegisterForDispose(regionLock);
                     regionLock = null;
                     return result;
                  }
                  else
                  {
                     return new NotFoundResult();
                  }
               }
               else
               {
                  if (regionLock != null)
                  {
                     regionLock.Dispose();
                     regionLock = null;
                  }
                  PhysicalFileResult result = new PhysicalFileResult(path, contentType);
                  result.EnableRangeProcessing = true;
                  return result;
               }
            }
            else
            {

               if (ShouldConvert(contentType, mime))
               {
                  if (!CanConvert(contentType, mime))
                     return new NotFoundResult();

                  regionLock = await GetRegionReadLock(region);
                  String cachePath = GetCachedMP4(region);
                  if (cachePath == null)
                  {
                     await regionLock.LockWrite();
                     String cacheSourcePath = await DownloadToCache(region, url, contentType, context.RequestAborted);
                     if (cacheSourcePath != null)
                        cachePath = await ConvertToCachedMP4(region, cacheSourcePath, context.RequestAborted);
                     await regionLock.LockRead();
                  }
                  if (cachePath != null)
                  {
                     PhysicalFileResult result = new PhysicalFileResult(cachePath, "video/mp4");
                     result.EnableRangeProcessing = true;
                     context.Response.RegisterForDispose(regionLock);
                     regionLock = null;
                     return result;
                  }
                  else
                  {
                     return new NotFoundResult();
                  }

               }
               else
               {
                  using (HttpRequestMessage targetRequstMessage = CreateTargetMessage(context, uri))
                  {
                     HttpResponseMessage responseMessage = await _httpClient.SendAsync(targetRequstMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
                     context.Response.RegisterForDispose(responseMessage);
                     return new HttpResponseMessageResult(responseMessage);
                  }
               }
            }
         }
         catch (OperationCanceledException)
         {
            throw;
         }
         catch (Exception)
         {

            throw;
         }
         finally
         {
            if (regionLock != null)
               regionLock.Dispose();
         }


      }

      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "Multimedia GetVideo action failed")]
      [HttpHead("api/[controller]/[action]"), HttpGet("api/[controller]/[action]"), AlwaysCorsFilter]
      public async Task<IActionResult> GetVideo([FromQuery] GetVideoRequest request)
      {
         HttpContext context = this.HttpContext;
         return await GetVideoHelper(context, request, null);
      }


      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "Multimedia GetVideo action failed")]
      [HttpHead("api/[controller]/GetVideo.mp4"), HttpGet("api/[controller]/GetVideo.mp4"), AlwaysCorsFilter]
      public async Task<IActionResult> GetVideoMP4([FromQuery] GetVideoRequest request)
      {
         HttpContext context = this.HttpContext;
         return await GetVideoHelper(context, request, "video/mp4");
      }


      /// <summary>
      /// Converts a video file to the specified mime type.
      /// The mime type must currently equal video/mp4.
      /// </summary>

      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "Multimedia ConvertVideo action failed")]
      [HttpPost("api/[controller]/[action]")]
      public async Task<IActionResult> ConvertVideo([FromQuery] ConvertVideoRequest request, IFormFile file)
      {
         try
         {
            var context = this.HttpContext;

            if (request == null)
               return new BadRequestResult();

            if (string.IsNullOrEmpty(request.mime))
               return new BadRequestResult();


            if (file == null)
               return new BadRequestResult();

            if (!string.IsNullOrEmpty(request.operation))
            {
               String operation = request.operation.Trim().ToLower();


               string url = null;

               if (!string.IsNullOrEmpty(request.url))
                  url = Uri.UnescapeDataString(request.url);

               url = ValidateCacheUrl(url, operation.Equals("begin-upload"));
               if (string.IsNullOrEmpty(url))
                  return new BadRequestResult();

               string region = GetRegionName(url, ServiceHelper.GetUserToken(this.Request.Headers, request));

               string contentType = GetContentType(file.FileName);
               if (string.IsNullOrEmpty(contentType) && !string.IsNullOrEmpty(file.ContentType))
                  contentType = file.ContentType;
               if (string.IsNullOrEmpty(contentType))
                  return new BadRequestResult();

               if (operation.Equals("begin-upload"))
               {
                  if (!ShouldStore(contentType, request.mime) && !CanConvert(contentType, request.mime))
                     return new BadRequestResult();

                  using (AutoReadWriteLock regionLock = await GetRegionWriteLock(region))
                  {
                     String cachePath = await UploadToCache(region, file, contentType, context.RequestAborted);
                     if (cachePath == null)
                        return new NotFoundResult();
                     ConvertVideoResponse result = new ConvertVideoResponse { url = url };
                     return new JsonResult(result);
                  }


               }
               else if (operation.Equals("end-upload"))
               {
                  if (!ShouldStore(contentType, request.mime) && !CanConvert(contentType, request.mime))
                     return new BadRequestResult();
                  using (AutoReadWriteLock regionLock = await GetRegionWriteLock(region))
                  {
                     String cachePath = await UploadToCache(region, file, contentType, context.RequestAborted, true);
                     if (cachePath == null)
                        return new NotFoundResult();

                     if (!ShouldStore(contentType, request.mime) && CanConvert(contentType, request.mime))
                     {
                        cachePath = await ConvertToCachedMP4(region, cachePath, context.RequestAborted);
                        if (cachePath == null)
                           return new NotFoundResult();
                     }

                     ConvertVideoResponse result = new ConvertVideoResponse { url = url };
                     return new JsonResult(result);
                  }


               }
               else if (operation.Equals("append-upload"))
               {
                  if (!ShouldStore(contentType, request.mime) && !CanConvert(contentType, request.mime))
                     return new BadRequestResult();

                  using (AutoReadWriteLock regionLock = await GetRegionWriteLock(region))
                  {
                     String cachePath = await UploadToCache(region, file, contentType, context.RequestAborted, true);
                     if (cachePath == null)
                        return new NotFoundResult();
                     ConvertVideoResponse result = new ConvertVideoResponse { url = url };
                     return new JsonResult(result);
                  }

               }
               else
               {
                  return new BadRequestResult();

               }
            }
            else
            {


               string url = null;

               if (!string.IsNullOrEmpty(request.url))
                  url = Uri.UnescapeDataString(request.url);

               url = ValidateCacheUrl(url, true);
               if (string.IsNullOrEmpty(url))
                  return new BadRequestResult();

               string contentType = GetContentType(file.FileName);
               if (string.IsNullOrEmpty(contentType) && !string.IsNullOrEmpty(file.ContentType))
                  contentType = file.ContentType;
               if (string.IsNullOrEmpty(contentType))
                  return new BadRequestResult();

               string region = GetRegionName(url, ServiceHelper.GetUserToken(this.Request.Headers, request));

               if (ShouldStore(contentType, request.mime))
               {
                  using (AutoReadWriteLock regionLock = await GetRegionWriteLock(region))
                  {
                     String cachePath = await UploadToCache(region, file, contentType, context.RequestAborted);
                     if (cachePath == null)
                        return new NotFoundResult();
                     ConvertVideoResponse result = new ConvertVideoResponse { url = url };
                     return new JsonResult(result);
                  }

               }
               else if (CanConvert(contentType, request.mime))
               {

                  using (AutoReadWriteLock regionLock = await GetRegionWriteLock(region))
                  {
                     String cachePath = await UploadToCache(region, file, contentType, context.RequestAborted);
                     if (cachePath != null)
                        cachePath = await ConvertToCachedMP4(region, cachePath, context.RequestAborted);
                     if (cachePath == null)
                        return new NotFoundResult();
                     ConvertVideoResponse result = new ConvertVideoResponse { url = url };
                     return new JsonResult(result);
                  }
               }
               else
               {
                  return new BadRequestResult();
               }
            }
         }
         catch (OperationCanceledException)
         {
            throw;
         }
         catch (Exception)
         {

            throw;
         }
      }

      /// <summary>
      /// Deletes a video from the multimedia cache.
      /// If the mime type is specified, then only that that associated type will be removed from the cache.
      /// Otherwise, the entire video will be removed.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "Multimedia DeleteCachedVideo action failed", MethodName = "DeleteCachedVideo")]
      [HttpPost("api/[controller]/[action]")]
      public async Task<IActionResult> DeleteCachedVideo([FromQuery] DeleteCachedVideoRequest request)
      {
         var context = this.HttpContext;

         if (request == null)
            return new BadRequestResult();

         string url = Uri.UnescapeDataString(request.url);
         if (string.IsNullOrEmpty(url))
            return new BadRequestResult();

         string region = GetRegionName(url, ServiceHelper.GetUserToken(this.Request.Headers, request));

         using (AutoReadWriteLock regionLock = await GetRegionWriteLock(region))
         {
            DeleteFromCache(region, request.mime);
         }

         DeleteCachedVideoResponse result = new DeleteCachedVideoResponse();
         return new JsonResult(result);
      }

   }



}