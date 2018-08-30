using EpicsSharp.ChannelAccess.Client;
using EpicsSharp.ChannelAccess.Server;
using EpicsSharp.ChannelAccess.Server.RecordTypes;
using GatewayLogic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpeedTest
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    class Program
    {
        delegate CAClient CATest(int i, ParallelLoopState loop, CAClient client);

        static void Main(string[] args)
        {
            //Log();
            Routing();
        }

        static void Log()
        {
            foreach (var i in Directory.EnumerateFiles(GWLogger.Backend.DataContext.DataFile.StorageDirectory, "*.*.*"))
            {
                if (i.Split('\\').Last().Split('.').Length != 3 || i.Split('\\').Last().Split('.')[1].Length != 8)
                    continue;
                File.Delete(i);
            }


            Stopwatch sw = new Stopwatch();
            sw.Start();
            using (var ctx = new GWLogger.Backend.DataContext.Context())
            {
                // Remove all
                ctx.CleanOlderThan(-1);
                var startDate = new DateTime(2000, 1, 1);


                for (var i = 0; i < 1000000; i++)
                    ctx.Save(new GWLogger.Backend.DataContext.LogEntry { Gateway = "GW" + i % 20, EntryDate = startDate.AddSeconds(i), MessageTypeId = i % 59, LogEntryDetails = new List<GWLogger.Backend.DataContext.LogEntryDetail>(), RemoteIpPoint = "127.0.0.1:5050" });
                ctx.Flush();
            }
            sw.Stop();
            Console.WriteLine("Total time: " + sw.Elapsed.ToString());
            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        static void Routing()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Log.Handler += Log_Handler;
                //gateway.Log.Filter = (level) => { return level >= GatewayLogic.Services.LogLevel.Error; };
                gateway.Start();

                // Serverside
                using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5056, 5056))
                {
                    var serverChannels = new List<CAStringRecord>();
                    for (var i = 0; i < 3000; i++)
                    {
                        var serverChannel = server.CreateRecord<CAStringRecord>("SPEED-TEST-" + i);
                        serverChannel.Value = "Works fine! - " + i;
                        serverChannels.Add(serverChannel);
                    }
                    server.Start();
                    Console.WriteLine("Channels created, now starting clients and searching.");

                    CATest testToRun = MonitorTest;

                    var sw = new Stopwatch();
                    sw.Start();
                    // Client
                    //Enumerable.Range(0, 3000).AsParallel().ForAll(i =>
                    Parallel.ForEach<int, CAClient>(Enumerable.Range(0, 300), () =>
                    {
                        var client = new CAClient(); client.Configuration.SearchAddress = "127.0.0.1:5432";
                        client.Configuration.WaitTimeout = 1000;
                        return client;
                    },
                    (i, loop, client) => testToRun(i, loop, client)
                     ,
                     (client) => { client.Dispose(); });
                    sw.Stop();
                    Console.WriteLine("Time: " + sw.Elapsed.ToString());
                }
            }
            Console.ReadKey();
        }

        private static void Log_Handler(GatewayLogic.Services.LogLevel level, string source, string message)
        {

        }

        static CAClient MonitorTest(int i, ParallelLoopState loop, CAClient client)
        {
            using (var clientChannel = client.CreateChannel<string>("SPEED-TEST-" + i))
            {
                using (AutoResetEvent autoEvt = new AutoResetEvent(false))
                {
                    clientChannel.MonitorChanged += (sender, val) =>
                      {
                          //Console.WriteLine(i);
                          autoEvt.Set();
                      };
                    autoEvt.WaitOne(1000);
                }
            }
            return client;
        }

        static CAClient GetTest(int i, ParallelLoopState loop, CAClient client)
        {
            using (var clientChannel = client.CreateChannel<string>("SPEED-TEST-" + i))
            {
                clientChannel.Get();
            }
            return client;
        }
    }
}
