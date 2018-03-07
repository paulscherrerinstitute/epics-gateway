using GatewayLogic.Connections;
using GatewayLogic.Services;

namespace GatewayLogic.Commands
{
    internal class ClearChannel : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            var channel = connection.Gateway.ChannelInformation.Get(packet.Parameter1);

            if (channel == null)
                return;

            connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.ClearChannel, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channel.ChannelName } });
            //connection.Gateway.Log.Write(Services.LogLevel.Detail, "Clear channel on " + channel.ChannelName);
            channel.DisconnectClient((TcpClientConnection)connection);
            connection.Send(packet);
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
        }
    }
}