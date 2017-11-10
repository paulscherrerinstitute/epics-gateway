using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace GWWatchdog
{
    static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            /*ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new WatchdogService()
            };
            ServiceBase.Run(ServicesToRun);*/

            if (Environment.UserInteractive)
            {
                AllocConsole();
                WatchdogService w = new WatchdogService();
                w.Start();
            }
            else
            {
                ServiceBase.Run(new ServiceBase[] { new WatchdogService() });
            }
        }
    }
}
