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
            var channel = ChannelInformation.Get(packet.Parameter1);
            if (channel == null)
            {
                Console.WriteLine("Read notify on wrong channel.");
                return;
            }
            Console.WriteLine("Read notify on " + channel.ChannelName);
            var read = ReadNotifyInformation.Get(channel, packet.Parameter2, (TcpClientConnection)connection);
            packet.Parameter2 = read.GatewayId;
            packet.Destination = channel.TcpConnection.Destination;
            channel.TcpConnection.Send(packet);
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
            var read = ReadNotifyInformation.GetByGatewayId(packet.Parameter2);
            Console.WriteLine("Read notify response on " + read.ChannelInformation.ChannelName);
            packet.Parameter2 = read.ClientId;
            read.Client.Send(packet);
        }
    }
}
