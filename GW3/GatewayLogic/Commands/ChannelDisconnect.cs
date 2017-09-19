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
            connection.Gateway.ChannelInformation.ServerDrop(packet.Parameter1);
        }
    }
}