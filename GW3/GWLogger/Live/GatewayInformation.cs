namespace GWLogger.Live
{
    public class GatewayInformation
    {
        public string Name { get; set; }
        public double? Cpu { get; set; }
        public int? Mem { get; set; }
        public int? Searches { get; set; }
        public string Build { get; set; }
        public int? Messages { get; set; }
        public int? PVs { get; set; }
        public string RunningTime { get; set; }
    }
}