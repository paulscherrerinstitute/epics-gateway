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
    class EventAdd : CommandHandler
    {
        object lockObject = new object();

        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            DataPacket newPacket = null;
            ChannelInformation.ChannelInformationDetails channel;
            lock (lockObject)
            {
                channel = connection.Gateway.ChannelInformation.Get(packet.Parameter1);
                if (channel == null)
                {
                    connection.Gateway.Log.Write(Services.LogLevel.Error, "Event add on wrong channel.");
                    connection.Dispose();
                    return;
                }
                connection.Gateway.Log.Write(Services.LogLevel.Detail, "Event add on " + channel.ChannelName + " client id " + packet.Parameter2);

                // A monitor on datacount 0 will always be a new monitor
                var monitorMask = packet.GetUInt16(12 + (int)packet.HeaderSize);
                var monitor = connection.Gateway.MonitorInformation.Get(channel, packet.DataType, packet.DataCount, monitorMask);
                // A fresh new monitor
                if (monitor.FirstTime == true)
                {
                    connection.Gateway.Log.Write(Services.LogLevel.Detail, "First event");
                    monitor.FirstTime = false;

                    monitor.AddClient(new ClientId { Client = packet.Sender, Id = packet.Parameter2 });
                    newPacket = (DataPacket)packet.Clone();
                    newPacket.Parameter1 = channel.ServerId.Value;
                    newPacket.Parameter2 = monitor.GatewayId;
                    newPacket.Destination = channel.TcpConnection.RemoteEndPoint;
                    connection.Gateway.Log.Write(Services.LogLevel.Detail, "New channel monitor " + monitor.GatewayId);
                    //channel.TcpConnection.Send(newPacket);
                }
                // We must send a Read Notify to get the first result
                else if (monitor.HasReceivedFirstResult == true)
                {
                    monitor.AddClient(new ClientId { Client = packet.Sender, Id = packet.Parameter2, WaitingReadyNotify = true });

                    var read = connection.Gateway.ReadNotifyInformation.Get(channel, packet.Parameter2, (TcpClientConnection)connection);
                    read.IsEventAdd = true;
                    read.EventClientId = packet.Parameter2;
                    read.Monitor = monitor;

                    connection.Gateway.Log.Write(Services.LogLevel.Detail, "First event result already sent. Sent ReadNotify (client id: " + packet.Parameter2 + " gw id: " + read.GatewayId + ").");

                    newPacket = DataPacket.Create(0);
                    newPacket.Command = 15;
                    newPacket.DataType = packet.DataType;
                    newPacket.DataCount = packet.DataCount;
                    newPacket.Parameter1 = channel.ServerId.Value;
                    newPacket.Parameter2 = read.GatewayId;
                    //channel.TcpConnection.Send(newPacket);
                }
                else
                {
                    connection.Gateway.Log.Write(Services.LogLevel.Detail, "Add client to the waiting list.");
                    monitor.AddClient(new ClientId { Client = packet.Sender, Id = packet.Parameter2 });
                }
            }
            if (newPacket != null)
                channel.TcpConnection.Send(newPacket);
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
            IEnumerable<ClientId> clients = null;
            MonitorInformation.MonitorInformationDetail monitor;
            lock (lockObject)
            {
                monitor = connection.Gateway.MonitorInformation.GetByGatewayId(packet.Parameter2);
                if (monitor == null)
                {
                    connection.Gateway.Log.Write(Services.LogLevel.Error, "Event add response on unknown");
                    ThreadPool.QueueUserWorkItem((obj) => { connection.Dispose(); });
                    return;
                }
                monitor.HasReceivedFirstResult = true;
                clients = monitor.GetClients();
                connection.Gateway.Log.Write(Services.LogLevel.Detail, "Event add response on " + monitor.ChannelInformation.ChannelName + " clients " + clients.Count());
            }
            foreach (var client in clients)
            {
                if (client.WaitingReadyNotify)
                {
                    connection.Gateway.Log.Write(Services.LogLevel.Detail, "Event waiting first response on " + monitor.ChannelInformation.ChannelName);
                    continue;
                }
                var newPacket = (DataPacket)packet.Clone();
                var conn = connection.Gateway.ClientConnection.Get(client.Client);
                if (conn == null)
                {
                    connection.Gateway.Log.Write(Services.LogLevel.Error, "Event response for client which disappeared");
                    monitor.RemoveClient(connection.Gateway, client.Client, client.Id);
                    continue;
                }
                newPacket.Destination = conn.RemoteEndPoint;
                newPacket.Parameter2 = client.Id;
                conn.Send(newPacket);
                connection.Gateway.Log.Write(Services.LogLevel.Detail, "Sending event response on " + monitor.ChannelInformation.ChannelName + " client " + client.Id);
            }
        }
    }
}
