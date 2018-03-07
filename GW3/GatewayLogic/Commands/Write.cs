using GatewayLogic.Connections;
using GatewayLogic.Services;

namespace GatewayLogic.Commands
{
    internal class Write : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            var channel = connection.Gateway.ChannelInformation.Get(packet.Parameter1);
            if (channel == null)
            {
                connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.WriteWrongChannel);
                //connection.Gateway.Log.Write(Services.LogLevel.Error, "Write on wrong channel.");
                return;
            }
            connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.Write, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channel.ChannelName } });
            //connection.Gateway.Log.Write(Services.LogLevel.Detail, "Write on " + channel.ChannelName);
            packet.Parameter1 = channel.ServerId.Value;
            packet.Destination = channel.TcpConnection.RemoteEndPoint;
            channel.TcpConnection.Send(packet);
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
        }
    }
}