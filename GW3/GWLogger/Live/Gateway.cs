using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

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
        public List<HistoricData> networkHistory { get; set; }
    }

    public class Gateway
    {
        public const int GraphPoints = 500;
        //private const int NbHistoricPoint = 500;
        //private const int NbHistoricPoint = 90000; // 5 days at 5 sec interval
        private const int NbHistoricPoint = 5000;
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
        private GatewayNullableValue<int> netIn;
        private GatewayNullableValue<int> netOut;
        private GatewayValue<string> runningTime;
        private GatewayValue<string> build;
        private GatewayValue<string> version;
        private List<HistoricData> cpuHistory = new List<HistoricData>();
        private List<HistoricData> searchHistory = new List<HistoricData>();
        private List<HistoricData> pvsHistory = new List<HistoricData>();
        private List<HistoricData> msgSecHistory = new List<HistoricData>();
        private List<HistoricData> clientsHistory = new List<HistoricData>();
        private List<HistoricData> serversHistory = new List<HistoricData>();
        private List<HistoricData> networkHistory = new List<HistoricData>();

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
            netIn = new GatewayNullableValue<int>(this.liveInformation.Client, gatewayName + ":NET-IN");
            netOut = new GatewayNullableValue<int>(this.liveInformation.Client, gatewayName + ":NET-OUT");
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
                serversHistory = ServersHistory,
                networkHistory = NetworkHistory
            };
        }

        internal void RecoverFromHistory(GatewayHistory history)
        {
            // We have way too many points to recover => useless.
            if ((DateTime.UtcNow - history.cpuHistory.Last().Date).TotalSeconds / 5 > NbHistoricPoint)
                return;
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

            lock(networkHistory)
            {
                networkHistory.Clear();
                networkHistory.AddRange(history.networkHistory);
                while (networkHistory.Any() && (DateTime.UtcNow - networkHistory.Last().Date).TotalSeconds > 5)
                {
                    networkHistory.Add(new HistoricData { Value = null, Date = networkHistory.Last().Date.AddSeconds(5) });
                    while (networkHistory.Count > NbHistoricPoint)
                        networkHistory.RemoveAt(0);
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

            lock (networkHistory)
            {
                networkHistory.Add(new HistoricData { Value = netIn.Value + netOut.Value });
                while (networkHistory.Count > NbHistoricPoint)
                    networkHistory.RemoveAt(0);
            }
        }

        public int State => Math.Max(CpuState, SearchState);

        private readonly object AllAnomaliesLock = new object();
        private List<GraphAnomaly> AllAnomalies = null;

        internal void AnalyzeGraphs()
        {
            var anomalyDateFormat = "yyyy-MM-dd-HH-mm-ss";
            var anomalyStorage = Global.AnomalyStorage;
            var anomalySerializer = new XmlSerializer(typeof(GraphAnomaly));

            // Double-Checked locking
            if(AllAnomalies == null)
            {
                lock (AllAnomaliesLock)
                {
                    if(AllAnomalies == null)
                    {
                        var dateMatcher = Regex.Replace(anomalyDateFormat, @"([^-\s])", "?");
                        AllAnomalies = Directory.EnumerateFiles(anomalyStorage, $"{Name}_{dateMatcher}.xml", SearchOption.TopDirectoryOnly)
                            .Select(path => {
                                GraphAnomaly anomaly = null;
                                try
                                {
                                    using (var file = File.OpenRead(path))
                                    {
                                        anomaly = (GraphAnomaly)anomalySerializer.Deserialize(file);
                                        anomaly.IsDirty = false;
                                        anomaly.FileName = Path.GetFileNameWithoutExtension(path);
                                        anomaly.Name = Name;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine("Exception while reading anomaly xml file: " + path);
                                    Debug.WriteLine(ex.GetType().Name + ": " + ex.Message);
                                }
                                return anomaly;
                            })
                            .Where(anomaly => anomaly != null)
                            .OrderByDescending(anomaly => anomaly.From)
                            .ToList();
                    }
                }
            }

            List<(DateTime Created, double Value)> cpuAvg;
            lock (cpuHistory)
                cpuAvg = PlateauAverage(cpuHistory);

            var cpuAnomalies = FindAnomalies(cpuAvg, threshold: 5)
                .Where(c => c.From > Global.ApplicationStartUtc) // Only handle events that happened after application start
                .ToList();

            if (cpuAnomalies.Count == 0)
                return;

            lock (AllAnomaliesLock)
            {
                foreach (var (From, To) in cpuAnomalies)
                {
                    if (From <= Global.ApplicationStartUtc)
                        continue; // Skip events that occured before application was started

                    // Try to get an already existing anomaly in this range
                    var anomaly = AllAnomalies.FirstOrDefault(a =>  From >= a.From && From <= a.To);

                    if(anomaly == null)
                        anomaly = new GraphAnomaly { From = From, To = To, Name = Name, IsDirty = true }; // Create a new anomaly
                    else
                        anomaly.To = To; // Combine the anomalies into one 

                    // Skip querying and storing of data if the datetime range is still the same
                    if (!anomaly.IsDirty)
                        continue;

                    // Collect data
                    var before = From.Add(-(To - From));

                    var typeQuery = "select count(*) nb, type group by type order by nb desc";
                    var beforeEventTypes = Global.DataContext.ReadLog(Name, before, From, typeQuery)?.Select(o => new QueryResultValue(o)).ToList();
                    var duringEventTypes = Global.DataContext.ReadLog(Name, From, To, typeQuery)?.Select(o => new QueryResultValue(o)).ToList();

                    // Don't try to analyze if there is no log data available
                    if (beforeEventTypes != null && duringEventTypes != null)
                    {
                        var typeDifferences = beforeEventTypes
                        .Concat(duringEventTypes)
                        .GroupBy(g => g.Text)
                        .Select(g => new QueryResultValue
                        {
                            Text = g.Key,
                            Value = g.Max(q => q.Value) - g.Min(q => q.Value)
                        });

                        var interestingEventTypes = typeDifferences
                            .OrderByDescending(v => v.Value)
                            .Take(5)
                            .ToList();

                        var interestingEventTypeRemotes = new List<InterestingEventType>();

                        foreach (var interestingEventType in interestingEventTypes)
                        {
                            var interestingTypeQuery = $"select count(*) nb, remote where type=\"{interestingEventType.Text.Replace("\"","")}\" group by remote order by nb desc";
                            var groupedRemotes = new Dictionary<string, int>();

                            foreach (var remote in Global.DataContext.ReadLog(Name, From, To, interestingTypeQuery).Select(o => new QueryResultValue(o)))
                            {
                                var host = remote.Text.Split(':').First();
                                if (groupedRemotes.TryGetValue(host, out int prev))
                                    groupedRemotes[host] = prev + (int)remote.Value;
                                else
                                    groupedRemotes.Add(host, (int)remote.Value);
                            }

                            interestingEventTypeRemotes.Add(new InterestingEventType
                            {
                                EventType = interestingEventType,
                                TopRemotes = groupedRemotes
                                    .OrderByDescending(g => g.Value)
                                    .Take(3)
                                    .Select(r => new QueryResultValue { Text = r.Key, Value = r.Value })
                                    .ToList()
                            });
                        }

                        var remoteQuery = "select count(*) nb, remote group by remote order by nb desc";
                        var beforeRemoteCounts = Global.DataContext.ReadLog(Name, before, From, remoteQuery).Select(o => new QueryResultValue(o)).ToList();
                        var duringRemoteCounts = Global.DataContext.ReadLog(Name, From, To, remoteQuery).Select(o => new QueryResultValue(o)).ToList();

                        anomaly.InterestingEventTypeRemotes = interestingEventTypeRemotes;
                        anomaly.BeforeEventTypes = beforeEventTypes.Take(5).ToList();
                        anomaly.DuringEventTypes = duringEventTypes.Take(5).ToList();
                        anomaly.BeforeRemoteCounts = beforeRemoteCounts.Take(5).ToList();
                        anomaly.DuringRemoteCounts = duringRemoteCounts.Take(5).ToList();
                    }

                    anomaly.History = GetHistory();

                    var writingFailed = true;
                    try
                    {
                        var filename = $"{Name}_{anomaly.From.ToString(anomalyDateFormat, CultureInfo.InvariantCulture)}";
                        using (var xmlWriter = XmlWriter.Create(File.Open(Path.Combine(anomalyStorage, $"{filename}.xml"), FileMode.Create), new XmlWriterSettings { Indent = false }))
                        {
                            anomaly.FileName = filename;
                            anomalySerializer.Serialize(xmlWriter, anomaly);
                        }
                        writingFailed = false;
                    }
                    catch
                    {
                    }

                    anomaly.IsDirty = writingFailed;
                    AllAnomalies.Add(anomaly);
                }
            }
        }

        private List<(DateTime Created, double Value)> PlateauAverage(List<HistoricData> rawData, int groupSize = 10)
        {
            var averaged = new List<(DateTime Created, double Value)>();
            for (int i = 0; i < rawData.Count; i += groupSize)
            {
                double sum = 0;
                var count = 0;
                var lastIndex = 0;
                for (int j = 0; j < groupSize && i + j < rawData.Count; j++)
                {
                    lastIndex = i + j;
                    sum += rawData[i + j].Value ?? -1;
                    count++;
                }
                var avgValue = Math.Round(sum / count, 1, MidpointRounding.AwayFromZero);
                averaged.Add((rawData[i].Date, avgValue));
            }
            return averaged;
        }

        private List<(DateTime From, DateTime To)> FindAnomalies(List<(DateTime Created, double Value)> entries, double threshold)
        {
            var allRiseOrDrops = new List<(DateTime From, DateTime To)>();
            var totalDiff = 0.0;
            var lastUp = true;
            int? lastEnd = null;
            DateTime? start = entries[0].Created;
            for (int i = 0; i < entries.Count - 1; i++)
            {
                var diff = entries[i + 1].Value - entries[i].Value;

                if (Math.Abs(diff) >= threshold)
                {
                    allRiseOrDrops.Add((entries[i].Created, entries[i + 1].Created));
                    lastEnd = i + 1;
                    totalDiff = 0;
                }

                var up = diff >= 0;
                if (up != lastUp)
                {
                    lastUp = up;

                    if (start.HasValue && Math.Abs(totalDiff) >= threshold)
                    {
                        allRiseOrDrops.Add((start.Value, entries[i].Created));
                        lastEnd = i;
                    }

                    start = entries[i].Created;
                    totalDiff = 0;
                }

                totalDiff += diff;
            }

            return CombineOverlappingRanges(allRiseOrDrops);
        }

        private List<(DateTime, DateTime)> CombineOverlappingRanges(List<(DateTime From, DateTime To)> ranges)
        {
            var combined = new List<(DateTime, DateTime)>();
            for (int i = 0; i < ranges.Count - 1; i++)
            {
                var startOfRange = ranges[i].From;
                while (i < ranges.Count - 1 && ranges[i + 1].From <= ranges[i].To)
                    i++;
                combined.Add((startOfRange, ranges[i].To));
            }
            return combined;
        }

        public List<GraphAnomaly> GetGatewayAnomalies()
        {
            lock (AllAnomaliesLock)
                return AllAnomalies?.ToList() ?? new List<GraphAnomaly>();
        }

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
                    if (avg > 3000)
                        return 3;
                    if (avg > 2000)
                        return 2;
                    if (avg > 1000)
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

        public List<HistoricData> ServersHistory
        {
            get
            {
                lock (serversHistory)
                    return serversHistory.ToList();
            }
        }

        public List<HistoricData> NetworkHistory
        {
            get
            {
                lock (networkHistory)
                    return networkHistory.ToList();
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

        public int? Network => netIn.Value + netOut.Value;
    }
}