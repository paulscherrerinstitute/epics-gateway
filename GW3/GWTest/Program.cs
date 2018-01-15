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
            TestDiagnosticServer();
            //Test();
            //S1();
            Console.ReadKey();
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

            // Client

            var client = new CAClient();
            client.Configuration.WaitTimeout = 200;
            client.Configuration.SearchAddress = "127.0.0.1:5432";
            var clientChannel = client.CreateChannel<string>("TEST-DATE");
            server.Start();

            Console.WriteLine(clientChannel.Get());

            //Assert.AreEqual("Works fine!", clientChannel.Get());

            var autoReset = new AutoResetEvent(false);
            clientChannel.StatusChanged += (sender, newStatus) =>
            {
                Console.WriteLine("=== " + newStatus);
                if (newStatus == EpicsSharp.ChannelAccess.Constants.ChannelStatus.DISCONNECTED)
                    autoReset.Set();
            };
            server.Dispose();
            try
            {
                clientChannel.Get();
            }
            catch
            {
            }
            autoReset.WaitOne();

            gateway.Dispose();
            client.Dispose();
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
            gateway.Configuration.RemoteSideB = "129.129.130.255:5064";
            gateway.Configuration.ConfigurationType = GatewayLogic.Configuration.ConfigurationType.BIDIRECTIONAL;
            //gateway.Configuration.ConfigurationType = GatewayLogic.Configuration.ConfigurationType.UNIDIRECTIONAL;
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
