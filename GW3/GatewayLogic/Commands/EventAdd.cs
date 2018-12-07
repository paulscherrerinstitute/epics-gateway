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
    class EventAdd : CommandHandler
    {
        SafeLock lockObject = new SafeLock();

        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            DataPacket newPacket = null;
            ChannelInformation.ChannelInformationDetails channel;

            using (lockObject.Aquire())
            {
                channel = connection.Gateway.ChannelInformation.Get(packet.Parameter1);
                if (channel == null)
                {
                    connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.EventAddWrongChannel);
                    connection.Dispose();
                    return;
                }

                var dataCount = packet.DataCount;
                if (channel.TcpConnection.Version < Gateway.CA_PROTO_VERSION && dataCount == 0)
                {
                    dataCount = channel.DataCount;
                    connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.EventAddDynOldIoc, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channel.ChannelName }, new LogMessageDetail { TypeId = MessageDetail.DataCount, Value = dataCount.ToString() } });
                }

                connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.EventAdd, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channel.ChannelName }, new LogMessageDetail { TypeId = MessageDetail.CID, Value = packet.Parameter2.ToString() } });

                // A monitor on datacount 0 will always be a new monitor
                var monitorMask = packet.GetUInt16(12 + (int)packet.HeaderSize);
                var monitor = connection.Gateway.MonitorInformation.Get(channel, packet.DataType, dataCount, monitorMask);
                // A fresh new monitor
                if (monitor.FirstTime == true)
                {
                    //connection.Gateway.Log.Write(Services.LogLevel.Detail, "First event");
                    monitor.FirstTime = false;

                    monitor.AddClient(new ClientId { Client = packet.Sender, Id = packet.Parameter2 });
                    newPacket = (DataPacket)packet.Clone();
                    newPacket.Parameter1 = channel.ServerId.Value;
                    newPacket.Parameter2 = monitor.GatewayId;
                    newPacket.DataCount = dataCount;
                    newPacket.Destination = channel.TcpConnection.RemoteEndPoint;
                    connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.EventAddFirstEvent, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channel.ChannelName }, new LogMessageDetail { TypeId = MessageDetail.CID, Value = packet.Parameter2.ToString() }, new LogMessageDetail { TypeId = MessageDetail.GatewayMonitorId, Value = monitor.GatewayId.ToString() } });
                }
                // We must send a Read Notify to get the first result
                else if (monitor.HasReceivedFirstResult == true)
                {
                    monitor.AddClient(new ClientId { Client = packet.Sender, Id = packet.Parameter2, WaitingReadyNotify = true });

                    var read = connection.Gateway.ReadNotifyInformation.Get(channel, packet.Parameter2, (TcpClientConnection)connection);
                    read.IsEventAdd = true;
                    read.EventClientId = packet.Parameter2;
                    read.Monitor = monitor;

                    connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.EventAddNotFirst, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channel.ChannelName }, new LogMessageDetail { TypeId = MessageDetail.CID, Value = packet.Parameter2.ToString() }, new LogMessageDetail { TypeId = MessageDetail.GatewayMonitorId, Value = monitor.GatewayId.ToString() } });

                    newPacket = DataPacket.Create(0);
                    newPacket.Command = 15;
                    newPacket.DataType = packet.DataType;
                    newPacket.DataCount = dataCount;
                    newPacket.Parameter1 = channel.ServerId.Value;
                    newPacket.Parameter2 = read.GatewayId;
                }
                else
                {
                    connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.EventAddMonitorList, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channel.ChannelName }, new LogMessageDetail { TypeId = MessageDetail.CID, Value = packet.Parameter2.ToString() }, new LogMessageDetail { TypeId = MessageDetail.GatewayMonitorId, Value = monitor.GatewayId.ToString() } });
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

            if (packet.PayloadSize == 0 || packet.DataCount == 0)
                return;

            using (lockObject.Aquire())
            {
                monitor = connection.Gateway.MonitorInformation.GetByGatewayId(packet.Parameter2);
                if (monitor == null)
                {
                    //connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.EventResponseOnUnknown, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.GatewayMonitorId, Value = packet.Parameter2.ToString() } });
                    return;
                }
                monitor.HasReceivedFirstResult = true;
                clients = monitor.GetClients();
                connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.EventAddResponse, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = monitor.ChannelInformation.ChannelName }, new LogMessageDetail { TypeId = MessageDetail.ClientCounts, Value = clients.Count().ToString() } });
            }

            foreach (var client in clients)
            {
                if (client.WaitingReadyNotify)
                {
                    //connection.Gateway.MessageLogger.Write(client.Client.ToString(), Services.LogMessageType.EventAddResponseSkipForRead, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = monitor.ChannelInformation.ChannelName } });
                    continue;
                }
                var conn = connection.Gateway.ClientConnection.Get(client.Client);
                if (conn == null)
                {
                    connection.Gateway.MessageLogger.Write(client.Client.ToString(), Services.LogMessageType.EventAddResponseClientDisappeared, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = monitor.ChannelInformation.ChannelName } });
                    monitor.RemoveClient(connection.Gateway, client.Client, client.Id);
                    continue;
                }

                // Events have been disabled
                if (connection.Gateway.EventsOnHold.Contains(client.Client))
                    continue;

                var newPacket = (DataPacket)packet.Clone();
                newPacket.Destination = conn.RemoteEndPoint;
                newPacket.Parameter2 = client.Id;
                conn.Send(newPacket);
                //connection.Gateway.MessageLogger.Write(client.Client.ToString(), Services.LogMessageType.EventAddResponseSending, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = monitor.ChannelInformation.ChannelName }, new LogMessageDetail { TypeId = MessageDetail.CID, Value = client.Id.ToString() } });
            }
        }
    }
}
