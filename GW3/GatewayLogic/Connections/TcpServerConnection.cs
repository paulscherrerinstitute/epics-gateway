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
        public Gateway Gateway { get; private set; }

        Socket socket;
        object lockObject = new object();
        bool isConnected = false;
        List<Action> toCallWhenReady = new List<Action>();
        readonly byte[] buffer = new byte[Gateway.BUFFER_SIZE];
        Splitter splitter = new Splitter();
        object disposedLock = new object();
        bool disposed = false;

        readonly List<ChannelInformation.ChannelInformationDetails> channels = new List<ChannelInformation.ChannelInformationDetails>();

        public TcpServerConnection(Gateway gateway, IPEndPoint destination) : base(gateway)
        {
            gateway.DiagnosticServer.NbTcpCreated++;

            RemoteEndPoint = destination;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
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
                    lock (lockObject)
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
                            Dispose();
                        }
                    }
                }, null);
            Gateway = gateway;
            ThreadPool.QueueUserWorkItem((obj) =>
            {
                if (!evt.WaitOne(5000))
                {
                    Gateway.Log.Write(LogLevel.Error, "Cannot connect to " + destination.ToString());
                    this.Dispose();
                }
                evt.Dispose();
            });
        }

        private void ConnectionBuilt(IAsyncResult ar)
        {
            lock (lockObject)
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
                    Dispose();
                }
            }
        }

        internal void WhenConnected(Action whenDone)
        {
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
            catch (Exception ex)
            {
                //Gateway.Log.Write(Services.LogLevel.Error, ex.ToString());
                this.Dispose();
                // Stop receiving
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
            catch (SocketException ex1)
            {
                this.Dispose();
            }
            catch (ObjectDisposedException ex2)
            {
                this.Dispose();
            }
            catch (Exception ex)
            {
                Gateway.Log.Write(Services.LogLevel.Critical, "Exception: " + ex);
                this.Dispose();
            }
        }

        public void LinkChannel(ChannelInformation.ChannelInformationDetails channel)
        {
            lock (channels)
            {
                channels.Add(channel);
            }
        }

        public List<string> Channels
        {
            get
            {
                lock (channels)
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
                lock (socket)
                {
                    socket.Send(packet.Data, packet.Offset, packet.BufferSize, SocketFlags.None);
                }
                //Console.WriteLine("Sending data to server: " + packet.Command + " size " + packet.BufferSize);
            }
            catch (Exception ex)
            {
                ThreadPool.QueueUserWorkItem((obj) => { this.Dispose(); });
            }
            //}
        }

        public override void Dispose()
        {
            lock (disposedLock)
            {
                if (disposed)
                    return;
                disposed = true;
            }
            socket?.Dispose();
            if (Gateway == null)
                return;
            Gateway.Log.Write(LogLevel.Connection, "Server " + this.Name + " disconnect");

            Gateway.ServerConnection.Remove(this);
            Gateway.GotDropedIoc(Name);

            List<ChannelInformation.ChannelInformationDetails> channelsCopy;
            lock (channels)
            {
                channelsCopy = channels.ToList();
            }
            var newPacket = DataPacket.Create(0);
            newPacket.Command = 27;
            foreach (var channel in channelsCopy)
            {
                Gateway.ChannelInformation.Remove(Gateway, channel);
                //Gateway.SearchInformation.Remove(channel.ChannelName);
                //Gateway.MonitorInformation.Drop(channel.GatewayId);
            }

            foreach (var channel in channelsCopy)
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

        public override string Name => RemoteEndPoint.ToString();

        public uint Version { get; internal set; }
    }
}
