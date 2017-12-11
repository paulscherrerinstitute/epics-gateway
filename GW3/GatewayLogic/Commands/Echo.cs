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
                ((GatewayTcpConnection)connection).LastMessage = DateTime.UtcNow;
                return;
            }
            connection.Gateway.Log.Write(LogLevel.Detail, "Echo request received from " + packet.Sender);
            if (((DateTime.UtcNow - ((GatewayTcpConnection)connection).LastMessage)).TotalSeconds > 10)
            {
                connection.Send(packet);
                ((GatewayTcpConnection)connection).LastMessage = DateTime.UtcNow;
            }
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
            DoRequest(connection, packet);
        }
    }
}
