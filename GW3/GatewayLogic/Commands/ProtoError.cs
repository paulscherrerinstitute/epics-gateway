using GatewayLogic.Connections;

namespace GatewayLogic.Commands
{
    internal class ProtoError : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            throw new System.NotImplementedException();
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
            throw new System.NotImplementedException();
        }
    }
}