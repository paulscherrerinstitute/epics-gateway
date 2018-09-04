using GWLogger.Backend.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;

namespace GWLogger.Backend.DataContext
{
    public class DataFile : IDisposable
    {
        private string gateway;
        public SemaphoreSlim LockObject { get; private set; } = new SemaphoreSlim(1);

        private FileStream file;
        private BinaryWriter DataWriter { get; set; }
        public BinaryReader DataReader { get; private set; }
        public long[] Index = new long[24 * 6];
        private string currentFile;
        private BinaryIndex<short> commandIndex = null;

        public long[,] Stats = new long[24 * 6, 3];
        private FileStream clientSession;
        private string currentClientSession;
        private Dictionary<string, SessionLocation> openClientSessions = new Dictionary<string, SessionLocation>();
        private FileStream serverSession;
        private string currentServerSession;
        private Dictionary<string, SessionLocation> openServerSessions = new Dictionary<string, SessionLocation>();
        private static Dictionary<string, int> memberNames = new Dictionary<string, int>();
        private static Dictionary<string, int> filePaths = new Dictionary<string, int>();
        private static Regex specialChars = new Regex(@"^[a-zA-Z0-9\.,\-\+ \:_\\/\?\*]+$");

        // SourceMemberName
        private static int sourceMemberNameId = -1;

        // SourceFilePath
        private static int sourceFilePathId = -1;
        private FileStream searches;
        private string currentSearches;

        private bool isAtEnd = true;
        private bool mustFlush = false;

        public static string StorageDirectory => System.Configuration.ConfigurationManager.AppSettings["storageDirectory"];

        static DataFile()
        {
            try
            {
                using (var stream = File.OpenRead(StorageDirectory + "\\MemberNames.xml"))
                {
                    var ser = new XmlSerializer(typeof(List<IdValue>));
                    var data = (List<IdValue>)ser.Deserialize(stream);
                    memberNames = data.ToDictionary(key => key.Value, val => val.Id);
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
                }
            }
            catch
            {
                filePaths = new Dictionary<string, int>();
            }
        }

        public static List<string> Gateways
        {
            get
            {
                return Directory.GetFiles(StorageDirectory, "*.data")
                    .Select(row => Path.GetFileName(row).Split(new char[] { '.' }).First())
                    .Distinct()
                    .OrderBy(row => row)
                    .ToList();
            }
        }

        private static HashSet<string> knownFiles = new HashSet<string>();
        private readonly Context Context;

        public static bool Exists(string gatewayName)
        {
            lock (knownFiles)
            {
                if (knownFiles.Contains(gatewayName.ToLower()))
                    return true;
                var res = Directory.GetFiles(StorageDirectory, gatewayName.ToLower() + ".*.data").Any();
                if (res)
                    knownFiles.Add(gatewayName.ToLower());
                return res;
            }
        }

        public void CleanOlderThan(int nbDays)
        {
            var toDel = DateTime.UtcNow.AddDays(-nbDays);

            var needToClose = true;
            try
            {
                LockObject.Wait();
                // Delete all the data files

                foreach (var i in Directory.GetFiles(StorageDirectory, gateway.ToLower() + ".*.*").Where(row => DateOfFile(row) <= toDel))
                {
                    if (needToClose)
                    {
                        needToClose = false;
                        CloseFiles();
                    }

                    File.Delete(i);
                }
            }
            finally
            {
                LockObject.Release();
            }

            lock (knownFiles)
            {
                knownFiles.Clear();
            }
        }

        public static void DeleteFiles(string gateway)
        {
            // Delete all the data files
            foreach (var i in Directory.GetFiles(StorageDirectory, gateway.ToLower() + ".*.*"))
                File.Delete(i);

            lock (knownFiles)
            {
                knownFiles.Clear();
            }
        }

        public DataFile(Context context, string gateway)
        {
            this.Context = context;
            try
            {
                LockObject.Wait();
                this.gateway = gateway;
                SetFile();
            }
            finally
            {
                LockObject.Release();
            }
        }

