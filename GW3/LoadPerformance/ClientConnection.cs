using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LoadPerformance
{
    internal class ClientConnection : IDisposable
    {
        private readonly LoadClient loadClient;
        private readonly IPEndPoint endPoint;
        private readonly List<int> channelIds = new List<int>();
        private readonly TcpClient tcpClient;
        private readonly byte[] receiveBuffer = new byte[10240];
        private readonly Splitter splitter = new Splitter();
        SemaphoreSlim socketLock = new SemaphoreSlim(1);

        public IEnumerable<int> Connected
        {
            get
            {
                lock (channelIds)
                {
                    return channelIds.ToList();
                }
            }
        }

        public ClientConnection(LoadClient loadClient, IPEndPoint endPoint)
        {
            this.loadClient = loadClient;
            this.endPoint = endPoint;

            tcpClient = new TcpClient();
            try
            {
                tcpClient.Connect(endPoint);
                tcpClient.Client.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, TcpReceive, null);
            }
            catch
            {
                ThreadPool.QueueUserWorkItem((obj) => { this.Dispose(); });
            }
        }

        public void ChannelConnect(int channelId)
        {
            lock (channelIds)
            {
                if (channelIds.Contains(channelId))
                    return;
                channelIds.Add(channelId);
            }

            // Just go to the create & add
            var channelName = "PERF-CHECK-IARR:" + channelId;
            var packet = DataPacket.Create(channelName.Length + 8 - channelName.Length % 8);
            packet.Command = 18; //CA_PROTO_CREATE_CHAN;
            packet.DataType = 0;
            packet.DataCount = 0;
            packet.Parameter1 = (uint)channelId; // CID
            packet.Parameter2 = Program.CA_PROTO_VERSION; // CA_MINOR_PROTOCOL_REVISION;
            packet.SetDataAsString(channelName);

            TcpSend(packet);
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

            var newPacket = DataPacket.Create(receiveBuffer, n, false);

            foreach (var p in splitter.Split(newPacket))
            {
                //Console.WriteLine("Client Cmd: " + p.Command + ", " + p.MessageSize);
                MessageVerifier.Verify(p, false);

                switch (p.Command)
                {
                    case (ushort)EpicsCommand.ACCESS_RIGHTS:
                        break;
                    case (ushort)EpicsCommand.CREATE_CHANNEL:

                        // Channel connected send EventAdd
                        var toSend = DataPacket.Create(16);
                        toSend.Command = 1;
                        toSend.DataType = 5;
                        toSend.DataCount = p.DataCount;
                        toSend.Parameter1 = p.Parameter2;
                        toSend.Parameter2 = p.Parameter1;
                        toSend.SetUInt16(12 + 16, (ushort)1);
                        TcpSend(toSend);
                        break;
                    case (ushort)EpicsCommand.EVENT_ADD:
                        if (p.DataType != 5)
                            throw new WrongDataTypeException("Expected type 5 received type " + p.DataType);
                        loadClient.Increment(p.MessageSize, p.PayloadSize);
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
                Dispose();
            }
        }

        private void TcpSend(DataPacket packet)
        {
            try
            {
                socketLock.Wait();
                tcpClient.Client.Send(packet.Data);
            }
            catch (Exception ex)
            {
                Dispose();
            }
            finally
            {
                socketLock.Release();
            }
        }

        public void Dispose()
        {
            try
            {
                tcpClient?.Dispose();
            }
            catch
            {
            }
            loadClient.Disconnect(this);
        }
    }
}
