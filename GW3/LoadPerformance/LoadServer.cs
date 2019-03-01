using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace LoadPerformance
{
    internal class LoadServer : IDisposable
    {
        private int basePort;
        private List<TcpListener> listeners=new List<TcpListener>();
        private UdpClient udpListener;
        private List<ServerConnection> connections = new List<ServerConnection>();

        public LoadServer(string ip, int port, int nbListeners)
        {
            this.basePort = port;
            for (var i = 0; i < nbListeners; i++)
            {
                var listener = new TcpListener(IPAddress.Parse(ip), port + i);
                listener.Start();
                listener.BeginAcceptSocket(ConnectionReceived, listener);
                listeners.Add(listener);
            }

            udpListener = new UdpClient();
            udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpListener.Client.Bind(new IPEndPoint(IPAddress.Parse(ip), port));

            udpListener.BeginReceive(UdpReceive, null);
        }

        private void UdpReceive(IAsyncResult ar)
        {
            var endPoint = new IPEndPoint(IPAddress.Any, 0);
            var buff = udpListener.EndReceive(ar, ref endPoint);
            udpListener.BeginReceive(UdpReceive, null);

            var splitter = new Splitter();
            foreach (var p in splitter.Split(DataPacket.Create(buff)))
            {
                if (!p.HasCompleteHeader || p.MessageSize != p.Data.Length)
                    continue;
                switch (p.Command)
                {
                    case (ushort)EpicsCommand.SEARCH:
                        // We want to react only to the searches not responses.
                        if (!(p.PayloadSize == 8 && p.DataCount == 0))
                        {
                            var channel = p.GetDataAsString();
                            //Console.WriteLine("Search for " + channel);
                            if (channel.StartsWith("PERF-CHECK-IARR:"))
                            {
                                var id = int.Parse(channel.Split(new char[] { ':' })[1]);
                                var newPacket = DataPacket.Create(8);

                                newPacket.Command = 6;
                                newPacket.DataType = (UInt16)(this.basePort + id % listeners.Count);
                                newPacket.DataCount = 0;
                                newPacket.Parameter1 = 0xffffffff;
                                newPacket.Parameter2 = p.Parameter1;
                                newPacket.Destination = endPoint;
                                newPacket.SetUInt16(16, Program.CA_PROTO_VERSION);
                                newPacket.ReverseAnswer = true;
                                UdpSend(newPacket);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void UdpSend(DataPacket packet)
        {
            udpListener.Client.SendTo(packet.Data, packet.Data.Length, SocketFlags.None, packet.Destination);
        }

        private void ConnectionReceived(IAsyncResult ar)
        {
            var listener = (TcpListener)ar.AsyncState;
            var socket = listener.EndAcceptSocket(ar);
            listener.BeginAcceptSocket(ConnectionReceived, listener);
            lock (connections)
            {
                connections.Add(new ServerConnection(this, socket));
            }
        }

        public long NbChangedPerSec
        {
            get
            {
                return 0;
            }
        }

        internal void RemoveConnection(ServerConnection serverConnection)
        {
            lock (connections)
                connections.Remove(serverConnection);
        }

        /*public long UpdateRecords(int nb)
        {
            pos++;
            for (var i = 0; i < nb; i++)
                records[i].Value[0] = pos;
            return nb * NbItems * 4;
        }*/

        public void Dispose()
        {
        }
    }
}
