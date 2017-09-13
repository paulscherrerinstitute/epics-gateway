using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic
{
    /// <summary>
    /// Receives data from the TCP connection
    /// </summary>
    class TcpClientConnection : GatewayConnection, IDisposable
    {
        readonly byte[] buffer = new byte[Gateway.BUFFER_SIZE];
        bool disposed = false;

        NetworkStream netStream;
        BufferedStream stream;
        bool isDirty = false;
        //AutoResetEvent dataSent = new AutoResetEvent(true);
        private Splitter splitter;

        public TcpClientConnection(Gateway gateway, IPEndPoint endPoint, Socket socket) : base(gateway)
        {
            splitter = new Splitter();
            RemoteEndPoint = endPoint;
            this.Socket = socket;
        }

        public IPEndPoint RemoteEndPoint
        {
            get;
            private set;
        }

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
                socket.SendTimeout = 30000;
                /*socket.SendTimeout = 500;
                socket.Blocking = false;*/

                RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;

                netStream = new NetworkStream(socket);
                netStream.WriteTimeout = 100;
                stream = new BufferedStream(netStream);

                try
                {
                    socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex);
                }
            }
        }

        public bool IsDirty { get { return isDirty; } }


        public override void Send(DataPacket packet)
        {
            lock (stream)
            {
                stream.Write(packet.Data, packet.Offset, packet.BufferSize);
                isDirty = true;
                stream.Flush();
            }
        }

        /*void Sent(IAsyncResult obj)
        {
            try
            {
                socket.EndSend(obj);
                dataSent.Set();
            }
            catch
            {
                Dispose();
            }
        }*/

        public void Flush()
        {
            lock (stream)
            {
                try
                {
                    stream.Flush();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex);
                }
                isDirty = false;
            }
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
                Console.WriteLine("Exception: " + ex);
                Dispose();
                return;
            }

            // Time to quit!
            if (n == 0)
            {
                /*if (Log.WillDisplay(System.Diagnostics.TraceEventType.Verbose))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, Chain.ChainId, "Socket closed on the other side");*/
                Dispose();
                return;
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
                Console.WriteLine("Exception: " + ex);
                Dispose();
            }

            try
            {
                if (n > 0)
                {
                    foreach (var p in splitter.Split(DataPacket.Create(buffer, n)))
                    {
                        p.Sender = (IPEndPoint)socket.RemoteEndPoint;
                        Commands.CommandHandler.ExecuteRequestHandler(p.Command, this, p);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
                Dispose();
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            IPEndPoint endPoint = null;
            try
            {
                endPoint = (IPEndPoint)socket.RemoteEndPoint;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
            }

            try
            {
                stream.Dispose();
            }
            catch
            {
            }

            try
            {
                netStream.Dispose();
            }
            catch
            {
            }


            try
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Disconnect(false);
                //Socket.Disconnect(true);
                Socket.Close();
            }
            catch
            {
            }
        }
    }
}
