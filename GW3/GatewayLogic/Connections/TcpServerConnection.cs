using GatewayLogic.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GatewayLogic.Connections
{
    internal class TcpServerConnection : GatewayTcpConnection
    {
        //public Gateway Gateway { get; private set; }

        private Socket socket;
        private SafeLock lockObject = new SafeLock();

        private SemaphoreSlim socketLock = new SemaphoreSlim(1);
        private bool isConnected = false;
        private List<Action> toCallWhenReady = new List<Action>();
        private readonly byte[] buffer = new byte[Gateway.BUFFER_SIZE];
        private Splitter splitter = new Splitter();
        private bool disposed = false;
        private SafeLock channelsLock = new SafeLock();
        private readonly List<ChannelInformation.ChannelInformationDetails> channels = new List<ChannelInformation.ChannelInformationDetails>();

        public TcpServerConnection(Gateway gateway, IPEndPoint destination) : base(gateway)
        {
            gateway.MessageLogger.Write(destination.ToString(), Services.LogMessageType.StartTcpServerConnection);

            gateway.DiagnosticServer.NbTcpCreated++;

            RemoteEndPoint = destination;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            socket.ReceiveBufferSize = Gateway.BUFFER_SIZE * 4;
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
                        catch (Exception ex)
                        {
                            //Gateway.Log.Write(Services.LogLevel.Error, "Exception: " + ex);
                            Dispose(LogMessageType.SocketCreationError, ex.ToString());
                        }
                    }
                }, null);
            ThreadPool.QueueUserWorkItem((obj) =>
            {
                if (!evt.WaitOne(5000))
                {
                    gateway.MessageLogger.Write(destination.ToString(), Services.LogMessageType.StartTcpServerConnectionFailed);
                    //Gateway.Log.Write(LogLevel.Error, "Cannot connect to " + destination.ToString());
                    this.Dispose(LogMessageType.SocketConnectionTimeout);
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
                catch (Exception ex)
                {
                    //Gateway.Log.Write(Services.LogLevel.Error, "Exception: " + ex);
                    Dispose(LogMessageType.SocketErrorReceiving, ex.ToString());
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
            catch (Exception ex)
            {
                this.Dispose(LogMessageType.SocketErrorReceiving, ex.ToString());
                // Stop receiving
                return;
            }
            //  End of the stream
            if (size == 0)
            {
                this.Dispose(LogMessageType.SocketClosed);
                return;
            }

            this.LastMessage = DateTime.UtcNow;
            //var mainPacket = DataPacket.Create(buffer, size, false);
            var mainPacket = DataPacket.Create(buffer, size, true);

            foreach (var p in splitter.Split(mainPacket))
            {
                Gateway.DiagnosticServer.NbMessages++;

                p.Sender = RemoteEndPoint;
                Commands.CommandHandler.ExecuteResponseHandler(p.Command, this, p);
            }

            try
            {
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);
            }
            catch (SocketException)
            {
                this.Dispose(LogMessageType.SocketClosed);
            }
            catch (ObjectDisposedException)
            {
                this.Dispose(LogMessageType.SocketDisposed);
            }
            catch (Exception ex)
            {
                //Gateway.Log.Write(Services.LogLevel.Critical, "Exception: " + ex);
                Gateway.MessageLogger.Write(RemoteEndPoint.ToString(), LogMessageType.Exception, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.Exception, Value = ex.ToString() } });
                this.Dispose(LogMessageType.SocketErrorReceiving, ex.ToString());
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

        public void RemoveChannel(ChannelInformation.ChannelInformationDetails channel)
        {
            using (channelsLock.Aquire())
            {
                channels.Remove(channel);
            }
        }

        public override void Send(DataPacket packet)
        {
            Gateway.DiagnosticServer.NbNewData++;
            this.LastMessage = DateTime.UtcNow;

            /*try
            {
                MessageVerifier.Verify(packet.Data, true);
            }
            catch (Exception ex)
            {
                ThreadPool.QueueUserWorkItem((obj) => { this.Dispose(); });
            }*/

            try
            {
                socketLock.Wait();
                //socket.Send(packet.Data, packet.Offset, packet.BufferSize, SocketFlags.None);
                socket.Send(packet.Data, packet.Offset, (int)packet.MessageSize, SocketFlags.None);
            }
            catch (Exception ex)
            {
                ThreadPool.QueueUserWorkItem((obj) => { this.Dispose(LogMessageType.SocketErrorSending, ex.ToString()); });
            }
            finally
            {
                socketLock.Release();
            }
        }

        ~TcpServerConnection()
        {
            lockObject.Dispose();
            socketLock.Dispose();
            channelsLock.Dispose();
        }

        public override bool IsDisposed => disposed;

        public override void Dispose(LogMessageType commandReason, string message = null)
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
            Gateway.MessageLogger.Write(this.RemoteEndPoint.ToString(), Services.LogMessageType.DiposeTcpServerConnection, new LogMessageDetail[]
            {
                new LogMessageDetail{ TypeId=MessageDetail.Reason, Value=commandReason.ToString()},
                new LogMessageDetail{ TypeId=MessageDetail.Message, Value=message}
            });
            //Gateway.Log.Write(LogLevel.Connection, "Server " + this.Name + " disconnect");

            Gateway.ServerConnection.Remove(this);
            Gateway.GotDropedIoc(Name);

            List<ChannelInformation.ChannelInformationDetails> channelsCopy;
            using (channelsLock.Aquire())
            {
                channelsCopy = channels.ToList();
            }

            Dictionary<ChannelInformation.ChannelInformationDetails, IEnumerable<Client>> clientsCopy = new Dictionary<ChannelInformation.ChannelInformationDetails, IEnumerable<Client>>();
            foreach (var channel in channelsCopy)
                clientsCopy.Add(channel, channel.GetClientConnections());

            foreach (var channel in channelsCopy)
                Gateway.ChannelInformation.Remove(Gateway, channel);

            var newPacket = DataPacket.Create(0);
            newPacket.Command = 27;

            foreach (var channel in channelsCopy)
            {
                Gateway.MessageLogger.Write(this.RemoteEndPoint.ToString(), Services.LogMessageType.ChannelDispose, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channel.ChannelName } });
                //Gateway.Log.Write(LogLevel.Detail, "Disposing channel " + channel.ChannelName);
                foreach (var client in clientsCopy[channel])
                {
                    newPacket.Parameter1 = client.Id;
                    newPacket.Destination = client.Connection.RemoteEndPoint;
                    client.Connection.Send(newPacket);
                }
            }
        }

        public override string Name => RemoteEndPoint.ToString();

        public uint Version { get; internal set; }
    }
}
