using EpicsSharp.ChannelAccess.Client;
using GatewayLogic.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Connections
{
    public enum DebugDataType : int
    {
        FULL_IOC,
        IOC_NEW_CHANNEL,
        DROP_IOC,
        CLIENT_NEW_CHANNEL,
        DROP_CLIENT,
        FULL_CLIENT,
        LOG,
        GW_NAME,
        FULL_LOGS,
        CRITICAL_LOGS,
        SEARCH_STATS,
        SEARCHERS_STATS
    }

    class DebugPortConnection : GatewayTcpConnection
    {
        readonly Socket socket;
        readonly BufferedStream sendStream;
        bool firstMessage = true;
        Socket forwarder = null;
        bool firstForward = true;

        readonly byte[] buffer = new byte[Gateway.BUFFER_SIZE];
        private bool disposed = false;

        public DebugPortConnection(Gateway gateway, Socket socket, IPEndPoint client, IPEndPoint server, DataPacket packet) : base(gateway)
        {
            this.socket = socket;
            string destGateway = GetString(packet, 1);

            // We need to act as a forwarder
            if (destGateway != gateway.Configuration.GatewayName)
            {
                System.Threading.ThreadPool.QueueUserWorkItem((obj) =>
                {
                    using (var caClient = new CAClient())
                    {
                        caClient.Configuration.SearchAddress = Gateway.Configuration.RemoteSideA + ";" + Gateway.Configuration.RemoteSideB;
                        using (var destChan = caClient.CreateChannel<string>(destGateway + ":VERSION"))
                        {
                            try
                            {
                                destChan.Connect();

                                forwarder = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                forwarder.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                                forwarder.Connect(Configuration.Configuration.ParseAddress(destChan.IOC));
                                forwarder.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);
                                forwarder.Send(packet.Data, 0, packet.BufferSize, SocketFlags.None);
                            }
                            catch
                            {
                                System.Threading.ThreadPool.QueueUserWorkItem(action => this.Dispose());
                            }
                        }
                    }
                });
                return;
            }

            sendStream = new BufferedStream(new NetworkStream(socket));
            try
            {
                lock (sendStream)
                {
                    Send((int)DebugDataType.GW_NAME);
                    Send(gateway.Configuration.GatewayName);

                    Send((int)DebugDataType.FULL_IOC);
                    var iocs = gateway.KnownIocs;
                    Send(iocs.Count);
                    foreach (var i in iocs)
                    {
                        Send(i.Key);
                        Send(i.Value.Count);
                        foreach (var j in i.Value)
                        {
                            Send(j);
                        }
                    }
                    Flush();

                    Send((int)DebugDataType.FULL_CLIENT);
                    var clients = gateway.KnownClients;
                    Send(clients.Count);
                    foreach (var i in clients)
                    {
                        Send(i.Key);
                        Send(i.Value.Count);
                        foreach (var j in i.Value)
                        {
                            Send(j);
                        }
                    }
                    Flush();

                    /*if (PBCaGw.Services.DebugTraceListener.TraceAll)
                        Send((int)DebugDataType.FULL_LOGS);
                    else
                        Send((int)DebugDataType.CRITICAL_LOGS);*/
                    Flush();

                    /*PBCaGw.Services.LogEntry[] logs = PBCaGw.Services.DebugTraceListener.LastEntries;
                    foreach (var i in logs)
                    {
                        Send((int)DebugDataType.LOG);
                        Send(i.Source);
                        Send((int)i.EventType);
                        Send(i.Id);
                        Send(i.Message);
                    }*/
                    Flush();
                }
                SendSearch(null, null);
            }
            catch
            {
                this.Dispose();
            }

            gateway.NewIocChannel += new NewIocChannelDelegate(GatewayNewIocChannel);
            gateway.DropedIoc += new DropIocDelegate(GatewayDropIoc);
            gateway.NewClientChannel += new NewClientChannelDelegate(GatewayNewClientChannel);
            gateway.DropedClient += new DropClientDelegate(GatewayDropClient);
            gateway.UpdateSearch += SendSearch;

            //gateway.Log.Handler += GatewayLogEntry;
        }

        void ReceiveTcpData(System.IAsyncResult ar)
        {
            if (disposed)
                return;

            int n = 0;

            //Log.TraceEvent(TraceEventType.Information, Chain.ChainId, "Got TCP");

            try
            {
                SocketError err;
                n = forwarder.EndReceive(ar, out err);
                switch (err)
                {
                    case SocketError.Success:
                        break;
                    case SocketError.ConnectionReset:
                        Dispose();
                        return;
                    default:
                        Dispose();
                        return;
                }
            }
            catch (System.ObjectDisposedException)
            {
                Dispose();
                return;
            }
            catch (System.Exception)
            {
                Dispose();
                return;
            }

            // Time to quit!
            if (n == 0)
            {
                Dispose();
                return;
            }

            try
            {
                //this.Chain.LastMessage = Gateway.Now;
                if (firstForward)
                    firstForward = false;
                else if (n > 0)
                    socket.Send(buffer, n, SocketFlags.None);
                forwarder.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);
            }
            catch (SocketException)
            {
                Dispose();
            }
            catch (System.ObjectDisposedException)
            {
                Dispose();
            }
            catch
            {
                Dispose();
            }
        }


        void SendSearch(object sender, System.EventArgs e)
        {
            try
            {
                lock (sendStream)
                {
                    var searches = Gateway.Searches;
                    Send((int)DebugDataType.SEARCH_STATS);
                    Send(searches.Count);
                    foreach (var i in searches.OrderByDescending(row => row.Value))
                    {
                        Send(i.Key);
                        Send(i.Value);
                    }

                    var searchers = Gateway.Searchers;
                    Send((int)DebugDataType.SEARCHERS_STATS);
                    Send(searchers.Count);
                    foreach (var i in searchers.OrderByDescending(row => row.Value))
                    {
                        Send(i.Key);
                        Send(i.Value);
                    }

                }
                Flush();
            }
            catch
            {
                this.Dispose();
            }
        }

        void DebugTraceListenerTraceLevelChanged(object sender, System.EventArgs e)
        {
            try
            {
                lock (sendStream)
                {
                    /*if (PBCaGw.Services.DebugTraceListener.TraceAll)
                        Send((int)DebugDataType.FULL_LOGS);
                    else
                        Send((int)DebugDataType.CRITICAL_LOGS);*/
                    Flush();
                }
            }
            catch
            {
                this.Dispose();
            }
        }

        void GatewayLogEntry(LogLevel level, string source, string message)
        {
            try
            {
                lock (sendStream)
                {
                    Send((int)DebugDataType.LOG);
                    Send(source);
                    Send((int)level);
                    Send(0);
                    Send(message);
                    Flush();
                }
            }
            catch
            {
                this.Dispose();
            }
        }

        void GatewayDropClient(string client)
        {
            try
            {
                lock (sendStream)
                {
                    Send((int)DebugDataType.DROP_CLIENT);
                    Send(client);
                    Flush();
                }
            }
            catch
            {
                this.Dispose();
            }
        }

        void GatewayNewClientChannel(string client, string channel)
        {
            try
            {
                lock (sendStream)
                {
                    Send((int)DebugDataType.CLIENT_NEW_CHANNEL);
                    Send(client);
                    Send(channel);
                    Flush();
                }
            }
            catch
            {
                this.Dispose();
            }
        }

        void Flush()
        {
            sendStream.Flush();
        }

        void GatewayDropIoc(string ioc)
        {
            try
            {
                lock (sendStream)
                {
                    Send((int)DebugDataType.DROP_IOC);
                    Send(ioc);
                    Flush();
                }
            }
            catch
            {
                this.Dispose();
            }
        }

        void GatewayNewIocChannel(string ioc, string channel)
        {
            try
            {
                lock (sendStream)
                {
                    Send((int)DebugDataType.IOC_NEW_CHANNEL);
                    Send(ioc);
                    Send(channel);
                    Flush();
                }
            }
            catch
            {
                this.Dispose();
            }
        }

        void Send(int data)
        {
            byte[] buff = new byte[4];
            buff[0] = (byte)((data & 0xFF000000u) >> 24);
            buff[1] = (byte)((data & 0x00FF0000u) >> 16);
            buff[2] = (byte)((data & 0x0000FF00u) >> 8);
            buff[3] = (byte)(data & 0x000000FFu);
            Send(buff);
        }

        void Send(string data)
        {
            byte[] buff = System.Text.Encoding.UTF8.GetBytes(data);
            Send(buff.Length);
            Send(buff);
        }

        void Send(byte[] data)
        {
            sendStream.Write(data, 0, data.Length);
        }

        public int GetInt(DataPacket packet, int pos)
        {
            return (int)packet.GetUInt32(pos);
        }

        public string GetString(DataPacket packet, int pos)
        {
            int lenght = GetInt(packet, pos);
            byte[] data = new byte[lenght];
            System.Array.Copy(packet.Data, pos + 4, data, 0, lenght);
            return System.Text.Encoding.UTF8.GetString(data);
        }

        public void ProcessData(DataPacket packet)
        {
            int pos = 0;
            if (firstMessage)
            {
                firstMessage = false;
            }
            else if (forwarder != null)
                forwarder.Send(packet.Data, 0, packet.BufferSize, SocketFlags.None);
            else switch ((DebugDataType)packet.GetUInt32(pos))
                {
                    case DebugDataType.FULL_LOGS:
                        break;
                    case DebugDataType.CRITICAL_LOGS:
                        break;
                }
        }

        public override void Dispose()
        {
            disposed = true;
            try
            {
                sendStream.Dispose();
            }
            catch
            {
            }

            if (forwarder != null)
            {
                try
                {
                    forwarder.Dispose();
                }
                catch
                {
                }
            }

            Gateway.ClientConnection.Remove(this);
            Gateway.NewIocChannel -= new NewIocChannelDelegate(GatewayNewIocChannel);
            Gateway.DropedIoc -= new DropIocDelegate(GatewayDropIoc);
            Gateway.NewClientChannel -= new NewClientChannelDelegate(GatewayNewClientChannel);
            Gateway.DropedClient -= new DropClientDelegate(GatewayDropClient);
            Gateway.UpdateSearch -= SendSearch;

            //Gateway.Log.Handler -= GatewayLogEntry;
        }

        public override void Send(DataPacket packet)
        {
        }

        public new DateTime LastMessage { get { return DateTime.UtcNow; } set { } }

        public override string Name => null;
    }
}
