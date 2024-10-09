// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.IO;

using Leadtools.Document;
using Leadtools.Document.Analytics;
using Leadtools.Document.Data;

using Leadtools.Services.Models.Analytics;
using Leadtools.Services.Tools.Exceptions;
using Leadtools.Services.Tools.Helpers;

using Leadtools.Annotations.Engine;
using Leadtools.Ocr;
using Leadtools.Caching;
using System.Text;

using System.Diagnostics;
using Leadtools.Document.Unstructured;
using Leadtools.Services.Models;

namespace Leadtools.DocumentViewer.Controllers
{
   /// <summary>
   /// Used with the DocumentFactory class of the LEADTOOLS Document JavaScript library.
   /// </summary>
   public class AnalyticsController : Controller
   {
      private static IDictionary<string, RuleSet> _ruleSets;
      private static IDictionary<string, ActionSet> _actionSets;

      public static IDictionary<string, RuleSet> GetRules()
      {
         return _ruleSets;
      }

      public static IDictionary<string, ActionSet> GetActions()
      {
         return _actionSets;
      }

      public static string GetFormsDirectory()
      {
         return ServiceHelper.GetAbsoluteWebPath(ServiceHelper.GetSettingValue(ServiceHelper.Key_Analytics_Directory));
      }

      public static void InitializeService()
      {
         // Cache the rules
         LoadRuleSets();
         CreateActionSets();
      }

      private static void CreateActionSets()
      {
         var set = new Dictionary<string, ActionSet>();

         var redactionSet = new ActionSet()
         {
            Id = "Redactor",
            Title = "Redactor",
            Description = "Redacts all regions in the result set.",
            generator = () => new RedactAction()
         };

         var highlightSet = new ActionSet()
         {
            Id = "Highlighter",
            Title = "Highlighter",
            Description = "Highlights all regions in the result set.",
            generator = () => new HighlightAction()
         };

         // If you want to add any other custom actions, they can be registered in the service here.
         set.Add(redactionSet.Id, redactionSet);
         set.Add(highlightSet.Id, highlightSet);

         _actionSets = set;
      }

      private static void LoadRuleSets()
      {
         _ruleSets = new Dictionary<string, RuleSet>();

         // Load the rule sets found
         string formsDir = GetFormsDirectory();
         if (Directory.Exists(formsDir))
         {
            foreach (string file in Directory.EnumerateFiles(formsDir, "*.json"))
            {
               // Try to load it, if it works, extract its name and add it to our sets
               var ruleSet = new RuleSet();

               ruleSet.File = Path.GetFileName(file);

               var repository = new DocumentAnalyzerRepository(file);
               repository.Context.AddFeatures(new UnstructuredDataReader().GetSupportedFeatures());
               try
               {
                  // Query into the repository to make sure that it's valid
                  repository.Query(null);
                  repository.QueryActions();
               }
               catch (Exception e)
               {
                  Trace.WriteLine($"Error loading elements file {file} with exception {e.Message}");
                  // We won't add this rule file to our list of valid rules.
                  continue;
               }

               var indentifier = Path.GetFileNameWithoutExtension(file);
               ruleSet.Id = indentifier;
               ruleSet.File = file;
               ruleSet.Title = indentifier;

               _ruleSets.Add(ruleSet.Id, ruleSet);
            }
         }
      }

      public static void CleanupService()
      {
         _ruleSets = null;
         _actionSets = null;
      }

      public AnalyticsController()
      {
         ServiceHelper.InitializeController();
      }

      /// <summary>
      /// Gets all the RuleSets that are registered in the service.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "request")]
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The rule sets could not be retrieved")]
      [HttpGet("api/[controller]/[action]")]
      public GetRuleSetsResponse GetRuleSets([FromQuery] GetRuleSetsRequest request)
      {
         var response = new GetRuleSetsResponse();
         IDictionary<string, RuleSet> ruleSets = GetRules();
         if (ruleSets != null)
         {
            response.RuleSets = ruleSets.Values.ToArray();
         }
         else
         {
            response.RuleSets = new RuleSet[0];
         }

         return response;
      }

