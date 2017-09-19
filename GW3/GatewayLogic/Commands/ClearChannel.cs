using GatewayLogic.Connections;

namespace GatewayLogic.Commands
{
    internal class ClearChannel : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            var channel = connection.Gateway.ChannelInformation.Get(packet.Parameter1);

            if (channel != null)
                connection.Gateway.Log.Write(Services.LogLevel.Detail, "Clear channel on " + channel.ChannelName);

            channel.DisconnectClient((TcpClientConnection)connection);
            connection.Send(packet);
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
        }
    }
}