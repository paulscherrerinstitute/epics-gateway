﻿using GraphAnomalies;
using GWLogger.Backend;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
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
        private GatewayNullableValue<int> nbGets;
        private GatewayNullableValue<int> nbPuts;
        private GatewayNullableValue<int> nbNewMons;
        private GatewayNullableValue<int> nbMons;
        private GatewayNullableValue<int> nbCreates;
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

        private readonly AnomalyDetector CPUAnomalyDetector = new AnomalyDetector(5, 100); // 500 Points

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
            nbGets = new GatewayNullableValue<int>(this.liveInformation.Client, gatewayName + ":NBCAGET");
            nbPuts = new GatewayNullableValue<int>(this.liveInformation.Client, gatewayName + ":NBCAPUT");
            nbNewMons = new GatewayNullableValue<int>(this.liveInformation.Client, gatewayName + ":NBNEWCAMON");
            nbMons = new GatewayNullableValue<int>(this.liveInformation.Client, gatewayName + ":NBCAMONANSWER");
            nbCreates = new GatewayNullableValue<int>(this.liveInformation.Client, gatewayName + ":NBCREATECHANNEL");
            netIn = new GatewayNullableValue<int>(this.liveInformation.Client, gatewayName + ":NET-IN");
            netOut = new GatewayNullableValue<int>(this.liveInformation.Client, gatewayName + ":NET-OUT");

            CPUAnomalyDetector.AnomalyDetected += CPUAnomalyDetected;

            var anomalyDateFormat = "yyyy-MM-dd-HH-mm-ss";
            var anomalyStorage = Global.AnomalyStorage;
            var anomalySerializer = new XmlSerializer(typeof(GraphAnomaly));
            var dateMatcher = Regex.Replace(anomalyDateFormat, @"([^-\s])", "?");
            AllAnomalies = Directory.EnumerateFiles(anomalyStorage, $"{Name}_{dateMatcher}.xml", SearchOption.TopDirectoryOnly)
                .Select(path => {
                    GraphAnomaly anomaly = null;
                    try
                    {
                        using (var file = File.OpenRead(path))
                        {
                            anomaly = (GraphAnomaly)anomalySerializer.Deserialize(file);
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
                var cpuData = new HistoricData { Value = cpuChannel.Value };
                cpuHistory.Add(cpuData);
                while (cpuHistory.Count > NbHistoricPoint)
                    cpuHistory.RemoveAt(0);
                CPUAnomalyDetector.Update(cpuData.Date, cpuData.Value ?? -1);
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

        private void CPUAnomalyDetected(AnomalyRange range)
        {
            var anomalyDateFormat = "yyyy-MM-dd-HH-mm-ss";
            var anomalyStorage = Global.AnomalyStorage;
            var anomalySerializer = new XmlSerializer(typeof(GraphAnomaly));

            lock (AllAnomaliesLock)
            {
                var anomaly = new GraphAnomaly()
                {
                    From = range.From,
                    To = range.To,
                    Name = Name,
                };

                // Collect data
                var before = range.From.Add(-(range.To - range.From));

                var typeQuery = "select count(*) nb, type group by type order by nb desc";
                var beforeEventTypes = Global.DataContext.ReadLog(Name, before, range.From, typeQuery)?.Select(o => new QueryResultValue(o)).ToList();
                var duringEventTypes = Global.DataContext.ReadLog(Name, range.From, range.To, typeQuery)?.Select(o => new QueryResultValue(o)).ToList();

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
                        var interestingTypeQuery = $"select count(*) nb, remote where type=\"{interestingEventType.Text.Replace("\"", "")}\" group by remote order by nb desc";
                        var groupedRemotes = new Dictionary<string, int>();

                        foreach (var remote in Global.DataContext.ReadLog(Name, range.From, range.To, interestingTypeQuery).Select(o => new QueryResultValue(o)))
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
                                .Select(PerformDNSLookup)
                                .ToList()
                        });
                    }

                    var remoteQuery = "select count(*) nb, remote group by remote order by nb desc";
                    var beforeRemoteCounts = Global.DataContext.ReadLog(Name, before, range.From, remoteQuery).Select(o => new QueryResultValue(o)).ToList();
                    var duringRemoteCounts = Global.DataContext.ReadLog(Name, range.From, range.To, remoteQuery).Select(o => new QueryResultValue(o)).ToList();

                    anomaly.InterestingEventTypeRemotes = interestingEventTypeRemotes;
                    anomaly.BeforeEventTypes = beforeEventTypes.Take(5).ToList();
                    anomaly.DuringEventTypes = duringEventTypes.Take(5).ToList();
                    anomaly.BeforeRemoteCounts = beforeRemoteCounts.Take(5).Select(PerformDNSLookup).ToList();
                    anomaly.DuringRemoteCounts = duringRemoteCounts.Take(5).Select(PerformDNSLookup).ToList();
                }

                anomaly.History = new GatewayHistoricData
                {
                    // We are already in the cpu lock through the anomaly event
                    CPU = cpuHistory.Last(GraphPoints).ToList()
                };

                lock (pvsHistory)
                    anomaly.History.PVs = pvsHistory.Last(GraphPoints).ToList();
                lock (searchHistory)
                    anomaly.History.Searches = searchHistory.Last(GraphPoints).ToList();
                lock (networkHistory)
                    anomaly.History.Network = networkHistory.Last(GraphPoints).ToList();

                var writingFailed = true;
                var failCount = 0;
                do
                {
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
                        failCount++;
                    }
                }
                while (writingFailed && failCount <= 5);

                AllAnomalies.Add(anomaly);
            }
        }

        public List<HistoricData> GetGraphAnomalyPreview(string filename)
        {
            lock (AllAnomaliesLock)
            {
                if (AllAnomalies == null)
                    return new List<HistoricData>();
                var anomaly = AllAnomalies.FirstOrDefault(a => a.FileName == filename) ?? throw new ArgumentException(nameof(filename));
                return anomaly.History.CPU.ToList();
            }
        }

        public void DeleteGraphAnomaly(string filename)
        {
            lock (AllAnomaliesLock)
            {
                var anomaly = AllAnomalies.FirstOrDefault(a => a.FileName == filename) ?? throw new ArgumentException(nameof(filename));
                File.Delete(Path.Combine(Global.AnomalyStorage, $"{anomaly.FileName}.xml"));
                AllAnomalies.Remove(anomaly);
            }
        }

        private QueryResultValue PerformDNSLookup(QueryResultValue value)
        {
            try
            {
                var ip = value.Text?.Trim();
                if (ip == null)
                    return value;
                if (ip.Contains(':'))
                    ip = ip.Split(':')[0];
                var hostEntry = Dns.GetHostEntry(ip);
                if (hostEntry == null)
                    return value;
                var host = hostEntry.HostName;
                if (!string.IsNullOrEmpty(host))
                    value.Text += $" ({host})";
            }
            catch
            {
            }
            return value;
        }

        private List<(DateTime Created, double Value)> PlateauAverage(List<HistoricData> rawData, int groupSize = 10)
        {
            var averaged = new List<(DateTime Created, double Value)>();
            for (int i = 0; i < rawData.Count; i += groupSize)
            {
                double sum = 0;
                var count = 0;
                for (int j = 0; j < groupSize && i + j < rawData.Count; j++)
                {
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
            DateTime? start = entries[0].Created;
            for (int i = 0; i < entries.Count - 1; i++)
            {
                var diff = entries[i + 1].Value - entries[i].Value;

                if (Math.Abs(diff) >= threshold)
                {
                    allRiseOrDrops.Add((entries[i].Created, entries[i + 1].Created));
                    totalDiff = 0;
                }

                var up = diff >= 0;
                if (up != lastUp)
                {
                    lastUp = up;

                    if (start.HasValue && Math.Abs(totalDiff) >= threshold)
                    {
                        allRiseOrDrops.Add((start.Value, entries[i].Created));
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

        public int? NbGets => nbGets.Value;

        public int? NbPuts => nbPuts.Value;

        public int? NbNewMons => nbNewMons.Value;

        public int? NbMons => nbMons.Value;

        public int? NbCreates => nbCreates.Value;
    }
}