using GatewayLogic.Connections;
using GatewayLogic.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Commands
{
    class Echo : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            packet.Destination = packet.Sender;
            if (((GatewayTcpConnection)connection).HasSentEcho)
            {
                connection.Gateway.Log.Write(LogLevel.Detail, "Echo answer received from " + packet.Sender);
                ((GatewayTcpConnection)connection).HasSentEcho = false;
                return;
            }
            connection.Gateway.Log.Write(LogLevel.Detail, "Echo request received from "+packet.Sender);
            connection.Send(packet);
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
            DoRequest(connection, packet);
        }
    }
}
