using System;
using System.Web;

namespace CheckIn.API
{
    using System.Web.Http;
    using System.Web.Mvc;
    using System.Web.Optimization;
    using System.Web.Routing;
    using System.Web.SessionState;
    public class WebApiApplication : System.Web.HttpApplication
    {
        public override void Init() { this.PostAuthenticateRequest += MvcApplication_PostAuthenticateRequest; base.Init(); }
        void MvcApplication_PostAuthenticateRequest(object sender, EventArgs e) { System.Web.HttpContext.Current.SetSessionStateBehavior(SessionStateBehavior.Required); }
        //protected void Application_PostAuthorizeRequest()
        //{
        //    if (HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath.StartsWith("~/api"))
        //    {
        //        HttpContext.Current.SetSessionStateBehavior(SessionStateBehavior.Required);
        //    }
        //}
        //private static bool IsWebApiRequest()
        //{
        //    return HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath.StartsWith(WebApiConfig._WebApiExecutionPath);
        //}
        protected void Application_Start()
        {

            this.CheckRolesAndSuperUser();
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        private void CheckRolesAndSuperUser()
        {
           // UsersHelper.CheckRole("Admin");
           // UsersHelper.CheckRole("User");
           // UsersHelper.CheckSuperUser();
        }
    }
}
