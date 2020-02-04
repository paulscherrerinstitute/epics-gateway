using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogReadSpeed
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!Directory.Exists(args[0]))
            {
                Console.WriteLine("Directory " + args[0] + " doesn't exists");
                ShowHelp();
                return;
            }
            /*var files = Directory.GetFiles(args[0], args[1] + ".*.data");
            if (files.Length == 0)
            {
                Console.WriteLine("Gateway " + args[1] + " not found");
                ShowHelp();
                return;
            }
            string filename;
            if (files.Length > 1)
                filename = files.OrderByDescending(row => row).Skip(1).First();
            else
                filename = files.First();

            var dt = filename.Substring(args[0].Length + 1).Split(new char[] { '.' })[1];*/
            //var startDate = new DateTime(int.Parse(dt.Substring(0, 4)), int.Parse(dt.Substring(4, 2)), int.Parse(dt.Substring(6, 2)));
            var startDate = new DateTime(2020, 1, 20);

            //var gateway = "cryo-cagw02";
            var gateway = "block";

            var sw = new Stopwatch();
            sw.Start();
            using (var ctx = new GWLogger.Backend.DataContext.Context(args[0]))
            {
                var nbRead = 0;
                foreach (var row in ctx.GetLogs(gateway, startDate, startDate.AddDays(1), "channel = \"CRYO-CAGW02:CPU\""))
                {
                    //Console.WriteLine(row.RemoteIpPoint);
                    //Console.WriteLine(row.LogEntryDetails.Count);
                    //var s = row.RemoteIpPoint + row.LogEntryDetails.Count + row.EntryDate.ToShortDateString();
                    nbRead++;
                    if (nbRead > 5000000)
                        break;
                    /*row.Gateway = "seq";
                    ctx.Save(row);*/
                }
            }
            sw.Stop();
            Console.WriteLine(sw.Elapsed.ToString());
        }

        private static void ShowHelp()
        {
        }
    }
}
