using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Owin.Security.OAuth;
using Newtonsoft.Json.Serialization;
using System.Web.Http.WebHost;
using CheckIn.API.ViewModels;
using CheckIn.API.Controllers;

namespace CheckIn.API
{
    public static class WebApiConfig
    {
   //     public static string _WebApiExecutionPath = "api";
        public static void Register(HttpConfiguration config)
        {
            // Configuración y servicios de API web
            config.EnableCors();
            // Rutas de API web
            config.MapHttpAttributeRoutes();

            config.MessageHandlers.Add(new TokenValidationHandler());

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }

        //public static void Register(HttpConfiguration config)
        //{
        //    var httpControllerRouteHandler = typeof(HttpControllerRouteHandler).GetField("_instance",
        //     System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        //    if (httpControllerRouteHandler != null)
        //    {
        //        httpControllerRouteHandler.SetValue(null,
        //            new Lazy<HttpControllerRouteHandler>(() => new SessionHttpControllerRouteHandler(), true));
        //    }
        //    //var basicRouteTemplate = string.Format("{0}/{1}", _WebApiExecutionPath, "{controller}");
        //    // Web API configuration and services
        //    // Configure Web API to use only bearer token authentication.
        //    config.SuppressDefaultHostAuthentication();
        //    config.Filters.Add(new HostAuthenticationFilter(OAuthDefaults.AuthenticationType));

        //    config.Formatters.XmlFormatter.SupportedMediaTypes.Add(new System.Net.Http.Headers.MediaTypeHeaderValue("multipart/form-data"));

        //    // Web API routes
        //    config.MapHttpAttributeRoutes();

        //    config.Routes.MapHttpRoute(
        //        name: "DefaultApi",
        //        routeTemplate: "api/{controller}/{id}",
        //        defaults: new { id = RouteParameter.Optional }
        //    );
        //}
    }
}
