namespace GWLogger.Live
{
    public class GatewayShortInformation
    {
        public string Name { get; set; }
        public double? CPU { get; set; }
        public int? Mem { get; set; }
        public int? Searches { get; set; }
        public string Build { get; set; }
        public int State { get; set; }
        public string Version { get; set; }
        public string RunningTime { get; set; }
    }
}