using GatewayLogic.Connections;
using GatewayLogic.Services;
using System;

namespace GatewayLogic.Commands
{
    internal class Echo : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            var gwConnection = (GatewayTcpConnection)connection;
            packet.Destination = packet.Sender;
            if (gwConnection.HasSentEcho && (DateTime.UtcNow - gwConnection.LastEcho).TotalSeconds < 2)
            {
                connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.EchoAnswerReceived, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.Origin, Value = connection.GetType().Name } });
                //connection.Gateway.Log.Write(LogLevel.Detail, "Echo answer received from " + packet.Sender);
                gwConnection.HasSentEcho = false;
                gwConnection.LastEcho = DateTime.UtcNow;
                return;
            }
            gwConnection.HasSentEcho = false;
            connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.EchoRequestReceived, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.Origin, Value = connection.GetType().Name } });
            //connection.Gateway.Log.Write(LogLevel.Detail, "Echo request received from " + packet.Sender);
            if (((DateTime.UtcNow - gwConnection.LastEcho)).TotalSeconds > 0.3)
            {
                connection.Send(packet);
                gwConnection.LastEcho = DateTime.UtcNow;
            }
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
            DoRequest(connection, packet);
        }
    }
}
