using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace GWLogger
{
    public class Global : System.Web.HttpApplication
    {
        public static Backend.DataContext.Context DataContext { get; } = new Backend.DataContext.Context();


        protected void Application_Start(object sender, EventArgs e)
        {
            // Execute migration
            var configuration = new Migrations.Configuration();
            var migrator = new System.Data.Entity.Migrations.DbMigrator(configuration);
            migrator.Update();

            Backend.Controllers.LogController.CleanLogs();
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            string fullOrigionalpath = Request.Url.AbsolutePath;

            if (fullOrigionalpath.StartsWith("/GW/"))
                Context.RewritePath("/index.html");
            else if (fullOrigionalpath.ToLower().StartsWith("/logs"))
                //Context.RemapHandler("/Logs.ashx");
                Context.RemapHandler(new Logs());

        }
        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {
            DataContext.Dispose();
        }
    }
}