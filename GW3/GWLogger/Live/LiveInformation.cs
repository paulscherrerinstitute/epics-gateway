using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GWLogger.Live
{
    public class LiveInformation : IDisposable
    {
        public List<Gateway> Gateways { get; } = new List<Gateway>();
        public EpicsSharp.ChannelAccess.Client.CAClient Client { get; } = new EpicsSharp.ChannelAccess.Client.CAClient();

        private Thread backgroundUpdater;

        public LiveInformation()
        {
            backgroundUpdater = new Thread(UpdateGateways);
            backgroundUpdater.IsBackground = true;
            backgroundUpdater.Start();
        }

        private void UpdateGateways()
        {
            var onErrors = new List<string>();
            while (true)
            {
                Thread.Sleep(5000);
                List<string> newOnErrors;
                lock (Gateways)
                {
                    Gateways.ForEach(row => row.UpdateGateway());
                    newOnErrors = Gateways.Where(row => row.State >= 3).OrderBy(row => row.Name).Select(row => row.Name).ToList();
                }

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
                Gateways.Add(new Gateway(this, gatewayName));
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
                return Gateways.Select(row => new GatewayShortInformation
                {
                    Name = row.Name,
                    CPU = row.Cpu,
                    Mem = row.Mem,
                    Searches = row.Searches,
                    Build = row.BuildTime,
                    State = row.State,
                    Version = row.Version,
                    RunningTime = row.RunningTime
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
                return Gateways.FirstOrDefault(row => row.Name == gatewayName)?.CpuHistory;
        }

        public List<HistoricData> SearchHistory(string gatewayName)
        {
            lock (Gateways)
                return Gateways.FirstOrDefault(row => row.Name == gatewayName)?.SearchHistory;
        }

        public List<HistoricData> PVsHistory(string gatewayName)
        {
            lock (Gateways)
                return Gateways.FirstOrDefault(row => row.Name == gatewayName)?.PvsHistory;
        }

        public void Dispose()
        {
        }
    }
}