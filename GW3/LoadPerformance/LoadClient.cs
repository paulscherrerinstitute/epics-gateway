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
        private UdpClient udpClient = null;
        private TcpClient tcpClient = null;
        private object clientLock = new object();
        private byte[] receiveBuffer = new byte[10240];
        private Splitter splitter = new Splitter();
        private long totCount = 0;
        private long dataCount = 0;
        private DateTime start = DateTime.UtcNow;

        public long DataPerSeconds
        {
            get
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

        public LoadClient(string searchAddress, int nbMons, int udpPort)
        {
            this.searchAddress = searchAddress;
            this.nbMons = nbMons;

            udpClient = new UdpClient(udpPort);
            udpClient.BeginReceive(UdpReceive, null);

            for (var i = 0; i < nbMons; i++)
            {
                var channelName = "PERF-CHECK-IARR:" + i;
                var search = DataPacket.Create(channelName.Length + 8 - channelName.Length % 8);
                search.Command = 6; // CA_PROTO_SEARCH
                search.DataType = 4; // DONT_REPLY
                search.DataCount = 11; // MINOR PROTO VERSION
                search.Parameter1 = 1; // CID
                search.Parameter2 = 1; // CID
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
                                if (tcpClient == null)
                                {
                                    tcpClient = new TcpClient();
                                    tcpClient.Connect(new IPEndPoint(endPoint.Address, p.DataType));
                                    tcpClient.Client.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, TcpReceive, null);
                                }
                                // Just go to the create & add
                                var channelName = "PERF-CHECK-IARR:" + p.Parameter2;
                                var packet = DataPacket.Create(channelName.Length + 8 - channelName.Length % 8);
                                packet.Command = 18; //CA_PROTO_CREATE_CHAN;
                                packet.DataType = 0;
                                packet.DataCount = 0;
                                packet.Parameter1 = p.Parameter2; // CID
                                packet.Parameter2 = Program.CA_PROTO_VERSION; // CA_MINOR_PROTOCOL_REVISION;
                                packet.SetDataAsString(channelName);

                                TcpSend(packet);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void TcpSend(DataPacket packet)
        {
            try
            {
                tcpClient.Client.Send(packet.Data);
            }
            catch(Exception ex)
            {
                Dispose();
            }
        }

        private void TcpReceive(IAsyncResult ar)
        {
            int n;
            try
            {
                n = tcpClient.Client.EndReceive(ar);
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

            var newPacket = DataPacket.Create(receiveBuffer, n);

            foreach (var p in splitter.Split(newPacket))
            {
                switch (p.Command)
                {
                    case (ushort)EpicsCommand.CREATE_CHANNEL:
                        var toSend = DataPacket.Create(16 + 16);
                        toSend.Command = 1;
                        toSend.DataType = 5;
                        toSend.DataCount = p.DataCount;
                        toSend.Parameter1 = p.Parameter2;
                        toSend.Parameter2 = p.Parameter1;
                        TcpSend(toSend);
                        break;
                    case (ushort)EpicsCommand.EVENT_ADD:
                        totCount += newPacket.MessageSize;
                        dataCount += newPacket.PayloadSize;
                        break;
                    default:
                        break;
                }
            }
            try
            {
                tcpClient.Client.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, TcpReceive, null);
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            udpClient?.Dispose();
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
