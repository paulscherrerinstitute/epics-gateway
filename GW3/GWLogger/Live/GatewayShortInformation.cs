namespace GWLogger.Live
{
    public class GatewayShortInformation
    {
        public string Name { get; set; }
        public double? Cpu { get; set; }
        public int? Mem { get; set; }
        public int? Searches { get; set; }
        public string Build { get; set; }
        public int State { get; set; }
    }
}