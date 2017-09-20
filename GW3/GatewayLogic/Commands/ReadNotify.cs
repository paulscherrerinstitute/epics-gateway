using GatewayLogic.Connections;
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
                connection.Gateway.Log.Write(Services.LogLevel.Error, "Read notify on wrong channel.");
                return;
            }
            connection.Gateway.Log.Write(Services.LogLevel.Detail, "Read notify on " + channel.ChannelName);
            var read = connection.Gateway.ReadNotifyInformation.Get(channel, packet.Parameter2, (TcpClientConnection)connection);
            packet.Parameter2 = read.GatewayId;
            packet.Destination = channel.TcpConnection.RemoteEndPoint;
            channel.TcpConnection.Send(packet);
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
            var read = connection.Gateway.ReadNotifyInformation.GetByGatewayId(packet.Parameter2);

            if (read.IsEventAdd)
            {
                var client = read.Monitor.GetClients().FirstOrDefault(row => row.Id == read.EventClientId);

                connection.Gateway.Log.Write(Services.LogLevel.Detail, "Read notify response for event add on " + read.ChannelInformation.ChannelName);
                packet.Command = 1;
                packet.Parameter1 = 1;
                packet.Parameter2 = read.ClientId;
                read.Client.Send(packet);

                if(client.WaitingReadyNotify)
                    client.WaitingReadyNotify = false;
            }
            else
            {
                connection.Gateway.Log.Write(Services.LogLevel.Detail, "Read notify response on " + read.ChannelInformation.ChannelName);
                packet.Parameter2 = read.ClientId;
                read.Client.Send(packet);
            }
        }
    }
}