        private void SetFile(string filename = null)
        {
            if (file != null)
            {
                /*if (!isAtEnd)
                    file.Seek(0, SeekOrigin.End);

                DataWriter.Dispose();
                DataReader.Dispose();
                file.Dispose();*/

                CloseFiles();
            }

            currentFile = filename ?? FileName();
            file = System.IO.File.Open(currentFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            DataWriter = new BinaryWriter(file, System.Text.Encoding.UTF8, true);
            DataReader = new BinaryReader(file, System.Text.Encoding.UTF8, true);

            file.Seek(0, SeekOrigin.End);
            isAtEnd = true;

            ReadIndex();
            ReadStats();
        }

        public void SaveIndex()
        {
            var idxFileName = FileName(CurrentDate, ".idx");
            using (var writer = new BinaryWriter(File.Open(idxFileName, FileMode.Create, FileAccess.Write)))
            {
                foreach (var i in Index)
                    writer.Write(i);
            }
        }

        public void ReadIndex()
        {
            var idxFileName = FileName(CurrentDate, ".idx");
            try
            {
                using (var reader = new BinaryReader(File.Open(idxFileName, FileMode.Open, FileAccess.Read)))
                {
                    for (var i = 0; i < Index.Length; i++)
                        Index[i] = reader.ReadInt64();
                }
            }
            catch
            {
                for (var i = 0; i < Index.Length; i++)
                    Index[i] = -1;

                SaveIndex();
            }
        }

        public void SaveStats(bool mustLock = false)
        {
            try
            {
                if (mustLock)
                    LockObject.Wait();
                var statFileName = FileName(CurrentDate, ".stats");
                using (var writer = new BinaryWriter(File.Open(statFileName, FileMode.Create, FileAccess.Write)))
                {
                    for (var s = 0; s < Stats.GetLength(1); s++)
                        for (var i = 0; i < Stats.GetLength(0); i++)
                            writer.Write(Stats[i, s]);
                }
            }
            finally
            {
                if (mustLock)
                    LockObject.Release();
            }
        }

        public void ReadStats()
        {
            var statFileName = FileName(CurrentDate, ".stats");
            try
            {
                using (var reader = new BinaryReader(File.Open(statFileName, FileMode.Open, FileAccess.Read)))
                {
                    for (var s = 0; s < Stats.GetLength(1); s++)
                        for (var i = 0; i < Stats.GetLength(0); i++)
                            Stats[i, s] = reader.ReadInt64();
                }
            }
            catch
            {
                for (var s = 0; s < Stats.GetLength(1); s++)
                    for (var i = 0; i < Stats.GetLength(0); i++)
                        Stats[i, s] = 0;

                SaveStats();
            }
        }

        public string FileName(DateTime? forDate = null, string extention = ".data")
        {
            if (!forDate.HasValue)
                forDate = DateTime.UtcNow;

            return StorageDirectory + "\\" + gateway.ToLower() + "." + forDate.Value.Year + ("" + forDate.Value.Month).PadLeft(2, '0') + ("" + forDate.Value.Day).PadLeft(2, '0') + extention;
        }

        public DateTime CurrentDate => DateOfFile(currentFile);

        public DateTime DateOfFile(string filename)
        {
            var dt = filename.Split(new char[] { '.' }).Reverse().Take(2).Last();
            return new DateTime(int.Parse(dt.Substring(0, 4)), int.Parse(dt.Substring(4, 2)), int.Parse(dt.Substring(6, 2)));

        }

        private int IndexPosition(DateTime date)
        {
            return date.Minute / 10 + date.Hour * 6;
        }

        public void Save(LogEntry entry, bool isAnError)
        {
            try
            {
                LockObject.Wait();

                var fileToUse = FileName(entry.EntryDate);
                if (fileToUse != currentFile)
                    SetFile(fileToUse);

                StartWriting();

                // Check if we must update the index file
                var idxPos = IndexPosition(entry.EntryDate);
                // Index is not yet set, then set it
                if (Index[idxPos] == -1)
                {
                    Index[idxPos] = DataWriter.BaseStream.Position;

                    var idxFileName = FileName(CurrentDate, ".idx");
                    using (var writer = new BinaryWriter(File.Open(idxFileName, FileMode.OpenOrCreate, FileAccess.Write)))
                    {
                        writer.Seek(sizeof(long) * idxPos, SeekOrigin.Begin);
                        writer.Write(Index[idxPos]);
                        writer.Seek(0, SeekOrigin.End);
                    }
                }

                WriteEntry(DataWriter, entry);

                /*DataWriter.Write(entry.EntryDate.ToBinary());
                DataWriter.Write((byte)entry.MessageTypeId);
                DataWriter.Write(entry.RemoteIpPoint ?? "");

                DataWriter.Write((byte)entry.LogEntryDetails.Count);
                foreach (var i in entry.LogEntryDetails)
                {
                    DataWriter.Write((byte)i.DetailTypeId);
                    DataWriter.Write(i.Value);
                }*/

                Stats[idxPos, 0]++;
                if (isAnError)
                    Stats[idxPos, 2]++;

                switch (entry.MessageTypeId)
                {
                    case 4: // Client session start
                        if (currentClientSession != FileName(entry.EntryDate, ".clientSessions"))
                        {
                            if (clientSession != null)
                            {
                                clientSession.Seek(0, SeekOrigin.End);
                                clientSession.Dispose();
                            }
                            currentClientSession = FileName(entry.EntryDate, ".clientSessions");
                            clientSession = File.Open(FileName(entry.EntryDate, ".clientSessions"), FileMode.OpenOrCreate, FileAccess.ReadWrite);
                        }

                        using (var writer = new BinaryWriter(clientSession, System.Text.Encoding.UTF8, true))
                        {
                            clientSession.Seek(0, SeekOrigin.End);
                            if (!openClientSessions.ContainsKey(entry.RemoteIpPoint))
                                openClientSessions.Add(entry.RemoteIpPoint, new SessionLocation
                                {
                                    FileName = currentClientSession,
                                    Position = clientSession.Position
                                });
                            writer.Write(entry.EntryDate.ToBinary());
                            writer.Write(0L);
                            writer.Write(entry.RemoteIpPoint);
                        }
                        break;
                    case 6: // Client session stop
                        {
                            if (openClientSessions.ContainsKey(entry.RemoteIpPoint))
                            {

                                if (!File.Exists(openClientSessions[entry.RemoteIpPoint].FileName))
                                    break;

                                if (currentClientSession != openClientSessions[entry.RemoteIpPoint].FileName)
                                {
                                    if (clientSession != null)
                                    {
                                        clientSession.Seek(0, SeekOrigin.End);
                                        clientSession.Dispose();
                                    }
                                    currentClientSession = openClientSessions[entry.RemoteIpPoint].FileName;
                                    clientSession = File.Open(openClientSessions[entry.RemoteIpPoint].FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                                }

                                clientSession.Seek(openClientSessions[entry.RemoteIpPoint].Position + sizeof(long), SeekOrigin.Begin);
                                using (var writer = new BinaryWriter(clientSession, System.Text.Encoding.UTF8, true))
                                {
                                    writer.Write(entry.EntryDate.ToBinary());
                                }


                                openClientSessions.Remove(entry.RemoteIpPoint);
                                break;
                            }

                            var dateTried = entry.EntryDate;
                            var found = false;
                            while (!found)
                            {
                                if (!File.Exists(FileName(dateTried, ".clientSessions")))
                                    break;

                                if (currentClientSession != FileName(dateTried, ".clientSessions"))
                                {
                                    if (clientSession != null)
                                    {
                                        clientSession.Seek(0, SeekOrigin.End);
                                        clientSession.Dispose();
                                    }
                                    currentClientSession = FileName(dateTried, ".clientSessions");
                                    clientSession = File.Open(FileName(dateTried, ".clientSessions"), FileMode.OpenOrCreate, FileAccess.ReadWrite);
                                }
                                else
                                    clientSession.Seek(0, SeekOrigin.Begin);

                                using (var reader = new BinaryReader(clientSession, System.Text.Encoding.UTF8, true))
                                {
                                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                                    {
                                        reader.ReadInt64();
                                        var pos = reader.BaseStream.Position;
                                        var endDate = reader.ReadInt64();
                                        var client = reader.ReadString();
                                        if (endDate == 0 && client == entry.RemoteIpPoint)
                                        {
                                            using (var writer = new BinaryWriter(clientSession, System.Text.Encoding.UTF8, true))
                                            {
                                                writer.BaseStream.Seek(pos, SeekOrigin.Begin);
                                                writer.Write(entry.EntryDate.ToBinary());

                                                found = true;
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (!found)
                                    dateTried = dateTried.AddDays(-1);
                            }
                            break;
                        }
                    case 39: // Search
                        {
                            Stats[idxPos, 1]++;
                            if (currentSearches != FileName(entry.EntryDate, ".searches"))
                            {
                                if (searches != null)
                                {
                                    searches.Seek(0, SeekOrigin.End);
                                    searches.Dispose();
                                }
                                currentSearches = FileName(entry.EntryDate, ".searches");
                                searches = File.Open(FileName(entry.EntryDate, ".searches"), FileMode.OpenOrCreate, FileAccess.ReadWrite);
                            }

                            using (var writer = new BinaryWriter(searches, System.Text.Encoding.UTF8, true))
                            {
                                searches.Seek(0, SeekOrigin.End);
                                writer.Write(entry.EntryDate.ToBinary());
                                writer.Write(entry.RemoteIpPoint);
                                writer.Write((entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == 7)?.Value) ?? "");
                            }
                            break;
                        }
                    case 55: // Server session start
                        if (currentServerSession != FileName(entry.EntryDate, ".serverSessions"))
                        {
                            if (serverSession != null)
                            {
                                serverSession.Seek(0, SeekOrigin.End);
                                serverSession.Dispose();
                            }
                            currentServerSession = FileName(entry.EntryDate, ".serverSessions");
                            serverSession = File.Open(FileName(entry.EntryDate, ".serverSessions"), FileMode.OpenOrCreate, FileAccess.ReadWrite);
                        }

                        using (var writer = new BinaryWriter(serverSession, System.Text.Encoding.UTF8, true))
                        {
                            serverSession.Seek(0, SeekOrigin.End);
                            if (!openServerSessions.ContainsKey(entry.RemoteIpPoint))
                                openServerSessions.Add(entry.RemoteIpPoint, new SessionLocation
                                {
                                    FileName = currentServerSession,
                                    Position = serverSession.Position
                                });
                            writer.Write(entry.EntryDate.ToBinary());
                            writer.Write(0L);
                            writer.Write(entry.RemoteIpPoint);
                        }
                        break;
                    case 53: // Server session stop
                        {
                            if (openServerSessions.ContainsKey(entry.RemoteIpPoint))
                            {

                                if (!File.Exists(openServerSessions[entry.RemoteIpPoint].FileName))
                                    break;

                                if (currentServerSession != openServerSessions[entry.RemoteIpPoint].FileName)
                                {
                                    if (serverSession != null)
                                    {
                                        serverSession.Seek(0, SeekOrigin.End);
                                        serverSession.Dispose();
                                    }
                                    currentServerSession = openServerSessions[entry.RemoteIpPoint].FileName;
                                    serverSession = File.Open(openServerSessions[entry.RemoteIpPoint].FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                                }

                                serverSession.Seek(openServerSessions[entry.RemoteIpPoint].Position + sizeof(long), SeekOrigin.Begin);
                                using (var writer = new BinaryWriter(serverSession, System.Text.Encoding.UTF8, true))
                                {
                                    writer.Write(entry.EntryDate.ToBinary());
                                }


                                openServerSessions.Remove(entry.RemoteIpPoint);
                                break;
                            }

                            var dateTried = entry.EntryDate;
                            var found = false;
                            while (!found)
                            {
                                if (!File.Exists(FileName(dateTried, ".serverSessions")))
                                    break;

                                if (currentServerSession != FileName(dateTried, ".serverSessions"))
                                {
                                    if (serverSession != null)
                                    {
                                        serverSession.Seek(0, SeekOrigin.End);
                                        serverSession.Dispose();
                                    }
                                    currentServerSession = FileName(dateTried, ".serverSessions");
                                    serverSession = File.Open(FileName(dateTried, ".serverSessions"), FileMode.OpenOrCreate, FileAccess.ReadWrite);
                                }
                                else
                                    serverSession.Seek(0, SeekOrigin.Begin);

                                using (var reader = new BinaryReader(serverSession, System.Text.Encoding.UTF8, true))
                                {
                                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                                    {
                                        reader.ReadInt64();
                                        var pos = reader.BaseStream.Position;
                                        var endDate = reader.ReadInt64();
                                        var client = reader.ReadString();
                                        if (endDate == 0 && client == entry.RemoteIpPoint)
                                        {
                                            using (var writer = new BinaryWriter(serverSession, System.Text.Encoding.UTF8, true))
                                            {
                                                writer.BaseStream.Seek(pos, SeekOrigin.Begin);
                                                writer.Write(entry.EntryDate.ToBinary());

                                                found = true;
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (!found)
                                    dateTried = dateTried.AddDays(-1);
                            }
                            break;
                        }
                    default:
                        break;
                }
            }
            finally
            {
                LockObject.Release();
            }
        }

        private static int SerializedStringLength(int length)
        {
            var nbBits = (Math.Log(length) / Math.Log(2)) / 7.0;
            var sizePrefix = (int)Math.Ceiling(Math.Max(1, nbBits));
            /*if ((length & (1 << (sizePrefix * 8 - 1))) != 0)
                sizePrefix++;*/
            return sizePrefix + length;
        }

        internal void StartWriting()
        {
            mustFlush = true;
            if (isAtEnd)
                return;
            file.Seek(0, SeekOrigin.End);
            isAtEnd = true;
        }

        public void Seek(long position)
        {
            file.Seek(position, SeekOrigin.Begin);
            isAtEnd = (file.Position == file.Length);
        }

        internal void Flush()
        {
            try
            {
                LockObject.Wait();
                if (!mustFlush)
                    return;
                mustFlush = false;
                commandIndex?.Flush();
                DataWriter.Flush();
            }
            finally
            {
                LockObject.Release();
            }
        }

        internal List<LogEntry> ReadLog(DateTime start, DateTime end, Query.Statement.QueryNode query = null, int nbMaxEntries = -1, List<int> messageTypes = null)
        {
            try
            {
                var result = new List<LogEntry>();
                LockObject.Wait();

                var currentDate = new DateTime(start.Year, start.Month, start.Day);
                var firstLoop = true;
                var firstItem = true;

                while (currentDate < end)
                {
                    var fileToUse = FileName(currentDate);
                    if (File.Exists(fileToUse))
                    {
                        if (fileToUse != currentFile)
                            SetFile(fileToUse);

                        if (firstLoop && Index[IndexPosition(start)] != -1)
                            Seek(Index[IndexPosition(start)]);
                        else
                            Seek(0);
                        isAtEnd = false;

                        var streamLength = DataReader.BaseStream.Length;
                        while (DataReader.BaseStream.Position < streamLength && (nbMaxEntries < 1 || result.Count < nbMaxEntries))
                        {
                            var entry = ReadEntry(DataReader, start, streamLength);
                            if (firstItem && query != null)
                            {
                                try
                                {
                                    query.CheckCondition(Context, entry);
                                }
                                catch
                                {
                                    query = null;
                                }
                            }

                            if (entry != null && entry.EntryDate >= start && entry.EntryDate <= end && (query == null || query.CheckCondition(Context, entry)) && (messageTypes == null || messageTypes.Contains(entry.MessageTypeId)))
                            {
                                entry.Gateway = gateway;
                                result.Add(entry);
                            }
                            if (entry != null && entry.EntryDate > end)
                                break;
                        }
                    }

                    currentDate = currentDate.AddDays(1);
                    firstLoop = false;
                }

                return result;
            }
            finally
            {
                LockObject.Release();
            }
        }

        internal List<LogEntry> ReadLastLogs(int nbEntries)
        {
            try
            {
                LockObject.Wait();
                var files = new Queue<string>(Directory.GetFiles(StorageDirectory, gateway.ToLower() + ".*.data").OrderByDescending(row => row));

                var result = new List<LogEntry>();
                while (result.Count < nbEntries && files.Count > 0)
                {
                    var lastFile = files.Dequeue();

                    if (lastFile != currentFile)
                        SetFile(lastFile);

                    long pos = 0;
                    for (var i = Index.Length - 1; i > 0; i--)
                    {
                        if (Index[i] != -1)
                        {
                            pos = i;
                            break;
                        }
                    }
                    isAtEnd = false;

                    var streamLength = DataReader.BaseStream.Length;

                    while (pos >= 0 && result.Count < nbEntries && Index[pos] >= 0)
                    {
                        var chunk = new List<LogEntry>();
                        DataReader.BaseStream.Seek(Index[pos], SeekOrigin.Begin);
                        while (DataReader.BaseStream.Position < streamLength)
                        {
                            if (pos < Index.Length - 1 && DataReader.BaseStream.Position >= Index[pos + 1])
                                break;
                            try
                            {
                                chunk.Add(ReadEntry(DataReader, this.CurrentDate.Date, streamLength));
                            }
                            catch
                            {
                            }
                        }
                        result.InsertRange(0, chunk);
                        pos--;
                    }
                }

                if (result.Count > nbEntries)
                    return result.Skip(result.Count - nbEntries).ToList();
                return result;
            }
            finally
            {
                LockObject.Release();
            }
        }

        private void WriteEntry(BinaryWriter stream, LogEntry entry)
        {
            if (sourceMemberNameId == -1)
                sourceMemberNameId = Context.MessageDetailTypes.First(row => row.Value == "SourceMemberName").Id;
            if (sourceFilePathId == -1)
                sourceFilePathId = Context.MessageDetailTypes.First(row => row.Value == "SourceFilePath").Id;

#warning Disabled due to poor performances
            /*if (commandIndex == null || commandIndex.Filename != FileName(entry.EntryDate, ".cmd_idx"))
            {
                commandIndex?.Dispose();
                commandIndex = new BinaryIndex<short>(FileName(entry.EntryDate, ".cmd_idx"));
            }
            commandIndex.AddEntry((short)entry.MessageTypeId, stream.BaseStream.Position);*/

            stream.Write(entry.EntryDate.ToBinary());
            stream.Write((byte)entry.MessageTypeId);
            if (string.IsNullOrWhiteSpace(entry.RemoteIpPoint))
                stream.Write(new byte[] { 0, 0, 0, 0, 0, 0 });
            else
            {
                var p = entry.RemoteIpPoint.Split(':');
                stream.Write(System.Net.IPAddress.Parse(p[0]).GetAddressBytes());
                stream.Write(ushort.Parse(p[1]));
            }

            stream.Write((byte)entry.LogEntryDetails.Count);
            foreach (var i in entry.LogEntryDetails)
            {
                stream.Write((byte)i.DetailTypeId);
                if (i.DetailTypeId == sourceMemberNameId)
                {
                    lock (memberNames)
                    {
                        if (!memberNames.ContainsKey(i.Value))
                        {
                            memberNames.Add(i.Value, memberNames.Count == 0 ? 1 : memberNames.Values.Max() + 1);
                            StoreMemberNames();
                        }
                        stream.Write((ushort)memberNames[i.Value]);
                    }
                }
                else if (i.DetailTypeId == sourceFilePathId)
                {
                    lock (filePaths)
                    {
                        if (!filePaths.ContainsKey(i.Value))
                        {
                            filePaths.Add(i.Value, filePaths.Count == 0 ? 1 : filePaths.Values.Max() + 1);
                            StoreFilePaths();
                        }
                        stream.Write((ushort)filePaths[i.Value]);
                    }
                }
                else
                    stream.Write(i.Value);
            }
        }

        private void StoreFilePaths()
        {
            using (var stream = File.OpenWrite(StorageDirectory + "\\FilePaths.xml"))
            {
                var ser = new XmlSerializer(typeof(List<IdValue>));
                ser.Serialize(stream, filePaths.Select(row => new IdValue { Id = row.Value, Value = row.Key }).ToList());
            }
        }

        private void StoreMemberNames()
        {
            using (var stream = File.OpenWrite(StorageDirectory + "\\MemberNames.xml"))
            {
                var ser = new XmlSerializer(typeof(List<IdValue>));
                ser.Serialize(stream, memberNames.Select(row => new IdValue { Id = row.Value, Value = row.Key }).ToList());
            }
        }

        private LogEntry ReadEntry(BinaryReader stream, DateTime approxiateDay, long streamLength)
        {
            if (sourceMemberNameId == -1)
                sourceMemberNameId = Context.MessageDetailTypes.First(row => row.Value == "SourceMemberName").Id;
            if (sourceFilePathId == -1)
                sourceFilePathId = Context.MessageDetailTypes.First(row => row.Value == "SourceFilePath").Id;

            var max = DateTime.UtcNow.Ticks;
            int cmd = 0;
            DateTime dt = DateTime.UtcNow;
            bool found = false;
            var pos = stream.BaseStream.Position;
            for (var i = 0; i < 1024 && pos + i < streamLength; i++)
            {
                if(i != 0)
                    stream.BaseStream.Seek(pos + i, SeekOrigin.Begin);
                try
                {
                    var l = stream.ReadInt64();
                    dt = DateTime.FromBinary(l).ToUniversalTime();
                }
                catch
                {
                    continue;
                }
                // Still a possible date
                if ((dt - approxiateDay).TotalDays <= 30)
                {
                    cmd = stream.ReadByte();
                    if (cmd <= Global.DataContext.MaxMessageTypes)
                    {
                        found = true;
                        break;
                    }
                }
            }
            if (!found)
                throw new Exception("Cannot find data");

            var result = new LogEntry
            {
                EntryDate = dt,
                MessageTypeId = cmd,
                LogEntryDetails = new List<LogEntryDetail>()
            };

            var bytes = stream.ReadBytes(4);
            if (bytes[0] == 0)
            {
                stream.ReadUInt16();
                result.RemoteIpPoint = "";
            }
            else
            {
                var ip = new System.Net.IPAddress(bytes);
                result.RemoteIpPoint = ip.ToString() + ":" + stream.ReadUInt16();
            }

            var nbDetails = (int)stream.ReadByte();
            if (nbDetails > 20)
                return result;
            for (var i = 0; i < nbDetails; i++)
            {
                var detail = new LogEntryDetail
                {
                    DetailTypeId = (int)stream.ReadByte(),
                };
                if (detail.DetailTypeId == sourceFilePathId)
                {
                    var n = stream.ReadUInt16();
                    lock (filePaths)
                    {
                        detail.Value = filePaths.First(row => row.Value == n).Key;
                    }
                }
                else if (detail.DetailTypeId == sourceMemberNameId)
                {
                    var n = stream.ReadUInt16();
                    lock (filePaths)
                    {
                        detail.Value = memberNames.First(row => row.Value == n).Key;
                    }
                }
                else
                {
                    pos = stream.BaseStream.Position;
                    try
                    {
                        detail.Value = stream.ReadString();
                        // oddies as string, we may have an issue with the record
                        //if (detail.Value.Length > 128 || !specialChars.IsMatch(detail.Value))
                        if (detail.Value.Length > 128)
                        {
                            stream.BaseStream.Seek(pos, SeekOrigin.Begin);
                            break;
                        }
                    }
                    catch
                    {
                        stream.BaseStream.Seek(pos, SeekOrigin.Begin);
                        break;
                    }
                }
                result.LogEntryDetails.Add(detail);
            }

            return result;
        }

        /*LogEntry ReadEntry(BinaryReader stream)
        {
            var result = new LogEntry
            {
                EntryDate = DateTime.FromBinary(stream.ReadInt64()).ToUniversalTime(),
                MessageTypeId = stream.ReadByte(),
                RemoteIpPoint = stream.ReadString(),
                LogEntryDetails = new List<LogEntryDetail>()
            };

            var nbDetails = (int)stream.ReadByte();
            for (var i = 0; i < nbDetails; i++)
            {
                result.LogEntryDetails.Add(new LogEntryDetail
                {
                    DetailTypeId = (int)stream.ReadByte(),
                    Value = stream.ReadString()
                });
            }

            return result;
        }*/

        public List<LogSession> ReadClientSessions(DateTime start, DateTime end)
        {
            if (end == start)
                end = end.AddMinutes(10);

            var result = new List<LogSession>();

            try
            {
                LockObject.Wait();

                if (clientSession != null)
                {
                    clientSession.Seek(0, SeekOrigin.End);
                    clientSession.Dispose();
                    clientSession = null;
                    currentClientSession = null;
                }

                var dayStart = new DateTime(start.Year, start.Month, start.Day);
                /*foreach (var i in Directory.GetFiles(StorageDirectory, gateway.ToLower() + ".*.clientSessions").OrderBy(row => row)                 
                .Where(row => DateOfFile(row) >= dayStart && DateOfFile(row) <= end))*/
                foreach (var i in Directory.GetFiles(StorageDirectory, gateway.ToLower() + ".*.clientSessions").OrderBy(row => row))
                {
                    using (var reader = new BinaryReader(File.Open(i, FileMode.Open, FileAccess.Read), System.Text.Encoding.UTF8))
                    {
                        while (reader.BaseStream.Position < reader.BaseStream.Length)
                        {
                            var entryStartDate = DateTime.FromBinary(reader.ReadInt64());
                            var ed = reader.ReadInt64();
                            DateTime? entryEndDate;
                            if (ed == 0)
                                entryEndDate = null;
                            else
                                entryEndDate = DateTime.FromBinary(ed).ToUniversalTime();
                            var endPoint = reader.ReadString();

                            if ((entryEndDate.HasValue && start <= entryStartDate && end >= entryEndDate.Value) || (!entryEndDate.HasValue && start <= entryStartDate))
                                result.Add(new LogSession
                                {
                                    Start = entryStartDate,
                                    End = entryEndDate,
                                    Remote = endPoint
                                });
                        }
                    }
                }
            }
            finally
            {
                LockObject.Release();
            }

            return result.OrderBy(row => row.Start).ThenBy(row => row.Remote).ToList();
        }

        public List<LogSession> ReadServerSessions(DateTime start, DateTime end)
        {
            if (end == start)
                end = end.AddMinutes(10);

            var result = new List<LogSession>();

            try
            {
                LockObject.Wait();

                if (serverSession != null)
                {
                    serverSession.Seek(0, SeekOrigin.End);
                    serverSession.Dispose();
                    serverSession = null;
                    currentServerSession = null;
                }

                var dayStart = new DateTime(start.Year, start.Month, start.Day);
                /*foreach (var i in Directory.GetFiles(StorageDirectory, gateway.ToLower() + ".*.serverSessions").OrderBy(row => row)
                    .Where(row => DateOfFile(row) >= dayStart && DateOfFile(row) <= end))*/
                foreach (var i in Directory.GetFiles(StorageDirectory, gateway.ToLower() + ".*.serverSessions").OrderBy(row => row))
                {
                    using (var reader = new BinaryReader(File.Open(i, FileMode.Open, FileAccess.Read), System.Text.Encoding.UTF8))
                    {
                        while (reader.BaseStream.Position < reader.BaseStream.Length)
                        {
                            var entryStartDate = DateTime.FromBinary(reader.ReadInt64());
                            var ed = reader.ReadInt64();
                            DateTime? entryEndDate;
                            if (ed == 0)
                                entryEndDate = null;
                            else
                                entryEndDate = DateTime.FromBinary(ed);
                            var endPoint = reader.ReadString();

                            if ((entryEndDate.HasValue && start <= entryStartDate && end >= entryEndDate.Value) || (!entryEndDate.HasValue && start <= entryStartDate))
                                result.Add(new LogSession
                                {
                                    Start = entryStartDate,
                                    End = entryEndDate,
                                    Remote = endPoint
                                });
                        }
                    }
                }
            }
            finally
            {
                LockObject.Release();
            }

            return result.OrderBy(row => row.Start).ThenBy(row => row.Remote).ToList();
        }

        public List<SearchEntry> ReadSearches(DateTime start, DateTime end)
        {
            if (end == start)
                end = end.AddMinutes(10);
            var result = new List<SearchEntry>();

            try
            {
                LockObject.Wait();

                if (searches != null)
                {
                    searches.Seek(0, SeekOrigin.End);
                    searches.Dispose();
                    searches = null;
                    currentSearches = null;
                }
                var dayStart = new DateTime(start.Year, start.Month, start.Day);

                foreach (var i in Directory.GetFiles(StorageDirectory, gateway.ToLower() + ".*.searches")
                    .OrderBy(row => row)
                    .Where(row => DateOfFile(row) >= dayStart && DateOfFile(row) <= end))
                {
                    using (var reader = new BinaryReader(File.Open(i, FileMode.Open, FileAccess.Read), System.Text.Encoding.UTF8))
                    {
                        while (reader.BaseStream.Position < reader.BaseStream.Length)
                        {
                            var entry = new SearchEntry
                            {
                                Date = DateTime.FromBinary(reader.ReadInt64()),
                                Remote = reader.ReadString(),
                                Channel = reader.ReadString()
                            };


                            if (entry.Date >= start && entry.Date <= end)
                                result.Add(entry);
                        }
                    }
                }
            }
            finally
            {
                LockObject.Release();
            }

            return result.OrderBy(row => row.Channel).ToList();
        }

        public GatewayStats GetStats(DateTime start, DateTime end)
        {
            var result = new GatewayStats
            {
                Searches = new List<LogStat>(),
                Logs = new List<LogStat>(),
                Errors = new List<LogStat>()
            };

            var stats = new List<LogStat>[] { result.Logs, result.Searches, result.Errors };

            try
            {
                LockObject.Wait();

                var dayStart = new DateTime(start.Year, start.Month, start.Day);

                foreach (var i in Directory.GetFiles(StorageDirectory, gateway.ToLower() + ".*.stats")
                    .OrderBy(row => row)
                    .Where(row => DateOfFile(row) >= dayStart && DateOfFile(row) <= end))
                {

                    using (var reader = new BinaryReader(File.Open(i, FileMode.Open, FileAccess.Read)))
                    {
                        try
                        {
                            for (var s = 0; s < Stats.GetLength(1); s++)
                            {
                                for (var j = 0; j < Stats.GetLength(0); j++)
                                {
                                    var v = reader.ReadInt64();
                                    var t = DateOfFile(i).AddMinutes(10 * j);
                                    if (t >= start && t <= end)
                                        stats[s].Add(new LogStat { Date = t, Value = v });
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            finally
            {
                LockObject.Release();
            }

            return result;
        }


        internal void CloseFiles()
        {

            file.Seek(0, SeekOrigin.End);

            SaveStats();

            if (clientSession != null)
            {
                clientSession.Seek(0, SeekOrigin.End);
                clientSession.Dispose();
                clientSession = null;
                currentClientSession = null;
            }
            openClientSessions.Clear();

            if (serverSession != null)
            {
                serverSession.Seek(0, SeekOrigin.End);
                serverSession.Dispose();
                serverSession = null;
                currentServerSession = null;
            }
            openServerSessions.Clear();

            if (currentSearches != null)
            {
                searches.Dispose();
                searches = null;
                currentSearches = null;
            }

            if (currentFile != null)
            {
                DataReader.Dispose();
                DataReader = null;
                DataWriter.Dispose();
                DataWriter = null;
                file.Dispose();

                file = null;
                currentFile = null;
            }

        }

        public void Dispose()
        {
            if (LockObject == null)
                return;
            commandIndex?.Dispose();
            commandIndex = null;
            try
            {
                LockObject.Wait();
            }
            finally
            {
                LockObject.Release();
            }
            CloseFiles();
            LockObject.Dispose();
            LockObject = null;
        }
    }
}