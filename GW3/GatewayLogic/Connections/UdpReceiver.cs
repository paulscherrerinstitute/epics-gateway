using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Connections
{
    class UdpReceiver : GatewayConnection
    {
        const int SioUdpConnReset = -1744830452;
        readonly IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        readonly Splitter splitter;

        Socket receiver;
        readonly byte[] buff = new byte[Gateway.BUFFER_SIZE];

        public IPEndPoint EndPoint { get; }

        // Found on http://stackoverflow.com/questions/5199026/c-sharp-async-udp-listener-socketexception
        // Allows to reset the socket in case of malformed UDP packet.

        public UdpReceiver(Gateway gateway, IPEndPoint endPoint) : base(gateway)
        {
            splitter = new Splitter();

            receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            receiver.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            receiver.IOControl(SioUdpConnReset, new byte[] { 0, 0, 0, 0 }, null);
            receiver.Bind(endPoint);
            this.EndPoint = endPoint;

            if (endPoint == gateway.Configuration.SideAEndPoint)
                this.Destinations = gateway.Configuration.remoteBEndPoints;
            else
                this.Destinations = gateway.Configuration.remoteAEndPoints;

            EndPoint tempRemoteEp = sender;
            receiver.BeginReceiveFrom(buff, 0, buff.Length, SocketFlags.None, ref tempRemoteEp, GotUdpMessage, tempRemoteEp);
        }

        public override void Dispose()
        {
            receiver.Dispose();
            splitter.Dispose();
        }

        public override void Send(DataPacket packet)
        {
            try
            {
                if (packet.ReverseAnswer)
                {
                    if (this == Gateway.udpSideA)
                        Gateway.udpSideA.receiver.SendTo(packet.Data, packet.BufferSize, SocketFlags.None, packet.Destination);
                    else
                        Gateway.udpSideB.receiver.SendTo(packet.Data, packet.BufferSize, SocketFlags.None, packet.Destination);
                }
                else
                {
                    if (this == Gateway.udpSideA)
                        Gateway.udpSideB.receiver.SendTo(packet.Data, packet.BufferSize, SocketFlags.None, packet.Destination);
                    else
                        Gateway.udpSideA.receiver.SendTo(packet.Data, packet.BufferSize, SocketFlags.None, packet.Destination);
                }
            }
            catch
            {
            }
        }

        void GotUdpMessage(IAsyncResult ar)
        {
            IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint epSender = ipeSender;
            int size = 0;

            try
            {
                size = receiver.EndReceiveFrom(ar, ref epSender);
            }
            catch (ObjectDisposedException)
            {
                // Stop receiving
                return;
            }
            catch (Exception ex)
            {
                Gateway.MessageLogger.Write(EndPoint.ToString(), Services.LogMessageType.Exception, new Services.LogMessageDetail[] { new Services.LogMessageDetail { TypeId = Services.MessageDetail.Exception, Value = ex.ToString() } });
            }

            DataPacket data = null;

            try
            {
                data = DataPacket.Create(buff, size, false);
                EndPoint tempRemoteEp = sender;
                receiver.BeginReceiveFrom(buff, 0, buff.Length, SocketFlags.None, ref tempRemoteEp, GotUdpMessage, tempRemoteEp);
            }
            catch (ObjectDisposedException)
            {
                // Stop receiving
                return;
            }
            catch (Exception ex)
            {
                Gateway.MessageLogger.Write(EndPoint.ToString(), Services.LogMessageType.Exception, new Services.LogMessageDetail[] { new Services.LogMessageDetail { TypeId = Services.MessageDetail.Exception, Value = ex.ToString() } });
            }

            try
            {
                //Gateway.Log.Write(Services.LogLevel.Detail, "Receiving: " + epSender.ToString());
                splitter.Reset();
                foreach (var p in splitter.Split(data))
                {
                    Gateway.DiagnosticServer.NbMessages++;
                    p.Sender = (IPEndPoint)epSender;
                    if (this is UdpResponseReceiver)
                        Commands.CommandHandler.ExecuteResponseHandler(p.Command, this, p);
                    else
                        Commands.CommandHandler.ExecuteRequestHandler(p.Command, this, p);
                }
            }
            catch (Exception ex)
            {
                Gateway.MessageLogger.Write(EndPoint.ToString(), Services.LogMessageType.Exception, new Services.LogMessageDetail[] { new Services.LogMessageDetail { TypeId = Services.MessageDetail.Exception, Value = ex.ToString() } });
            }
        }
    }
}
