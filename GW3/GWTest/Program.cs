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
    class Program
    {
        static void Main(string[] args)
        {

            var gateway = new Gateway();
            //gateway.Configuration.SideA = "129.129.130.45:5054";
            gateway.Configuration.SideA = "129.129.130.45:5432";
            gateway.Configuration.RemoteSideB = "129.129.130.45:5056";
            gateway.Configuration.SideB = "129.129.130.45:5055";
            gateway.Start();

            // Serverside
            var server = new CAServer(IPAddress.Parse("129.129.130.45"), 5056, 5056);
            var serverChannel = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAStringRecord>("TEST-DATE");
            serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
            serverChannel.Value = DateTime.Now.ToLongTimeString();
            serverChannel.PrepareRecord += (sender, e) =>
            {
                serverChannel.Value = DateTime.Now.ToLongTimeString();
            };

            // Client

            var client = new CAClient();
            client.Configuration.SearchAddress = "129.129.130.45:5432";
            var clientChannel = client.CreateChannel<string>("TEST-DATE");
            clientChannel.MonitorChanged += (sender, newValue)=>
            {
                Console.WriteLine(newValue);
            };

            //Thread.Sleep(1000);
            server.Start();


            //Thread.Sleep(2000);
            //Console.WriteLine(clientChannel.Get());

            Console.ReadKey();
        }
    }
}
