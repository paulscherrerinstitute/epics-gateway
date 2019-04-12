using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GWLogger.Backend;

namespace GWLogger.Live
{
    public class LiveInformation : IDisposable
    {
        public List<Gateway> Gateways { get; } = new List<Gateway>();
        public EpicsSharp.ChannelAccess.Client.CAClient Client { get; } = new EpicsSharp.ChannelAccess.Client.CAClient();

        private Thread backgroundUpdater;

        private Dictionary<string, GatewayHistory> historicData = null;


        public LiveInformation()
        {
            if (System.Diagnostics.Debugger.IsAttached)
                Client.Configuration.SearchAddress += ";129.129.194.45:5055";

            Thread taskTread = null;
            var task = Task.Run(() =>
             {
                 taskTread = Thread.CurrentThread;
                 if (File.Exists(Global.HistoryStorage + "\\history.dump"))
                 {
                     try
                     {
                         RecoverHistory(Global.HistoryStorage + "\\history.dump");
                     }
                     catch
                     {
                         try
                         {
                             RecoverHistory(Global.HistoryStorage + "\\history.dump.new");
                         }
                         catch
                         {
                         }
                     }
                 }
                 else if (File.Exists(Global.HistoryStorage + "\\history.dump.new"))
                 {
                     try
                     {
                         RecoverHistory(Global.HistoryStorage + "\\history.dump.new");
                     }
                     catch
                     {
                     }
                 }
             });
            if (!task.Wait(2000))
                taskTread.Abort();

            backgroundUpdater = new Thread(UpdateGateways);
            backgroundUpdater.IsBackground = true;
            backgroundUpdater.Start();
        }

        private void RecoverHistory(string filename)
        {
            using (var stream = new StreamReader(new GZipStream(File.OpenRead(filename), CompressionMode.Decompress)))
            {
                var serializer = Newtonsoft.Json.JsonSerializer.Create();
                historicData = (Dictionary<string, GatewayHistory>)serializer.Deserialize(stream, typeof(Dictionary<string, GatewayHistory>));
            }
        }

        private Dictionary<string, string> RecoverDirectionInventoryInformation()
        {
            try
            {
                var result = Global.Inventory.FindObjects(new Inventory.SearchDefinition
                {
                    Columns = new string[] { "Hostname", "Directions" },
                    Query = new Inventory.SearchCondition[] { new Inventory.SearchCondition { Field = "Part Type", Operator = "is", Value = "CaGateway" } }
                });
                return result.Rows.Where(row => !string.IsNullOrWhiteSpace(row[0])).ToDictionary(key => key[0].ToUpper(), val => val[1]);
            }
            catch
            {

            }
            return null;
        }

        private void UpdateGateways()
        {
            var onErrors = new List<string>();
            var sleepCounter = 0;
            while (true)
            {
                Thread.Sleep(5000);
                List<string> newOnErrors;
                List<Gateway> snapshot;
                lock (Gateways)
                    snapshot = Gateways.ToList();

                snapshot.ForEach(row => row.UpdateGateway());
                newOnErrors = snapshot
                    .Where(row => row.State >= 3)
                    .OrderBy(row => row.Name)
                    .Select(row => row.Name)
                    .ToList();

                // Run this part only every 7 * 5 = 35 seconds
                if (sleepCounter > 6)
                {
                    // Store history dump
                    try
                    {
                        var historyDump = snapshot.ToDictionary(key => key.Name, val => val.GetHistory());
                        using (var stream = new StreamWriter(new GZipStream(File.Create(Global.HistoryStorage + "\\history.dump.new"), CompressionLevel.Fastest)))
                        {
                            var serializer = Newtonsoft.Json.JsonSerializer.Create();
                            serializer.Serialize(stream, historyDump);
                        }
                        File.Copy(Global.HistoryStorage + "\\history.dump.new", Global.HistoryStorage + "\\history.dump", true);
                    }
                    catch
                    {
                    }

                    // Analyze graph data
                    //snapshot.ForEach(row => row.AnalyzeGraphs());

                    sleepCounter = 0;
                }
                else
                    sleepCounter++;

                snapshot.ForEach(row => row.AnalyzeGraphs());

                // Check if we have some gateways on errors
                var emails = new Dictionary<string, string>();
                var subscriptions = AuthAccess.AuthService.GetAllSubscriptions();

                var newEntries = newOnErrors.Where(row => !onErrors.Contains(row));
                foreach (var i in subscriptions.Where(row => row.Gateways.Any(r2 => newEntries.Contains(r2))))
                {
                    if (!emails.ContainsKey(i.EMail))
                        emails.Add(i.EMail, "");
                    emails[i.EMail] += "The following gateway(s) are in error now:\n" + string.Join("\n", newEntries.Where(row => i.Gateways.Contains(row)).Select(row => "- " + row));
                }

                var goneEntries = onErrors.Where(row => !newEntries.Contains(row) && !newOnErrors.Contains(row));
                foreach (var i in subscriptions.Where(row => row.Gateways.Any(r2 => goneEntries.Contains(r2))))
                {
                    if (!emails.ContainsKey(i.EMail))
                        emails.Add(i.EMail, "");
                    emails[i.EMail] += "The following gateway(s) are now back in operation:\n" + string.Join("\n", goneEntries.Where(row => i.Gateways.Contains(row)).Select(row => "- " + row));
                }

                foreach (var i in emails)
                {
                    //SendEmail(i.Key, "CAESAR Update", i.Value);
                }
                onErrors = newOnErrors;
            }
        }