      /// <summary>
      /// Gets all the ActionSets that are registered in the service.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "request")]
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "The rule sets could not be retrieved")]
      [HttpGet("api/[controller]/[action]")]
      public GetActionSetsResponse GetActionSets([FromQuery] GetActionSetsRequest request)
      {
         var response = new GetActionSetsResponse();
         IDictionary<string, ActionSet> actionSets = GetActions();
         if (actionSets != null)
         {
            response.ActionSets = actionSets.Values.ToArray();
         }
         else
         {
            response.ActionSets = new ActionSet[0];
         }

         return response;
      }

      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "Run could be performed")]
      [HttpPost("api/[controller]/[action]")]
      public RunAnalysisResponse Run(RunAnalysisRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         if (string.IsNullOrEmpty(request.Elements) && request.RuleSetIds == null)
            throw new ArgumentNullException("Either elements or ruleSetIds must be specified");

         var response = new RunAnalysisResponse();

         if (!IsValidRules(request))
         {
            // The user did not pass any ruleset ID or JSON data
            return response;
         }

         var analyzer = new DocumentAnalyzer();
         var reader = new UnstructuredDataReader();

         DocumentAnalyzerRunOptions options = BuildAnalyzerOptions(request);

         ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.DocumentId);
         var loadFromCacheOptions = new LoadFromCacheOptions
         {
            Cache = cache,
            DocumentId = request.DocumentId,
            UserToken = ServiceHelper.GetUserToken(this.Request.Headers, request)
         };

         using (LEADDocument document = DocumentFactory.LoadFromCache(loadFromCacheOptions))
         {
            DocumentHelper.CheckLoadFromCache(document);

            document.Text.OcrEngine = ServiceHelper.GetOCREngine();
            document.Text.ImagesRecognitionMode = DocumentTextImagesRecognitionMode.Auto;

            analyzer.Readers.Add(reader);

            IList<ElementSetResult> results = analyzer.Run(document, options);

            // User wants us to return the results
            if (request.ReturnResults)
               response.Results = results.Select((result) =>
                result.Items.Select(item => ToAnalysisResult(item))).SelectMany(item => item).ToArray();


            // User wants to apply some actions to the results
            if (request.ActionIds != null && request.ActionIds.Length != 0)
            {
               ActionElementSet actions = GetRegisteredActions(request.ActionIds);
               actions.Run(document, results);

               document.CacheStatus = DocumentCacheStatus.NotSynced;
               document.SaveToCache();
            }
         }

         return response;
      }

      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
      [ServiceErrorAttribute(Message = "Run could be performed")]
      [HttpPost("api/[controller]/[action]")]
      public Response ApplyActions(ApplyActionsRequest request)
      {
         if (request == null)
            throw new ArgumentNullException("request");

         if (request.ActionIds == null || request.ActionIds.Length == 0)
            throw new ArgumentNullException("Action Ids must be provided");

         if (request.Results == null || request.Results.Length == 0)
            throw new ArgumentNullException("No results provided");

         ObjectCache cache = ServiceHelper.CacheManager.GetCacheForDocumentOrDefault(request.DocumentId);
         var loadFromCacheOptions = new LoadFromCacheOptions
         {
            Cache = cache,
            DocumentId = request.DocumentId,
            UserToken = ServiceHelper.GetUserToken(this.Request.Headers, request)
         };

         using (LEADDocument document = DocumentFactory.LoadFromCache(loadFromCacheOptions))
         {
            DocumentHelper.CheckLoadFromCache(document);
            ActionElementSet actions = GetRegisteredActions(request.ActionIds);
            IList<ElementSetResult> results = BuildElementSets(request.Results);

            actions.Run(document, results);
            document.CacheStatus = DocumentCacheStatus.NotSynced;
            document.SaveToCache();
         }

         return new Response();
      }

      private static IList<ElementSetResult> BuildElementSets(AnalysisResult[] results)
      {
         var dictionary = new Dictionary<string, ElementSetResult>();

         foreach (var result in results)
         {
            if (!dictionary.ContainsKey(result.DocumentId))
               dictionary.Add(result.DocumentId, new ElementSetResult()
               {
                  DocumentId = result.DocumentId
               });

            var elementSet = dictionary[result.DocumentId];
            elementSet.Items.Add(ToElementResult(result));
         }

         return dictionary.Values.ToList();
      }

      private static ActionElementSet GetRegisteredActions(string[] actionIds)
      {
         var actions = GetActions();

         var set = new ActionElementSet();
         foreach (var id in actionIds)
         {
            var action = actions[id];
            if (action == null)
               continue;

            set.ActionElements.Add(action.generator());
         }

         return set;
      }

      private static AnalysisResult ToAnalysisResult(ElementResult result)
      {
         if (result is TextResult)
         {
            return ToTextAnalysisResult(result as TextResult);
         }

         // By default we will just build an AnalysisResult

         return new AnalysisResult()
         {
            Bounds = result.ListOfBounds.Select(x => x.ToLeadRectD()).ToArray(),
            Confidence = result.Confidence,
            DocumentId = result.DocumentID,
            ElementName = result.ElementName,
            PageNumber = result.PageNumber,
            Value = result.Value,
         };
      }

      private static TextAnalysisResult ToTextAnalysisResult(TextResult result)
      {
         return new TextAnalysisResult()
         {
            Bounds = result.ListOfBounds.Select(x => x.ToLeadRectD()).ToArray(),
            Confidence = result.Confidence,
            DocumentId = result.DocumentID,
            ElementName = result.ElementName,
            PageNumber = result.PageNumber,
            Value = result.Value,
            DocumentCharacters = result.DocumentCharacters.ToArray()
         };
      }

      private static ElementResult ToElementResult(AnalysisResult analysisResult)
      {
         var result = new TextResult()
         {
            ListOfBounds = analysisResult.Bounds.Select(x => x.ToLeadRect()).ToList(),
            Value = analysisResult.Value,
            DocumentID = analysisResult.DocumentId,
            PageNumber = analysisResult.PageNumber,
            Confidence = analysisResult.Confidence,
         };

         if (analysisResult is TextAnalysisResult)
            foreach (var docChar in (analysisResult as TextAnalysisResult).DocumentCharacters)
               result.DocumentCharacters.Add(docChar);


         return result;
      }

      private static DocumentAnalyzerRunOptions BuildAnalyzerOptions(RunAnalysisRequest request)
      {
         var options = new DocumentAnalyzerRunOptions()
         {
            FirstPage = request.FirstPageNumber != 0 ? request.FirstPageNumber : 1,
            LastPage = request.LastPageNumber != 0 ? request.LastPageNumber : -1
         };

         var elements = new List<IElementSet>();

         // The user passed elements
         if (!string.IsNullOrWhiteSpace(request.Elements))
         {
            try
            {
               using (var stream = new MemoryStream((Encoding.UTF8.GetBytes(request.Elements))))
               {
                  elements.AddRange(new DocumentAnalyzerRepository(stream).Query(null));
               }
            }
            catch
            {
               throw new InvalidDataException("Provided elements are not valid");
            }
         }

         var rules = GetRules();
         foreach (var id in request.RuleSetIds)
         {
            var rule = rules[id];
            if (rule == null)
               continue;

            var repository = new DocumentAnalyzerRepository(rule.File);
            repository.Context.AddFeatures(new UnstructuredDataReader().GetSupportedFeatures());
            elements.AddRange(repository.Query(null));
         }

         if (elements.Count > 0)
            foreach (var element in elements)
               options.Elements.Add(element);

         return options;
      }

      private static bool IsValidRules(RunAnalysisRequest request)
      {
         // Check if the user passed any rules directly
         if (!string.IsNullOrEmpty(request.Elements))
            return true;

         if (request.RuleSetIds != null)
         {
            // See if the user passed any ruleset that we can run
            IDictionary<string, RuleSet> ruleSets = GetRules();
            foreach (string ruleSetId in request.RuleSetIds)
            {
               if (ruleSets.ContainsKey(ruleSetId))
                  return true;
            }
         }

         return false;
      }
   }
}
