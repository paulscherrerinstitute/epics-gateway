using GatewayLogic.Services;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GatewayLogic.Connections
{
    /// <summary>
    /// Receives data from the TCP connection
    /// </summary>
    internal class TcpClientConnection : GatewayTcpConnection
    {
        private readonly byte[] buffer = new byte[Gateway.BUFFER_SIZE];
        private object disposedLock = new object();
        private bool disposed = false;
        //private SafeLock socketLock = new SafeLock();
        private SemaphoreSlim socketLock = new SemaphoreSlim(1);

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

        private Socket socket;
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
                socket.SendBufferSize = 64 * 1024;

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
                    this.Dispose(LogMessageType.SocketErrorReceiving, ex.ToString());
                }
            }
        }

        public override string Name => this.RemoteEndPoint.ToString();

        public override void Send(DataPacket packet)
        {
            Interlocked.Increment(ref Gateway.DiagnosticServer.NbNewData);
            this.LastMessage = DateTime.UtcNow;

            /*try
            {
                MessageVerifier.Verify(packet.Data, false);
            }
            catch(Exception ex)
            {
                Dispose();
            }*/

            try
            {
                socketLock.Wait();
                //using (socketLock.Aquire())
                //{
                socket.Send(packet.Data, packet.Offset, packet.BufferSize, SocketFlags.None);
                //}
            }
            catch (Exception ex)
            {
                Dispose(LogMessageType.SocketErrorSending, ex.ToString());
            }
            finally
            {
                socketLock.Release();
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
        private void ReceiveTcpData(IAsyncResult ar)
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
                        Dispose(LogMessageType.SocketClosed);
                        return;
                    default:
                        /*if (Log.WillDisplay(System.Diagnostics.TraceEventType.Error))
                            Log.TraceEvent(System.Diagnostics.TraceEventType.Error, Chain.ChainId, err.ToString());*/
                        Dispose(LogMessageType.SocketErrorReceiving);
                        return;
                }
            }
            catch (ObjectDisposedException)
            {
                Dispose(LogMessageType.SocketDisposed);
                return;
            }
            catch (Exception ex)
            {
                /*if (Log.WillDisplay(System.Diagnostics.TraceEventType.Error))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Error, Chain.ChainId, ex.Message);*/
                Gateway.MessageLogger.Write(RemoteEndPoint.ToString(), LogMessageType.Exception, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.Exception, Value = ex.ToString() } });
                //Gateway.Log.Write(Services.LogLevel.Critical, "Exception: " + ex);
                Dispose(LogMessageType.Exception, ex.ToString());
                return;
            }
            //Log.Write("Client received " + n + " bytes from " + this.RemoteEndPoint);

            // Time to quit!
            if (n == 0)
            {
                /*if (Log.WillDisplay(System.Diagnostics.TraceEventType.Verbose))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, Chain.ChainId, "Socket closed on the other side");*/
                Dispose(LogMessageType.SocketClosed);
                return;
            }

            this.LastMessage = DateTime.UtcNow;

            var mainPacket = DataPacket.Create(buffer, n);

            /*// We receive a debug port request.
            if (buffer[0] == 126)
            {
                Gateway.ClientConnection.Remove(this);
                Gateway.ClientConnection.Add(new DebugPortConnection(Gateway, this.Socket, this.RemoteEndPoint, (IPEndPoint)this.Socket.LocalEndPoint, mainPacket));
                return;
            }*/

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
                Dispose(LogMessageType.Exception, ex.ToString());
            }

            try
            {
                Socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);
            }
            catch (SocketException)
            {
                Dispose(LogMessageType.SocketClosed);
            }
            catch (ObjectDisposedException)
            {
                Dispose(LogMessageType.SocketDisposed);
            }
            catch (Exception ex)
            {
                //Gateway.Log.Write(Services.LogLevel.Critical, "Exception: " + ex);
                Gateway.MessageLogger.Write(RemoteEndPoint.ToString(), LogMessageType.Exception, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.Exception, Value = ex.ToString() } });
                Dispose(LogMessageType.SocketErrorReceiving, ex.ToString());
            }

            if (!socket.Connected)
                Dispose(LogMessageType.NotConnected);
        }

        ~TcpClientConnection()
        {
            socketLock.Dispose();
        }

        public override void Dispose(LogMessageType commandReason, string message = null)
        {
            if (disposed)
                return;
            disposed = true;

            splitter.Dispose();
            //Gateway.Log.Write(LogLevel.Connection, "Client " + this.Name + " disconnect");
            Gateway.MessageLogger.Write(this.RemoteEndPoint.ToString(), LogMessageType.ClientDisconnect, new LogMessageDetail[] {
                new LogMessageDetail { TypeId = MessageDetail.Reason, Value = commandReason.ToString() },
                new LogMessageDetail { TypeId = MessageDetail.Message, Value = message }
            });

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
