using GWLogger.Backend;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

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
                 if (File.Exists(Global.StorageDirectory + "\\history.dump"))
                 {
                     try
                     {
                         RecoverHistory(Global.StorageDirectory + "\\history.dump");
                     }
                     catch
                     {
                         try
                         {
                             RecoverHistory(Global.StorageDirectory + "\\history.dump.new");
                         }
                         catch
                         {
                         }
                     }
                 }
                 else if (File.Exists(Global.StorageDirectory + "\\history.dump.new"))
                 {
                     try
                     {
                         RecoverHistory(Global.StorageDirectory + "\\history.dump.new");
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
            using (var stream = File.OpenRead(filename))
            {
                var formatter = new BinaryFormatter();
                formatter.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
                historicData = (Dictionary<string, GatewayHistory>)formatter.Deserialize(stream);
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
            catch (Exception ex)
            {

            }
            return null;
        }

        private void UpdateGateways()
        {
            var onErrors = new List<string>();
            while (true)
            {
                Thread.Sleep(5000);
                List<string> newOnErrors;
                Dictionary<string, GatewayHistory> historyDump;
                lock (Gateways)
                {
                    Gateways.ForEach(row => row.UpdateGateway());
                    newOnErrors = Gateways.Where(row => row.State >= 3).OrderBy(row => row.Name).Select(row => row.Name).ToList();
                    historyDump = Gateways.ToDictionary(key => key.Name, val => val.GetHistory());
                }


                try
                {
                    // Store historyDump
                    using (var stream = File.OpenWrite(Global.StorageDirectory + "\\history.dump.new"))
                    {
                        var formatter = new BinaryFormatter();
                        formatter.Serialize(stream, historyDump);
                    }
                    File.Copy(Global.StorageDirectory + "\\history.dump.new", Global.StorageDirectory + "\\history.dump", true);
                }
                catch
                {
                }
                /*if (File.Exists(Global.StorageDirectory + "\\history.dump"))
                    File.Delete(Global.StorageDirectory + "\\history.dump");
                File.Move(Global.StorageDirectory + "\\history.dump.new", Global.StorageDirectory + "\\history.dump");*/

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

                var goneEntries = onErrors.Where(row => !newEntries.Contains(row));
                foreach (var i in subscriptions.Where(row => row.Gateways.Any(r2 => goneEntries.Contains(r2))))
                {
                    if (!emails.ContainsKey(i.EMail))
                        emails.Add(i.EMail, "");
                    emails[i.EMail] += "The following gateway(s) are now back in operation:\n" + string.Join("\n", goneEntries.Where(row => i.Gateways.Contains(row)).Select(row => "- " + row));
                }

                foreach (var i in emails)
                {
                    SendEmail(i.Key, "CAESAR Update", i.Value);
                }
                onErrors = newOnErrors;
            }
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
                    NbServers = row.NbServers
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

        public void Dispose()
        {
        }
    }
}