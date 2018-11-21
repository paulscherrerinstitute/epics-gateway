namespace LoadTest
{
    internal class ResultPoint
    {
        public int Expected { get; set; }
        public int Received { get; set; }
        public int Difference { get; set; }
        public int NbServers { get; internal set; }
        public int NbClients { get; internal set; }
        public int NbChannelsUsed { get; internal set; }
        public int CPU { get; internal set; }
    }
}