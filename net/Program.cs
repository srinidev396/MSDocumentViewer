//*************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.
// All Rights Reserved.
//*************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DocumentServiceCore.Tools;
using Leadtools.DocumentViewer.Controllers;
using Leadtools.DocumentViewer.Models.Test;
using Leadtools.Services.Tools.Exceptions;
using Leadtools.Services.Tools.Helpers;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog.Extensions.Logging;
using NLog;

namespace Leadtools.DocumentViewer
{
    public class Program
    {
        public static ILogger<Program> _logger;
        public static void Main(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddNLog();
            });
            _logger = loggerFactory.CreateLogger<Program>();
            var app = BuildWebApplication(args);
            app.Lifetime.ApplicationStopping.Register(Shutdown);
            _logger.LogWarning("Document Service Started!");
            app.Run();
            
        }

        public static WebApplication BuildWebApplication(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            LogManager.Configuration = new NLogLoggingConfiguration(builder.Configuration.GetSection("NLog"));
           
            Startup.ConfigBuilder(builder);
            builder.Logging.AddNLog();
            builder.Logging.AddConsole();
            builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
            builder.Services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            var app = builder.Build();

            Startup.ConfigApp(app);
            Startup.InitializeApp(app);

            return app;
        }

        private static void Shutdown()
        {
            AnalyticsController.CleanupService();
            ServiceHelper.CleanupService();
        }
    }
}
