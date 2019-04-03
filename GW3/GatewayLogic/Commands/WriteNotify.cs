using GatewayLogic.Connections;
using GatewayLogic.Services;

namespace GatewayLogic.Commands
{
    internal class WriteNotify : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            var channel = connection.Gateway.ChannelInformation.Get(packet.Parameter1);
            if (channel == null)
            {
                connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.WriteNotifyRequestWrongChannel);
                connection.Dispose(Services.LogMessageType.WriteNotifyRequestWrongChannel);
                return;
            }

            Configuration.SecurityAccess access;
            if (((TcpClientConnection)connection).Listener == connection.Gateway.tcpSideA)
                access = connection.Gateway.Configuration.Security.EvaluateSideA(channel.ChannelName, "", "", packet.Sender.Address.ToString());
            else
                access = connection.Gateway.Configuration.Security.EvaluateSideB(channel.ChannelName, "", "", packet.Sender.Address.ToString());
            if (!access.HasFlag(Configuration.SecurityAccess.WRITE))
            {
                connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.WriteNotifyRequestNoAccess, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channel.ChannelName } });
                return;
            }

            connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.WriteNotifyRequest, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channel.ChannelName } });
            var write = connection.Gateway.WriteNotifyInformation.Get(channel, packet.Parameter2, (TcpClientConnection)connection);
            packet.Parameter1 = channel.ServerId.Value;
            packet.Parameter2 = write.GatewayId;
            packet.Destination = channel.TcpConnection.RemoteEndPoint;
            channel.TcpConnection.Send(packet);
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
            var write = connection.Gateway.WriteNotifyInformation.GetByGatewayId(packet.Parameter2);
            if (write == null)
            {
                connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.WriteNotifyAnswerWrong, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ClientIoId, Value = packet.Parameter2.ToString() } });
                return;
            }
            connection.Gateway.MessageLogger.Write(write.Client.RemoteEndPoint.ToString(), Services.LogMessageType.WriteNotifyAnswer, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = write.ChannelInformation.ChannelName } });
            packet.Parameter2 = write.ClientId;
            write.Client.Send(packet);
            connection.Gateway.WriteNotifyInformation.Remove(write);
        }
    }
}