namespace LoadPerformance
{
    internal class ChannelSubscription
    {
        public uint ChannelId { get; internal set; }
        public uint ClientId { get; internal set; }
        public uint DataCount { get; internal set; }
        public ushort DataType { get; internal set; }
    }
}