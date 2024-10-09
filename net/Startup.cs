//*************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.
// All Rights Reserved.
//*************************************************************

using System;
using System.IO;
using System.Web;
using DocumentServiceCore.Tools;
using Leadtools.DocumentViewer.Models.Test;
using Leadtools.Services.Tools.Exceptions;
using Leadtools.Services.Tools.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using NLog.Extensions.Logging;
using NLog;

namespace Leadtools.DocumentViewer
{
    public static class Startup
    {
        
        public static void ConfigBuilder(WebApplicationBuilder builder)
        {
            builder.Services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddMvc(config =>
            {
                // Add our new Exception Filter
                config.Filters.Add(new GlobalExceptionFilterAttribute());
                config.Filters.Add(new RequestResponseFilter());
                config.ReturnHttpNotAcceptable = false;
                config.EnableEndpointRouting = false;//so we can call app.UseMvc();
            })
            //.AddJsonOptions(jsonOptions =>
            //{
            //   jsonOptions.JsonSerializerOptions.WriteIndented = true;
            //   jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            //})
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                options.SerializerSettings.ContractResolver = new NotToStringContractResolver();
                options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Include;
                options.SerializerSettings.NullValueHandling = NullValueHandling.Include;
            })
            .AddMvcOptions(options =>
            {
                IHttpRequestStreamReaderFactory readerFactory = builder.Services.BuildServiceProvider().GetRequiredService<IHttpRequestStreamReaderFactory>();
                options.ModelBinderProviders.Insert(0, new DocumentServiceModelBinderProvider(options.InputFormatters, readerFactory));
            })
            .AddWebApiConventions()
            .AddXmlSerializerFormatters();//keep this last

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                builder => builder
                .WithOrigins(ServiceHelper.CORSOrigins)
                .WithHeaders(ServiceHelper.CORSHeaders)
                .WithMethods(ServiceHelper.CORSMethods));
            });

            builder.Logging.AddConsole();

            //builder.Services.AddSwaggerGen(options =>
            //{
            //    options.CustomSchemaIds(type => type.FullName);

            //    var filePath = Path.Combine(builder.Environment.WebRootPath, "DocumentService.xml");
            //    options.IncludeXmlComments(filePath);
            //});
        }

        public static void ConfigApp(WebApplication app)
        {
            var appsettings_json = Path.Combine(app.Environment.ContentRootPath, @"appsettings.json");
            var configBuilder = new ConfigurationBuilder()
               .SetBasePath(app.Environment.ContentRootPath)
               .AddJsonFile(appsettings_json);
            var config = configBuilder.Build();
            ServiceHelper.ContentRootPath = app.Environment.ContentRootPath;
            ServiceHelper.WebRootPath = app.Environment.WebRootPath;
            ServiceHelper.InitializeService(config);
        }

        public static void InitializeApp(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                //app.UseSwagger();
                //app.UseSwaggerUI();
            }

            {
                if (app.Environment.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

                //   app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, @"wwwroot")),
                    RequestPath = ""
                });

                app.UseStaticHttpContext();

                app.UseCors("CorsPolicy");
                app.UseMvc();

                // Enable middleware to serve generated Swagger as a JSON endpoint.
                //app.UseSwagger();
                //app.UseSwaggerUI(c =>
                //{
                //    //c.SwaggerEndpoint($"v{ServiceHelper.LTVersion}/swagger.json", "FUSIONRMS Document Service");
                //    c.SwaggerEndpoint($"v1/swagger.json", "FUSIONRMS Document Service");
                //});

                app.UseMvc(routes =>
                {
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
            }
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
                    var objectContract = base.CreateObjectContract(objectType);

                    //we need to skip converters for these types for compatibility with Net 4.x
                    if (objectType == typeof(LeadLengthD) ||
                       objectType == typeof(LeadMatrix) ||
                       objectType == typeof(LeadPointD) ||
                       objectType == typeof(LeadRectD) ||
                       objectType == typeof(LeadSizeD))
                    {
                        objectContract.Converter = null;
                    }

                    return objectContract;
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
