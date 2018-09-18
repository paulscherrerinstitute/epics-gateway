using EpicsSharp.ChannelAccess.Client;

namespace GWLogger.Live
{
    public class GatewayValue<TType> where TType : class
    {
        private Channel<TType> channel;

        public TType Value { get; private set; } = null;

        public GatewayValue(CAClient client, string name)
        {
            channel = client.CreateChannel<TType>(name);
            channel.MonitorChanged += Channel_MonitorChanged;
            channel.StatusChanged += Channel_StatusChanged;
        }

        private void Channel_StatusChanged(Channel sender, EpicsSharp.ChannelAccess.Constants.ChannelStatus newStatus)
        {
            switch (newStatus)
            {
                case EpicsSharp.ChannelAccess.Constants.ChannelStatus.CONNECTED:
                    break;
                case EpicsSharp.ChannelAccess.Constants.ChannelStatus.REQUESTED:
                case EpicsSharp.ChannelAccess.Constants.ChannelStatus.DISCONNECTED:
                case EpicsSharp.ChannelAccess.Constants.ChannelStatus.DISPOSED:
                case EpicsSharp.ChannelAccess.Constants.ChannelStatus.CONNECTING:
                    HasValue = false;
                    Value = null;
                    break;
                default:
                    break;
            }
        }

        private void Channel_MonitorChanged(Channel<TType> sender, TType newValue)
        {
            Value = newValue;
            HasValue = true;
        }
        public bool HasValue { get; private set; } = false;
    }
}