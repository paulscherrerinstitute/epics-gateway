using GatewayLogic.Connections;

namespace GatewayLogic.Commands
{
    internal class ChannelDisconnect : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
            var channel = connection.Gateway.ChannelInformation.Get(packet.Parameter1);
            if(channel != null)
                connection.Gateway.Log.Write(Services.LogLevel.Detail, "Channel disconnect on " + channel.ChannelName);
            connection.Gateway.ChannelInformation.ServerDrop(packet.Parameter1);
        }
    }
}