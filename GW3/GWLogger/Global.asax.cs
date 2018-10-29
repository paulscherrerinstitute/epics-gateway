using System;
using System.IO;
using System.Linq;

namespace GWLogger
{
    public class Global : System.Web.HttpApplication
    {
        public static string StorageDirectory { get; } = System.Configuration.ConfigurationManager.AppSettings["storageDirectory"];
        public static Backend.DataContext.Context DataContext { get; } = new Backend.DataContext.Context(StorageDirectory);
        public static Live.LiveInformation LiveInformation { get; } = new Live.LiveInformation();

        public static Inventory.DataAccessSoapClient Inventory { get; } = new Inventory.DataAccessSoapClient();

        public static Inventory.Controller.DirectCommandsSoapClient DirectCommands { get; } = new Inventory.Controller.DirectCommandsSoapClient();

        protected void Application_Start(object sender, EventArgs e)
        {
            Inventory.ServerName();

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(File.ReadAllText(Server.MapPath("/index.html")));
            doc.DocumentNode.SelectNodes("//div")
                .Where(row => row.Attributes["class"]?.Value == "GWDisplay")
                .Select(row => row.Attributes["id"]?.Value).ToList()
                .ForEach(row => Global.LiveInformation.Register(row));
            Backend.Controllers.LogController.CleanLogs();

            DataContext.StoreHistory += (file) =>
              {
                  var idxPos = Backend.DataContext.DataFile.IndexPosition(DateTime.UtcNow);
                  var gw = Global.LiveInformation[file.Gateway];
                  if (gw == null)
                  {
                      file.Stats[idxPos, 3] = 0;
                      file.Stats[idxPos, 4] = 0;
                      file.Stats[idxPos, 5] = 0;
                      file.Stats[idxPos, 6] = 0;
                      file.Stats[idxPos, 7] = 0;
                  }
                  else
                  {
                      file.Stats[idxPos, 3] = gw.AvgCPU;
                      file.Stats[idxPos, 4] = gw.AvgPvs;
                      file.Stats[idxPos, 5] = gw.AvgNbClients;
                      file.Stats[idxPos, 6] = gw.AvgNbServers;
                      file.Stats[idxPos, 7] = gw.AvgMsgSec;
                  }
              };
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            Context.Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Context.Response.Headers.Add("Cache-Control", "no-cache, no-store");
            Context.Response.Cache.SetExpires(DateTime.UtcNow.AddYears(-1));

            string fullOrigionalpath = Request.Url.AbsolutePath;

            if (fullOrigionalpath.StartsWith("/GW/") || fullOrigionalpath.StartsWith("/Status/"))
            {
                Context.RewritePath("/index.html");
            }
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