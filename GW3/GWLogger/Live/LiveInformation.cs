using System;
using System.Collections.Generic;
using System.Linq;

namespace GWLogger.Live
{
    public class LiveInformation : IDisposable
    {
        public List<Gateway> Gateways { get; } = new List<Gateway>();
        public EpicsSharp.ChannelAccess.Client.CAClient Client { get; } = new EpicsSharp.ChannelAccess.Client.CAClient();

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

        public void Dispose()
        {
        }
    }
}