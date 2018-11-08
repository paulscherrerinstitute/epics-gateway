using GatewayLogic.Connections;
using GatewayLogic.Services;
using System;

namespace GatewayLogic.Commands
{
    internal class EventOff : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            connection.Gateway.EventsOnHold.Add(packet.Sender);
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
        }
    }
}

