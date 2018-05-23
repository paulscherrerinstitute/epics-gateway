using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using GWLogger.Backend.DTOs;

namespace GWLogger.Backend.DataContext
{
    public class Context : IDisposable
    {
        DataFiles files;
        Thread autoFlusher;

        public Context()
        {
            files = new DataFiles(this);

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

            errorMessages = messageTypes.Where(row => row.LogLevel >= 3).Select(row => row.Id).ToList();

            autoFlusher = new Thread((obj) =>
              {
                  var step = 0;
                  while (!isDisposed)
                  {
                      Thread.Sleep(5000);
                      step++;
                      if (step >= 60)
                      {
                          step = 0;
                          Flush();
                      }
                      else
                          files.SaveStats();
                  }
              });

            autoFlusher.IsBackground = true;
            autoFlusher.Start();
        }

        public GatewayStats GetStats(string gatewayName, DateTime start, DateTime end)
        {
            if (!files.Exists(gatewayName))
                return null;
            return files[gatewayName].GetStats(start, end);
        }

        public void Save(LogEntry entry)
        {
            bool isAnError = false;
            lock (messageTypes)
                isAnError = errorMessages.Contains(entry.MessageTypeId);

            files[entry.Gateway].Save(entry, isAnError);
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

                        errorMessages = messageTypes.Where(row => row.LogLevel >= 3).Select(row => row.Id).ToList();
                        Logs.RefreshLookup();
                    }
                }
            }
        }

        List<IdValue> messageDetailTypes = new List<IdValue>();
        private bool isDisposed = false;

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

                    Logs.RefreshLookup();
                }
            }
        }

        public List<int> errorMessages { get; private set; }

        public List<LogEntry> ReadLastLogs(string gatewayName, int nbEntries = 100)
        {
            if (!files.Exists(gatewayName))
                return null;
            return files[gatewayName].ReadLastLogs(nbEntries);
        }

        public List<LogEntry> ReadLog(string gatewayName, DateTime start, DateTime end, int nbMaxEntries = -1, List<int> messageTypes = null)
        {
            if (!files.Exists(gatewayName))
                return null;
            return files[gatewayName].ReadLog(start, end, nbMaxEntries, messageTypes);
        }

        public List<LogSession> ReadClientSessions(string gatewayName, DateTime start, DateTime end)
        {
            if (!files.Exists(gatewayName))
                return null;
            return files[gatewayName].ReadClientSessions(start, end);
        }

        public List<LogSession> ReadServerSessions(string gatewayName, DateTime start, DateTime end)
        {
            if (!files.Exists(gatewayName))
                return null;
            return files[gatewayName].ReadServerSessions(start, end);
        }

        public List<SearchEntry> ReadSearches(string gatewayName, DateTime start, DateTime end)
        {
            if (!files.Exists(gatewayName))
                return null;
            return files[gatewayName].ReadSearches(start, end);
        }

        public void CleanOlderThan(int nbDays = 10)
        {
            files.CleanOlderThan(nbDays);
        }

        public void Flush()
        {
            files.Flush();
            files.SaveStats();
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            files.Dispose();
        }
    }
}
