using System;
using System.Collections.Generic;
using System.Linq;

namespace GWLogger.Live
{
    [Serializable]
    public class GatewayHistory
    {
        public List<HistoricData> cpuHistory { get; set; }
        public List<HistoricData> searchHistory { get; set; }
        public List<HistoricData> pvsHistory { get; set; }
        public List<HistoricData> msgSecHistory { get; set; }
        public List<HistoricData> clientsHistory { get; set; }
        public List<HistoricData> serversHistory { get; set; }
    }

    public class Gateway
    {
        public const int GraphPoints = 500;
        //private const int NbHistoricPoint = 500;
        private const int NbHistoricPoint = 90000; // 5 days at 5 sec interval
        private const int NbStateAvg = 24;

        private LiveInformation liveInformation;

        public string Name { get; }

        private GatewayNullableValue<double> cpuChannel;
        private GatewayNullableValue<int> memChannel;
        private GatewayNullableValue<int> nbPvs;
        private GatewayNullableValue<int> nbSearches;
        private GatewayNullableValue<int> nbMessages;
        private GatewayNullableValue<int> nbClients;
        private GatewayNullableValue<int> nbServers;
        private GatewayValue<string> runningTime;
        private GatewayValue<string> build;
        private GatewayValue<string> version;
        private List<HistoricData> cpuHistory = new List<HistoricData>();
        private List<HistoricData> searchHistory = new List<HistoricData>();
        private List<HistoricData> pvsHistory = new List<HistoricData>();
        private List<HistoricData> msgSecHistory = new List<HistoricData>();
        private List<HistoricData> clientsHistory = new List<HistoricData>();
        private List<HistoricData> serversHistory = new List<HistoricData>();

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
            nbClients = new GatewayNullableValue<int>(this.liveInformation.Client, gatewayName + ":NBCLIENTS");
            nbServers = new GatewayNullableValue<int>(this.liveInformation.Client, gatewayName + ":NBSERVERS");
        }

        internal GatewayHistory GetHistory()
        {
            return new GatewayHistory
            {
                cpuHistory = CpuHistory,
                clientsHistory = NbClientsHistory,
                msgSecHistory = MsgSecHistory,
                pvsHistory = PvsHistory,
                searchHistory = SearchHistory,
                serversHistory = serversHistory
            };
        }

        internal void RecoverFromHistory(GatewayHistory history)
        {
            lock (cpuHistory)
            {
                cpuHistory.Clear();
                cpuHistory.AddRange(history.cpuHistory);
                while (cpuHistory.Any() && (DateTime.UtcNow - cpuHistory.Last().Date).TotalSeconds > 5)
                {
                    cpuHistory.Add(new HistoricData { Value = null, Date = cpuHistory.Last().Date.AddSeconds(5) });
                    while (cpuHistory.Count > NbHistoricPoint)
                        cpuHistory.RemoveAt(0);
                }
            }

            lock (searchHistory)
            {
                searchHistory.Clear();
                searchHistory.AddRange(history.searchHistory);
                while (searchHistory.Any() && (DateTime.UtcNow - searchHistory.Last().Date).TotalSeconds > 5)
                { 
                    searchHistory.Add(new HistoricData { Value = null, Date = searchHistory.Last().Date.AddSeconds(5) });
                    while (searchHistory.Count > NbHistoricPoint)
                        searchHistory.RemoveAt(0);
                }
            }

            lock (pvsHistory)
            {
                pvsHistory.Clear();
                pvsHistory.AddRange(history.pvsHistory);
                while (pvsHistory.Any() && (DateTime.UtcNow - pvsHistory.Last().Date).TotalSeconds > 5)
                { 
                    pvsHistory.Add(new HistoricData { Value = null, Date = pvsHistory.Last().Date.AddSeconds(5) });
                    while (pvsHistory.Count > NbHistoricPoint)
                        pvsHistory.RemoveAt(0);
                }
            }

            lock (clientsHistory)
            {
                clientsHistory.Clear();
                clientsHistory.AddRange(history.clientsHistory);
                while (clientsHistory.Any() && (DateTime.UtcNow - clientsHistory.Last().Date).TotalSeconds > 5)
                { 
                    clientsHistory.Add(new HistoricData { Value = null, Date = clientsHistory.Last().Date.AddSeconds(5) });
                    while (clientsHistory.Count > NbHistoricPoint)
                        clientsHistory.RemoveAt(0);
                }
            }

            lock (serversHistory)
            {
                serversHistory.Clear();
                serversHistory.AddRange(history.serversHistory);
                while (serversHistory.Any() && (DateTime.UtcNow - serversHistory.Last().Date).TotalSeconds > 5)
                { 
                    serversHistory.Add(new HistoricData { Value = null, Date = serversHistory.Last().Date.AddSeconds(5) });
                    while (serversHistory.Count > NbHistoricPoint)
                        serversHistory.RemoveAt(0);
                }
            }

            lock (msgSecHistory)
            {
                msgSecHistory.Clear();
                msgSecHistory.AddRange(history.msgSecHistory);
                while (msgSecHistory.Any() && (DateTime.UtcNow - msgSecHistory.Last().Date).TotalSeconds > 5)
                { 
                    msgSecHistory.Add(new HistoricData { Value = null, Date = msgSecHistory.Last().Date.AddSeconds(5) });
                    while (msgSecHistory.Count > NbHistoricPoint)
                        msgSecHistory.RemoveAt(0);
                }
            }
        }

