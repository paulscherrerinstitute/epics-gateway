using GatewayLogic.Connections;

namespace GatewayLogic.Commands
{
    internal class EventCancel : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            var monitor = connection.Gateway.MonitorInformation.GetByGatewayId(packet.Parameter2);

            if (monitor != null && monitor.ChannelInformation != null)
            {
                connection.Gateway.Log.Write(Services.LogLevel.Detail, "Clear channel monitor on " + monitor.ChannelInformation.ChannelName);

                monitor.RemoveClient(connection.Gateway, packet.Parameter2);
                connection.Send(packet);
            }
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
        }
    }
}