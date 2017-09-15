using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Connections
{
    class TcpServerConnection : GatewayConnection
    {
        public IPEndPoint RemoteEndPoint { get; }
        public Gateway Gateway { get; private set; }

        Socket socket;
        object lockObject = new object();
        bool isConnected = false;
        List<Action> toCallWhenReady = new List<Action>();
        readonly byte[] buffer = new byte[Gateway.BUFFER_SIZE];
        Splitter splitter = new Splitter();

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

            var mainPacket = DataPacket.Create(buffer, size, false);

            try
            {
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);
            }
            catch (Exception ex)
            {
                Gateway.Log.Write(Services.LogLevel.Error, "Exception: " + ex);
            }

            foreach (var p in splitter.Split(mainPacket))
            {
                //Log.Write("+> Packet size " + p.MessageSize + " (command " + p.Command + ")");
                p.Sender = RemoteEndPoint;
                Commands.CommandHandler.ExecuteResponseHandler(p.Command, this, p);
                //Log.Write(" ++> End of packet");
            }
        }

        public override void Send(DataPacket packet)
        {
            lock (lockObject)
            {
                socket.Send(packet.Data, packet.BufferSize, SocketFlags.None);
            }
        }

        public override void Dispose()
        {
            socket.Dispose();
            Gateway.ServerConnection.Remove(this);
        }
    }
}
