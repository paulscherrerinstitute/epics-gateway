using GatewayLogic.Connections;

namespace GatewayLogic.Commands
{
    internal class EventCancel : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            connection.Gateway.Log.Write(Services.LogLevel.Detail, "Request cancel channel monitor " + packet.Parameter2);
            var monitor = connection.Gateway.MonitorInformation.GetByClientId(packet.Sender, packet.Parameter2);

            if (monitor != null && monitor.ChannelInformation != null)
            {
                connection.Gateway.Log.Write(Services.LogLevel.Detail, "Cancel channel monitor on " + monitor.ChannelInformation.ChannelName);


                var newPacket = DataPacket.Create(0);
                newPacket.Command = 1;
                newPacket.Destination = packet.Sender;
                newPacket.DataType = monitor.DataType;
                newPacket.DataCount = 0;
                newPacket.Parameter1 = packet.Parameter1;
                newPacket.Parameter2 = packet.Parameter2;
                connection.Send(newPacket);

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