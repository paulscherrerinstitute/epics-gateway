using GatewayLogic.Connections;
using GatewayLogic.Services;
using System;

namespace GatewayLogic.Commands
{
    internal class EventOn : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            connection.Gateway.EventsOnHold.Remove(packet.Sender);
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
        }
    }
}
