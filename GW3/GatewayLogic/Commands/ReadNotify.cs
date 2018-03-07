﻿using GatewayLogic.Connections;
using GatewayLogic.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Commands
{
    class ReadNotify : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            var channel = connection.Gateway.ChannelInformation.Get(packet.Parameter1);
            if (channel == null)
            {
                connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.ReadNotifyRequestWrongChannel, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.GWID, Value = packet.Parameter1.ToString() } });
                //connection.Gateway.Log.Write(Services.LogLevel.Error, "Read notify on wrong channel. " + packet.Parameter1);
                return;
            }
            var read = connection.Gateway.ReadNotifyInformation.Get(channel, packet.Parameter2, (TcpClientConnection)connection);
            connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.ReadNotifyRequest, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ClientIoId, Value = packet.Parameter2.ToString() }, new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channel.ChannelName }, new LogMessageDetail { TypeId = MessageDetail.SID, Value = channel.ServerId.ToString() } });
            //connection.Gateway.Log.Write(Services.LogLevel.Detail, "Read notify on " + channel.ChannelName + " SID " + channel.ServerId + " IOID " + read.GatewayId + " CIOID " + packet.Parameter2);
            packet.Parameter1 = channel.ServerId.Value;
            packet.Parameter2 = read.GatewayId;
            packet.Destination = channel.TcpConnection.RemoteEndPoint;
            channel.TcpConnection.Send(packet);
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
            var read = connection.Gateway.ReadNotifyInformation.GetByGatewayId(packet.Parameter2);
            if (read == null)
                return;

            if (read.IsEventAdd)
            {
                var client = read.Monitor.GetClients().FirstOrDefault(row => row.Id == read.EventClientId && row.Client == read.Client.RemoteEndPoint);
                if (client == null)
                {
                    connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.ReadNotifyResponseMonitorClientNotFound, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.CID, Value = read.EventClientId.ToString() } });
                    //connection.Gateway.Log.Write(LogLevel.Error, "Read back for monitor => monitor client not found (id: " + read.EventClientId + ")");
                    return;
                }

                connection.Gateway.MessageLogger.Write(client.Client.ToString(), Services.LogMessageType.ReadNotifyResponseMonitor, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.CID, Value = read.EventClientId.ToString() }, new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = read.ChannelInformation.ChannelName } });
                //connection.Gateway.Log.Write(Services.LogLevel.Detail, "Read notify response for event add on " + read.ChannelInformation.ChannelName);
                packet.Command = 1;
                packet.Parameter1 = 1;
                packet.Parameter2 = read.ClientId;
                read.Client.Send(packet);

                /*var clientMonitor = read.Monitor.GetClients().FirstOrDefault(row => row.Id == read.EventClientId && row.Client == read.Client.RemoteEndPoint);
                if (clientMonitor != null)
                    clientMonitor.WaitingReadyNotify = false;*/

                //if (client.WaitingReadyNotify)
                client.WaitingReadyNotify = false;
            }
            else
            {
                connection.Gateway.MessageLogger.Write(read.Client.RemoteEndPoint.ToString(), Services.LogMessageType.ReadNotifyResponse, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = read.ChannelInformation.ChannelName } });
                //connection.Gateway.Log.Write(Services.LogLevel.Detail, "Read notify response on " + read.ChannelInformation.ChannelName + " IOID " + packet.Parameter2 + " CIOID " + read.ClientId);
                packet.Parameter2 = read.ClientId;
                read.Client.Send(packet);
            }
            connection.Gateway.ReadNotifyInformation.Remove(read);
        }
    }
}
