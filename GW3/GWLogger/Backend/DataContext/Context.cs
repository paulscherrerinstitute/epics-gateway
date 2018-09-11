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
        private const int MaxBufferedEntries = 80000;
        private List<IdValue> messageDetailTypes = new List<IdValue>();
        private bool isDisposed = false;
        private Thread bufferConsumer;
        private BufferBlock<LogEntry> bufferedEntries = new BufferBlock<LogEntry>();
        private CancellationTokenSource cancelOperation = new CancellationTokenSource();

        // will be used in reverse as we don't want to parallelize the read
        private static ReaderWriterLockSlim readerLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

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

            bufferConsumer = new Thread(BufferConsumer);
            bufferConsumer.IsBackground = true;
            bufferConsumer.Start();
        }

        public double BufferUsage => Math.Round(bufferedEntries.Count * 10000.0 / MaxBufferedEntries) / 100;

        public GatewayStats GetStats(string gatewayName, DateTime start, DateTime end)
        {
            if (!files.Exists(gatewayName))
                return null;
            return files[gatewayName].GetStats(start, end);
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
                try
                {
                    readerLock.EnterReadLock();
                    foreach (var entry in entries)
                        files[entry.Gateway].Save(entry, knownErrors.Contains(entry.MessageTypeId));
                }
                finally
                {
                    readerLock.ExitReadLock();
                }
            }
        }

        internal List<string> Gateways
        {
            get
            {
                return DataFile.Gateways;
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
                    maxMessageTypes = -1;

                    Logs.RefreshLookup();
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
            try
            {
                readerLock.EnterWriteLock();
                return files[gatewayName].ReadLastLogs(nbEntries);
            }
            finally
            {
                readerLock.ExitWriteLock();
            }
        }
        /*
                public List<LogEntry> ReadLog(string gatewayName, DateTime start, DateTime end, int nbMaxEntries = -1, List<int> messageTypes = null)
                {
                    if (!files.Exists(gatewayName))
                        return null;
                    return files[gatewayName].ReadLog(start, end, null, nbMaxEntries, messageTypes);
                }*/

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
            try
            {
                readerLock.EnterWriteLock();
                return files[gatewayName].ReadLog(start, end, node, nbMaxEntries, messageTypes, startFile, offset, cancellationToken);
            }
            finally
            {
                readerLock.ExitWriteLock();
            }
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
