using GatewayLogic.Connections;
using GatewayLogic.Services;
using System.Collections.Generic;
using System.Linq;

namespace GatewayLogic.Commands
{
    internal class EventAdd : CommandHandler
    {
        private SafeLock lockObject = new SafeLock();

        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            DataPacket newPacket = null;
            ChannelInformation.ChannelInformationDetails channel;

            channel = connection.Gateway.ChannelInformation.Get(packet.Parameter1);
            if (channel == null || channel.ConnectionIsBuilding)
            {
                connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.EventAddWrongChannel);
                connection.Dispose(Services.LogMessageType.EventAddWrongChannel);
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

            System.Threading.Interlocked.Increment(ref connection.Gateway.DiagnosticServer.NbNewCAMON);

            /*using (lockObject.Aquire())
            {*/
            // A fresh new monitor
            if (monitor.FirstTime == true)
            {
                //connection.Gateway.Log.Write(Services.LogLevel.Detail, "First event");
                monitor.FirstTime = false;

                monitor.AddClient(new ClientId { Client = packet.Sender, Id = packet.Parameter2, Connection = connection });
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
                monitor.AddClient(new ClientId { Client = packet.Sender, Id = packet.Parameter2, WaitingReadyNotify = true, Connection = connection });

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
                monitor.AddClient(new ClientId { Client = packet.Sender, Id = packet.Parameter2, Connection = connection });
            }
            //}
            if (newPacket != null)
                channel.TcpConnection.Send(newPacket);
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
            IEnumerable<ClientId> clients = null;
            MonitorInformation.MonitorInformationDetail monitor;

            if (packet.PayloadSize == 0 || packet.DataCount == 0)
                return;

            monitor = connection.Gateway.MonitorInformation.GetByGatewayId(packet.Parameter2);
            if (monitor == null)
                return;

            monitor.ChannelInformation.ChannelTransferPerSecond += packet.MessageSize;
            if (monitor.HasReceivedFirstResult)
            {
                if (monitor.ChannelInformation.ChannelMustThrottle && monitor.ChannelInformation.GotThrottledData)
                    return;
                monitor.ChannelInformation.GotThrottledData = true;
            }
            else
            {
                /*using (lockObject.Aquire())
                {*/
                monitor.HasReceivedFirstResult = true;
                //}
            }

            clients = monitor.GetClients();
            connection.Gateway.MessageLogger.Write(packet.Sender.ToString(),
                Services.LogMessageType.EventAddResponse,
                new LogMessageDetail[] {
                    new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = monitor.ChannelInformation.ChannelName },
                    new LogMessageDetail { TypeId = MessageDetail.ClientCounts, Value = clients.Count().ToString() },
                    new LogMessageDetail { TypeId = MessageDetail.PacketSize, Value = packet.MessageSize.ToString() }
                });

            foreach (var client in clients)
            {
                if (client.WaitingReadyNotify)
                    continue;

                //var conn = connection.Gateway.ClientConnection.Get(client.Client);
                var conn = client.Connection;
                if (conn == null || conn.IsDisposed)
                {
                    connection.Gateway.MessageLogger.Write(client.Client.ToString(), Services.LogMessageType.EventAddResponseClientDisappeared, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = monitor.ChannelInformation.ChannelName } });
                    monitor.RemoveClient(connection.Gateway, client.Client, client.Id);
                    continue;
                }

                // Events have been disabled
                if (connection.Gateway.EventsOnHold.Contains(client.Client))
                    continue;

                connection.Gateway.MessageLogger.Write(client.Client.ToString(), Services.LogMessageType.EventAddResponseSending, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = monitor.ChannelInformation.ChannelName }, new LogMessageDetail { TypeId = MessageDetail.PacketSize, Value = packet.MessageSize.ToString() }, new LogMessageDetail { TypeId = MessageDetail.CID, Value = client.Id.ToString() } });

                System.Threading.Interlocked.Increment(ref connection.Gateway.DiagnosticServer.NbCAMONAnswers);

                packet.Parameter2 = client.Id;
                conn.Send(packet);
            }
        }
    }
}
