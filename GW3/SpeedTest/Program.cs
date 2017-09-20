using EpicsSharp.ChannelAccess.Client;
using EpicsSharp.ChannelAccess.Server;
using EpicsSharp.ChannelAccess.Server.RecordTypes;
using GatewayLogic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpeedTest
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Log.Filter = (level) => { return level >= GatewayLogic.Services.LogLevel.Error; };
                gateway.Start();

                using (var autoReset = new AutoResetEvent(false))
                {
                    // Serverside
                    using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5056, 5056))
                    {
                        var serverChannels = new List<CAStringRecord>();
                        for (var i = 0; i < 1000; i++)
                        {
                            var serverChannel = server.CreateRecord<CAStringRecord>("SPEED-TEST-" + i);
                            serverChannel.Value = "Works fine! - " + i;
                            serverChannels.Add(serverChannel);
                        }
                        server.Start();
                        Console.WriteLine("Channels created, now starting clients and searching.");

                        var sw = new Stopwatch();
                        sw.Start();
                        // Client
                        Enumerable.Range(0, 1000).AsParallel().ForAll(i =>
                         {
                             using (var client = new CAClient())
                             {
                                 client.Configuration.SearchAddress = "127.0.0.1:5432";
                                 client.Configuration.WaitTimeout = 1000;
                                 var clientChannel = client.CreateChannel<string>("SPEED-TEST-" + i);
                                 clientChannel.Get();
                             }
                         });
                        sw.Stop();
                        Console.WriteLine("Time: " + sw.Elapsed.ToString());
                    }
                }
            }
            Console.ReadKey();
        }
    }
}
