using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Humanizer;
using Raven.Client;
using RavenFusion;
using RavenFusion.Models.RavenSide;

namespace RavenFusionSearch.Controllers
{
    public class RavenFusionController:RavenControllerBase
    {
        [HttpGet]
        public bool GenerateRavenDBDataFromSqlServer()
        {
            try
            {
                new RavenFusionLoader().LoadSqlDataToRavenDB();
                return true;
            }
            catch
            {
                return false;
            }
        }

        [HttpGet]
        public ActionResult FullTextSearch(string text)
        {
            var sp = Stopwatch.StartNew();

            var q = Session.Query<FullTextQueryIndex.Result, FullTextQueryIndex>()
                .Search(x => x.Query, text);

            var results = q
                .OfType<WorkItem>()
                .Select(wi => new { wi.Id, wi.Summary })
                .ToList();

            sp.Stop();

            if (results.Count == 0)
            {
                return Json(new
                {
                    Timing = sp.Elapsed.Humanize(),
                    Results = new { NoResultsFor = text }
                });
                
            }
            

            return Json(new
            {
                Timing = sp.Elapsed.Humanize(),
                Results = results
            });

        }
    }
}