        internal void UpdateGateway()
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

            lock (clientsHistory)
            {
                clientsHistory.Add(new HistoricData { Value = nbClients.Value });
                while (clientsHistory.Count > NbHistoricPoint)
                    clientsHistory.RemoveAt(0);
            }

            lock (serversHistory)
            {
                serversHistory.Add(new HistoricData { Value = nbServers.Value });
                while (serversHistory.Count > NbHistoricPoint)
                    serversHistory.RemoveAt(0);
            }

            lock (msgSecHistory)
            {
                msgSecHistory.Add(new HistoricData { Value = nbMessages.Value });
                while (msgSecHistory.Count > NbHistoricPoint)
                    msgSecHistory.RemoveAt(0);
            }
        }

        public int State => Math.Max(CpuState, SearchState);

        /// <summary>
        /// Average PVs in the last 10 minutes
        /// </summary>
        public int AvgPvs
        {
            get
            {
                try
                {
                    return (int)(((IEnumerable<HistoricData>)PvsHistory).Reverse().Take(120).Where(row => row.Value.HasValue).Select(row => row.Value).Average());
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Average Searches in the last 10 minutes
        /// </summary>
        public int AvgSearches
        {
            get
            {
                try
                {
                    return (int)(((IEnumerable<HistoricData>)SearchHistory).Reverse().Take(120).Where(row => row.Value.HasValue).Select(row => row.Value).Average());
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Average CPU in the last 10 minutes
        /// </summary>
        public int AvgCPU
        {
            get
            {
                try
                {
                    return (int)(((IEnumerable<HistoricData>)CpuHistory).Reverse().Take(120).Where(row => row.Value.HasValue).Select(row => row.Value).Average());
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Average Clients in the last 10 minutes
        /// </summary>
        public int AvgNbClients
        {
            get
            {
                try
                {
                    return (int)(((IEnumerable<HistoricData>)NbClientsHistory).Reverse().Take(120).Where(row => row.Value.HasValue).Select(row => row.Value).Average());
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Average Servers in the last 10 minutes
        /// </summary>
        public int AvgNbServers
        {
            get
            {
                try
                {
                    return (int)(((IEnumerable<HistoricData>)NbServersHistory).Reverse().Take(120).Where(row => row.Value.HasValue).Select(row => row.Value).Average());
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Average Messages Per Seconds in the last 10 minutes
        /// </summary>
        public int AvgMsgSec
        {
            get
            {
                try
                {
                    return (int)(((IEnumerable<HistoricData>)MsgSecHistory).Reverse().Take(120).Where(row => row.Value.HasValue).Select(row => row.Value).Average());
                }
                catch
                {
                    return 0;
                }
            }
        }

        public int CpuState
        {
            get
            {
                // We need to wait till we have enough data before sending an error
                if (CpuHistory.Count < NbStateAvg)
                    return 0;
                var lasts = ((IEnumerable<HistoricData>)CpuHistory).Reverse().Take(NbStateAvg);
                if (!(lasts.First().Value.HasValue || lasts.Count(l => l.Value.HasValue) >= lasts.Count() / 2))
                    return 3;
                try
                {
                    var avg = lasts.Where(row => row.Value.HasValue).Average(row => row.Value.Value);
                    if (avg > 70)
                        return 3;
                    if (avg > 50)
                        return 2;
                    if (avg > 30)
                        return 1;
                    return 0;
                }
                catch
                {
                    return 3;
                }
            }
        }

        public int SearchState
        {
            get
            {
                // We need to wait till we have enough data before sending an error
                if (SearchHistory.Count < NbStateAvg)
                    return 0;
                var lasts = ((IEnumerable<HistoricData>)SearchHistory).Reverse().Take(NbStateAvg);
                if (!(lasts.First().Value.HasValue || lasts.Count(l => l.Value.HasValue) >= lasts.Count() / 2))
                    return 3;
                try
                {
                    var avg = lasts.Where(row => row.Value.HasValue).Average(row => Math.Min(150, row.Value.Value));
                    if (avg > 290)
                        return 3;
                    if (avg > 190)
                        return 2;
                    if (avg > 90)
                        return 1;
                    return 0;
                }
                catch
                {
                    return 3;
                }
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

        public List<HistoricData> NbClientsHistory
        {
            get
            {
                lock (clientsHistory)
                    return clientsHistory.ToList();
            }
        }

        public List<HistoricData> NbServersHistory
        {
            get
            {
                lock (serversHistory)
                    return serversHistory.ToList();
            }
        }

        public List<HistoricData> MsgSecHistory
        {
            get
            {
                lock (msgSecHistory)
                    return msgSecHistory.ToList();
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

        public int? NbClients => nbClients.Value;

        public int? NbServers => nbServers.Value;
    }
}