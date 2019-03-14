using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LoadPerformance
{
    internal class LoadClient : IDisposable
    {
        private string searchAddress;
        private int nbMons;
        private readonly int nbClients;
        private readonly DataPacket search;
        private UdpClient udpClient = null;
        private object clientLock = new object();
        private long totCount = 0;
        private long dataCount = 0;
        private object counterLock = new object();
        private DateTime start = DateTime.UtcNow;
        private ClientConnection[] connections;
        private IPEndPoint tcpServerAddress;
        private Thread reconnectingThread;
        private CancellationTokenSource cancel;

        public int NbConnected
        {
            get
            {
                lock (connections)
                    return connections.Where(row => row != null).Sum(row => row.Connected.Count());
            }
        }

        public LoadClient(string searchAddress, int nbMons, int nbClients, int udpPort)
        {
            this.searchAddress = searchAddress;
            this.nbMons = nbMons;
            this.nbClients = nbClients;
            connections = new ClientConnection[nbClients];

            udpClient = new UdpClient(udpPort);
            udpClient.BeginReceive(UdpReceive, null);
            udpClient.Connect(ParseAddress(searchAddress));

            for (var i = 0; i < nbMons; i++)
            {
                var channelName = "PERF-CHECK-IARR:" + i;
                search = DataPacket.Create(channelName.Length + 8 - channelName.Length % 8);
                search.Command = 6; // CA_PROTO_SEARCH
                search.DataType = 4; // DONT_REPLY
                search.DataCount = 11; // MINOR PROTO VERSION
                search.Parameter1 = (uint)i; // CID
                search.Parameter2 = (uint)i; // CID
                search.SetDataAsString(channelName);

                udpClient.Send(search.Data, search.Data.Length);
            }

            cancel = new CancellationTokenSource();
            reconnectingThread = new Thread(Reconnect);
            reconnectingThread.Start();
        }

        private void Reconnect()
        {
            while (!cancel.IsCancellationRequested)
            {
                Thread.Sleep(1000);

                try
                {
                    lock (clientLock)
                    {
                        var connected = connections.Where(row => row != null).SelectMany(row => row.Connected).ToList();
                        var toConnect = Enumerable.Range(0, nbMons).Where(row => !connected.Contains(row)).ToList();
                        toConnect.ForEach(row =>
                        {
                            var channelName = "PERF-CHECK-IARR:" + row;
                            search.Command = 6; // CA_PROTO_SEARCH
                            search.DataType = 4; // DONT_REPLY
                            search.DataCount = 11; // MINOR PROTO VERSION
                            search.Parameter1 = (uint)row; // CID
                            search.Parameter2 = (uint)row; // CID
                            search.SetDataAsString(channelName);

                            udpClient.Send(search.Data, search.Data.Length);
                        });
                    }
                }
                catch (Exception ex)
                {
                }
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
                                this.tcpServerAddress = new IPEndPoint(endPoint.Address, p.DataType);
                                if (connections[clientId] == null)
                                    connections[clientId] = new ClientConnection(this, this.tcpServerAddress);

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
            lock (counterLock)
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
            cancel.Cancel();
            udpClient?.Dispose();
            foreach (var c in connections)
                c?.Dispose();
            reconnectingThread.Join();
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
