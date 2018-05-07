using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GWLogger.Backend.DataContext
{
    public class LogEntry
    {
        public string Gateway { get; set; }
        public int MessageTypeId { get; set; }
        public DateTime EntryDate { get; set; }
        public string RemoteIpPoint { get; set; }
        public List<LogEntryDetail> LogEntryDetails { get; set; }
    }
}
