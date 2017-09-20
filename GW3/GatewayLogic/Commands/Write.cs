using GatewayLogic.Connections;

namespace GatewayLogic.Commands
{
    internal class Write : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            var channel = connection.Gateway.ChannelInformation.Get(packet.Parameter1);
            if (channel == null)
            {
                connection.Gateway.Log.Write(Services.LogLevel.Error, "Write on wrong channel.");
                return;
            }
            connection.Gateway.Log.Write(Services.LogLevel.Detail, "Write on " + channel.ChannelName);
            packet.Parameter1 = channel.ServerId.Value;
            packet.Destination = channel.TcpConnection.RemoteEndPoint;
            channel.TcpConnection.Send(packet);
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
        }
    }
}