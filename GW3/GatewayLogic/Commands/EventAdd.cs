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
                    //connection.Gateway.Log.Write(Services.LogLevel.Error, "Event add on wrong channel.");
                    //connection.Dispose();
                    return;
                }

                var dataCount = packet.DataCount;
                if (channel.TcpConnection.Version < Gateway.CA_PROTO_VERSION && dataCount == 0)
                {
                    dataCount = channel.DataCount;
                    connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.EventAddDynOldIoc, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channel.ChannelName }, new LogMessageDetail { TypeId = MessageDetail.DataCount, Value = dataCount.ToString() } });
                    //connection.Gateway.Log.Write(Services.LogLevel.Detail, "CA Version too old, must set the datacount for " + channel.ChannelName + " to " + dataCount);
                }

                connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.EventAdd, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channel.ChannelName }, new LogMessageDetail { TypeId = MessageDetail.CID, Value = packet.Parameter2.ToString() } });
                //connection.Gateway.Log.Write(Services.LogLevel.Detail, "Event add on " + channel.ChannelName + " client id " + packet.Parameter2);

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
                    //connection.Gateway.Log.Write(Services.LogLevel.Detail, "New channel monitor " + monitor.GatewayId);
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

                    connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.EventAddNotFirst, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channel.ChannelName }, new LogMessageDetail { TypeId = MessageDetail.CID, Value = packet.Parameter2.ToString() }, new LogMessageDetail { TypeId = MessageDetail.GatewayMonitorId, Value = monitor.GatewayId.ToString() } });
                    //connection.Gateway.Log.Write(Services.LogLevel.Detail, "First event result already sent. Sent ReadNotify (client id: " + packet.Parameter2 + " gw id: " + read.GatewayId + ").");

                    newPacket = DataPacket.Create(0);
                    newPacket.Command = 15;
                    newPacket.DataType = packet.DataType;
                    newPacket.DataCount = dataCount;
                    newPacket.Parameter1 = channel.ServerId.Value;
                    newPacket.Parameter2 = read.GatewayId;
                    //channel.TcpConnection.Send(newPacket);
                }
                else
                {
                    connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.EventAddMonitorList, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channel.ChannelName }, new LogMessageDetail { TypeId = MessageDetail.CID, Value = packet.Parameter2.ToString() }, new LogMessageDetail { TypeId = MessageDetail.GatewayMonitorId, Value = monitor.GatewayId.ToString() } });
                    //connection.Gateway.Log.Write(Services.LogLevel.Detail, "Add client to the waiting list.");
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
                    connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.EventResponseOnUnknown, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.GatewayMonitorId, Value = packet.Parameter2.ToString() } });
                    //connection.Gateway.Log.Write(Services.LogLevel.Error, "Event add response on unknown (" + packet.Parameter2 + ")");
                    /*var newPacket = DataPacket.Create(0);
                    newPacket.Command = 2;
                    newPacket.DataType = 1;
                    newPacket.DataCount = 1;*/
                    //ThreadPool.QueueUserWorkItem((obj) => { connection.Dispose(); });
                    return;
                }
                monitor.HasReceivedFirstResult = true;
                clients = monitor.GetClients();
                connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.EventAddResponse, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = monitor.ChannelInformation.ChannelName }, new LogMessageDetail { TypeId = MessageDetail.ClientCounts, Value = clients.Count().ToString() } });
                //connection.Gateway.Log.Write(Services.LogLevel.Detail, "Event add response on " + monitor.ChannelInformation.ChannelName + " clients " + clients.Count());
            }

            foreach (var client in clients)
            {
                if (client.WaitingReadyNotify)
                {
                    connection.Gateway.MessageLogger.Write(client.Client.ToString(), Services.LogMessageType.EventAddResponseSkipForRead, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = monitor.ChannelInformation.ChannelName } });
                    //connection.Gateway.Log.Write(Services.LogLevel.Detail, "Event waiting first response on " + monitor.ChannelInformation.ChannelName);
                    continue;
                }
                var newPacket = (DataPacket)packet.Clone();
                var conn = connection.Gateway.ClientConnection.Get(client.Client);
                if (conn == null)
                {
                    connection.Gateway.MessageLogger.Write(client.Client.ToString(), Services.LogMessageType.EventAddResponseClientDisappeared, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = monitor.ChannelInformation.ChannelName } });
                    //connection.Gateway.Log.Write(Services.LogLevel.Detail, "Event response for client which disappeared");
                    monitor.RemoveClient(connection.Gateway, client.Client, client.Id);
                    continue;
                }
                newPacket.Destination = conn.RemoteEndPoint;
                newPacket.Parameter2 = client.Id;
                conn.Send(newPacket);
                connection.Gateway.MessageLogger.Write(client.Client.ToString(), Services.LogMessageType.EventAddResponseSending, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = monitor.ChannelInformation.ChannelName }, new LogMessageDetail { TypeId = MessageDetail.CID, Value = client.Id.ToString() } });
                //connection.Gateway.Log.Write(Services.LogLevel.Detail, "Sending event response on " + monitor.ChannelInformation.ChannelName + " client " + client.Id);
            }
        }
    }
}
