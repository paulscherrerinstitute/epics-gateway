using GatewayLogic.Connections;

namespace GatewayLogic.Commands
{
    internal class WriteNotify : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            var channel = connection.Gateway.ChannelInformation.Get(packet.Parameter1);
            if (channel == null)
            {
                connection.Gateway.Log.Write(Services.LogLevel.Error, "Write notify on wrong channel.");
                return;
            }
            connection.Gateway.Log.Write(Services.LogLevel.Detail, "Write notify on " + channel.ChannelName);
            var write = connection.Gateway.WriteNotifyInformation.Get(channel, packet.Parameter2, (TcpClientConnection)connection);
            packet.Parameter1 = channel.ServerId.Value;
            packet.Parameter2 = write.GatewayId;
            packet.Destination = channel.TcpConnection.RemoteEndPoint;
            channel.TcpConnection.Send(packet);
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
            var write = connection.Gateway.WriteNotifyInformation.GetByGatewayId(packet.Parameter2);
            connection.Gateway.Log.Write(Services.LogLevel.Detail, "Write notify response on " + write.ChannelInformation.ChannelName);
            packet.Parameter2 = write.ClientId;
            write.Client.Send(packet);
            connection.Gateway.WriteNotifyInformation.Remove(write);
        }
    }
}