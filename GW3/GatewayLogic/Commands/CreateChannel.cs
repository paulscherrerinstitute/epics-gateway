using GatewayLogic.Connections;
using GatewayLogic.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayLogic.Commands
{
    class CreateChannel : CommandHandler
    {
        SafeLock locker = new SafeLock();

        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            string channelName = packet.GetDataAsString();

            Configuration.SecurityAccess access;
            if (((TcpClientConnection)connection).Listener == connection.Gateway.tcpSideA)
                access = connection.Gateway.Configuration.Security.EvaluateSideA(channelName, "", "", packet.Sender.Address.ToString());
            else
                access = connection.Gateway.Configuration.Security.EvaluateSideB(channelName, "", "", packet.Sender.Address.ToString());
            if (access == Configuration.SecurityAccess.WRITE)
                access = Configuration.SecurityAccess.ALL;

            // Rules prevent searching
            if (access == Configuration.SecurityAccess.NONE)
                return;

            if (!connection.Gateway.ChannelInformation.HasChannelInformation(channelName) && !connection.Gateway.SearchInformation.HasChannelServerInformation(channelName))
            {
                connection.Gateway.SearchInformation.Remove(channelName);
                connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.ChannelUnknown, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channelName } });
                connection.Dispose(Services.LogMessageType.ChannelUnknown);
                return;
            }
            connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.CreateChannel, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channelName }, new LogMessageDetail { TypeId = MessageDetail.CID, Value = packet.Parameter1.ToString() } });
            var locked = true;

            System.Threading.Interlocked.Increment(ref connection.Gateway.DiagnosticServer.NbCreateChannel);
            try
            {
                locker.Wait();
                var searchInfo = connection.Gateway.SearchInformation.Get(channelName);
                var channelInfo = connection.Gateway.ChannelInformation.Get(channelName, searchInfo);
                channelInfo.RegisterClient(packet.Parameter1, (TcpClientConnection)connection);
                connection.Gateway.GotNewClientChannel(packet.Sender.ToString(), channelName);

                if (channelInfo.ServerId.HasValue && searchInfo.Server != null)
                {
                    locked = false;
                    locker.Release();
                    connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.CreateChannelInfoKnown, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channelName }, new LogMessageDetail { TypeId = MessageDetail.SID, Value = channelInfo.ServerId.ToString() } });
                    //connection.Gateway.Log.Write(Services.LogLevel.Detail, "Create channel info is known (" + channelName + " => " + channelInfo.ServerId + ").");
                    DataPacket resPacket = DataPacket.Create(0);
                    resPacket.Command = 22;
                    resPacket.DataType = 0;
                    resPacket.DataCount = 0;
                    resPacket.Parameter1 = packet.Parameter1;
                    resPacket.Parameter2 = (uint)access;
                    resPacket.Destination = packet.Sender;
                    connection.Send(resPacket);

                    //resPacket = (DataPacket)packet.Clone();
                    resPacket = DataPacket.Create(0);
                    resPacket.Command = 18;
                    resPacket.Destination = packet.Sender;
                    resPacket.DataType = channelInfo.DataType;
                    resPacket.DataCount = channelInfo.DataCount;
                    resPacket.Parameter1 = packet.Parameter1;
                    resPacket.Parameter2 = channelInfo.GatewayId;
                    connection.Send(resPacket);
                }
                else
                {
                    connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.CreateChannelInfoRequired, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channelName } });
                    //connection.Gateway.Log.Write(Services.LogLevel.Detail, "Create channel for " + channelName + " info must be found.");
                    channelInfo.AddClient(new ClientId { Client = packet.Sender, Id = packet.Parameter1 });

                    if (!channelInfo.ConnectionIsBuilding)
                    {
                        connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.CreateChannelConnectionRequired, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channelName } });
                        channelInfo.ConnectionIsBuilding = true;
                        channelInfo.StartBuilding = DateTime.UtcNow;
                        locked = false;
                        locker.Release();
                        if (channelInfo.TcpConnection == null)
                        {
                            ThreadPool.QueueUserWorkItem((obj) =>
                            {
                                connection.Gateway.ServerConnection.CreateConnection(connection.Gateway, searchInfo.Server, (tcpConnection) =>
                                {
                                    connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.CreateChannelConnectionMade, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channelName } });
                                    channelInfo.TcpConnection = tcpConnection;
                                    tcpConnection.Version = searchInfo.Version;

                                    // Send version
                                    var newPacket = DataPacket.Create(0);
                                    newPacket.Command = 0;
                                    newPacket.PayloadSize = 0;
                                    newPacket.DataType = 1;
                                    newPacket.DataCount = Gateway.CA_PROTO_VERSION;
                                    newPacket.Parameter1 = 0;
                                    newPacket.Parameter2 = 0;
                                    channelInfo.TcpConnection.Send(newPacket);

                                    connection.Gateway.GotNewIocChannel(tcpConnection.Name, channelInfo.ChannelName);
                                    tcpConnection.LinkChannel(channelInfo);
                                    newPacket = (DataPacket)packet.Clone();

                                    // Old EPICS version
                                    newPacket.Parameter1 = channelInfo.GatewayId;
                                    newPacket.Parameter2 = Gateway.CA_PROTO_VERSION;
                                    newPacket.Destination = searchInfo.Server;
                                    channelInfo.TcpConnection.Send(newPacket);

                                    // Send Client Name
                                    newPacket = DataPacket.Create(Gateway.CLIENT_NAME.Length + DataPacket.Padding(Gateway.CLIENT_NAME.Length));
                                    newPacket.Command = 20;
                                    newPacket.DataType = 0;
                                    newPacket.DataCount = 0;
                                    newPacket.Parameter1 = 0;
                                    newPacket.Parameter2 = 0;
                                    newPacket.SetDataAsString(Gateway.CLIENT_NAME);
                                    channelInfo.TcpConnection.Send(newPacket);

                                    // Send Host Name
                                    newPacket = DataPacket.Create(connection.Gateway.Configuration.GatewayName.Length + DataPacket.Padding(connection.Gateway.Configuration.GatewayName.Length));
                                    newPacket.Command = 21;
                                    newPacket.DataType = 0;
                                    newPacket.DataCount = 0;
                                    newPacket.Parameter1 = 0;
                                    newPacket.Parameter2 = 0;
                                    newPacket.SetDataAsString(connection.Gateway.Configuration.GatewayName);
                                    channelInfo.TcpConnection.Send(newPacket);

                                });
                            });
                        }
                    }
                    else
                    {
                        locked = false;
                        locker.Release();
                    }
                }
            }
            finally
            {
                if (locked)
                    locker.Release();
            }
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
            var locked = true;
            try
            {
                locker.Wait();
                var channelInfo = connection.Gateway.ChannelInformation.Get(packet.Parameter1);
                if (channelInfo == null)
                {
                    locked = false;
                    locker.Release();
                    //connection.Dispose(Services.LogMessageType.CreateChannelAnswerForUnknown);
                    connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.CreateChannelAnswerForUnknown);
                    return;
                }

                connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.CreateChannelAnswer, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channelInfo?.ChannelName } });
                IEnumerable<ClientId> clients;

                channelInfo.DataCount = packet.DataCount;
                channelInfo.DataType = packet.DataType;

                channelInfo.ServerId = packet.Parameter2;
                clients = channelInfo.GetClients();
                channelInfo.ConnectionIsBuilding = false;
                locked = false;
                locker.Release();

                foreach (var client in clients)
                {
                    var destConn = connection.Gateway.ClientConnection.Get(client.Client);
                    if (destConn == null)
                        continue;
                    connection.Gateway.MessageLogger.Write(client.Client.ToString(), Services.LogMessageType.CreateChannelSendingAnswer, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channelInfo?.ChannelName }, new LogMessageDetail { TypeId = MessageDetail.GWID, Value = channelInfo.GatewayId.ToString() } });

                    Configuration.SecurityAccess access;
                    if (((TcpClientConnection)destConn).Listener == connection.Gateway.tcpSideA)
                        access = connection.Gateway.Configuration.Security.EvaluateSideA(channelInfo.ChannelName, "", "", ((TcpClientConnection)destConn).RemoteEndPoint.Address.ToString());
                    else
                        access = connection.Gateway.Configuration.Security.EvaluateSideB(channelInfo.ChannelName, "", "", ((TcpClientConnection)destConn).RemoteEndPoint.Address.ToString());

                    if (access == Configuration.SecurityAccess.WRITE)
                        access = Configuration.SecurityAccess.ALL;

                    // Rules prevent searching
                    if (access == Configuration.SecurityAccess.NONE)
                        return;

                    DataPacket resPacket = DataPacket.Create(0);
                    resPacket.Command = 22;
                    resPacket.DataType = 0;
                    resPacket.DataCount = 0;
                    resPacket.Parameter1 = client.Id;
                    resPacket.Parameter2 = (uint)access;
                    resPacket.Destination = client.Client;
                    destConn.Send(resPacket);

                    resPacket = DataPacket.Create(0);
                    resPacket.Command = 18;
                    resPacket.DataCount = packet.DataCount;
                    resPacket.DataType = packet.DataType;
                    resPacket.Destination = client.Client;
                    resPacket.Parameter1 = client.Id;
                    resPacket.Parameter2 = channelInfo.GatewayId;
                    destConn.Send(resPacket);
                }
            }
            finally
            {
                if (locked)
                    locker.Release();
            }
        }
    }
}
