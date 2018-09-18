using System;
using System.IO;
using System.Linq;

namespace GWLogger
{
    public class Global : System.Web.HttpApplication
    {
        public static Backend.DataContext.Context DataContext { get; } = new Backend.DataContext.Context();
        public static Live.LiveInformation LiveInformation { get; } = new Live.LiveInformation();


        protected void Application_Start(object sender, EventArgs e)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(File.ReadAllText(Server.MapPath("/index.html")));
            doc.DocumentNode.SelectNodes("//div")
                .Where(row => row.Attributes["class"]?.Value == "GWDisplay")
                .Select(row => row.Attributes["id"]?.Value).ToList()
                .ForEach(row => Global.LiveInformation.Register(row));
            Backend.Controllers.LogController.CleanLogs();
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            string fullOrigionalpath = Request.Url.AbsolutePath;

            if (fullOrigionalpath.StartsWith("/GW/") || fullOrigionalpath.StartsWith("/Status/"))
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