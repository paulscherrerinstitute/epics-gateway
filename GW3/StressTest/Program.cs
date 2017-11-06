using EpicsSharp.ChannelAccess.Client;
using EpicsSharp.ChannelAccess.Server;
using EpicsSharp.ChannelAccess.Server.RecordTypes;
using GatewayLogic;
using System;
using System.Collections.Generic;
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
            if (args[1] == "server")
            {
                Server(int.Parse(args[2]));
                return;
            }

            if (args[1] == "client")
            {
                Client();
                return;
            }

            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Log.Filter = (level) => { return level >= GatewayLogic.Services.LogLevel.Error; };
                gateway.Start();

                for (var i = 0; i < NB_SERVERS; i++)
                    gateway.Configuration.RemoteSideB = (i != 0 ? ";" : "") + "127.0.0.1:" + (5056 + i);
            }
        }

        private static void Client()
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

        static void Server(int port)
        {
            using (var evt = new AutoResetEvent(false))
            {
                using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), port, port))
                {
                    var serverChannels = new List<CAStringRecord>();
                    for (var i = 0; i < NB_CHANNELS; i++)
                    {
                        var serverChannel = server.CreateRecord<CAStringRecord>("STRESS-TEST-" + ((i + 1) + (port - 5056 * NB_SERVERS)));
                        serverChannel.Value = "Works fine! - " + i;
                        serverChannels.Add(serverChannel);
                    }
                    server.Start();
                    evt.WaitOne();
                }
            }
        }
    }
}
