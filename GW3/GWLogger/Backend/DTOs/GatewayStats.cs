using System.Collections.Generic;

namespace GWLogger.Backend.DTOs
{
    public class GatewayStats
    {
        public List<LogStat> Logs { get; set; }
        public List<LogStat> Searches { get; set; }
        public List<LogStat> Errors { get; set; }
        public List<LogStat> CPU { get; set; }
        public List<LogStat> PVs { get; set; }
        public List<LogStat> Clients { get; set; }
        public List<LogStat> Servers { get; set; }
    }
}