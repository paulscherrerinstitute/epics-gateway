using EpicsSharp.ChannelAccess.Client;
using EpicsSharp.ChannelAccess.Server;
using GatewayLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GWTest
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Some change");
            //TestDiagnosticServer();
            //Test();
            //S1();
            S2();
            //Console.ReadKey();
        }

        private static void S2()
        {
            var gw1 = new Gateway();
            gw1.Configuration.DiagnosticPort = 1234;
            gw1.Configuration.SideA = "127.0.0.1:5432";
            gw1.Configuration.RemoteSideB = "127.0.0.1:5056";
            gw1.Configuration.SideB = "127.0.0.1:5055";
            gw1.Log.Handler += (level, source, message) =>
            {
                if (message.StartsWith("Echo"))
                    return;
                Console.WriteLine("GW1: " + message);
            };
            gw1.Start();

            var gw2 = new Gateway();
            gw2.Configuration.DiagnosticPort = 1236;
            gw2.Configuration.SideA = "127.0.0.1:5056";
            gw2.Configuration.RemoteSideB = "127.0.0.1:6058";
            gw2.Configuration.SideB = "127.0.0.1:5057";
            gw2.Log.Handler += (level, source, message) =>
            {
                if (message.StartsWith("Echo"))
                    return;
                Console.WriteLine("GW2: " + message);
            };
            gw2.Start();


            var server = new CAServer(IPAddress.Parse("127.0.0.1"), 6058, 6058);
            var serverChannel = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAStringRecord>("TEST-DATE");
            serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
            serverChannel.PrepareRecord += (handler, obj) =>
              {
                  serverChannel.Value = DateTime.UtcNow.ToString();
              };
            serverChannel.Value = "Works fine!";
            server.Start();

            var client = new CAClient();
            client.Configuration.WaitTimeout = 2000;
            client.Configuration.SearchAddress = "127.0.0.1:5432";
            var clientChannel = client.CreateChannel<string>("TEST-DATE");

            clientChannel.MonitorChanged += (chann, val) =>
            {
                Console.WriteLine("New value: " + val);
            };
            clientChannel.StatusChanged += (chann, newStatus) =>
            {
                Console.WriteLine("!!! New status: " + newStatus.ToString());
            };

            Thread.Sleep(2000);

            /*Console.WriteLine("Kill gw1");
            gw1.Dispose();

            Console.WriteLine("Starting again gw1");
            gw1 = new Gateway();
            gw1.Configuration.DiagnosticPort = 1234;
            gw1.Configuration.SideA = "127.0.0.1:5432";
            gw1.Configuration.RemoteSideB = "127.0.0.1:5056";
            gw1.Configuration.SideB = "127.0.0.1:5055";
            gw1.Log.Handler += (level, source, message) =>
            {
                if (message.StartsWith("Echo"))
                    return;
                Console.WriteLine("GW1: " + message);
            };
            gw1.Start();*/

            Console.WriteLine("Kill gw2");
            gw2.Dispose();

            Console.WriteLine("Starting again gw2");
            gw2 = new Gateway();
            gw2.Configuration.DiagnosticPort = 1236;
            gw2.Configuration.SideA = "127.0.0.1:5056";
            gw2.Configuration.RemoteSideB = "127.0.0.1:6058";
            gw2.Configuration.SideB = "127.0.0.1:5057";
            gw2.Log.Handler += (level, source, message) =>
            {
                if (message.StartsWith("Echo"))
                    return;
                Console.WriteLine("GW2: " + message);
            };
            gw2.Start();

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
            client.Dispose();
        }

        static void S1()
        {
            var gateway = new Gateway();
            gateway.Configuration.SideA = "127.0.0.1:5432";
            gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
            gateway.Configuration.SideB = "127.0.0.1:5055";
            gateway.Start();

            // Serverside
            var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5056, 5056);
            var serverChannel = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAStringRecord>("TEST-DATE");
            serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
            serverChannel.Value = "Works fine!";

            var serverChannel2 = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CADoubleRecord>("TEST-DOUBLE");
            serverChannel2.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.ON_CHANGE;
            serverChannel2.Value = 5;

            var t = new Thread((obj) =>
              {
                  var rnd = new Random();
                  while (true)
                  {
                      Thread.Sleep(10);
                      serverChannel2.Value = rnd.NextDouble();
                  }
              });
            t.IsBackground = true;
            t.Start();

            // Client

            var client = new CAClient();
            client.Configuration.WaitTimeout = 200;
            client.Configuration.SearchAddress = "127.0.0.1:5432";
            var clientChannel = client.CreateChannel<string>("TEST-DATE");
            var clientChannel2 = client.CreateChannel<string>("TEST-DOUBLE");

            server.Start();

            //Console.WriteLine(clientChannel.Get());

            //Assert.AreEqual("Works fine!", clientChannel.Get());

            clientChannel.StatusChanged += (sender, newStatus) =>
            {
                Console.WriteLine(sender.ChannelName + " === " + newStatus);
            };

            clientChannel2.StatusChanged += (sender, newStatus) =>
            {
                Console.WriteLine(sender.ChannelName + " === " + newStatus);
            };

            clientChannel.MonitorChanged += (sender, newval) =>
            {
            };

            clientChannel2.MonitorChanged += (sender, newval) =>
            {
            };

            Console.WriteLine("Press any key to stop");
            Console.ReadKey();
            server.Dispose();
            client.Dispose();
            Environment.Exit(0);
        }

        static void Test()
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
        static void TestDiagnosticServer()
        {
            var gateway = new Gateway();
            gateway.Configuration.GatewayName = "TESTGW-DIAG";
            gateway.Configuration.SideB = "129.129.194.45:5436";
            gateway.Configuration.SideA = "129.129.194.45:5055";

            /*gateway.Configuration.SideA = "129.129.130.45:5436";
            gateway.Configuration.SideB = "127.0.0.2:5055";*/

            //gateway.Configuration.RemoteSideA = "127.0.0.1:5058";
            //gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
            gateway.Configuration.RemoteSideA = "127.0.0.1:5058";
            //gateway.Configuration.RemoteSideB = "129.129.130.255:5064";
            gateway.Configuration.RemoteSideB = "sls-cagw02.psi.ch:5062";
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
