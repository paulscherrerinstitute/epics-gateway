using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GWLogger.Backend.DataContext
{
    class DataContext : IDisposable
    {
        DataFiles files = new DataFiles();

        public void Save(LogEntry entry)
        {
            files[entry.Gateway].Save(entry);
        }

        public List<LogEntry> ReadLog(string gatewayName, DateTime start, DateTime end)
        {
            return files[gatewayName].ReadLog(start, end);
        }

        public List<LogSession> ReadClientSessions(string gatewayName, DateTime start, DateTime end)
        {
            return files[gatewayName].ReadClientSessions(start, end);
        }

        public List<LogSession> ReadServerSessions(string gatewayName, DateTime start, DateTime end)
        {
            return files[gatewayName].ReadServerSessions(start, end);
        }

        public List<SearchEntry> ReadSearches(string gatewayName, DateTime start, DateTime end)
        {
            return files[gatewayName].ReadSearches(start, end);
        }

        public void Flush()
        {
            files.Flush();
        }

        public void Dispose()
        {
            files.Dispose();
        }
    }
}
