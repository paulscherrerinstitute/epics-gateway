using System;

namespace GWLogger.Backend.DTOs
{
    public enum RestartType : byte
    {
        Unknown = 0,
        WatchdogNoResponse = 1,
        WatchdogCPULimit = 2,
        GatewayUpdate = 3,
        ManualRestart = 4
    }

    public class GatewaySession
    {
        public DateTime? EndDate { get; set; }
        public DateTime? StartDate { get; set; }
        public long NbEntries { get; set; }
        public string Description { get; set; }
        public RestartType RestartType { get; set; }
    }
}