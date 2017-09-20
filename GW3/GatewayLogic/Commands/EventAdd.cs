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
        SemaphoreSlim lockObject = new SemaphoreSlim(1);

        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            lockObject.Wait();
            var channel = connection.Gateway.ChannelInformation.Get(packet.Parameter1);
            if (channel == null)
            {
                connection.Gateway.Log.Write(Services.LogLevel.Error, "Event add on wrong channel.");
                lockObject.Release();
                return;
            }
            connection.Gateway.Log.Write(Services.LogLevel.Detail, "Event add on " + channel.ChannelName);

            var monitor = connection.Gateway.MonitorInformation.Get(channel, packet.DataType, packet.DataCount);
            if (monitor.FirstTime == true)
            {
                connection.Gateway.Log.Write(Services.LogLevel.Detail, "First event");
                monitor.FirstTime = false;

                monitor.AddClient(new ClientId { Client = packet.Sender, Id = packet.Parameter2 });
                var newPacket = (DataPacket)packet.Clone();
                newPacket.Parameter1 = channel.ServerId.Value;
                newPacket.Parameter2 = monitor.GatewayId;
                newPacket.Destination = channel.TcpConnection.RemoteEndPoint;
                channel.TcpConnection.Send(newPacket);
            }
            // We must send a Read Notify to get the first result
            else if (monitor.HasReceivedFirstResult == true)
            {
                monitor.AddClient(new ClientId { Client = packet.Sender, Id = packet.Parameter2, WaitingReadyNotify = true });

                connection.Gateway.Log.Write(Services.LogLevel.Detail, "First event result already sent. Sent ReadNotify.");

                var read = connection.Gateway.ReadNotifyInformation.Get(channel, packet.Parameter2, (TcpClientConnection)connection);
                read.IsEventAdd = true;
                read.EventClientId = packet.Parameter2;
                read.Monitor = monitor;

                var newPacket = DataPacket.Create(0);
                newPacket.Command = 15;
                newPacket.DataType = packet.DataType;
                newPacket.DataCount = packet.DataCount;
                newPacket.Parameter1 = channel.ServerId.Value;
                newPacket.Parameter2 = read.GatewayId;
                channel.TcpConnection.Send(newPacket);
            }
            else
            {
                connection.Gateway.Log.Write(Services.LogLevel.Detail, "Add client to the waiting list.");
                monitor.AddClient(new ClientId { Client = packet.Sender, Id = packet.Parameter2 });
            }
            lockObject.Release();
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
            lockObject.Wait();
            var monitor = connection.Gateway.MonitorInformation.GetByGatewayId(packet.Parameter2);
            monitor.HasReceivedFirstResult = true;
            connection.Gateway.Log.Write(Services.LogLevel.Detail, "Event add response on " + monitor.ChannelInformation.ChannelName);
            var clients = monitor.GetClients();
            lockObject.Release();
            foreach (var client in clients)
            {
                if (client.WaitingReadyNotify)
                    continue;
                var newPacket = (DataPacket)packet.Clone();
                var conn = connection.Gateway.ClientConnection.Get(client.Client);
                newPacket.Destination = conn.RemoteEndPoint;
                newPacket.Parameter2 = client.Id;
                conn.Send(newPacket);
            }
        }
    }
}
