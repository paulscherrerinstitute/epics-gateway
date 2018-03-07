using System.Collections.Generic;

namespace GWLogger.Backend.DTOs
{
    public class LogEntry
    {
        public string Gateway { get; set; }
        public string RemoteIpPoint { get; set; }
        public int MessageType { get; set; }
        public List<LogEntryDetail> Details { get; set; }
    }
}