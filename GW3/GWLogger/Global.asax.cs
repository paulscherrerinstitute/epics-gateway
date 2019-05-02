using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using FileTime = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace GWLogger
{
    public class Global : System.Web.HttpApplication
    {
        public static string StorageDirectory { get; } = System.Configuration.ConfigurationManager.AppSettings["storageDirectory"];
        public static string HistoryStorage { get; } = System.Configuration.ConfigurationManager.AppSettings["historyStorage"] ?? StorageDirectory;
        public static string AnomalyStorage { get; } = Path.Combine(StorageDirectory, "anomalies");
        public static DateTime ApplicationStartUtc;

        public static Backend.DataContext.Context DataContext { get; } = new Backend.DataContext.Context(StorageDirectory);
        public static Live.LiveInformation LiveInformation { get; private set; }

        public static Inventory.DataAccessSoapClient Inventory { get; } = new Inventory.DataAccessSoapClient();

        public static Inventory.Controller.DirectCommandsSoapClient DirectCommands { get; } = new Inventory.Controller.DirectCommandsSoapClient();

        protected void Application_Start(object sender, EventArgs e)
        {
            try
            {
                Inventory.ServerName();
            }
            catch
            {
            }

            Directory.CreateDirectory(AnomalyStorage);
            ApplicationStartUtc = DateTime.UtcNow.AddSeconds(1);
            Debug.WriteLine($"Application started at: {ApplicationStartUtc}");

            LiveInformation = new Live.LiveInformation();

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(File.ReadAllText(Server.MapPath("/index.html")));
            doc.DocumentNode.SelectNodes("//div")
                .Where(row => row.Attributes["class"]?.Value == "GWDisplay")
                .Select(row => row.Attributes["id"]?.Value).ToList()
                .ForEach(row => LiveInformation.Register(row));
            if (Debugger.IsAttached) // Debug local
                LiveInformation.Register("PBGW");
            Backend.Controllers.LogController.CleanLogs();

            DataContext.StoreHistory += (file) =>
              {
                  var idxPos = Backend.DataContext.DataFile.IndexPosition(DateTime.UtcNow);
                  var gw = LiveInformation[file.Gateway];
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

            CPUUpdated = new Thread(() =>
              {
                  while(true)
                  {
                      CPU = GetCPUUsagePercent();
                      Thread.Sleep(1000);
                  }
              });
            CPUUpdated.IsBackground = true;
            CPUUpdated.Start();
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

            if (fullOrigionalpath.StartsWith("/GW/") || fullOrigionalpath.StartsWith("/Status/") || fullOrigionalpath.StartsWith("/Map/") || fullOrigionalpath.StartsWith("/Anomalies"))
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

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(out FileTime lpIdleTime, out FileTime lpKernelTime, out FileTime lpUserTime);

        private static ulong lastIdleTime;
        private static ulong lastKernelTime;
        private static ulong lastUserTime;
        private Thread CPUUpdated;

        private static ulong ULong(FileTime filetime)
        {
            return ((ulong)filetime.dwHighDateTime << 32) + (uint)filetime.dwLowDateTime;
        }

        private static double GetCPUUsagePercent()
        {
            if (!GetSystemTimes(out FileTime f_idleTime, out FileTime f_kernelTime, out FileTime f_userTime))
                throw new Exception("Native call to GetSystemTimes() failed");
            var idleTime = ULong(f_idleTime);
            var kernelTime = ULong(f_kernelTime);
            var userTime = ULong(f_userTime);

            var usr = userTime - lastUserTime;
            var ker = kernelTime - lastKernelTime;
            var idl = idleTime - lastIdleTime;

            var sys = ker + usr;
            double cpu = (double)((sys - idl) * 100) / sys;

            lastIdleTime = idleTime;
            lastKernelTime = kernelTime;
            lastUserTime = userTime;
            return cpu;
        }

        public static double CPU { get; private set;}
    }
}