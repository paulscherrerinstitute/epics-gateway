using System;
using System.Collections.Generic;
using System.Linq;

namespace GWLogger.Live
{
    public class Gateway
    {
        private const int NbHistoricPoint = 500;
        private const int NbStateAvg = 20;

        private LiveInformation liveInformation;

        public string Name { get; }

        private GatewayNullableValue<double> cpuChannel;
        private GatewayNullableValue<int> memChannel;
        private GatewayNullableValue<int> nbPvs;
        private GatewayNullableValue<int> nbSearches;
        private GatewayNullableValue<int> nbMessages;
        private GatewayValue<string> runningTime;
        private GatewayValue<string> build;
        private GatewayValue<string> version;
        private List<HistoricData> cpuHistory = new List<HistoricData>();
        private List<HistoricData> searchHistory = new List<HistoricData>();
        private List<HistoricData> pvsHistory = new List<HistoricData>();

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
            version = new GatewayValue<string>(this.liveInformation.Client, gatewayName + ":VERSION");
        }

        internal void UpdateGraph()
        {
            lock (cpuHistory)
            {
                cpuHistory.Add(new HistoricData { Value = cpuChannel.Value });
                while (cpuHistory.Count > NbHistoricPoint)
                    cpuHistory.RemoveAt(0);
            }

            lock (searchHistory)
            {
                searchHistory.Add(new HistoricData { Value = nbSearches.Value });
                while (searchHistory.Count > NbHistoricPoint)
                    searchHistory.RemoveAt(0);
            }

            lock (pvsHistory)
            {
                pvsHistory.Add(new HistoricData { Value = nbPvs.Value });
                while (pvsHistory.Count > NbHistoricPoint)
                    pvsHistory.RemoveAt(0);
            }
        }

        public int State => Math.Max(CpuState, SearchState);

        public int CpuState
        {
            get
            {
                if (CpuHistory.Count == 0)
                    return 0;
                var lasts = ((IEnumerable<HistoricData>)CpuHistory).Reverse().Take(NbStateAvg);
                if (!(lasts.First().Value.HasValue || lasts.Count(l => l.Value.HasValue) >= lasts.Count() / 2))
                    return 3;
                var avg = lasts.Where(row => row.Value.HasValue).Average(row => row.Value.Value);
                if (avg > 70)
                    return 3;
                if (avg > 50)
                    return 2;
                if (avg > 30)
                    return 1;
                return 0;
            }
        }

        public int SearchState
        {
            get
            {
                if (CpuHistory.Count == 0)
                    return 0;
                var lasts = ((IEnumerable<HistoricData>)SearchHistory).Reverse().Take(NbStateAvg);
                if (!(lasts.First().Value.HasValue || lasts.Count(l => l.Value.HasValue) >= lasts.Count() / 2))
                    return 3;
                var avg = lasts.Where(row => row.Value.HasValue).Average(row => Math.Min(150, row.Value.Value));
                if (avg > 90)
                    return 3;
                if (avg > 50)
                    return 2;
                if (avg > 30)
                    return 1;
                return 0;
            }
        }

        public List<HistoricData> CpuHistory
        {
            get
            {
                lock (cpuHistory)
                    return cpuHistory.ToList();
            }
        }

        public List<HistoricData> SearchHistory
        {
            get
            {
                lock (searchHistory)
                    return searchHistory.ToList();
            }
        }

        public List<HistoricData> PvsHistory
        {
            get
            {
                lock (pvsHistory)
                    return pvsHistory.ToList();
            }
        }

        public double? Cpu => cpuChannel.Value;

        public int? Mem => memChannel.Value;

        public int? PVs => nbPvs.Value;

        public int? Searches => nbSearches.Value;

        public int? Messages => nbMessages.Value;

        public string RunningTime => runningTime.Value;

        public string BuildTime => build.Value;

        public string Version => version.Value;
    }
}