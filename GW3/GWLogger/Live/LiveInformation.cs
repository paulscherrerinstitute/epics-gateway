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
            backgroundUpdater = new Thread(UpdateGraphValues);
            backgroundUpdater.IsBackground = true;
            backgroundUpdater.Start();
        }

        private void UpdateGraphValues()
        {
            while (true)
            {
                Thread.Sleep(5000);
                lock (Gateways)
                    Gateways.ForEach(row => row.UpdateGraph());
            }
        }

        public void Register(string gatewayName)
        {
            lock (Gateways)
            {
                Gateways.Add(new Gateway(this, gatewayName));
            }
        }

        public List<GatewayShortInformation> GetShortInformation()
        {
            lock (Gateways)
            {
                return Gateways.Select(row => new GatewayShortInformation
                {
                    Name = row.Name,
                    Cpu = row.Cpu,
                    Mem = row.Mem,
                    Searches = row.Searches,
                    Build = row.BuildTime
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
                    Cpu = row.Cpu,
                    Mem = row.Mem,
                    Searches = row.Searches,
                    Build = row.BuildTime,
                    Messages = row.Messages,
                    PVs = row.PVs,
                    RunningTime = row.RunningTime
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