        public List<GraphAnomalyInfo> GetGraphAnomalies()
        {
            List<Gateway> snapshot;
            lock (Gateways)
                snapshot = Gateways.ToList();
            return snapshot
                .SelectMany(gateway => gateway.GetGatewayAnomalies())
                .OrderByDescending(anomaly => anomaly.From)
                .Select(anomaly => new GraphAnomalyInfo {
                    FileName = anomaly.FileName,
                    Name = anomaly.Name,
                    From = anomaly.From,
                    To = anomaly.To,
                })
                .ToList();
        }

        public GraphAnomaly GetGraphAnomaly(string filename)
        {
            List<Gateway> snapshot;
            lock (Gateways)
                snapshot = Gateways.ToList();
            return snapshot
                .SelectMany(gateway => gateway.GetGatewayAnomalies())
                .Where(anomaly => anomaly.FileName == filename)
                .FirstOrDefault();
        }

        public void DeleteGraphAnomaly(string filename)
        {
            throw new NotImplementedException();
        }

        internal static void SendEmail(string destination, string subject, string content)
        {
            using (var mail = new System.Net.Mail.MailMessage(System.Configuration.ConfigurationManager.AppSettings["smtpSenderAddress"], destination))
            {
                mail.Subject = subject;
                mail.Body = content;
                using (var smtp = new System.Net.Mail.SmtpClient(System.Configuration.ConfigurationManager.AppSettings["smtpServer"]))
                    smtp.Send(mail);
            }
        }

        public void Register(string gatewayName)
        {
            lock (Gateways)
            {
                var gw = new Gateway(this, gatewayName);
                if (historicData != null && historicData.ContainsKey(gatewayName))
                    gw.RecoverFromHistory(historicData[gatewayName]);
                Gateways.Add(gw);
            }
        }

        public Gateway this[string key]
        {
            get
            {
                lock (Gateways)
                {
                    return Gateways.FirstOrDefault(row => string.Compare(row.Name, key, true) == 0);
                }
            }
        }

        public List<GatewayShortInformation> GetShortInformation()
        {
            lock (Gateways)
            {
                var ivDirection = RecoverDirectionInventoryInformation();

                return Gateways.Select(row => new GatewayShortInformation
                {
                    Name = row.Name,
                    CPU = row.Cpu,
                    Mem = row.Mem,
                    Searches = row.Searches,
                    Build = row.BuildTime,
                    State = row.State,
                    Version = row.Version,
                    RunningTime = row.RunningTime,
                    Direction = (ivDirection != null && ivDirection.ContainsKey(row.Name.ToUpper())) ? ivDirection[row.Name.ToUpper()] : ""
                }).ToList();
            }
        }

        public GatewayInformation GetGatewayInformation(string gatewayName)
        {
            lock (Gateways)
            {
                return Gateways.Select(row => new GatewayInformation
                {
                    Name = row.Name,
                    CPU = row.Cpu,
                    Mem = row.Mem,
                    Searches = row.Searches,
                    Build = row.BuildTime,
                    Version = row.Version,
                    Messages = row.Messages,
                    PVs = row.PVs,
                    RunningTime = row.RunningTime,
                    NbClients = row.NbClients,
                    NbServers = row.NbServers,
                    Network = row.Network.HasValue ? Math.Round((double)row.Network / (1024 * 1024) * 100.0) / 100.0 : (double?)null,
                    NbCreates = row.NbCreates,
                    NbGets = row.NbGets,
                    NbPuts = row.NbPuts,
                    NbMons = row.NbMons,
                    NbNewMons = row.NbNewMons,
                }).FirstOrDefault(row => row.Name.ToLower() == gatewayName.ToLower());
            }
        }

        public List<HistoricData> CpuHistory(string gatewayName)
        {
            lock (Gateways)
                return Gateways.FirstOrDefault(row => row.Name == gatewayName)?.CpuHistory.Last(Gateway.GraphPoints).ToList();
        }

        public List<HistoricData> SearchHistory(string gatewayName)
        {
            lock (Gateways)
                return Gateways.FirstOrDefault(row => row.Name == gatewayName)?.SearchHistory.Last(Gateway.GraphPoints).ToList();
        }

        public List<HistoricData> PVsHistory(string gatewayName)
        {
            lock (Gateways)
                return Gateways.FirstOrDefault(row => row.Name == gatewayName)?.PvsHistory.Last(Gateway.GraphPoints).ToList();
        }

        public List<HistoricData> NetworkHistory(string gatewayName)
        {
            lock (Gateways)
                return Gateways.FirstOrDefault(row => row.Name == gatewayName)?.NetworkHistory.Last(Gateway.GraphPoints).ToList();
        }

        public void Dispose()
        {
        }
    }
}