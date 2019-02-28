using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace LoadPerformance
{
    internal class ServerConnection : IDisposable
    {
        private readonly Socket socket;
        private readonly LoadServer server;
        private readonly Splitter splitter;
        private byte[] buffer = new byte[10240];
        private Thread runnerThread;
        private bool needToStop = false;
        private List<ChannelSubscription> subscriptions = new List<ChannelSubscription>();
        private DataPacket intArray = DataPacket.Create(Program.ArraySize * 4);

        public ServerConnection(LoadServer server, Socket socket)
        {
            Console.WriteLine("Connection received " + socket.RemoteEndPoint.ToString());
            this.socket = socket;
            this.server = server;
            this.splitter = new Splitter();
            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveData, null);
            runnerThread = new Thread(DataSender);
            runnerThread.IsBackground = true;
            runnerThread.Start();

            intArray.Command = 1; // Event add
            intArray.DataType = 5; // Int
            intArray.Parameter1 = 1;
        }

        private void DataSender()
        {
            Stopwatch sw = new Stopwatch();
            while (!needToStop)
            {
                sw.Restart();
                List<ChannelSubscription> allSubs;
                lock (subscriptions) // Make a local copy
                    allSubs = subscriptions.ToList();
                foreach (var subs in allSubs)
                {
                    if (subs.ChannelId >= 10000) // Int array
                    {
                        intArray.Parameter2 = subs.ClientId;
                        intArray.DataCount = (subs.DataCount == 0 ? (uint)Program.ArraySize : subs.DataCount);
                        //Send(intArray.Data, intArray.DataCount * 4 + 16);
                        Send(intArray);
                    }
                }
                sw.Stop();
                var toWait = 100 - (int)sw.ElapsedMilliseconds;
                if (toWait > 0)
                    Thread.Sleep(toWait);
            }
        }

        public void Dispose()
        {
            //Console.WriteLine("Dispose " + socket.RemoteEndPoint.ToString());
            needToStop = true;
            server.RemoveConnection(this);
            try
            {
                socket.Disconnect(false);
                socket.Dispose();
            }
            catch
            {
            }
        }

        private void ReceiveData(IAsyncResult ar)
        {
            int n = 0;
            try
            {
                n = socket.EndReceive(ar);
                if (n == 0) // End of the socket
                {
                    Dispose();
                }
            }
            catch
            {
                Dispose();
            }

            var newPacket = DataPacket.Create(buffer, n);

            foreach (var p in splitter.Split(newPacket))
            {
                switch (p.Command)
                {
                    case (ushort)EpicsCommand.CREATE_CHANNEL: // Connect to a channel
                        string channelName = p.GetDataAsString();
                        var id = int.Parse(channelName.Split(new char[] { ':' })[1]);

                        DataPacket resPacket = DataPacket.Create(0);
                        resPacket.Command = 22;
                        resPacket.DataType = 0;
                        resPacket.DataCount = 0;
                        resPacket.Parameter1 = p.Parameter1;
                        resPacket.Parameter2 = (uint)1; // Read only
                        resPacket.Destination = p.Sender;
                        Send(resPacket);

                        resPacket = DataPacket.Create(0);
                        resPacket.Command = 18;
                        resPacket.Destination = p.Sender;
                        resPacket.DataType = 5; // Int
                        resPacket.DataCount = (uint)Program.ArraySize;
                        resPacket.Parameter1 = p.Parameter1;
                        resPacket.Parameter2 = (uint)(10000 + id);
                        Send(resPacket);
                        break;
                    case (ushort)EpicsCommand.EVENT_ADD: // Subscribe to a channel
                        lock (subscriptions)
                        {
                            subscriptions.Add(new ChannelSubscription
                            {
                                ChannelId = p.Parameter1,
                                ClientId = p.Parameter2,
                                DataCount = p.DataCount
                            });
                        }
                        break;
                    case (ushort)EpicsCommand.EVENT_CANCEL:
                        lock (subscriptions)
                        {
                            subscriptions.RemoveAll(row => row.ClientId == p.Parameter2);

                            resPacket = DataPacket.Create(0);
                            newPacket.Command = 1;
                            newPacket.Destination = p.Sender;
                            newPacket.DataType = 5; // int
                            newPacket.DataCount = 0;
                            newPacket.Parameter1 = p.Parameter1;
                            newPacket.Parameter2 = p.Parameter2;
                            Send(newPacket);
                        }
                        break;
                    default:
                        break;
                }
            }

            try
            {
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveData, null);
            }
            catch
            {
            }
        }

        private void Send(DataPacket packet)
        {
            try
            {
                //this.socket.Send(packet.Data, SocketFlags.None);
                if(packet.BufferSize > 40 && packet.BufferSize != 8208)
                    Console.WriteLine("Wrong size");
                socket.Send(packet.Data, packet.Offset, packet.BufferSize, SocketFlags.None);
            }
            catch
            {
                Dispose();
            }
        }
        private void Send(byte[] data, uint nb)
        {
            try
            {
                this.socket.Send(data, (int)nb, SocketFlags.None);
            }
            catch
            {
                Dispose();
            }
        }

    }
}
