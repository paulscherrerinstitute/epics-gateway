using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GWConsoleLogger
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var ctx = new GWLogger.Backend.DataContext.Context(@"C:\temp\tt"))
            {
                var gateways = ctx.Gateways;
                var start = new DateTime(2018, 09, 05);
                ctx.GetLogs(gateways.First(), start, start.AddDays(1).Date, "").Take(10000).ToList().ForEach(item => Console.WriteLine($"{item.EntryDate}|{item.RemoteIpPoint}|{ctx.MessageTypes.First(row => row.Id == item.MessageTypeId).Name}"));
            }

            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine("Press Enter to continue...");
                Console.ReadLine();
            }
        }
    }
}
