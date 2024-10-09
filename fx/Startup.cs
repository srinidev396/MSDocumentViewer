//*************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.
// All Rights Reserved.
//*************************************************************

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using DocumentServiceCore.Tools;
using Leadtools.DocumentViewer.Controllers;
using Leadtools.DocumentViewer.Models.Test;
using Leadtools.Services.Tools.Exceptions;
using Leadtools.Services.Tools.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;

namespace Leadtools.DocumentViewer
{
   public class Startup
   {
      public IConfiguration Configuration { get; set; }
      public static IHostingEnvironment HostingEnvironment { get; set; }

      public Startup(IHostingEnvironment env)
      {
         HostingEnvironment = env;
         var appsettings_json = Path.Combine(env.ContentRootPath, @"appsettings.json");

         var configBuilder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile(appsettings_json);

         var config = configBuilder.Build();

         ServiceHelper.ContentRootPath = env.ContentRootPath;
         ServiceHelper.WebRootPath = env.WebRootPath;
         ServiceHelper.InitializeService(config);
      }

      private void Shutdown()
      {
         AnalyticsController.CleanupService();
         ServiceHelper.CleanupService();
      }

      // This method gets called by the runtime. Use this method to add services to the container.
      // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
      public void ConfigureServices(IServiceCollection services)
      {
         services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
         services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
         services.AddMvc(config =>
         {
            // Add our new Exception Filter
            config.Filters.Add(new GlobalExceptionFilterAttribute());
            config.Filters.Add(new RequestResponseFilter());
            config.ReturnHttpNotAcceptable = false;
         })
         .AddXmlSerializerFormatters()
         .AddJsonOptions(options =>
         {
            options.SerializerSettings.Formatting = Formatting.Indented;
            options.SerializerSettings.ContractResolver = new NotToStringContractResolver();
         })
         .AddMvcOptions(options =>
         {
            IHttpRequestStreamReaderFactory readerFactory = services.BuildServiceProvider().GetRequiredService<IHttpRequestStreamReaderFactory>();
            options.ModelBinderProviders.Insert(0, new DocumentServiceModelBinderProvider(options.InputFormatters, readerFactory));
         })
         .AddWebApiConventions();

         services.AddCors(options =>
         {
            options.AddPolicy("CorsPolicy",
               builder => builder
               .WithOrigins(ServiceHelper.CORSOrigins)
               .WithHeaders(ServiceHelper.CORSHeaders)
               .WithMethods(ServiceHelper.CORSMethods));
         });

         services.AddSwaggerGen(c =>
         {
            c.SwaggerDoc($"v{ServiceHelper.LTVersion}", new Info
            {
               Title = "LEADTOOLS Document Service Help Page",
               Description = "The LEADTOOLS Document Viewer SDK is an OEM-ready, document-viewing solution for .NET (C# & VB), Java, and HTML5/JavaScript. Developers can create robust, fully featured applications with rich document-viewing features, including text search, annotation, memory-efficient paging, inertial scrolling, and vector display. With only a few lines of code, the LEADTOOLS Document Viewer can be added to any project. It can be used to view raster and document formats alike, making it ideal for Enterprise Content Management (ECM), document retrieval, and document normalization solutions.",
               TermsOfService = $"https://www.leadtools.com/help/sdk/v{ServiceHelper.LTVersion}/licensing/",
               Contact = new Contact { Name = "LEAD Technologies Inc.", Url = "https://www.leadtools.com/" },
               License = new License { Name = "Licensing", Url = "https://www.leadtools.com/corporate/licensing" }
            });

            // Avoid duplicate names in different namespaces
            c.CustomSchemaIds(t => t.FullName);

            var filePath = Path.Combine(HostingEnvironment.WebRootPath, "DocumentService.xml");
            c.IncludeXmlComments(filePath);
         });
      }

      // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
      public void Configure(IApplicationBuilder app, IApplicationLifetime applicationLifetime, IHostingEnvironment env, ILoggerFactory loggerFactory)
      {
         loggerFactory.AddConsole();

         if (env.IsDevelopment())
         {
            app.UseDeveloperExceptionPage();
         }

         app.UseHttpsRedirection();
         app.UseStaticFiles();
         app.UseStaticFiles(new StaticFileOptions
         {
            FileProvider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, @"wwwroot")),
            RequestPath = ""
         });
      
         app.UseStaticHttpContext();

         app.UseCors("CorsPolicy");
         app.UseMvc();

         // Enable middleware to serve generated Swagger as a JSON endpoint.
         app.UseSwagger();
         app.UseSwaggerUI(c =>
         {
            c.SwaggerEndpoint($"v{ServiceHelper.LTVersion}/swagger.json", "LEADTOOLS Document Service");
         });

         app.UseMvc(routes =>
         {
            //web api compat
            routes.MapWebApiRoute("webapi-named-action", "api/webapi/{controller}/{id?}");

            // For retrieving items from URL from the cache, like converted documents
            routes.MapRoute(
                  name: "DocumentViewerCaching",
                  template: "api/Cache/Item/{region}/{key}",
                  defaults: new { controller = "Cache", action = "Item" });

            // If you change "api", change it in CacheController as well.
            routes.MapRoute(
                  name: "DocumentViewerApi",
                  template: "api/{controller}/{action}",
                  defaults: null);
         });

         applicationLifetime.ApplicationStopping.Register(Shutdown);
      }

      /*
      * A "ContractResolver" in Newtonsoft's Json.NET is used to control how objects are
      * deserialized and serialized with JSON. in Application_Start(), we change it from
      * "DefaultContractResolver" to this one.
      * NotToStringContractResolver comes from CamelCasePropertyNamesContractResolver,
      * which makes all properties camelCase.
      * 
      * The added code below is used when the serializer is serializing the types for 
      * the return. We override the CreateContract method to see what contract was
      * going to be used. If we had a non-string that was being serialized to a string,
      * we overrule that decision and make it serialize as an object.
      * 
      * relevant area of Json.NET source:
      * https://github.com/JamesNK/Newtonsoft.Json/blob/52d9c0fca365ebc4342c612490a9d8bde4f65841/Src/Newtonsoft.Json/Serialization/DefaultContractResolver.cs
      */
      class NotToStringContractResolver : CamelCasePropertyNamesContractResolver
      {
         protected override JsonContract CreateContract(Type objectType)
         {
            JsonContract contract = base.CreateContract(objectType);
            if (objectType != typeof(string) && (contract is JsonStringContract))
            {
               // We don't want a string contract unless the objectType was actually a string
               return base.CreateObjectContract(objectType);
            }
            return base.CreateContract(objectType);
         }
      }
   }
}

namespace System.Web
{
   public static class HttpContext
   {
      private static IHttpContextAccessor _contextAccessor;

      public static Microsoft.AspNetCore.Http.HttpContext Current => _contextAccessor.HttpContext;

      internal static void Configure(IHttpContextAccessor contextAccessor)
      {
         _contextAccessor = contextAccessor;
      }
   }

   public static class StaticHttpContextExtensions
   {
      public static IApplicationBuilder UseStaticHttpContext(this IApplicationBuilder app)
      {
         var httpContextAccessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
         System.Web.HttpContext.Configure(httpContextAccessor);
         return app;
      }
   }
}
