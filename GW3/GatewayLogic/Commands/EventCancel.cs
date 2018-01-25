using GatewayLogic.Connections;

namespace GatewayLogic.Commands
{
    internal class EventCancel : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            connection.Gateway.Log.Write(Services.LogLevel.Detail, "Request clear channel monitor " + packet.Parameter2);
            var monitor = connection.Gateway.MonitorInformation.GetByClientId(packet.Sender, packet.Parameter2);

            if (monitor != null && monitor.ChannelInformation != null)
            {
                connection.Gateway.Log.Write(Services.LogLevel.Detail, "Clear channel monitor on " + monitor.ChannelInformation.ChannelName);

                monitor.RemoveClient(connection.Gateway, packet.Sender, packet.Parameter2);
                //packet.DataCount = monitor.ChannelInformation.DataCount;
                //connection.Send(packet);
            }
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
        }
    }
}