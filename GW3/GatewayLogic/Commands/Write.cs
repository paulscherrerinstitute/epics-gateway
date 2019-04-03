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
                connection.Dispose(Services.LogMessageType.WriteWrongChannel);
                return;
            }

            Configuration.SecurityAccess access;
            if (((TcpClientConnection)connection).Listener == connection.Gateway.tcpSideA)
                access = connection.Gateway.Configuration.Security.EvaluateSideA(channel.ChannelName, "", "", packet.Sender.Address.ToString());
            else
                access = connection.Gateway.Configuration.Security.EvaluateSideB(channel.ChannelName, "", "", packet.Sender.Address.ToString());
            if (!access.HasFlag(Configuration.SecurityAccess.WRITE))
            {
                connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.WriteRequestNoAccess, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channel.ChannelName } });
                return;
            }

            connection.Gateway.MessageLogger.Write(packet.Sender.ToString(),
                Services.LogMessageType.Write,
                new LogMessageDetail[] {
                    new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channel.ChannelName },
                    new LogMessageDetail { TypeId = MessageDetail.PacketSize, Value = packet.MessageSize.ToString() }
                });
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