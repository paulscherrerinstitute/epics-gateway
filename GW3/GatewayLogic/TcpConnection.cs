using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic
{
    class TcpConnection
    {
        Socket socket;
        object lockObject = new object();
        bool isConnected = false;
        List<Action> toCallWhenReady = new List<Action>();

        public TcpConnection(IPEndPoint destination)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            IAsyncResult result = socket.BeginConnect(destination, ConnectionBuilt, null);
        }

        private void ConnectionBuilt(IAsyncResult ar)
        {
            lock(lockObject)
            {
                isConnected = true;

                foreach (var action in toCallWhenReady)
                    action();
                toCallWhenReady.Clear();
            }
        }

        internal void WhenConnected(Action whenDone)
        {
            lock(lockObject)
            {
                if(isConnected)
                {
                    whenDone();
                    return;
                }

                toCallWhenReady.Add(whenDone);
            }
        }

        public void Send(DataPacket packet)
        {
            lock(lockObject)
            {
                socket.Send(packet.Data, packet.BufferSize, SocketFlags.None);
            }
        }
    }
}
