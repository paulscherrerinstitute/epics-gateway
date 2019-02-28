using System;
using System.Net.Sockets;

namespace LoadPerformance
{
    internal class ServerConnection : IDisposable
    {
        private readonly Socket socket;
        private readonly LoadServer server;
        private readonly Splitter splitter;
        private byte[] buffer = new byte[10240];

        public ServerConnection(LoadServer server, Socket socket)
        {
            Console.WriteLine("Connection received " + socket.RemoteEndPoint.ToString());
            this.socket = socket;
            this.server = server;
            this.splitter = new Splitter();
            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveData, null);
        }

        public void Dispose()
        {
            Console.WriteLine("Dispose " + socket.RemoteEndPoint.ToString());
            server.RemoveConnection(this);
        }

        private void ReceiveData(IAsyncResult ar)
        {
            int n = socket.EndReceive(ar);
            if (n == 0) // End of the socket
            {
                Dispose();
            }

            var newPacket = DataPacket.Create(buffer, n);

            foreach (var p in splitter.Split(newPacket))
            {
                switch (p.Command)
                {
                    case (ushort)EpicsCommand.CREATE_CHANNEL:
                        string channelName = p.GetDataAsString();
                        var id = int.Parse(channelName.Split(new char[] { ':' })[0]);

                        DataPacket resPacket = DataPacket.Create(0);
                        resPacket.Command = 22;
                        resPacket.DataType = 0;
                        resPacket.DataCount = 0;
                        resPacket.Parameter1 = p.Parameter1;
                        resPacket.Parameter2 = (uint)1; // Read only
                        resPacket.Destination = p.Sender;
                        Send(resPacket);

                        //resPacket = (DataPacket)packet.Clone();
                        resPacket = DataPacket.Create(0);
                        resPacket.Command = 18;
                        resPacket.Destination = p.Sender;
                        resPacket.DataType = channelInfo.DataType;
                        resPacket.DataCount = (uint)Program.ArraySize;
                        resPacket.Parameter1 = p.Parameter1;
                        resPacket.Parameter2 = id;
                        Send(resPacket);

                        break;
                    case (ushort)EpicsCommand.EVENT_ADD:
                        break;
                    default:
                        break;
                }
            }

            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveData, null);
        }

        private void Send(DataPacket resPacket)
        {
        }
    }
}
