using System;
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
        private TcpClient[] tcpClients;
        private object clientLock = new object();
        private byte[][] receiveBuffers;
        private Splitter[] splitters;
        private long totCount = 0;
        private long dataCount = 0;
        private object counterLock = new object();
        private DateTime start = DateTime.UtcNow;
        private bool firstAnswer = true;

        public int NbConnected { get; private set; } = 0;

        public LoadClient(string searchAddress, int nbMons, int nbClients, int udpPort)
        {
            this.searchAddress = searchAddress;
            this.nbMons = nbMons;
            this.nbClients = nbClients;
            tcpClients = new TcpClient[nbClients];
            receiveBuffers = new byte[nbClients][];
            splitters = new Splitter[nbClients];
            for (var i = 0; i < nbClients; i++)
            {
                receiveBuffers[i] = new byte[10240];
                splitters[i] = new Splitter();
            }

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
                                if (tcpClients[clientId] == null)
                                {
                                    var tcpClient = new TcpClient();
                                    tcpClient.Connect(new IPEndPoint(endPoint.Address, p.DataType));
                                    tcpClient.Client.BeginReceive(receiveBuffers[clientId], 0, receiveBuffers[clientId].Length, SocketFlags.None, TcpReceive, clientId);
                                    tcpClients[p.Parameter2 % nbClients] = tcpClient;
                                }

                                //Console.WriteLine("Search answer for "+ p.Parameter2);

                                // Just go to the create & add
                                var channelName = "PERF-CHECK-IARR:" + p.Parameter2;
                                var packet = DataPacket.Create(channelName.Length + 8 - channelName.Length % 8);
                                packet.Command = 18; //CA_PROTO_CREATE_CHAN;
                                packet.DataType = 0;
                                packet.DataCount = 0;
                                packet.Parameter1 = p.Parameter2; // CID
                                packet.Parameter2 = Program.CA_PROTO_VERSION; // CA_MINOR_PROTOCOL_REVISION;
                                packet.SetDataAsString(channelName);

                                TcpSend(packet, clientId);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void TcpSend(DataPacket packet, int clientId)
        {
            try
            {
                tcpClients[clientId].Client.Send(packet.Data);
            }
            catch (Exception ex)
            {
                Dispose();
            }
        }

        private void TcpReceive(IAsyncResult ar)
        {
            int n;
            var clientId = (int)ar.AsyncState;
            try
            {
                n = tcpClients[clientId].Client.EndReceive(ar);
            }
            catch
            {
                return;
            }
            if (n == 0)
            {
                Dispose();
                return;
            }

            var newPacket = DataPacket.Create(receiveBuffers[clientId], n, false);

            foreach (var p in splitters[clientId].Split(newPacket))
            {
                //Console.WriteLine("Client Cmd: " + p.Command + ", " + p.MessageSize);
                MessageVerifier.Verify(p, false);

                switch (p.Command)
                {
                    case (ushort)EpicsCommand.ACCESS_RIGHTS:
                        break;
                    case (ushort)EpicsCommand.CREATE_CHANNEL:
                        var toSend = DataPacket.Create(16);
                        toSend.Command = 1;
                        toSend.DataType = 5;
                        toSend.DataCount = p.DataCount;
                        toSend.Parameter1 = p.Parameter2;
                        toSend.Parameter2 = p.Parameter1;
                        toSend.SetUInt16(12 + 16, (ushort)1);
                        NbConnected++;
                        //Thread.Sleep(100);
                        TcpSend(toSend, clientId);
                        break;
                    case (ushort)EpicsCommand.EVENT_ADD:
                        if (firstAnswer)
                        {
                            firstAnswer = false;
                            lock (counterLock)
                                start = DateTime.UtcNow;
                            //Console.WriteLine("Payload: " + p.PayloadSize);
                        }
                        /*if (p.PayloadSize != Program.ArraySize * 4)
                            Console.WriteLine("Size error: " + p.PayloadSize);*/
                        lock (counterLock)
                        {
                            totCount += p.MessageSize;
                            dataCount += p.PayloadSize;
                        }
                        break;
                    default:
                        //Console.WriteLine("Odd message: " + p.Command);
                        break;
                }
            }
            try
            {
                tcpClients[clientId].Client.BeginReceive(receiveBuffers[clientId], 0, receiveBuffers[clientId].Length, SocketFlags.None, TcpReceive, clientId);
            }
            catch
            {
                Dispose();
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
            foreach (var tcpClient in tcpClients)
                tcpClient?.Dispose();
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
