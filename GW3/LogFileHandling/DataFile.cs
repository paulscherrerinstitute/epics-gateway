using GWLogger.Backend.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace GWLogger.Backend.DataContext
{
    public class DataFile : IDisposable
    {
        public string Gateway { get; }
        public SemaphoreSlim LockObject { get; private set; } = new SemaphoreSlim(1);

        private FileStream file;
        private BinaryWriter DataWriter { get; set; }
        public BinaryReader DataReader { get; private set; }
        public long[] Index = new long[24 * 6];
        private string currentFile;
        private DateTime? lastLogEntry = null;
        private long nbLogEntries = 0;
        private BinaryIndex<short> commandIndex = null;

        // Logs, Searches, Errors, CPU, PVs, Clients, Servers, MsgSecs
        public long[,] Stats = new long[24 * 6, 8];
        private static Regex specialChars = new Regex(@"^[a-zA-Z0-9\.,\-\+ \:_\\/\?\*]+$");

        private static DateTime _jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // SourceMemberName
        private static int sourceMemberNameId = -1;

        // SourceFilePath
        private static int sourceFilePathId = -1;

        private bool isAtEnd = true;
        private bool mustFlush = false;

        //public static string Context.StorageDirectory => System.Configuration.ConfigurationManager.AppSettings["Context.StorageDirectory"];

        /*static DataFile()
        {
            try
            {
                using (var stream = File.OpenRead(Context.StorageDirectory + "\\MemberNames.xml"))
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
                using (var stream = File.OpenRead(Context.StorageDirectory + "\\FilePaths.xml"))
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
        }*/

        public static List<string> Gateways(string storageDirectory)
        {
            return Directory.GetFiles(storageDirectory, "*.data")
                .Select(row => Path.GetFileName(row).Split(new char[] { '.' }).First())
                .Distinct()
                .OrderBy(row => row)
                .ToList();
        }

        private static HashSet<string> knownFiles = new HashSet<string>();
        private readonly Context Context;

        public DataFile(Context context, string gateway)
        {
            this.Context = context;
            try
            {
                LockObject.Wait();
                this.Gateway = gateway;
                SetFile();

                // Recovers how many entries in that last session
                if (File.Exists(Context.StorageDirectory + "\\" + gateway.ToLower() + ".sessions"))
                {
                    using (var reader = new BinaryReader(File.Open(Context.StorageDirectory + "\\" + gateway.ToLower() + ".sessions", FileMode.Open, FileAccess.Read)))
                    {
                        if (reader.BaseStream.Length > sizeof(long))
                        {
                            reader.BaseStream.Seek(reader.BaseStream.Length - sizeof(long), SeekOrigin.Begin);
                            nbLogEntries = reader.ReadInt64();
                        }
                    }
                }
            }
            finally
            {
                LockObject.Release();
            }
        }

        public static bool Exists(string storageDirectory,string gatewayName)
        {
            if (string.IsNullOrWhiteSpace(gatewayName))
                return false;
            lock (knownFiles)
            {
                if (knownFiles.Contains(gatewayName.ToLower()))
                    return true;
                var res = Directory.GetFiles(storageDirectory, gatewayName.ToLower() + ".*.data").Any();
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

                foreach (var i in Directory.GetFiles(Context.StorageDirectory, Gateway.ToLower() + ".*.*").Where(row => DateOfFile(row) <= toDel))
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

        public static void DeleteFiles(string storageDirectory, string gateway)
        {
            // Delete all the data files
            foreach (var i in Directory.GetFiles(storageDirectory, gateway.ToLower() + ".*.*"))
                File.Delete(i);

            lock (knownFiles)
            {
                knownFiles.Clear();
            }
        }

        public DataFileStats GetLogsStats()
        {
            try
            {
                LockObject.Wait();

                var totSize = Directory.GetFiles(Context.StorageDirectory, Gateway.ToLower() + ".*.data").Sum(row => new FileInfo(row).Length);

                var stats = new List<long>();
                var dataSize = 0L;

                // Day before
                SetFile(FileName(DateTime.UtcNow.AddDays(-1)));
                dataSize += DataReader.BaseStream.Length;


                for (var i = 0; i < Stats.GetLength(0); i++)
                    if (Stats[i, 0] != 0)
                        stats.Add(Stats[i, 0]);


                // Today
                SetFile();
                dataSize += DataReader.BaseStream.Length;

                for (var i = 0; i < Stats.GetLength(0); i++)
                    if (Stats[i, 0] != 0)
                        stats.Add(Stats[i, 0]);

                return new DataFileStats
                {
                    Name = Gateway,
                    LogsPerSeconds = stats.Average(row => row / 600.0),
                    AverageEntryBytes = dataSize / stats.Sum(),
                    TotalDataSize = totSize
                };
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

                UpdateGatewaySessions();
            }
            catch // Wrong save?
            {

            }
            finally
            {
                if (mustLock)
                    LockObject.Release();
            }
        }

        private void UpdateGatewaySessions(DateTime? startNewSession = null)
        {
            var sessionFile = Context.StorageDirectory + "\\" + Gateway.ToLower() + ".sessions";
            using (var writer = new BinaryWriter(File.Open(sessionFile, FileMode.OpenOrCreate, FileAccess.Write)))
            {
                if (lastLogEntry.HasValue && nbLogEntries > 0)
                {
                    if (writer.BaseStream.Length == 0)
                        writer.Write(0l); // Start date
                    else
                        writer.BaseStream.Seek(writer.BaseStream.Length - sizeof(long) * 2, SeekOrigin.Begin); // Move from the end
                    writer.Write(lastLogEntry.HasValue ? lastLogEntry.Value.ToBinary() : 0l); // End date
                    writer.Write(nbLogEntries);
                }
                if (startNewSession.HasValue)
                {
                    writer.BaseStream.Seek(writer.BaseStream.Length, SeekOrigin.Begin);
                    writer.Write(startNewSession.Value.ToBinary());
                    writer.Write(0l); // End date
                    writer.Write(0l);
                }
            }
        }

        public void ReadStats()
        {
            var statFileName = FileName(CurrentDate, ".stats");
            try
            {
                using (var reader = new BinaryReader(File.Open(statFileName, FileMode.Open, FileAccess.Read)))
                {
                    // Old formats
                    if (reader.BaseStream.Length != sizeof(long) * Stats.GetLength(1) * Stats.GetLength(0))
                    {
                        var nbColsAvailable = reader.BaseStream.Length / (sizeof(long) * Stats.GetLength(0));

                        for (var s = 0; s < nbColsAvailable; s++)
                            for (var i = 0; i < Stats.GetLength(0); i++)
                                Stats[i, s] = reader.ReadInt64();
                        // Fill with 0
                        for (var s = nbColsAvailable; s < Stats.GetLength(1); s++)
                            for (var i = 0; i < Stats.GetLength(0); i++)
                                Stats[i, s] = 0;
                    }
                    else
                    {
                        for (var s = 0; s < Stats.GetLength(1); s++)
                            for (var i = 0; i < Stats.GetLength(0); i++)
                                Stats[i, s] = reader.ReadInt64();
                    }
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

            return Context.StorageDirectory + "\\" + Gateway.ToLower() + "." + forDate.Value.Year + ("" + forDate.Value.Month).PadLeft(2, '0') + ("" + forDate.Value.Day).PadLeft(2, '0') + extention;
        }

        public DateTime CurrentDate => DateOfFile(currentFile);

        public DateTime DateOfFile(string filename)
        {
            var dt = filename.Split(new char[] { '.' }).Reverse().Take(2).Last();
            return new DateTime(int.Parse(dt.Substring(0, 4)), int.Parse(dt.Substring(4, 2)), int.Parse(dt.Substring(6, 2)));

        }

        public static int IndexPosition(DateTime date)
        {
            return date.Minute / 10 + date.Hour * 6;
        }

        public List<GatewaySession> GetGatewaySessions()
        {
            try
            {
                LockObject.Wait();

                var result = new List<GatewaySession>();

                var sessionFile = Context.StorageDirectory + "\\" + Gateway.ToLower() + ".sessions";
                using (var reader = new BinaryReader(File.Open(sessionFile, FileMode.Open)))
                {
                    var start = Math.Max(reader.BaseStream.Length - 100 * sizeof(long) * 3, 0);

                    reader.BaseStream.Seek(start, SeekOrigin.Begin);
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        var s = reader.ReadInt64();
                        var e = reader.ReadInt64();
                        var n = reader.ReadInt64();

                        result.Add(new GatewaySession
                        {
                            StartDate = (s == 0 ? null : (DateTime?)DateTime.FromBinary(s)),
                            EndDate = (e == 0 ? null : (DateTime?)DateTime.FromBinary(e)),
                            NbEntries = n
                        });
                    }
                }
                result.Reverse();
                return result;
            }
            catch
            {
                return new List<GatewaySession>();
            }
            finally
            {
                LockObject.Release();
            }
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

                if (entry.MessageTypeId < 2) // Skip starting info
                {
                }
                else if (entry.MessageTypeId == 2) // Start new session
                {
                    UpdateGatewaySessions(DateTime.UtcNow);
                    nbLogEntries = 1;
                    lastLogEntry = DateTime.UtcNow;
                }
                else
                {
                    nbLogEntries++;
                    lastLogEntry = DateTime.UtcNow;
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

        internal List<LogEntry> ReadLog(DateTime start, DateTime end, Query.Statement.QueryNode query = null, int nbMaxEntries = -1, List<int> messageTypes = null, string startFile = null, long offset = 0, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var result = new List<LogEntry>();
                LockObject.Wait();

                var currentDate = new DateTime(start.Year, start.Month, start.Day);
                var firstLoop = true;
                var firstItem = true;

                while (currentDate < end && (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested))
                {
                    var fileToUse = FileName(currentDate);

                    if (startFile != null)
                        fileToUse = Path.GetDirectoryName(fileToUse) + "\\" + Path.GetFileName(startFile) + ".data";

                    if (File.Exists(fileToUse))
                    {
                        if (fileToUse != currentFile)
                            SetFile(fileToUse);

                        if (firstLoop && currentFile != null && offset != 0)
                            Seek(offset);
                        else if (firstLoop && Index[IndexPosition(start)] != -1)
                            Seek(Index[IndexPosition(start)]);
                        else
                            Seek(0);
                        isAtEnd = false;

                        var streamLength = DataReader.BaseStream.Length;
                        while (DataReader.BaseStream.Position < streamLength && (nbMaxEntries < 1 || result.Count < nbMaxEntries) && (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested))
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
                                entry.Gateway = Gateway;
                                entry.CurrentFile = Path.GetFileName(fileToUse).Replace(".data", "");
                                result.Add(entry);
                            }
                            firstItem = false;
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
                var files = new Queue<string>(Directory.GetFiles(Context.StorageDirectory, Gateway.ToLower() + ".*.data").OrderByDescending(row => row));

                var result = new List<LogEntry>();
                while (result.Count < nbEntries && files.Count > 0)
                {
                    var lastFile = files.Dequeue();

                    if (lastFile != currentFile)
                        SetFile(lastFile);

                    isAtEnd = false;

                    var streamLength = DataReader.BaseStream.Length;

                    var chunk = new List<LogEntry>();
                    // 48 being an average of bytes per entries
                    DataReader.BaseStream.Seek(Math.Max(0, DataReader.BaseStream.Length - 57 * nbEntries), SeekOrigin.Begin);
                    while (DataReader.BaseStream.Position < streamLength)
                    {
                        /*if (pos < Index.Length - 1 && DataReader.BaseStream.Position >= Index[pos + 1])
                            break;*/
                        try
                        {
                            chunk.Add(ReadEntry(DataReader, this.CurrentDate.Date, streamLength));
                        }
                        catch(EndOfStreamException)
                        {
                            break;
                        }
                        catch
                        {
                        }
                    }
                    result.InsertRange(0, chunk);
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
                    lock (Context.memberNames)
                    {
                        if (!Context.memberNames.ContainsKey(i.Value))
                        {
                            Context.memberNames.Add(i.Value, Context.memberNames.Count == 0 ? 1 : Context.memberNames.Values.Max() + 1);
                            Context.reverseMemberNames.Add(Context.memberNames[i.Value], i.Value);
                            Context.StoreMemberNames();
                        }
                        stream.Write((ushort)Context.memberNames[i.Value]);
                    }
                }
                else if (i.DetailTypeId == sourceFilePathId)
                {
                    lock (Context.filePaths)
                    {
                        if (!Context.filePaths.ContainsKey(i.Value))
                        {
                            Context.filePaths.Add(i.Value, Context.filePaths.Count == 0 ? 1 : Context.filePaths.Values.Max() + 1);
                            Context.reverseFilePaths.Add(Context.filePaths[i.Value], i.Value);
                            Context.StoreFilePaths();
                        }
                        stream.Write((ushort)Context.filePaths[i.Value]);
                    }
                }
                else
                    stream.Write(i.Value);
            }
        }

        public static long ToJsDate(DateTime from)
        {
            return System.Convert.ToInt64((from - _jan1st1970).TotalMilliseconds);
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
                if (i != 0)
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
                if ((dt - approxiateDay).TotalDays <= 30 && DataFile.ToJsDate(dt) > 0)
                {
                    cmd = stream.ReadByte();
                    if (cmd <= Context.MaxMessageTypes)
                    {
                        pos += i;
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
                LogEntryDetails = new List<LogEntryDetail>(),
                Position = pos
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
                    lock (Context.filePaths)
                    {
                        //detail.Value = filePaths.First(row => row.Value == n).Key;
                        detail.Value = Context.reverseFilePaths[n];
                    }
                }
                else if (detail.DetailTypeId == sourceMemberNameId)
                {
                    var n = stream.ReadUInt16();
                    lock (Context.filePaths)
                    {
                        detail.Value = Context.reverseMemberNames[n];// memberNames.First(row => row.Value == n).Key;
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

        public GatewayStats GetStats(DateTime start, DateTime end)
        {
            var result = new GatewayStats
            {
                Searches = new List<LogStat>(),
                Logs = new List<LogStat>(),
                Errors = new List<LogStat>(),
                CPU = new List<LogStat>(),
                PVs = new List<LogStat>(),
                Clients = new List<LogStat>(),
                Servers = new List<LogStat>(),
                MsgSecs = new List<LogStat>()
            };

            var stats = new List<LogStat>[] { result.Logs, result.Searches, result.Errors, result.CPU, result.PVs, result.Clients, result.Servers, result.MsgSecs };

            try
            {
                LockObject.Wait();

                var dayStart = new DateTime(start.Year, start.Month, start.Day);

                foreach (var i in Directory.GetFiles(Context.StorageDirectory, Gateway.ToLower() + ".*.stats")
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