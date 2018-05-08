using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using GWLogger.Backend.DTOs;

namespace GWLogger.Backend.DataContext
{
    public class Context : IDisposable
    {
        DataFiles files = new DataFiles();

        public Context()
        {
            try
            {
                using (var stream = File.OpenRead(DataFile.StorageDirectory + "\\MessageTypes.xml"))
                {
                    XmlSerializer ser = new XmlSerializer(typeof(List<DTOs.MessageType>));
                    var v = (List<DTOs.MessageType>)ser.Deserialize(stream);
                    messageTypes.Clear();
                    messageTypes.AddRange(v);
                }
            }
            catch
            {
            }
            try
            {
                using (var stream = File.OpenRead(DataFile.StorageDirectory + "\\MessageDetails.xml"))
                {
                    XmlSerializer ser = new XmlSerializer(typeof(List<IdValue>));
                    var v = (List<IdValue>)ser.Deserialize(stream);
                    messageDetailTypes.Clear();
                    messageDetailTypes.AddRange(v);
                }
            }
            catch
            {
            }
        }

        public void Save(LogEntry entry)
        {
            files[entry.Gateway].Save(entry);
        }

        internal List<string> Gateways
        {
            get
            {
                return DataFile.Gateways;
            }
        }

        List<DTOs.MessageType> messageTypes = new List<DTOs.MessageType>();
        public List<DTOs.MessageType> MessageTypes
        {
            get
            {
                lock (messageTypes)
                {
                    return messageTypes.ToList();
                }
            }
            set
            {
                lock (messageTypes)
                {
                    // There is some changes
                    if (messageTypes.Any(row => !value.Select(r2 => r2.Id).Contains(row.Id)) ||
                        value.Any(row => !messageTypes.Select(r2 => r2.Id).Contains(row.Id)))
                    {
                        using (var stream = File.OpenWrite(DataFile.StorageDirectory + "\\MessageTypes.xml"))
                        {
                            XmlSerializer ser = new XmlSerializer(typeof(List<DTOs.MessageType>));
                            ser.Serialize(stream, value);
                        }
                        messageTypes.Clear();
                        messageTypes.AddRange(value);
                    }
                }
            }
        }

        List<IdValue> messageDetailTypes = new List<IdValue>();
        public List<IdValue> MessageDetailTypes
        {
            get
            {
                lock (messageDetailTypes)
                {
                    return messageDetailTypes.ToList();
                }
            }
            set
            {
                if (messageDetailTypes.Any(row => !value.Select(r2 => r2.Id).Contains(row.Id)) ||
                    value.Any(row => !messageDetailTypes.Select(r2 => r2.Id).Contains(row.Id)))
                {
                    using (var stream = File.OpenWrite(DataFile.StorageDirectory + "\\MessageDetails.xml"))
                    {
                        XmlSerializer ser = new XmlSerializer(typeof(List<IdValue>));
                        ser.Serialize(stream, value);
                    }
                    messageDetailTypes.Clear();
                    messageDetailTypes.AddRange(value);
                }
            }
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
