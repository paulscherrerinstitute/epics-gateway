﻿using GatewayLogic.Connections;
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
        SemaphoreSlim locker = new SemaphoreSlim(1);

        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            string channelName = packet.GetDataAsString();

            Configuration.SecurityAccess access;
            if (((TcpClientConnection)connection).Listener == connection.Gateway.tcpSideA)
                access = connection.Gateway.Configuration.Security.EvaluateSideA(channelName, "", "", packet.Sender.Address.ToString());
            else
                access = connection.Gateway.Configuration.Security.EvaluateSideB(channelName, "", "", packet.Sender.Address.ToString());

            // Rules prevent searching
            if (access == Configuration.SecurityAccess.NONE)
                return;

            if (!connection.Gateway.ChannelInformation.HasChannelInformation(channelName) && !connection.Gateway.SearchInformation.HasChannelServerInformation(channelName))
            {

                connection.Gateway.Log.Write(Services.LogLevel.Error, "Channel is not known: "+channelName);
                connection.Dispose();
                return;
            }
            connection.Gateway.Log.Write(Services.LogLevel.Detail, "Create channel for " + channelName + "  from " + ((TcpClientConnection)connection).RemoteEndPoint + " CID " + packet.Parameter1);
            var locked = true;
            try
            {
                locker.Wait();
                var searchInfo = connection.Gateway.SearchInformation.Get(channelName);
                var channelInfo = connection.Gateway.ChannelInformation.Get(channelName, searchInfo);
                channelInfo.RegisterClient(packet.Parameter1, (TcpClientConnection)connection);
                connection.Gateway.GotNewClientChannel(packet.Sender.ToString(), channelName);

                /*lock (channelInfo.LockObject)
                {*/
                // We have all the info, we shall answer
                if (channelInfo.ServerId.HasValue && searchInfo.Server != null)
                {
                    locked = false;
                    locker.Release();
                    connection.Gateway.Log.Write(Services.LogLevel.Detail, "Create channel info is known (" + channelName + " => " + channelInfo.ServerId + ").");
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
                    connection.Gateway.Log.Write(Services.LogLevel.Detail, "Create channel info must be found.");
                    channelInfo.AddClient(new ClientId { Client = packet.Sender, Id = packet.Parameter1 });

                    if (!channelInfo.ConnectionIsBuilding)
                    {
                        connection.Gateway.Log.Write(Services.LogLevel.Detail, "Connection must be made");
                        channelInfo.ConnectionIsBuilding = true;
                        locked = false;
                        locker.Release();
                        if (channelInfo.TcpConnection == null)
                        {
                            ThreadPool.QueueUserWorkItem((obj) =>
                            {
                                connection.Gateway.ServerConnection.CreateConnection(connection.Gateway, searchInfo.Server, (tcpConnection) =>
                                {
                                    channelInfo.TcpConnection = tcpConnection;
                                    connection.Gateway.GotNewIocChannel(tcpConnection.Name, channelInfo.ChannelName);
                                    tcpConnection.LinkChannel(channelInfo);
                                    var newPacket = (DataPacket)packet.Clone();
                                    newPacket.Parameter1 = channelInfo.GatewayId;
                                    newPacket.Parameter2 = Gateway.CA_PROTO_VERSION;
                                    //if(connection.Gateway.s)
                                    //newPacket.Sender
                                    newPacket.Destination = searchInfo.Server;
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
            //}
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
                    connection.Dispose();
                    return;
                }

                connection.Gateway.Log.Write(Services.LogLevel.Detail, "Answer for create channel " + channelInfo?.ChannelName);
                IEnumerable<ClientId> clients;

                /*lock (channelInfo.LockObject)
                {*/
                channelInfo.DataCount = packet.DataCount;
                channelInfo.DataType = packet.DataType;

                channelInfo.ServerId = packet.Parameter2;
                clients = channelInfo.GetClients();
                locked = false;
                locker.Release();

                foreach (var client in clients)
                {
                    connection.Gateway.Log.Write(Services.LogLevel.Detail, "Sending answer to " + client.Client + " GWID " + channelInfo.GatewayId);
                    var destConn = connection.Gateway.ClientConnection.Get(client.Client);
                    if (destConn == null)
                        continue;

                    Configuration.SecurityAccess access;
                    if (((TcpClientConnection)destConn).Listener == connection.Gateway.tcpSideA)
                        access = connection.Gateway.Configuration.Security.EvaluateSideA(channelInfo.ChannelName, "", "", ((TcpClientConnection)destConn).RemoteEndPoint.Address.ToString());
                    else
                        access = connection.Gateway.Configuration.Security.EvaluateSideB(channelInfo.ChannelName, "", "", ((TcpClientConnection)destConn).RemoteEndPoint.Address.ToString());

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
