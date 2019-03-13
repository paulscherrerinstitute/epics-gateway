using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace LoadPerformance
{
    internal class LoadClient : IDisposable
    {
        private string searchAddress;
        private int nbMons;
        private readonly int nbClients;
        private UdpClient udpClient = null;
        private object clientLock = new object();
        private long totCount = 0;
        private long dataCount = 0;
        private object counterLock = new object();
        private DateTime start = DateTime.UtcNow;
        private ClientConnection[] connections;

        public int NbConnected { get; set; } = 0;

        public LoadClient(string searchAddress, int nbMons, int nbClients, int udpPort)
        {
            this.searchAddress = searchAddress;
            this.nbMons = nbMons;
            this.nbClients = nbClients;
            connections = new ClientConnection[nbClients];

            udpClient = new UdpClient(udpPort);
            udpClient.BeginReceive(UdpReceive, null);

            for (var i = 0; i < nbMons; i++)
            {
                var channelName = "PERF-CHECK-IARR:" + i;
                var search = DataPacket.Create(channelName.Length + 8 - channelName.Length % 8);
                search.Command = 6; // CA_PROTO_SEARCH
                search.DataType = 4; // DONT_REPLY
                search.DataCount = 11; // MINOR PROTO VERSION
                search.Parameter1 = (uint)i; // CID
                search.Parameter2 = (uint)i; // CID
                search.SetDataAsString(channelName);

                udpClient.Connect(ParseAddress(searchAddress));
                udpClient.Send(search.Data, search.Data.Length);
            }
        }

        private void UdpReceive(IAsyncResult ar)
        {
            var endPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] buff;
            try
            {
                buff = udpClient.EndReceive(ar, ref endPoint);
                udpClient.BeginReceive(UdpReceive, null);
            }
            catch
            {
                return;
            }

            var splitter = new Splitter();
            foreach (var p in splitter.Split(DataPacket.Create(buff)))
            {
                if (!p.HasCompleteHeader || p.MessageSize != p.Data.Length)
                    continue;
                switch (p.Command)
                {
                    case (ushort)EpicsCommand.SEARCH:
                        // We want to react only to the searches answers.
                        if (p.PayloadSize == 8 && p.DataCount == 0)
                        {
                            lock (clientLock)
                            {
                                // First answer => we need to connect
                                var clientId = (int)p.Parameter2 % nbClients;
                                if (connections[clientId] == null)
                                    connections[clientId] = new ClientConnection(this, new IPEndPoint(endPoint.Address, p.DataType));

                                connections[clientId].ChannelConnect((int)p.Parameter2);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        internal void Disconnect(ClientConnection clientConnection)
        {
            lock (clientLock)
            {
                for (var i = 0; i < connections.Length; i++)
                {
                    if (connections[i] == clientConnection)
                    {
                        connections[i] = null;
                        break;
                    }
                }
            }
        }

        internal void Increment(long totCount, long dataCount)
        {
            lock(counterLock)
            {
                this.totCount += totCount;
                this.dataCount += dataCount;
            }
        }


        public void ResetCounter()
        {
            lock (counterLock)
            {
                start = DateTime.UtcNow;
                totCount = 0;
                dataCount = 0;
            }
        }

        public long DataPerSeconds
        {
            get
            {
                lock (counterLock)
                {
                    try
                    {
                        return (long)(dataCount / (DateTime.UtcNow - start).TotalSeconds);
                    }
                    catch
                    {
                        return 0;
                    }
                }
            }
        }

        public long ExpectedDataPerSeconds
        {
            get
            {
                return Program.ArraySize * 4 * 10 * nbMons;
            }
        }


        public void Dispose()
        {
            udpClient?.Dispose();
            foreach (var c in connections)
                c?.Dispose();
        }

        static public IPEndPoint ParseAddress(string addr)
        {
            string[] parts = addr.Split(new char[] { ':' });
            try
            {
                return new IPEndPoint(IPAddress.Parse(parts[0].Trim()), int.Parse(parts[1].Trim()));
            }
            //catch (Exception ex)
            catch
            {
                return new IPEndPoint(Dns.GetHostEntry(parts[0]).AddressList.First(), int.Parse(parts[1].Trim()));
            }
        }
    }
}
