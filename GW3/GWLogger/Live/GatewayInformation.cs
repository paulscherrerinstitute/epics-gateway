namespace GWLogger.Live
{
    public class GatewayInformation
    {
        public string Name { get; set; }
        public double? CPU { get; set; }
        public int? Mem { get; set; }
        public int? Searches { get; set; }
        public string Build { get; set; }
        public string Version { get; set; }
        public int? Messages { get; set; }
        public int? PVs { get; set; }
        public string RunningTime { get; set; }
        public int? NbClients { get; set; }
        public int? NbServers { get; set; }
        public double? Network { get; set; }
    }
}