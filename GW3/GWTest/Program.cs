//#define LOG

using EpicsSharp.ChannelAccess.Client;
using EpicsSharp.ChannelAccess.Server;
using GatewayLogic;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace GWTest
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal class Program
    {
        private static void Main(string[] args)
        {
            //Console.WriteLine("Some change");
            //TestDiagnosticServer();
            //Test();
            //S1();
            //S2();
            //Console.ReadKey();

            ReadBrokenLogs();
        }

        private static void ReadBrokenLogs()
        {
            using (var ctx = new GWLogger.Backend.DataContext.Context())
            {
                var logs = ctx.ReadLog("cryo-cagw02", new DateTime(2018, 08, 31, 18, 08, 00), new DateTime(2018, 08, 31, 18, 40, 00), "class contains \"search\" and channel = \"ika-ka3\"", 100);
                //var logs = ctx.ReadLog("cryo-cagw02", new DateTime(2018, 08, 31, 18, 08, 00), new DateTime(2018, 08, 31, 18, 40, 00), "", 100);
            }
            Console.WriteLine("Done");
        }

        private static void S2()
        {
            var gw1 = new Gateway();
            gw1.Configuration.GatewayName = "GW1";
            gw1.Configuration.ConfigurationType = GatewayLogic.Configuration.ConfigurationType.BIDIRECTIONAL;
            gw1.Configuration.DiagnosticPort = 1234;
            gw1.Configuration.SideA = "127.0.0.1:5432";
            gw1.Configuration.RemoteSideB = "127.0.0.1:5056";
            gw1.Configuration.SideB = "127.0.0.1:5055";
            gw1.Configuration.DelayStartup = 0;
#if LOG
            gw1.Log.Handler += (level, source, message) =>
            {
                if (message.StartsWith("Echo"))
                    return;
                Console.WriteLine("GW1: " + message);
            };
#endif
            gw1.Start();

            var gw2 = new Gateway();
            gw2.Configuration.ConfigurationType = GatewayLogic.Configuration.ConfigurationType.BIDIRECTIONAL;
            gw2.Configuration.DiagnosticPort = 1236;
            gw2.Configuration.SideA = "127.0.0.1:5056";
            gw2.Configuration.RemoteSideB = "127.0.0.1:6058";
            gw2.Configuration.SideB = "127.0.0.1:5057";
            gw2.Configuration.DelayStartup = 0;
#if LOG
            gw2.Log.Handler += (level, source, message) =>
            {
                if (message.StartsWith("Echo"))
                    return;
                Console.WriteLine("GW2: " + message);
            };
#endif
            gw2.Start();


            var server = new CAServer(IPAddress.Parse("127.0.0.1"), 6058, 6058);
            var serverChannel = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAStringRecord>("TEST-DATE");
            serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
            serverChannel.PrepareRecord += (handler, obj) =>
              {
                  serverChannel.Value = DateTime.UtcNow.ToString();
              };
            serverChannel.Value = "Works fine!";
            var serverChannel2 = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAStringRecord>("TEST-GUID");
            serverChannel2.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
            serverChannel2.PrepareRecord += (handler, obj) =>
            {
                serverChannel2.Value = Guid.NewGuid().ToString();
            };
            serverChannel2.Value = "Works fine!";

            server.Start();

            var events = new AutoResetEvent[3 * 5];
            for (var i = 0; i < events.Length; i++)
                events[i] = new AutoResetEvent(false);

            Action<int> d = (clientNumber) =>
               {
                   var client = new CAClient();
                   client.Configuration.WaitTimeout = 2000;
                   client.Configuration.SearchAddress = "127.0.0.1:5432";
                   var clientChannel = client.CreateChannel<string>("TEST-DATE");
                   var clientChannel2 = client.CreateChannel<string>("TEST-GUID");
                   var debugChannel = client.CreateChannel<string>("GW1:BUILD");

                   clientChannel.MonitorChanged += (chann, val) =>
                   {
#if LOG
                       Console.WriteLine(chann.ChannelName + ": " + val);
#endif
                       events[clientNumber * 3].Set();
                   };
                   clientChannel.StatusChanged += (chann, newStatus) =>
                   {
#if LOG
                       Console.WriteLine($"!!! {clientNumber} {chann.ChannelName}: " + newStatus.ToString());
#endif
                   };
                   debugChannel.MonitorChanged += (chann, newStatus) =>
                   {
#if LOG
                       Console.WriteLine("GW1 BUILD: " + newStatus);
#endif
                       events[clientNumber * 3 + 1].Set();
                   };
                   clientChannel2.MonitorChanged += (chann, val) =>
                   {
#if LOG
                       Console.WriteLine($"{clientNumber} {chann.ChannelName}: {val}");
#endif
                       events[clientNumber * 3 + 2].Set();
                   };
                   clientChannel2.StatusChanged += (chann, newStatus) =>
                   {
#if LOG
                       Console.WriteLine($"!!! {clientNumber} {chann.ChannelName}: " + newStatus.ToString());
#endif
                   };
                   clientChannel2.Get();
                   clientChannel.Put("toto");
               };
            for (var i = 0; i < events.Length / 3; i++)
                d(i);

#if LOG
#else
            Console.WriteLine("Starting...");
#endif
            while (true)
            {
                //Thread.Sleep(5000);
                WaitHandle.WaitAll(events, 1000);

                Console.WriteLine("Kill gw1");
                gw1.Dispose();

                Thread.Sleep(500);

#if LOG
                Console.WriteLine("Starting again gw1");
#endif
                gw1 = new Gateway();
                gw1.Configuration.ConfigurationType = GatewayLogic.Configuration.ConfigurationType.BIDIRECTIONAL;
                gw1.Configuration.GatewayName = "GW1";
                gw1.Configuration.DiagnosticPort = 1234;
                gw1.Configuration.SideA = "127.0.0.1:5432";
                gw1.Configuration.RemoteSideB = "127.0.0.1:5056";
                gw1.Configuration.SideB = "127.0.0.1:5055";
                gw1.Configuration.DelayStartup = 0;
#if LOG
                gw1.Log.Handler += (level, source, message) =>
                {
                    if (message.StartsWith("Echo"))
                        return;
                    Console.WriteLine("GW1: " + message);
                };
#endif
                gw1.Start();
                Console.WriteLine("Re-starting GW1");
            }

            /*Console.WriteLine("Kill gw2");
            gw2.Dispose();

            //Thread.Sleep(10000);

            Console.WriteLine("Starting again gw2");
            gw2 = new Gateway();
            gw2.Configuration.DiagnosticPort = 1236;
            gw2.Configuration.SideA = "127.0.0.1:5056";
            gw2.Configuration.RemoteSideB = "127.0.0.1:6058";
            gw2.Configuration.SideB = "127.0.0.1:5057";
            gw2.Configuration.DelayStartup = 0;
#if LOG
            gw2.Log.Handler += (level, source, message) =>
            {
                if (message.StartsWith("Echo"))
                    return;
                Console.WriteLine("GW2: " + message);
            };
#endif
            gw2.Start();*/

            /*client.Dispose();
            client = new CAClient();
            client.Configuration.WaitTimeout = 2000;
            client.Configuration.SearchAddress = "127.0.0.1:5432";
            clientChannel = client.CreateChannel<string>("TEST-DATE");

            clientChannel.MonitorChanged += (chann, val) =>
            {
                Console.WriteLine("New value: " + val);
            };
            clientChannel.StatusChanged += (chann, newStatus) =>
            {
                Console.WriteLine("!!! New status: " + newStatus.ToString());
            };*/

            Console.ReadKey();
            gw1.Dispose();
            gw2.Dispose();
            server.Dispose();
        }

        private static void S1()
        {
            var gateway = new Gateway();
            gateway.Configuration.SideA = "127.0.0.1:5432";
            gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
            gateway.Configuration.SideB = "127.0.0.1:5055";
            gateway.Start();

            int nbChannels = 30;
            var waiter = new EventWaitHandle(false, EventResetMode.ManualReset);
            var rnd = new Random();

            ThreadStart serverThread = () =>
              {
                  var mustStop = false;
                  while (!mustStop)
                  {
                      // Serverside
                      using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5056, 5056))
                      {
                          var serverChannel = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAStringRecord>("TEST-DATE");
                          serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
                          serverChannel.Value = "Works fine!";

                          var serverChannels = new List<EpicsSharp.ChannelAccess.Server.RecordTypes.CADoubleRecord>();
                          for (var i = 0; i < nbChannels; i++)
                          {
                              var sc = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CADoubleRecord>("TEST-DOUBLE-" + i);
                              sc.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.HZ10;
                              sc.Value = 5;
                              sc.PrepareRecord += (obj, evt) =>
                                {
                                    sc.Value = rnd.NextDouble();
                                };
                              serverChannels.Add(sc);
                          }
                          server.Start();

                          mustStop = waiter.WaitOne(rnd.Next(5000, 10000));
                      }
                  }
              };

            var s = new Thread(serverThread);
            s.Start();

            ParameterizedThreadStart clientFunction = (objState) =>
              {
                  var threadId = (int)objState;
                  // Client
                  var mustStop = false;
                  while (!mustStop)
                  {
                      using (var client = new CAClient())
                      {
                          client.Configuration.WaitTimeout = 200;
                          client.Configuration.SearchAddress = "127.0.0.1:5432";
                          var clientChannel = client.CreateChannel<string>("TEST-DATE");
                          //Console.WriteLine("Started client " + threadId);

                          clientChannel.StatusChanged += (sender, newStatus) =>
                          {
                              //Console.WriteLine("Thread: " + threadId + ", " + sender.ChannelName + " === " + newStatus);
                          };

                          clientChannel.MonitorChanged += (sender, newval) =>
                          {
                          };

                          var clientChannels = new List<Channel<string>>();

                          for (var i = 0; i < nbChannels; i++)
                          {
                              var c = client.CreateChannel<string>("TEST-DOUBLE-" + i);

                              c.StatusChanged += (sender, newStatus) =>
                              {
                                  //Console.WriteLine("Thread: " + threadId + ", " + sender.ChannelName + " === " + newStatus);
                              };

                              c.MonitorChanged += (sender, newval) =>
                              {
                              };
                              clientChannels.Add(c);
                          }

                          mustStop = waiter.WaitOne(rnd.Next(5000, 10000));
                          /*if (!mustStop)
                              Console.WriteLine("Client stopped " + threadId);*/
                      }
                  }
              };

            var threads = new Thread[30];
            for (var i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(clientFunction);
                threads[i].Start(i);
            }

            Console.WriteLine("Press any key to stop");
            Console.ReadKey();
            waiter.Set();
            Environment.Exit(0);
        }

        private static void Test()
        {
            var gateway = new Gateway();
            //gateway.Configuration.SideA = "129.129.130.45:5054";
            gateway.Configuration.SideA = "129.129.194.45:5432";
            gateway.Configuration.RemoteSideB = "129.129.194.45:5056";
            gateway.Configuration.SideB = "129.129.194.45:5055";
            gateway.Start();

            // Serverside
            var server = new CAServer(IPAddress.Parse("129.129.194.45"), 5056, 5056);
            var serverChannel = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAStringRecord>("TEST-DATE");
            serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
            serverChannel.Value = DateTime.Now.ToLongTimeString();
            /*serverChannel.PrepareRecord += (sender, e) =>
            {
                serverChannel.Value = DateTime.Now.ToLongTimeString();
            };*/

            // Client

            var client = new CAClient();
            client.Configuration.SearchAddress = "129.129.194.45:5432";
            var clientChannel = client.CreateChannel<string>("TEST-DATE");
            /*clientChannel.MonitorChanged += (sender, newValue)=>
            {
                Console.WriteLine(newValue);
            };*/

            //Thread.Sleep(1000);
            server.Start();


            //Thread.Sleep(2000);
            Console.WriteLine(clientChannel.Get());

            gateway.Dispose();
            server.Dispose();
            client.Dispose();
        }

        private static void TestDiagnosticServer()
        {
            var gateway = new Gateway();
            gateway.Configuration.GatewayName = "TESTGW-DIAG";
            gateway.Configuration.SideB = "129.129.194.45:5436";
            gateway.Configuration.SideA = "129.129.194.45:5055";

            /*gateway.Configuration.SideA = "129.129.130.45:5436";
            gateway.Configuration.SideB = "127.0.0.2:5055";*/

            //gateway.Configuration.RemoteSideA = "127.0.0.1:5058";
            //gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
            gateway.Configuration.RemoteSideA = "129.129.194.86:5432";
            //gateway.Configuration.RemoteSideB = "129.129.130.255:5064";
            //gateway.Configuration.RemoteSideB = "sls-cagw02.psi.ch:5062";
            gateway.Configuration.RemoteSideB = "129.129.194.86:5055";
            gateway.Configuration.DelayStartup = 0;
            //gateway.Configuration.ConfigurationType = GatewayLogic.Configuration.ConfigurationType.BIDIRECTIONAL;
            gateway.Configuration.ConfigurationType = GatewayLogic.Configuration.ConfigurationType.UNIDIRECTIONAL;
            gateway.Log.Handler += GatewayLogic.Services.TextLogger.DefaultHandler;
            gateway.Start();

            /*var client = new CAClient();
            client.Configuration.SearchAddress = "129.129.130.45:5436";
            var cpuChannel = client.CreateChannel<double>("TESTGW-DIAG:CPU");
            var memChannel = client.CreateChannel<double>("TESTGW-DIAG:MEM-FREE");

            ChannelValueDelegate<double> handler = (sender, newValue)=>
            {
                Console.WriteLine(sender.ChannelName + ": " + newValue);
            };

            cpuChannel.MonitorChanged += handler;
            memChannel.MonitorChanged += handler;*/

            Console.ReadKey();

            gateway.Dispose();
            //client.Dispose();
        }
    }
}
