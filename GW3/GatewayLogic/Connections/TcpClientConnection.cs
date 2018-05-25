using GatewayLogic.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayLogic.Connections
{
    /// <summary>
    /// Receives data from the TCP connection
    /// </summary>
    class TcpClientConnection : GatewayTcpConnection
    {
        readonly byte[] buffer = new byte[Gateway.BUFFER_SIZE];
        object disposedLock = new object();
        bool disposed = false;

        private Splitter splitter;

        public TcpClientListener Listener { get; }

        public TcpClientConnection(Gateway gateway, IPEndPoint endPoint, Socket socket, TcpClientListener listener) : base(gateway)
        {
            gateway.DiagnosticServer.NbTcpCreated++;

            splitter = new Splitter();
            this.Socket = socket;
            this.Listener = listener;
            gateway.MessageLogger.Write(socket.RemoteEndPoint.ToString(), LogMessageType.StartTcpClientConnection);
            //gateway.Log.Write(LogLevel.Connection, "Start TCP client connection on " + endPoint);
        }

        SafeLock socketLock = new SafeLock();
        Socket socket;
        /// <summary>
        /// Defines the socket linked on the receiver.
        /// Once defined, the code will start to monitor the TCP socket for incoming data.
        /// </summary>
        public Socket Socket
        {
            get
            {
                return socket;
            }
            set
            {
                socket = value;
                socket.SendTimeout = 3000;

                RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
                //netStream = new NetworkStream(socket);
                //netStream.WriteTimeout = 100;
                //stream = new BufferedStream(netStream);

                try
                {
                    socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);
                }
                catch (SocketException)
                {
                    this.Dispose();
                }
                catch (Exception ex)
                {
                    Gateway.MessageLogger.Write(RemoteEndPoint.ToString(), LogMessageType.Exception, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.Exception, Value = ex.ToString() } });
                    //Gateway.Log.Write(Services.LogLevel.Critical, "Exception: " + ex);
                    this.Dispose();
                }
            }
        }

        public override string Name => this.RemoteEndPoint.ToString();

        public override void Send(DataPacket packet)
        {
            Gateway.DiagnosticServer.NbNewData++;
            this.LastMessage = DateTime.UtcNow;

            try
            {
                using (socketLock.Aquire())
                {
                    socket.Send(packet.Data, packet.Offset, packet.BufferSize, SocketFlags.None);
                }
                /*lock (stream)
                {
                    stream.Write(packet.Data, packet.Offset, packet.BufferSize);
                    isDirty = true;
                    stream.Flush();
                }*/
            }
            catch
            {
                //Gateway.Log.Write(Services.LogLevel.Error, "Exception: " + ex);
                Dispose();
            }
        }

        internal void LinkChannel(ChannelInformation.ChannelInformationDetails channelInformationDetails)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Got data from the TCP connection
        /// </summary>
        /// <param name="ar"></param>
        void ReceiveTcpData(IAsyncResult ar)
        {
            if (disposed)
                return;

            int n = 0;

            //Log.TraceEvent(TraceEventType.Information, Chain.ChainId, "Got TCP");

            try
            {
                SocketError err;
                n = Socket.EndReceive(ar, out err);
                switch (err)
                {
                    case SocketError.Success:
                        break;
                    case SocketError.ConnectionReset:
                        Dispose();
                        return;
                    default:
                        /*if (Log.WillDisplay(System.Diagnostics.TraceEventType.Error))
                            Log.TraceEvent(System.Diagnostics.TraceEventType.Error, Chain.ChainId, err.ToString());*/
                        Dispose();
                        return;
                }
            }
            catch (ObjectDisposedException)
            {
                Dispose();
                return;
            }
            catch (Exception ex)
            {
                /*if (Log.WillDisplay(System.Diagnostics.TraceEventType.Error))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Error, Chain.ChainId, ex.Message);*/
                Gateway.MessageLogger.Write(RemoteEndPoint.ToString(), LogMessageType.Exception, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.Exception, Value = ex.ToString() } });
                //Gateway.Log.Write(Services.LogLevel.Critical, "Exception: " + ex);
                Dispose();
                return;
            }
            //Log.Write("Client received " + n + " bytes from " + this.RemoteEndPoint);

            // Time to quit!
            if (n == 0)
            {
                /*if (Log.WillDisplay(System.Diagnostics.TraceEventType.Verbose))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, Chain.ChainId, "Socket closed on the other side");*/
                Dispose();
                return;
            }

            this.LastMessage = DateTime.UtcNow;

            var mainPacket = DataPacket.Create(buffer, n);

            // We receive a debug port request.
            if (buffer[0] == 126)
            {
                Gateway.ClientConnection.Remove(this);
                Gateway.ClientConnection.Add(new DebugPortConnection(Gateway, this.Socket, this.RemoteEndPoint, (IPEndPoint)this.Socket.LocalEndPoint, mainPacket));
                return;
            }

            try
            {
                if (n > 0)
                {
                    foreach (var p in splitter.Split(mainPacket))
                    {
                        Gateway.DiagnosticServer.NbMessages++;

                        //Console.WriteLine("=> Packet size " + p.MessageSize + " (command " + p.Command + ")");
                        //Log.Write("Packet number " + (pi++) + " command " + p.Command);
                        if (!socket.Connected)
                            break;
                        p.Sender = (IPEndPoint)socket.RemoteEndPoint;
                        Commands.CommandHandler.ExecuteRequestHandler(p.Command, this, p);
                    }
                    //Console.WriteLine(" ==> End of packet");
                }
            }
            catch (Exception ex)
            {
                //Gateway.Log.Write(Services.LogLevel.Critical, "Exception: " + ex);
                Gateway.MessageLogger.Write(RemoteEndPoint.ToString(), LogMessageType.Exception, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.Exception, Value = ex.ToString() } });
                Dispose();
            }

            try
            {
                Socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);
            }
            catch (SocketException)
            {
                Dispose();
            }
            catch (ObjectDisposedException)
            {
                Dispose();
            }
            catch (Exception ex)
            {
                //Gateway.Log.Write(Services.LogLevel.Critical, "Exception: " + ex);
                Gateway.MessageLogger.Write(RemoteEndPoint.ToString(), LogMessageType.Exception, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.Exception, Value = ex.ToString() } });
                Dispose();
            }

            if (!socket.Connected)
                Dispose();
        }

        public override void Dispose()
        {
            using (socketLock.Aquire())
            {
                if (disposed)
                    return;
                disposed = true;
            }
            splitter.Dispose();
            //Gateway.Log.Write(LogLevel.Connection, "Client " + this.Name + " disconnect");
            Gateway.MessageLogger.Write(this.RemoteEndPoint.ToString(), LogMessageType.ClientDisconnect);

            this.Gateway.DropClient(this);
            Gateway.GotDropedClient(Name);

            IPEndPoint endPoint = null;
            try
            {
                endPoint = (IPEndPoint)socket.RemoteEndPoint;
            }
            catch (Exception ex)
            {
                Gateway.MessageLogger.Write(RemoteEndPoint.ToString(), LogMessageType.Exception, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.Exception, Value = ex.ToString() } });
                //Gateway.Log.Write(Services.LogLevel.Critical, "Exception: " + ex);
            }

            try
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Disconnect(false);
                Socket.Close();
            }
            catch
            {
            }
        }
    }
}
