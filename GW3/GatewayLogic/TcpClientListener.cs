using GatewayLogic.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic
{
    /// <summary>
    /// Monitor a TCP port and creates a new worker chain for each incoming connection.
    /// </summary>
    public class TcpClientListener : IDisposable
    {
        TcpListener tcpListener = null;
        bool disposed = false;
        readonly IPEndPoint ipSource;
        //readonly ChainSide side = ChainSide.SIDE_A;
        readonly Gateway Gateway;

        public TcpClientListener(Gateway gateway, IPEndPoint ipSource)
        {
            this.Gateway = gateway;
            this.ipSource = ipSource;

            Rebuild();

            /*if (Log.WillDisplay(System.Diagnostics.TraceEventType.Start))
                Log.TraceEvent(System.Diagnostics.TraceEventType.Start, -1, "TCP Listener " + side.ToString() + " on " + ipSource);*/
        }

        void Rebuild()
        {
            if (disposed)
                return;
            if (tcpListener != null)
            {
                try
                {
                    tcpListener.Stop();
                }
                catch
                {
                }
                System.Threading.Thread.Sleep(100);
            }
            tcpListener = new TcpListener(ipSource);
            tcpListener.Start(10);
            tcpListener.BeginAcceptSocket(ReceiveConn, tcpListener);
        }

        void ReceiveConn(IAsyncResult result)
        {
            //DiagnosticServer.NbTcpCreated++;
            TcpListener listener = null;
            Socket client = null;

            try
            {
                listener = (TcpListener)result.AsyncState;
                client = listener.EndAcceptSocket(result);

                client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                //client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 0);
            }
            /*catch (ObjectDisposedException)
            {
                return;
            }*/
            catch (Exception ex)
            {
                /*if (Log.WillDisplay(System.Diagnostics.TraceEventType.Critical))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Critical, -1, "Error: " + ex.Message);*/

                try
                {
                    //Debug.Assert(listener != null, "listener != null");
                    listener.BeginAcceptSocket(new AsyncCallback(ReceiveConn), listener);
                }
                /*catch (ObjectDisposedException)
                {
                    return;
                }*/
                catch(ObjectDisposedException ex1)
                {
                    return;
                }
                catch(Exception ex2)
                {
                    Gateway.Log.Write("Exception: " + ex2);
                    if (!disposed)
                        Rebuild();
                }
                return;
            }

            if (disposed)
                return;

            if (client != null)
            {
                // Create the client chain and register the client in the Tcp Manager
                IPEndPoint clientEndPoint;
                //WorkerChain chain = null;
                try
                {
                    clientEndPoint = (IPEndPoint)client.RemoteEndPoint;

                    /*chain = WorkerChain.TcpChain(this.gateway, this.side, clientEndPoint, ipSource);
                    if (Log.WillDisplay(System.Diagnostics.TraceEventType.Start))
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Start, chain.ChainId, "New client connection: " + clientEndPoint);
                    TcpManager.RegisterClient(clientEndPoint, chain);*/
                    //TcpReceiver receiver = (TcpReceiver)chain[0];
                    var receiver = new TcpClientConnection(Gateway, ipSource, client);
                    Gateway.ClientConnection.Add(receiver);

                    // Send version
                    DataPacket packet = DataPacket.Create(16);
                    packet.Sender = ipSource;
                    packet.Destination = clientEndPoint;
                    packet.Command = 0;
                    packet.DataType = 1;
                    //CA_V411(MINOR)
                    packet.DataCount = 11;
                    //CA_V413 Allow zero length in requests
                    //packet.DataCount = 13;
                    packet.Parameter1 = 0;
                    packet.Parameter2 = 0;
                    packet.PayloadSize = 0;
                    receiver.Send(packet);
                }
                catch (Exception ex)
                {
                    //if (Log.WillDisplay(System.Diagnostics.TraceEventType.Verbose))
                    //    Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, -1, "Cannot get socket stream: " + ex.Message);

                    try
                    {
                        /*if (chain != null)
                            chain.Dispose();*/
                    }
                    catch
                    {
                    }
                }
            }

            // Wait for the next one
            try
            {
                //Debug.Assert(listener != null, "listener != null");
                listener.BeginAcceptSocket(new AsyncCallback(ReceiveConn), listener);
            }
            /*catch (ObjectDisposedException)
            {
                return;
            }*/
            catch (Exception ex)
            {
                //if (Log.WillDisplay(System.Diagnostics.TraceEventType.Critical))
                //    Log.TraceEvent(System.Diagnostics.TraceEventType.Critical, -1, "Error: " + ex.Message);
                Gateway.Log.Write("Exception: " + ex);
                if (!disposed)
                    Rebuild();
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            tcpListener.Server.Close();
        }
    }
}
