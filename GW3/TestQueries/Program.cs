using GWLogger.Backend.DataContext;
using System;
using System.Linq;

namespace TestQueries
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            using (var ctx = new Context(@"C:\temp\t2"))
            {
                var data = ctx.ReadLog("cryo-cagw02", new DateTime(2019, 03, 14, 12, 0, 0), new DateTime(2019, 03, 14, 12, 10, 0), "select count(*) nb,sum(packetsize) tot,channel group by channel order by tot desc", 20000);
                foreach (var r in data.Cast<object[]>())
                {
                    Console.WriteLine(string.Join(" ", r.Select(r2 => r2?.ToString()).ToArray()));
                }
            }
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }
    }
}
