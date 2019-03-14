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
        private DataPacket intArray = DataPacket.Create(Program.ArraySize * 4 + 200);
        private int step = 1;
        private SemaphoreSlim socketLock = new SemaphoreSlim(1);

        public ServerConnection(LoadServer server, Socket socket)
        {
            //Console.WriteLine("Connection received " + socket.RemoteEndPoint.ToString());
            this.socket = socket;
            this.server = server;
            this.splitter = new Splitter();
            try
            {
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveData, null);
            }
            catch (Exception ex)
            {
                ThreadPool.QueueUserWorkItem((obj) => { this.Dispose(); });
                Console.WriteLine("Server connection closed at start");
            }
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
                        var res = (DataPacket)intArray.Clone();
                        res.Command = (ushort)EpicsCommand.EVENT_ADD;
                        res.Parameter1 = 1;
                        res.Parameter2 = subs.ClientId;
                        res.DataCount = (subs.DataCount == 0 ? (uint)Program.ArraySize : subs.DataCount);
                        res.DataType = subs.DataType;

                        if (subs.DataType == 19)
                            res.SetDateTime(16 + 4, DateTime.Now);

                        var offset = 16 + (subs.DataType == 33 ? 80 : (subs.DataType == 19 ? 12 : 0));
                        res.Data[offset] = (byte)(step & 0xFF);
                        res.Data[offset + 1] = (byte)((step >> 8) | 0xFF);
                        Send(res, PaddedSize((int)(res.DataCount * 4 + offset)));

                        step = (step + 1) % 30000;
                    }
                    else
                    {

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
                //Console.WriteLine("Server Cmd: " + p.Command + ", " + p.MessageSize);
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
                    case (ushort)EpicsCommand.READ_NOTIFY: // CAGET
                        if (p.DataType != 5 && p.DataType != 33 && p.DataType != 19) // Wrong data type
                        {
                            Console.WriteLine("Wrong data type: " + p.DataType);
                            break;
                        }
                        //Console.WriteLine("Send get");
                        var res = (DataPacket)intArray.Clone();

                        res.Command = (ushort)EpicsCommand.READ_NOTIFY;
                        res.Parameter2 = p.Parameter2;
                        res.Parameter1 = p.Parameter1;
                        res.DataCount = p.DataCount;
                        res.DataType = p.DataType;
                        if (p.DataType == 19)
                            res.SetDateTime(16 + 4, DateTime.Now);

                        var offset = 16 + (p.DataType == 33 ? 80 : (p.DataType == 19 ? 12 : 0));
                        res.Data[offset] = (byte)(step & 0xFF);
                        res.Data[offset + 1] = (byte)((step >> 8) | 0xFF);
                        Send(res, PaddedSize((int)(intArray.DataCount * 4 + offset)));
                        step++;
                        break;
                    case (ushort)EpicsCommand.EVENT_ADD: // Subscribe to a channel
                        lock (subscriptions)
                        {
                            if (p.DataType != 5 && p.DataType != 33 && p.DataType != 19) // Wrong data type
                            {
                                Console.WriteLine("Wrong data type: " + p.DataType);
                                break;
                            }
                            //Console.WriteLine("New subscription...");
                            if (!subscriptions.Any(row => row.ChannelId == p.Parameter1))
                            {
                                subscriptions.Add(new ChannelSubscription
                                {
                                    ChannelId = p.Parameter1,
                                    ClientId = p.Parameter2,
                                    DataCount = p.DataCount,
                                    DataType = p.DataType
                                });
                            }
                        }
                        break;
                    case (ushort)EpicsCommand.EVENT_CANCEL:
                        lock (subscriptions)
                        {
                            subscriptions.RemoveAll(row => row.ClientId == p.Parameter2);

                            resPacket = DataPacket.Create(0);
                            resPacket.Command = 1;
                            resPacket.Destination = p.Sender;
                            resPacket.DataType = 5; // int
                            resPacket.DataCount = 0;
                            resPacket.Parameter1 = p.Parameter1;
                            resPacket.Parameter2 = p.Parameter2;
                            Send(resPacket);
                        }
                        break;
                    case (ushort)EpicsCommand.ECHO:
                        Send(p);
                        break;
                    case (ushort)EpicsCommand.HOST:
                        break;
                    case (ushort)EpicsCommand.USER:
                        break;
                    case (ushort)EpicsCommand.CLEAR_CHANNEL:
                        break;
                    default:
                        Console.WriteLine("Command " + p.Command);
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
                socket.Send(packet.Data, packet.Offset, packet.BufferSize, SocketFlags.None);
            }
            catch
            {
                Dispose();
            }
        }
        private void Send(DataPacket packet, int messageSize)
        {
            /*try
            {
                MessageVerifier.Verify(packet, false);
            }
            catch (Exception ex)
            {

            }*/

            try
            {
                socketLock.Wait();
                packet.MessageSize = (ushort)messageSize;
                socket.Send(packet.Data, packet.Offset, messageSize, SocketFlags.None);
            }
            catch
            {
                Dispose();
            }
            finally
            {
                socketLock.Release();
            }
        }

        private void Send(byte[] data, uint nb)
        {
            /*try
            {
                MessageVerifier.Verify(data, false);
            }
            catch(Exception ex)
            {

            }*/
            
            try
            {
                socketLock.Wait();
                this.socket.Send(data, (int)nb, SocketFlags.None);
            }
            catch
            {
                Dispose();
            }
            finally
            {
                socketLock.Release();
            }
        }

        private int PaddedSize(int size)
        {
            return (8 - (size % 8)) + size;
        }
    }
}
