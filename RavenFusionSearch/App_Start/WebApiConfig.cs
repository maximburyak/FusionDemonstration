using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace RavenFusionSearch
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();
            
//            config.Routes.MapHttpRoute(
//                name: "GenerateData",
//                routeTemplate: "api/{controller}/GenerateData",
//                defaults: new { action = "Get" }
//            );
//
//            config.Routes.MapHttpRoute(
//                name: "GetByAssigneeName",
//                routeTemplate: "api/{controller}/GetByAssigneeName/{name}",
//                defaults: new { action = "Get" }
//            );

            
        }
    }
}
