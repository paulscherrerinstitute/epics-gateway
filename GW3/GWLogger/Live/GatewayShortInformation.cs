namespace GWLogger.Live
{
    public class GatewayShortInformation
    {
        public string Name { get; set; }
        public double? Cpu { get; set; }
        public int? Mem { get; internal set; }
        public int? Searches { get; internal set; }
        public string Build { get; internal set; }
    }
}