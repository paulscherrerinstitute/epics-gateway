using GatewayLogic.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Commands
{
    class EventAdd : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            var channel = connection.Gateway.ChannelInformation.Get(packet.Parameter1);
            if (channel == null)
            {
                Console.WriteLine("Event add on wrong channel.");
                return;
            }
            Console.WriteLine("Event add on " + channel.ChannelName);

            var monitor = MonitorInformation.Get(channel, packet.DataType, packet.DataCount);
            monitor.AddClient(new ClientId { Client = packet.Sender, Id = packet.Parameter2 });
            var newPacket = (DataPacket)packet.Clone();
            newPacket.Parameter1 = channel.ServerId.Value;
            newPacket.Parameter2 = monitor.GatewayId;
            newPacket.Destination = channel.TcpConnection.Destination;
            channel.TcpConnection.Send(newPacket);
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
            var monitor = MonitorInformation.GetByGatewayId(packet.Parameter2);

            Console.WriteLine("Event add response on " + monitor.ChannelInformation.ChannelName);
            foreach (var client in monitor.GetClients())
            {
                var newPacket = (DataPacket)packet.Clone();
                var conn = ClientConnection.Get(client.Client);
                newPacket.Destination = conn.RemoteEndPoint;
                newPacket.Parameter2 = client.Id;
                conn.Send(newPacket);
            }
        }
    }
}
