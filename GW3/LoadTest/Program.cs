using EpicsSharp.ChannelAccess.Client;
using EpicsSharp.ChannelAccess.Server;
using EpicsSharp.ChannelAccess.Server.RecordTypes;
using GatewayLogic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace LoadTest
{
    internal class Program
    {
        public const string NetworkIP = "127.0.0.1";
        public const int IocPort = 2431;
        public const int GatewayNetworkPort = 5432;
        public static int ArraySize = 196 * 196;
        public static int WaitTime = 2;

        private static void Main(string[] args)
        {
            /*var NbServers = 10;
            var NbClients = 10;*/

            var servers = new List<CAServer>();
            for (var serverId = 0; serverId < NbServers; serverId++)
            {
                var server = new CAServer(System.Net.IPAddress.Parse(NetworkIP), IocPort + serverId, IocPort + serverId);
                //var record = server.CreateArrayRecord<CAIntArrayRecord>("LOAD-" + serverId + ":ARR", ArraySize);
                var record = server.CreateRecord<CAIntRecord>("LOAD-" + serverId + ":INT");
                record.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.HZ10;
                record.PrepareRecord += (sender, evt) =>
                {
                    record.Value = (serverChanges % 100) + serverId;
                    lock (changeStats)
                        serverChanges++;
                };
                server.Start();
                servers.Add(server);
            }


            var gateway = new Gateway();
            gateway.Configuration.RemoteSideA = NetworkIP + ":" + (GatewayNetworkPort - 2);
            gateway.Configuration.SideA = NetworkIP + ":" + GatewayNetworkPort;
            gateway.Configuration.SideB = NetworkIP + ":" + (GatewayNetworkPort + 2);
            var remote = new StringBuilder();
            for (var serverId = 0; serverId < NbServers; serverId++)
            {
                if (serverId != 0)
                    remote.Append(";");
                remote.Append(NetworkIP + ":" + (IocPort + serverId));
            }
            gateway.Configuration.RemoteSideB = remote.ToString();
            gateway.Configuration.ConfigurationType = GatewayLogic.Configuration.ConfigurationType.UNIDIRECTIONAL;
            gateway.Configuration.DelayStartup = 0;
            gateway.Configuration.GatewayName = "LOADTESTGW";
            gateway.Start();

            var data = new List<ResultPoint>();
            for (var i = 1; i < 100; i++)
            {
                var r = TestPointInt(3, i);
                data.Add(r);
                System.Console.WriteLine("" + i + "=>" + r.Difference + " CPU=" + r.CPU + "%");

                using (var file = new StreamWriter(File.OpenWrite("C:\\temp\\point_int.json")))
                {
                    var serialize = Newtonsoft.Json.JsonSerializer.Create();
                    serialize.Serialize(file, data);
                }
            }


            Console.ReadKey();
        }

        private static ResultPoint TestPointInt(int NbClients, int NbServers)
        {
            Console.WriteLine("Starting " + NbClients + " " + NbServers);

            var changeStats = new object();

            int serverChanges = 0;
            int clientReceived = 0;

            var clients = new List<CAClient>();
            var channels = new List<Channel<int>>();
            var channelData = new Dictionary<Channel<int>, bool>();
            for (var clientId = 0; clientId < NbClients; clientId++)
            {
                var client = new CAClient();
                client.Configuration.SearchAddress = NetworkIP + ":" + GatewayNetworkPort;

                for (var channelId = 0; channelId < NbServers; channelId++)
                {
                    var channel = client.CreateChannel<int>("LOAD-" + channelId + ":INT");
                    channel.MonitorMask = EpicsSharp.ChannelAccess.Constants.MonitorMask.ALL;
                    channel.WishedDataCount = 196 * 196;
                    channel.MonitorChanged += (sender, newValue) =>
                    {
                        lock (channelData)
                            channelData[sender] = true;
                        lock (changeStats)
                            clientReceived++;
                    };
                    channels.Add(channel);
                }
                clients.Add(client);
            }


            var x = DiagnosticInfo.GetCPUUsage();

            // Wait for all the connections
            var allConnected = false;
            for (var wait = 0; wait < 50 && !allConnected; wait++)
            {
                Thread.Sleep(100);
                lock (channelData)
                    allConnected = channelData.Count() == channels.Count;
            }
            Console.WriteLine("All connected for " + NbClients + " " + NbServers);
            Thread.Sleep(5000);
            var diff = 0;
            lock (changeStats)
                diff = (serverChanges * NbClients - clientReceived);

            Thread.Sleep(WaitTime * 500);
            var cpu = DiagnosticInfo.GetCPUUsage();
            Thread.Sleep(WaitTime * 500);

            clients.ForEach(row => row.Dispose());
            lock (changeStats)
            {
                return new ResultPoint
                {
                    NbClients = NbClients,
                    NbServers = NbServers,
                    NbChannelsUsed = channels.Count,
                    CPU = (int)Math.Round(cpu),
                    Expected = (serverChanges * NbClients) - diff,
                    Received = clientReceived,
                    Difference = ((serverChanges * NbClients - clientReceived) - diff)
                };
            }
        }


        private static ResultPoint TestPointArray(int NbClients, int NbServers)
        {
            Console.WriteLine("Starting " + NbClients + " " + NbServers);

            var changeStats = new object();

            int serverChanges = 0;
            int clientReceived = 0;

            var gateway = new Gateway();
            gateway.Configuration.RemoteSideA = NetworkIP + ":" + (GatewayNetworkPort - 2);
            gateway.Configuration.SideA = NetworkIP + ":" + GatewayNetworkPort;
            gateway.Configuration.SideB = NetworkIP + ":" + (GatewayNetworkPort + 2);
            var remote = new StringBuilder();
            for (var serverId = 0; serverId < NbServers; serverId++)
            {
                if (serverId != 0)
                    remote.Append(";");
                remote.Append(NetworkIP + ":" + (IocPort + serverId));
            }
            gateway.Configuration.RemoteSideB = remote.ToString();
            gateway.Configuration.ConfigurationType = GatewayLogic.Configuration.ConfigurationType.UNIDIRECTIONAL;
            gateway.Configuration.DelayStartup = 0;
            gateway.Configuration.GatewayName = "LOADTESTGW";
            gateway.Start();

            var clients = new List<CAClient>();
            var channels = new List<Channel<int[]>>();
            var channelData = new Dictionary<Channel<int[]>, bool>();
            for (var clientId = 0; clientId < NbClients; clientId++)
            {
                var client = new CAClient();
                client.Configuration.SearchAddress = NetworkIP + ":" + GatewayNetworkPort;

                for (var channelId = 0; channelId < NbServers; channelId++)
                {
                    var channel = client.CreateChannel<int[]>("LOAD-" + channelId + ":ARR");
                    channel.MonitorMask = EpicsSharp.ChannelAccess.Constants.MonitorMask.ALL;
                    channel.WishedDataCount = 196 * 196;
                    channel.MonitorChanged += (sender, newValue) =>
                    {
                        lock (channelData)
                            channelData[sender] = true;
                        lock (changeStats)
                            clientReceived++;
                    };
                    channels.Add(channel);
                }
                clients.Add(client);
            }

            var servers = new List<CAServer>();
            for (var serverId = 0; serverId < NbServers; serverId++)
            {
                var server = new CAServer(System.Net.IPAddress.Parse(NetworkIP), IocPort + serverId, IocPort + serverId);
                var record = server.CreateArrayRecord<CAIntArrayRecord>("LOAD-" + serverId + ":ARR", ArraySize);
                record.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.HZ10;
                record.PrepareRecord += (sender, evt) =>
                {
                    for (var i = 0; i < ArraySize; i++)
                        record.Value[i] = (serverChanges % 100) + i;
                    lock (changeStats)
                        serverChanges++;
                };
                server.Start();
                servers.Add(server);
            }

            var x = DiagnosticInfo.GetCPUUsage();

            // Wait for all the connections
            var allConnected = false;
            while (!allConnected)
            {
                Thread.Sleep(100);
                lock (channelData)
                    allConnected = channelData.Count() == channels.Count;
                //allConnected = !channels.Any(row => row.Status != EpicsSharp.ChannelAccess.Constants.ChannelStatus.CONNECTED);
            }
            Console.WriteLine("All connected for " + NbClients + " " + NbServers);
            Thread.Sleep(5000);
            var diff = 0;
            lock (changeStats)
                diff = (serverChanges * NbClients - clientReceived);

            Thread.Sleep(WaitTime * 500);
            var cpu = DiagnosticInfo.GetCPUUsage();
            Thread.Sleep(WaitTime * 500);

            gateway.Dispose();
            clients.ForEach(row => row.Dispose());
            servers.ForEach(row => row.Dispose());
            lock (changeStats)
            {
                return new ResultPoint
                {
                    NbClients = NbClients,
                    NbServers = NbServers,
                    NbChannelsUsed = channels.Count,
                    CPU = (int)Math.Round(cpu),
                    Expected = (serverChanges * NbClients) - diff,
                    Received = (clientReceived - diff),
                    Difference = ((serverChanges * NbClients - clientReceived) - diff)
                };
            }
        }
    }
}
