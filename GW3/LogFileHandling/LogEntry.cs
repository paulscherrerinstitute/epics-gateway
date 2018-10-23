using System;
using System.Collections.Generic;

namespace GWLogger.Backend.DataContext
{
    public class LogEntry
    {
        public string CurrentFile { get; set; }
        public DateTime EntryDate { get; set; }
        public string Gateway { get; set; }
        public List<LogEntryDetail> LogEntryDetails { get; set; }
        public int MessageTypeId { get; set; }
        public long Position { get; set; }
        public string RemoteIpPoint { get; set; }
    }
}