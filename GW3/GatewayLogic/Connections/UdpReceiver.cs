using GatewayLogic.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GatewayLogic.Connections
{
    internal class UdpReceiver : GatewayConnection
    {
        private const int SioUdpConnReset = -1744830452;
        private readonly IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        private readonly Splitter splitter;
        private bool disposed = false;
        private Socket receiver;
        private readonly byte[] buff = new byte[Gateway.BUFFER_SIZE];
        private SafeLock lockBuffer = new SafeLock();
        private Dictionary<IPEndPoint, MemoryStream> normalSendBuffer = new Dictionary<IPEndPoint, MemoryStream>();
        private Dictionary<IPEndPoint, MemoryStream> reverseSendBuffer = new Dictionary<IPEndPoint, MemoryStream>();
        private Thread flusher;

        public IPEndPoint EndPoint { get; }

        // Found on http://stackoverflow.com/questions/5199026/c-sharp-async-udp-listener-socketexception
        // Allows to reset the socket in case of malformed UDP packet.

        public UdpReceiver(Gateway gateway, IPEndPoint endPoint) : base(gateway)
        {
            splitter = new Splitter();

            receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            receiver.EnableBroadcast = true;
            receiver.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                receiver.IOControl(SioUdpConnReset, new byte[] { 0, 0, 0, 0 }, null);
            receiver.Bind(endPoint);
            this.EndPoint = endPoint;

            if (endPoint == gateway.Configuration.SideAEndPoint)
                this.Destinations = gateway.Configuration.remoteBEndPoints;
            else
                this.Destinations = gateway.Configuration.remoteAEndPoints;

            EndPoint tempRemoteEp = sender;
            receiver.BeginReceiveFrom(buff, 0, buff.Length, SocketFlags.None, ref tempRemoteEp, GotUdpMessage, tempRemoteEp);

            flusher = new Thread(() =>
              {
                  while (!disposed)
                  {
                      FlushBufferSend();
                      Thread.Sleep(50);
                  }
              });
            flusher.Start();
        }

        public override void Dispose(LogMessageType commandReason, string message = null)
        {
            disposed = true;
            receiver.Dispose();
            splitter.Dispose();
        }

        public override void Send(DataPacket packet)
        {
            //lock (normalSendBuffer)
            using (var lck = lockBuffer.Aquire())
            {
                var sendBuffer = (packet.ReverseAnswer ? reverseSendBuffer : normalSendBuffer);

                if (!sendBuffer.ContainsKey(packet.Destination))
                    sendBuffer.Add(packet.Destination, new MemoryStream());

                var stream = sendBuffer[packet.Destination];
                if (stream.Position + packet.BufferSize > Gateway.MAX_UDP_PACKET_SIZE && stream.Position > 0)
                {
                    var bytes = stream.ToArray();
                    stream.Position = 0;
                    stream.SetLength(0); // Reset

                    try
                    {
                        if (sendBuffer == reverseSendBuffer)
                        {
                            if (this == Gateway.udpSideA)
                                Gateway.udpSideA.receiver.SendTo(bytes, bytes.Length, SocketFlags.None, packet.Destination);
                            else
                                Gateway.udpSideB.receiver.SendTo(bytes, bytes.Length, SocketFlags.None, packet.Destination);
                        }
                        else
                        {
                            if (this == Gateway.udpSideA)
                                Gateway.udpSideB.receiver.SendTo(bytes, bytes.Length, SocketFlags.None, packet.Destination);
                            else
                                Gateway.udpSideA.receiver.SendTo(bytes, bytes.Length, SocketFlags.None, packet.Destination);
                        }
                    }
                    catch
                    {
                    }
                }
                stream.Write(packet.Data, 0, packet.BufferSize);
            }
        }

        private void FlushBufferSend()
        {
            //lock (normalSendBuffer)
            using (var lck = lockBuffer.Aquire())
            {
                foreach (var i in normalSendBuffer)
                {
                    if (i.Value.Position == 0)
                        continue;

                    var bytes = i.Value.ToArray();
                    i.Value.Position = 0;
                    i.Value.SetLength(0); // Reset

                    try
                    {
                        if (this == Gateway.udpSideA)
                            Gateway.udpSideB.receiver.SendTo(bytes, bytes.Length, SocketFlags.None, i.Key);
                        else
                            Gateway.udpSideA.receiver.SendTo(bytes, bytes.Length, SocketFlags.None, i.Key);
                    }
                    catch
                    {
                    }
                }

                foreach (var i in reverseSendBuffer)
                {
                    if (i.Value.Position == 0)
                        continue;

                    var bytes = i.Value.ToArray();
                    i.Value.Position = 0;
                    i.Value.SetLength(0); // Reset

                    try
                    {
                        if (this == Gateway.udpSideA)
                            Gateway.udpSideA.receiver.SendTo(bytes, bytes.Length, SocketFlags.None, i.Key);
                        else
                            Gateway.udpSideB.receiver.SendTo(bytes, bytes.Length, SocketFlags.None, i.Key);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void GotUdpMessage(IAsyncResult ar)
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
                Gateway.Log.Write(Services.LogLevel.Detail, "Receiving: " + epSender.ToString());
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
