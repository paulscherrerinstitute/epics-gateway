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

namespace StressTest
{
    class Program
    {
        const int NB_SERVERS = 20;
        const int NB_CLIENTS = 20;
        const int NB_CHANNELS = 10;
        const int NB_LOOPS = 10;
        const int NB_CHECKED = 30;

        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "server")
            {
                Console.WriteLine("Start server " + args[1]);
                Server(int.Parse(args[1]));
                return;
            }

            if (args.Length > 0 && args[0] == "client")
            {
                Console.WriteLine("Start client " + args[1]);
                Client();
                return;
            }

            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                var addrs = "";
                for (var i = 0; i < NB_SERVERS; i++)
                    addrs += (i != 0 ? ";" : "") + "127.0.0.1:" + (5056 + i);
                gateway.Configuration.RemoteSideB = addrs;
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Log.Filter = (level) => { return level >= GatewayLogic.Services.LogLevel.Error; };
                gateway.Start();

                var current = Process.GetCurrentProcess();

                var servers = Enumerable.Range(0, NB_SERVERS)
                    .Select(i =>
                    {
                        var p = new Process();
                        p.StartInfo = new ProcessStartInfo(current.ProcessName, "server " + i)
                        {
                            /*RedirectStandardOutput = true,
                            RedirectStandardError = true,*/
                            UseShellExecute = false
                        };
                        p.EnableRaisingEvents = true;
                        p.Start();
                        return p;
                    }).ToList();

                var clients = Enumerable.Range(0, NB_CLIENTS)
                    .Select(i =>
                    {
                        var p = new Process();
                        p.StartInfo = new ProcessStartInfo(current.ProcessName, "client " + i)
                        {
                            /*RedirectStandardOutput = true,
                            RedirectStandardError = true,*/
                            UseShellExecute = false
                        };
                        p.EnableRaisingEvents = true;
                        p.Start();
                        return p;
                    }).ToList();

                EventHandler serverExit = (obj, evt) => { ((Process)obj).Start(); };
                EventHandler clientExit = (obj, evt) => { ((Process)obj).Start(); };
                servers.ForEach(row => row.Exited += serverExit);
                clients.ForEach(row => row.Exited += clientExit);

                Console.WriteLine("Press any key to stop...");
                Console.ReadKey();

                AppDomain.CurrentDomain.ProcessExit += (obj, evt) =>
                 {
                     servers.ForEach(row => row.Exited -= serverExit);
                     clients.ForEach(row => row.Exited -= clientExit);

                     servers.ForEach(row => row.Kill());
                     clients.ForEach(row => row.Kill());
                 };
            }
        }

        private static void Client()
        {
            var rnd = new Random();
            switch (rnd.Next(0, 2))
            {
                case 0:
                    Console.WriteLine("Ten sec monitor");
                    MonitorTenSecAction();
                    break;
                case 1:
                    Console.WriteLine("All monitors");
                    MonitorOnceAction();
                    break;
                case 2:
                    Console.WriteLine("Get all");
                    GetOnceAction();
                    break;
            }
        }

        static void MonitorTenSecAction()
        {
            var rnd = new Random();
            var channelNames = Enumerable.Range(0, NB_SERVERS * NB_CHANNELS)
                .Select(row => new KeyValuePair<string, double>("STRESS-TEST-" + (row + 1), rnd.Next()))
                .OrderBy(row => row.Value)
                .Select(row => row.Key)
                .Take(NB_CHECKED)
                .ToList();

            for (var i = 0; i < NB_LOOPS; i++)
            {
                using (var client = new CAClient())
                {
                    client.Configuration.SearchAddress = "127.0.0.1:5432";
                    client.Configuration.WaitTimeout = 1000;

                    var channels = channelNames.Select(row => client.CreateChannel<string>(row)).ToList();

                    var multiEvt = new CountdownEvent(channels.Count());

                    channels.ForEach((row) =>
                    {
                        row.MonitorChanged += (chan, val) =>
                        {
                            multiEvt.Signal();
                        };
                    });
                    multiEvt.Wait();
                    Thread.Sleep(10000);
                }
            }
        }

        static void MonitorOnceAction()
        {
            var rnd = new Random();
            var channelNames = Enumerable.Range(0, NB_SERVERS * NB_CHANNELS)
                .Select(row => new KeyValuePair<string, double>("STRESS-TEST-" + (row + 1), rnd.Next()))
                .OrderBy(row => row.Value)
                .Select(row => row.Key)
                .Take(NB_CHECKED)
                .ToList();

            for (var i = 0; i < NB_LOOPS; i++)
            {
                using (var client = new CAClient())
                {
                    client.Configuration.SearchAddress = "127.0.0.1:5432";
                    client.Configuration.WaitTimeout = 1000;

                    var channels = channelNames.Select(row => client.CreateChannel<string>(row)).ToList();

                    var multiEvt = new CountdownEvent(channels.Count());

                    channels.ForEach((row) =>
                    {
                        row.MonitorChanged += (chan, val) =>
                            {
                                multiEvt.Signal();
                            };
                    });
                    multiEvt.Wait();
                }
            }
        }

        static void GetOnceAction()
        {
            var rnd = new Random();
            var channelNames = Enumerable.Range(0, NB_SERVERS * NB_CHANNELS)
                .Select(row => new KeyValuePair<string, double>("STRESS-TEST-" + (row + 1), rnd.Next()))
                .OrderBy(row => row.Value)
                .Select(row => row.Key)
                .Take(NB_CHECKED)
                .ToList();

            for (var i = 0; i < NB_LOOPS; i++)
            {
                using (var client = new CAClient())
                {
                    client.Configuration.SearchAddress = "127.0.0.1:5432";
                    client.Configuration.WaitTimeout = 1000;

                    var channels = channelNames.Select(row => client.CreateChannel<string>(row)).ToList();

                    var res = client.MultiGet<string>(channels);
                }
            }
        }

        static void Server(int serverId)
        {
            using (var evt = new AutoResetEvent(false))
            {
                using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), (5056 + serverId), (5056 + serverId)))
                {
                    var serverChannels = new List<CAStringRecord>();
                    for (var i = 0; i < NB_CHANNELS; i++)
                    {
                        var serverChannel = server.CreateRecord<CAStringRecord>("STRESS-TEST-" + ((i + 1) + (serverId * NB_SERVERS)));
                        serverChannel.Value = "Works fine! - " + i;
                        serverChannel.PrepareRecord += (rec, obj) =>
                          {
                              serverChannel.Value = "Works fine! - " + i + " - " + DateTime.UtcNow.ToLongTimeString();
                          };
                        serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.HZ10;
                        serverChannels.Add(serverChannel);
                    }
                    server.Start();
                    evt.WaitOne();
                }
            }
        }
    }
}
