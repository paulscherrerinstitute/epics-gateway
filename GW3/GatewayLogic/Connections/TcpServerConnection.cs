using GatewayLogic.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayLogic.Connections
{
    class TcpServerConnection : GatewayTcpConnection
    {
        //public Gateway Gateway { get; private set; }

        Socket socket;
        SafeLock lockObject = new SafeLock();
        SafeLock socketLock = new SafeLock();
        bool isConnected = false;
        List<Action> toCallWhenReady = new List<Action>();
        readonly byte[] buffer = new byte[Gateway.BUFFER_SIZE];
        Splitter splitter = new Splitter();
        bool disposed = false;

        SafeLock channelsLock = new SafeLock();
        readonly List<ChannelInformation.ChannelInformationDetails> channels = new List<ChannelInformation.ChannelInformationDetails>();

        public TcpServerConnection(Gateway gateway, IPEndPoint destination) : base(gateway)
        {
            gateway.MessageLogger.Write(destination.ToString(), Services.LogMessageType.StartTcpServerConnection);

            gateway.DiagnosticServer.NbTcpCreated++;

            RemoteEndPoint = destination;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            socket.SendTimeout = 3000;
            var evt = new AutoResetEvent(false);
            IAsyncResult result = socket.BeginConnect(destination, (IAsyncResult ar) =>
                {
                    try
                    {
                        evt.Set();
                    }
                    catch
                    {
                        return;
                    }
                    if (disposed)
                        return;
                    using (lockObject.Aquire())
                    {
                        isConnected = true;

                        foreach (var action in toCallWhenReady)
                            action();
                        toCallWhenReady.Clear();

                        try
                        {
                            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);
                        }
                        catch
                        {
                            //Gateway.Log.Write(Services.LogLevel.Error, "Exception: " + ex);
                            Dispose();
                        }
                    }
                }, null);
            ThreadPool.QueueUserWorkItem((obj) =>
            {
                if (!evt.WaitOne(5000))
                {
                    gateway.MessageLogger.Write(destination.ToString(), Services.LogMessageType.StartTcpServerConnectionFailed);
                    //Gateway.Log.Write(LogLevel.Error, "Cannot connect to " + destination.ToString());
                    this.Dispose();
                }
                evt.Dispose();
            });
        }

        private void ConnectionBuilt(IAsyncResult ar)
        {
            if (disposed)
                return;
            using (lockObject.Aquire())
            {
                isConnected = true;

                foreach (var action in toCallWhenReady)
                    action();
                toCallWhenReady.Clear();

                try
                {
                    socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);
                }
                catch
                {
                    //Gateway.Log.Write(Services.LogLevel.Error, "Exception: " + ex);
                    Dispose();
                }
            }
        }

        internal void WhenConnected(Action whenDone)
        {
            if (disposed)
                return;
            using (lockObject.Aquire())
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
            catch
            {
                //Gateway.Log.Write(Services.LogLevel.Error, ex.ToString());
                this.Dispose();
                // Stop receiving
                return;
            }
            //  End of the stream
            if (size == 0)
            {
                this.Dispose();
                return;
            }
            //Console.WriteLine("Server received " + size + " bytes from " + this.RemoteEndPoint);

            this.LastMessage = DateTime.UtcNow;
            var mainPacket = DataPacket.Create(buffer, size, false);

            foreach (var p in splitter.Split(mainPacket))
            {
                Gateway.DiagnosticServer.NbMessages++;

                //Console.WriteLine("+> Packet size " + p.MessageSize + " (command " + p.Command + ")");
                p.Sender = RemoteEndPoint;
                Commands.CommandHandler.ExecuteResponseHandler(p.Command, this, p);
                //Console.WriteLine(" ++> End of packet");
            }

            try
            {
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);
            }
            catch (SocketException)
            {
                this.Dispose();
            }
            catch (ObjectDisposedException)
            {
                this.Dispose();
            }
            catch (Exception ex)
            {
                //Gateway.Log.Write(Services.LogLevel.Critical, "Exception: " + ex);
                Gateway.MessageLogger.Write(RemoteEndPoint.ToString(), LogMessageType.Exception, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.Exception, Value = ex.ToString() } });
                this.Dispose();
            }
        }

        public void LinkChannel(ChannelInformation.ChannelInformationDetails channel)
        {
            using (channelsLock.Aquire())
            {
                channels.Add(channel);
            }
        }

        public List<string> Channels
        {
            get
            {
                using (channelsLock.Aquire())
                {
                    return channels.Select(row => row.ChannelName).ToList();
                }
            }
        }

        public override void Send(DataPacket packet)
        {
            Gateway.DiagnosticServer.NbNewData++;
            this.LastMessage = DateTime.UtcNow;

            /*lock (lockObject)
            {*/
            try
            {
                using (socketLock.Aquire(3000))
                {
                    socket.Send(packet.Data, packet.Offset, packet.BufferSize, SocketFlags.None);
                }
                //Console.WriteLine("Sending data to server: " + packet.Command + " size " + packet.BufferSize);
            }
            catch
            {
                ThreadPool.QueueUserWorkItem((obj) => { this.Dispose(); });
            }
            //}
        }

        ~TcpServerConnection()
        {
            lockObject.Dispose();
            socketLock.Dispose();
            channelsLock.Dispose();
        }

        public override void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            //lockObject.Dispose();
            //socketLock.Dispose();
            splitter.Dispose();
            socket?.Dispose();
            if (Gateway == null)
            {
                channelsLock.Dispose();
                return;
            }
            Gateway.MessageLogger.Write(this.RemoteEndPoint.ToString(), Services.LogMessageType.DiposeTcpServerConnection);
            //Gateway.Log.Write(LogLevel.Connection, "Server " + this.Name + " disconnect");

            Gateway.ServerConnection.Remove(this);
            Gateway.GotDropedIoc(Name);

            List<ChannelInformation.ChannelInformationDetails> channelsCopy;
            using (channelsLock.Aquire())
            {
                channelsCopy = channels.ToList();
            }
            var newPacket = DataPacket.Create(0);
            newPacket.Command = 27;

            foreach (var channel in channelsCopy)
            {
                Gateway.MessageLogger.Write(this.RemoteEndPoint.ToString(), Services.LogMessageType.ChannelDispose, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channel.ChannelName } });
                //Gateway.Log.Write(LogLevel.Detail, "Disposing channel " + channel.ChannelName);
                foreach (var client in channel.GetClientConnections())
                {
                    newPacket.Parameter1 = client.Id;
                    newPacket.Destination = client.Connection.RemoteEndPoint;
                    client.Connection.Send(newPacket);
                }
            }

            foreach (var channel in channelsCopy)
                Gateway.ChannelInformation.Remove(Gateway, channel);
        }

        public override string Name => RemoteEndPoint.ToString();

        public uint Version { get; internal set; }
    }
}
