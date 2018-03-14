using System.Collections.Generic;

namespace GWLogger.Backend.DTOs
{
    public class GatewayStats
    {
        public List<LogStat> Logs { get; set; }
        public List<LogStat> Searches { get; set; }
        public List<LogStat> Errors { get; set; }
    }
}