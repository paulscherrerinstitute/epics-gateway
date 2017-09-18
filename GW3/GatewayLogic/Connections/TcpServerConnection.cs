using GatewayLogic.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Connections
{
    class TcpServerConnection : GatewayTcpConnection
    {
        public Gateway Gateway { get; private set; }

        Socket socket;
        object lockObject = new object();
        bool isConnected = false;
        List<Action> toCallWhenReady = new List<Action>();
        readonly byte[] buffer = new byte[Gateway.BUFFER_SIZE];
        Splitter splitter = new Splitter();

        readonly List<ChannelInformation.ChannelInformationDetails> channels = new List<ChannelInformation.ChannelInformationDetails>();

        public TcpServerConnection(Gateway gateway, IPEndPoint destination) : base(gateway)
        {
            RemoteEndPoint = destination;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            IAsyncResult result = socket.BeginConnect(destination, ConnectionBuilt, null);
            Gateway = gateway;
        }

        private void ConnectionBuilt(IAsyncResult ar)
        {
            lock (lockObject)
            {
                isConnected = true;

                foreach (var action in toCallWhenReady)
                    action();
                toCallWhenReady.Clear();
            }
        }

        internal void WhenConnected(Action whenDone)
        {
            try
            {
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);
            }
            catch (Exception ex)
            {
                Gateway.Log.Write(Services.LogLevel.Error, "Exception: " + ex);
            }

            lock (lockObject)
            {
                if (isConnected)
                {
                    whenDone();
                    return;
                }

                toCallWhenReady.Add(whenDone);
            }
        }

        private void ReceiveTcpData(IAsyncResult ar)
        {
            int size = 0;
            try
            {
                size = socket.EndReceive(ar);
            }
            catch (ObjectDisposedException ex)
            {
                Gateway.Log.Write(Services.LogLevel.Error, ex.ToString());
                // Stop receiving
                return;
            }
            //Log.Write("Server received " + size + " bytes from " + this.Destination);

            this.LastMessage = DateTime.Now;
            var mainPacket = DataPacket.Create(buffer, size, false);

            try
            {
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);
            }
            catch (SocketException ex1)
            {
                this.Dispose();
            }
            catch (Exception ex)
            {
                Gateway.Log.Write(Services.LogLevel.Error, "Exception: " + ex);
                this.Dispose();
            }

            foreach (var p in splitter.Split(mainPacket))
            {
                //Log.Write("+> Packet size " + p.MessageSize + " (command " + p.Command + ")");
                p.Sender = RemoteEndPoint;
                Commands.CommandHandler.ExecuteResponseHandler(p.Command, this, p);
                //Log.Write(" ++> End of packet");
            }
        }

        public void LinkChannel(ChannelInformation.ChannelInformationDetails channel)
        {
            lock (channels)
            {
                channels.Add(channel);
            }
        }

        public override void Send(DataPacket packet)
        {
            lock (lockObject)
            {
                try
                {
                    socket.Send(packet.Data, packet.BufferSize, SocketFlags.None);
                }
                catch (Exception ex)
                {
                    this.Dispose();
                }
            }
        }

        public override void Dispose()
        {
            socket.Dispose();
            Gateway.ServerConnection.Remove(this);

            lock (channels)
            {
                var newPacket = DataPacket.Create(0);
                newPacket.Command = 27;
                foreach (var channel in channels)
                    Gateway.ChannelInformation.Remove(channel);

                foreach (var channel in channels)
                {
                    Gateway.Log.Write(LogLevel.Detail, "Disposing channel " + channel.ChannelName);
                    foreach (var client in channel.GetClientConnections())
                    {
                        newPacket.Parameter1 = client.Id;
                        newPacket.Destination = client.Connection.RemoteEndPoint;
                        client.Connection.Send(newPacket);
                    }
                }
            }
        }
    }
}
