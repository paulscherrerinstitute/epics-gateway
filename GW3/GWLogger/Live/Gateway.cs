namespace GWLogger.Live
{
    public class Gateway
    {
        private LiveInformation liveInformation;

        public string Name { get; }

        private GatewayNullableValue<double> cpuChannel;
        private GatewayNullableValue<int> memChannel;
        private GatewayNullableValue<int> nbPvs;
        private GatewayNullableValue<int> nbSearches;
        private GatewayNullableValue<int> nbMessages;
        private GatewayValue<string> runningTime;
        private GatewayValue<string> build;

        public Gateway(LiveInformation liveInformation, string gatewayName)
        {
            this.liveInformation = liveInformation;
            Name = gatewayName;
            cpuChannel = new GatewayNullableValue<double>(this.liveInformation.Client, gatewayName + ":CPU");
            memChannel = new GatewayNullableValue<int>(this.liveInformation.Client, gatewayName + ":MEM-FREE");
            nbPvs = new GatewayNullableValue<int>(this.liveInformation.Client, gatewayName + ":PVTOTAL");
            nbSearches = new GatewayNullableValue<int>(this.liveInformation.Client, gatewayName + ":SEARCH-SEC");
            nbMessages = new GatewayNullableValue<int>(this.liveInformation.Client, gatewayName + ":MESSAGES-SEC");
            runningTime = new GatewayValue<string>(this.liveInformation.Client, gatewayName + ":RUNNING-TIME");
            build = new GatewayValue<string>(this.liveInformation.Client, gatewayName + ":BUILD");
        }

        public double? Cpu => cpuChannel.Value;

        public int? Mem => memChannel.Value;

        public int? PVs => nbPvs.Value;

        public int? Searches => nbSearches.Value;

        public int? Messages => nbMessages.Value;

        public string RunningTime => runningTime.Value;

        public string BuildTime => build.Value;
    }
}