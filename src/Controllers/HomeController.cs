//*************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.
// All Rights Reserved.
//*************************************************************

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Leadtools.DocumentViewer.Controllers
{
   public class HomeController : Controller
   {
      [Route("")]
      [HttpGet]
      public IActionResult Index()
      {
         return Redirect("/index.html");
      }

      [Route("/help")]
      [HttpGet]
      public IActionResult Help()
      {
         return Redirect("swagger");
      }
   }
}