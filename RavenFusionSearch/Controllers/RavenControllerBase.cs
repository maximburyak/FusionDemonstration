using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Web.Mvc;
using Humanizer;
using Newtonsoft.Json;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Document;
using RavenFusion;
using RavenFusion.Models.RavenSide;
namespace RavenFusionSearch.Controllers
{

    public class RavenControllerBase : Controller
    {
        static readonly Lazy<IDocumentStore> _documentStore = new Lazy<IDocumentStore>(() =>
        {
            var docStore = new DocumentStore() { ConnectionStringName = "RavenDBFusion" };
            docStore.Initialize();

            return docStore;
        });

        public IDocumentStore DocumentStore { get { return _documentStore.Value; } }

        public new IDocumentSession Session { get; set; }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            using (Session)
            {
                if (filterContext.Exception != null)
                    return;
                if (filterContext.HttpContext.Request.HttpMethod != "GET")
                    Session.SaveChanges();
            }
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Session = DocumentStore.OpenSession();
        }

        protected override JsonResult Json(object data, string contentType, Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            return base.Json(data, contentType, contentEncoding, JsonRequestBehavior.AllowGet);
        }
        
    }
}
