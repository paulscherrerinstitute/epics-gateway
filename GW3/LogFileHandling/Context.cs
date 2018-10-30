using GWLogger.Backend.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using System.Xml.Serialization;

namespace GWLogger.Backend.DataContext
{
    public class Context : IDisposable
    {
        private DataFiles files;
        private Thread autoFlusher;
        private const int MaxBufferedEntries = 120000;
        private List<IdValue> messageDetailTypes = new List<IdValue>();

        private bool isDisposed = false;
        private Thread bufferConsumer;
        private BufferBlock<LogEntry> bufferedEntries = new BufferBlock<LogEntry>();
        private CancellationTokenSource cancelOperation = new CancellationTokenSource();

        // will be used in reverse as we don't want to parallelize the read
        private static object accessLock = new object();

        public delegate void DataFileEvent(DataFile file);
        public event DataFileEvent StoreHistory;
        public string StorageDirectory { get; }

        internal Dictionary<string, int> memberNames = new Dictionary<string, int>();
        internal Dictionary<int, string> reverseMemberNames = new Dictionary<int, string>();
        internal Dictionary<string, int> filePaths = new Dictionary<string, int>();
        internal Dictionary<int, string> reverseFilePaths = new Dictionary<int, string>();

        public Context(string storageDirectory)
        {
            StorageDirectory = storageDirectory;
            files = new DataFiles(this);

            try
            {
                using (var stream = File.OpenRead(StorageDirectory + "\\MessageTypes.xml"))
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
                using (var stream = File.OpenRead(StorageDirectory + "\\MessageDetails.xml"))
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

            try
            {
                using (var stream = File.OpenRead(StorageDirectory + "\\MemberNames.xml"))
                {
                    var ser = new XmlSerializer(typeof(List<IdValue>));
                    var data = (List<IdValue>)ser.Deserialize(stream);
                    memberNames = data.ToDictionary(key => key.Value, val => val.Id);
                    reverseMemberNames = data.ToDictionary(key => key.Id, val => val.Value);
                }
            }
            catch
            {
                memberNames = new Dictionary<string, int>();
            }
            try
            {
                using (var stream = File.OpenRead(StorageDirectory + "\\FilePaths.xml"))
                {
                    var ser = new XmlSerializer(typeof(List<IdValue>));
                    var data = (List<IdValue>)ser.Deserialize(stream);
                    filePaths = data.ToDictionary(key => key.Value, val => val.Id);
                    reverseFilePaths = data.ToDictionary(key => key.Id, val => val.Value);
                }
            }
            catch
            {
                filePaths = new Dictionary<string, int>();
            }

            errorMessages = messageTypes.Where(row => row.LogLevel >= 3).Select(row => row.Id).ToList();

            autoFlusher = new Thread((obj) =>
              {
                  var step = 0;
                  while (!isDisposed)
                  {
                      Thread.Sleep(5000);

                      foreach (var f in files)
                          StoreHistory?.Invoke(f);
                      //files.StoreHistory();

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

            bufferConsumer = new Thread(BufferConsumer);
            bufferConsumer.IsBackground = true;
            bufferConsumer.Start();
        }

        internal void StoreFilePaths()
        {
            using (var stream = File.OpenWrite(StorageDirectory + "\\FilePaths.xml"))
            {
                var ser = new XmlSerializer(typeof(List<IdValue>));
                ser.Serialize(stream, filePaths.Select(row => new IdValue { Id = row.Value, Value = row.Key }).ToList());
            }
        }

        internal void StoreMemberNames()
        {
            using (var stream = File.OpenWrite(StorageDirectory + "\\MemberNames.xml"))
            {
                var ser = new XmlSerializer(typeof(List<IdValue>));
                ser.Serialize(stream, memberNames.Select(row => new IdValue { Id = row.Value, Value = row.Key }).ToList());
            }
        }

        public double BufferUsage => Math.Round(bufferedEntries.Count * 10000.0 / MaxBufferedEntries) / 100;

        public List<GatewayStats> GetStats(string gatewayName)
        {
            if (!files.Exists(gatewayName))
                return null;
            return files[gatewayName].GetStats();
        }


        public GatewayStats GetStats(string gatewayName, DateTime start, DateTime end)
        {
            if (!files.Exists(gatewayName))
                return null;
            return files[gatewayName].GetStats(start, end);
        }

        public List<GatewaySession> GetGatewaySessions(string gatewayName)
        {
            if (!files.Exists(gatewayName))
                return null;
            return files[gatewayName].GetGatewaySessions();
        }

        public void Save(LogEntry entry)
        {
            while (bufferedEntries.Count > MaxBufferedEntries)
                Thread.Sleep(100);
            bufferedEntries.Post(entry);
        }

        public void BufferConsumer()
        {
            while (!isDisposed)
            {
                List<LogEntry> entries = new List<LogEntry>();
                try
                {
                    entries.Add(bufferedEntries.Receive(cancelOperation.Token));
                }
                catch
                {
                    break;
                }
                while (bufferedEntries.Count > 0)
                    entries.Add(bufferedEntries.Receive(cancelOperation.Token));
                if (entries.Count == 0)
                    break;
                List<int> knownErrors;
                lock (messageTypes)
                    knownErrors = errorMessages.ToList();
                //foreach (var entry in entries.OrderBy(row => row.Gateway))
                foreach (var entry in entries)
                {
                    lock (accessLock)
                    {
                        files[entry.Gateway].Save(entry, knownErrors.Contains(entry.MessageTypeId));
                    }
                }
            }
        }

        public List<string> Gateways
        {
            get
            {
                return DataFile.Gateways(StorageDirectory);
            }
        }

        private List<DTOs.MessageType> messageTypes = new List<DTOs.MessageType>();
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
                        using (var stream = File.OpenWrite(StorageDirectory + "\\MessageTypes.xml"))
                        {
                            XmlSerializer ser = new XmlSerializer(typeof(List<DTOs.MessageType>));
                            ser.Serialize(stream, value);
                        }
                        messageTypes.Clear();
                        messageTypes.AddRange(value);

                        errorMessages = messageTypes.Where(row => row.LogLevel >= 3).Select(row => row.Id).ToList();
                    }
                }
            }
        }

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
                    using (var stream = File.OpenWrite(StorageDirectory + "\\MessageDetails.xml"))
                    {
                        XmlSerializer ser = new XmlSerializer(typeof(List<IdValue>));
                        ser.Serialize(stream, value);
                    }
                    messageDetailTypes.Clear();
                    messageDetailTypes.AddRange(value);
                    maxMessageTypes = -1;
                }
            }
        }

        public List<int> errorMessages { get; private set; }

        private int maxMessageTypes = -1;
        public int MaxMessageTypes
        {
            get
            {
                if (maxMessageTypes == -1)
                    maxMessageTypes = MessageTypes.Max(row => row.Id);
                return maxMessageTypes;
            }
        }

        public List<LogEntry> ReadLastLogs(string gatewayName, int nbEntries = 100)
        {
            if (!files.Exists(gatewayName))
                return null;
            lock (accessLock)
            {
                return files[gatewayName].ReadLastLogs(nbEntries);
            }
        }

        public List<LogEntry> ReadLog(string gatewayName, DateTime start, DateTime end, string query, int nbMaxEntries = -1, List<int> messageTypes = null, string startFile = null, long offset = 0, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!files.Exists(gatewayName))
                return null;
            Query.Statement.QueryNode node = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(query))
                    node = Query.QueryParser.Parse(query.Trim());
            }
            catch
            {
            }
            lock (accessLock)
            {
                return files[gatewayName].ReadLog(start, end, node, nbMaxEntries, messageTypes, startFile, offset, cancellationToken);
            }
        }

        public List<DataFileStats> GetDataFileStats()
        {
            return files.Select(row => row.GetLogsStats()).OrderBy(row => row.Name).ToList();
        }

        public void CleanOlderThan(int nbDays = 10)
        {
            if (!System.Diagnostics.Debugger.IsAttached)
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
            cancelOperation.Cancel();
            isDisposed = true;
            files.SaveStats();
            files.Dispose();
        }
    }
}
