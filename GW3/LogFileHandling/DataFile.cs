#define LOCKINFO

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
        private SemaphoreSlim lockObject = new SemaphoreSlim(1);

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
            this.Gateway = gateway;
            SetFile();

            // Recovers how many entries in that last session
            if (File.Exists(Context.StorageDirectory + "\\" + gateway.ToLower() + ".sessions"))
            {
                ConvertSessionFile(Context.StorageDirectory + "\\" + gateway.ToLower() + ".sessions");

                using (var reader = new BinaryReader(File.Open(Context.StorageDirectory + "\\" + gateway.ToLower() + ".sessions", FileMode.Open, FileAccess.Read)))
                {
                    if (reader.BaseStream.Length > sizeof(long) * 4 + sizeof(uint) + 257)
                    {
                        reader.BaseStream.Seek(reader.BaseStream.Length - (sizeof(long) + 257), SeekOrigin.Begin);
                        nbLogEntries = reader.ReadInt64();
                    }
                }
            }
        }

        private void ConvertSessionFile(string sessionFile)
        {
            using (var reader = new BinaryReader(File.Open(sessionFile, FileMode.Open, FileAccess.Read)))
            {
                bool fileFormat = false;
                bool fileVersion = false;
                bool shouldConvert = true;

                try
                {
                    fileFormat = (reader.ReadInt64() == 1973);
                    fileVersion = (reader.ReadInt32() == 1);
                    if (fileFormat && fileVersion) // Format is correct, we can keep it as is
                        return;
                }
                catch
                {
                    using (var writer = new BinaryWriter(File.Open(sessionFile + ".new", FileMode.Create)))
                    {
                        writer.Write(1973L); // FILE Signature
                        writer.Write(1U); // Version format
                    }
                    shouldConvert = false;
                }

                if (shouldConvert)
                {
                    using (var writer = new BinaryWriter(File.Open(sessionFile + ".new", FileMode.Create)))
                    {
                        writer.Write(1973L); // FILE Signature
                        writer.Write(1U); // Version format

                        if (!fileFormat) // First version of the file format didn't even had the header
                        {
                            reader.BaseStream.Seek(0, SeekOrigin.Begin);
                            while (reader.BaseStream.Position < reader.BaseStream.Length)
                            {
                                writer.Write(reader.ReadInt64()); // Start date
                                writer.Write(reader.ReadInt64()); // End date
                                writer.Write(reader.ReadInt64()); // Nb records

                                // Those are fix and set as default
                                writer.Write((byte)0); // Type of restart
                                writer.Write((byte)0); // Nb chars of comment
                                writer.Write(new byte[255]); // Nb chars of comment
                            }
                        }
                        else // We don't have any other file version number supported!
                        {
                            throw new Exception("File version not supported!");
                        }
                    }
                }
            }

            // Delete the old file format
            File.Delete(sessionFile);
            // Replace the session file with the new file
            File.Move(sessionFile + ".new", sessionFile);
        }

        public static bool Exists(string storageDirectory, string gatewayName)
        {
            if (string.IsNullOrWhiteSpace(gatewayName))
                return false;
            /*lock (knownFiles)
            {*/
            if (knownFiles.Contains(gatewayName.ToLower()))
                return true;
            var res = Directory.GetFiles(storageDirectory, gatewayName.ToLower() + ".*.data").Any();
            if (res)
                knownFiles.Add(gatewayName.ToLower());
            return res;
            //}
        }

        public void CleanOlderThan(int nbDays)
        {
            var toDel = DateTime.UtcNow.AddDays(-nbDays);

            var needToClose = true;

            foreach (var i in Directory.GetFiles(Context.StorageDirectory, Gateway.ToLower() + ".*.*").Where(row => DateOfFile(row) <= toDel))
            {
                if (needToClose)
                {
                    needToClose = false;
                    CloseFiles();
                }

                File.Delete(i);
            }

            /*lock (knownFiles)
            {*/
            knownFiles.Clear();
            //}
        }

        public DataFileStats GetLogsStats()
        {
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


        private void SetFile(string filename = null)
        {
            if (file != null)
                CloseFiles();

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

        public void SaveStats()
        {
            try
            {
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
        }

        private void UpdateGatewaySessions(DateTime? startNewSession = null)
        {
            var sessionFile = Context.StorageDirectory + "\\" + Gateway.ToLower() + ".sessions";
            using (var writer = new BinaryWriter(File.Open(sessionFile, FileMode.OpenOrCreate, FileAccess.Write)))
            {
                if (lastLogEntry.HasValue && nbLogEntries > 0)
                {
                    if (writer.BaseStream.Length == 0 || writer.BaseStream.Length < (sizeof(long) * 4 + sizeof(uint) + 257))
                    {
                        writer.Write(1973L); // FILE Signature
                        writer.Write(1U); // Version format
                        writer.Write(0L); // Start date
                        writer.Write(0L); // End date
                        writer.Write(0L); // Nb records
                        writer.Write((byte)0); // Type of restart
                        writer.Write((byte)0); // Nb chars of comment
                        writer.Write(new byte[255]); // Nb chars of comment
                    }

                    writer.BaseStream.Seek(writer.BaseStream.Length - (sizeof(long) * 2 + 257), SeekOrigin.Begin); // Move from the end
                    writer.Write(lastLogEntry.HasValue ? lastLogEntry.Value.ToBinary() : 0L); // End date
                    writer.Write(nbLogEntries);
                }
                if (startNewSession.HasValue)
                {
                    lock (lockCachedGatewaySessions)
                    {
                        cachedGatewaySessions = null;
                    }

                    writer.BaseStream.Seek(writer.BaseStream.Length, SeekOrigin.Begin);
                    writer.Write(startNewSession.Value.ToBinary()); // Start Date
                    writer.Write(0L); // End date
                    writer.Write(0L); // Nb records
                    writer.Write((byte)0); // Type of restart
                    writer.Write((byte)0); // Nb chars of comment
                    writer.Write(new byte[255]); // Nb chars of comment
                }
            }
        }

        public void UpdateLastGatewaySessionInformation(RestartType restartType, string comment)
        {
            var data = System.Text.Encoding.UTF8.GetBytes(comment);
            if (data.Length > 255)
            {
                var tmp = new byte[255];
                Array.Copy(data, tmp, 255);
                data = tmp;
            }

            var sessionFile = Context.StorageDirectory + "\\" + Gateway.ToLower() + ".sessions";
            using (var writer = new BinaryWriter(File.Open(sessionFile, FileMode.OpenOrCreate, FileAccess.Write)))
            {
                writer.BaseStream.Seek(writer.BaseStream.Length - 257, SeekOrigin.Begin); // Move from the end
                writer.Write((byte)restartType); // Type of restart
                writer.Write((byte)data.Length); // Nb chars of comment
                writer.Write(data);
                if (data.Length < 255)
                    writer.Write(new byte[255 - data.Length]);
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
            if(dt == "data")
                dt = filename.Split(new char[] { '.' }).Reverse().Take(3).Last();
            return new DateTime(int.Parse(dt.Substring(0, 4)), int.Parse(dt.Substring(4, 2)), int.Parse(dt.Substring(6, 2)));

        }

        public static int IndexPosition(DateTime date)
        {
            return date.Minute / 10 + date.Hour * 6;
        }

        private object lockCachedGatewaySessions = new object();
        private List<GatewaySession> cachedGatewaySessions = null;
        public List<GatewaySession> GetGatewaySessions()
        {
            lock (lockCachedGatewaySessions)
            {
                if (cachedGatewaySessions != null && cachedGatewaySessions.Count == 0)
                {
                    cachedGatewaySessions.Add(new GatewaySession { });
                    cachedGatewaySessions[0].NbEntries = nbLogEntries;
                    cachedGatewaySessions[0].EndDate = lastLogEntry;
                }
                if (cachedGatewaySessions != null)
                    return cachedGatewaySessions;
            }

            try
            {
                var result = new List<GatewaySession>();

                var sessionFile = Context.StorageDirectory + "\\" + Gateway.ToLower() + ".sessions";
                using (var reader = new BinaryReader(File.Open(sessionFile, FileMode.Open)))
                {
                    if (reader.ReadInt64() != 1973) // Wrong file signature
                        throw new Exception("Wrong file signature");
                    if (reader.ReadInt32() != 1) // Wrong file version
                        throw new Exception("Wrong file version");

                    var start = Math.Max(reader.BaseStream.Length - 100 * (sizeof(long) * 3 + 257), 12);

                    reader.BaseStream.Seek(start, SeekOrigin.Begin);
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        var s = reader.ReadInt64();
                        var e = reader.ReadInt64();
                        var n = reader.ReadInt64();
                        var restartType = reader.ReadByte();
                        var nbCharDesc = reader.ReadByte();
                        var descBytes = reader.ReadBytes(255);
                        var desc = nbCharDesc == 0 ? "" : System.Text.Encoding.UTF8.GetString(descBytes, 0, nbCharDesc);

                        result.Add(new GatewaySession
                        {
                            StartDate = (s == 0 ? null : (DateTime?)DateTime.FromBinary(s)),
                            EndDate = (e == 0 ? null : (DateTime?)DateTime.FromBinary(e)),
                            NbEntries = n,
                            RestartType = (RestartType)restartType,
                            Description = desc
                        });
                    }
                }
                result.Reverse();
                lock (lockCachedGatewaySessions)
                {
                    cachedGatewaySessions = result;
                }
                return result;
            }
            catch
            {
                return new List<GatewaySession>();
            }
        }

        public void Save(LogEntry entry, bool isAnError)
        {
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

            if (entry.MessageTypeId == 39)
                Stats[idxPos, 1]++;

            Stats[idxPos, 0]++;
            if (isAnError)
                Stats[idxPos, 2]++;
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
            if (!mustFlush)
                return;
            mustFlush = false;
            commandIndex?.Flush();
            DataWriter.Flush();
        }

        internal IEnumerable<LogEntry> ReadLog(DateTime start, DateTime end, Query.Statement.QueryNode query = null, List<int> messageTypes = null, bool onlyErrors = false, string startFile = null, long offset = 0)
        {
            var currentDate = new DateTime(start.Year, start.Month, start.Day);
            var firstLoop = true;
            var firstItem = true;

            while (currentDate < end)
            {
                var fileToUse = FileName(currentDate);
                if (onlyErrors && File.Exists(fileToUse + ".errs") && File.Exists(fileToUse))
                {
                    if (fileToUse != currentFile)
                        SetFile(fileToUse);

                    var streamLength = DataReader.BaseStream.Length;
                    using (var errors = new BinaryReader(File.OpenRead(fileToUse + ".errs")))
                    {
                        while (errors.BaseStream.Position < errors.BaseStream.Length)
                        {
                            Seek(errors.ReadInt64());
                            LogEntry entry = null;
                            try
                            {
                                entry = ReadEntry(DataReader, start, streamLength);
                            }
                            catch
                            {
                            }
                            if (entry == null)
                                yield return null;

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
                                yield return entry;
                            }
                            firstItem = false;
                            if (entry != null && entry.EntryDate > end)
                                yield break;
                        }
                    }
                }
                else if (onlyErrors && !File.Exists(fileToUse + ".errs"))
                {
                }
                else if (File.Exists(fileToUse))
                {
                    if (fileToUse != currentFile)
                        SetFile(fileToUse);

                    if (firstLoop && Index[IndexPosition(start)] != -1)
                        Seek(Index[IndexPosition(start)]);
                    else
                        Seek(0);
                    isAtEnd = false;
                    var streamLength = DataReader.BaseStream.Length;
                    while (DataReader.BaseStream.Position < streamLength)
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
                            yield return entry;
                        }
                        firstItem = false;
                        if (entry != null && entry.EntryDate > end)
                            yield break;
                    }
                }

                currentDate = currentDate.AddDays(1);
                firstLoop = false;
            }
        }

        internal List<LogEntry> ReadLastLogs(int nbEntries)
        {
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
                    try
                    {
                        chunk.Add(ReadEntry(DataReader, this.CurrentDate.Date, streamLength));
                    }
                    catch (EndOfStreamException)
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

        private void WriteEntry(BinaryWriter stream, LogEntry entry)
        {
            if (sourceMemberNameId == -1)
                sourceMemberNameId = Context.MessageDetailTypes.First(row => row.Value == "SourceMemberName").Id;
            if (sourceFilePathId == -1)
                sourceFilePathId = Context.MessageDetailTypes.First(row => row.Value == "SourceFilePath").Id;

            // Adds current position in case it's an error
            if (this.Context.errorMessages.Contains(entry.MessageTypeId))
            {
                using (var errIndex = new BinaryWriter(File.OpenWrite(currentFile + ".errs")))
                {
                    // Move to the end
                    errIndex.BaseStream.Seek(errIndex.BaseStream.Length, SeekOrigin.Begin);
                    errIndex.Write(stream.BaseStream.Length);
                }
            }

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
            for (var i = 0; i < 128 && pos + i < streamLength; i++)
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
                        detail.Value = Context.reverseFilePaths[n];
                    }
                }
                else if (detail.DetailTypeId == sourceMemberNameId)
                {
                    var n = stream.ReadUInt16();
                    lock (Context.filePaths)
                    {
                        detail.Value = Context.reverseMemberNames[n];
                    }
                }
                else
                {
                    pos = stream.BaseStream.Position;
                    try
                    {
                        detail.Value = stream.ReadString();
                        // oddies as string, we may have an issue with the record
                        if (detail.Value.Length > 1280)
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

        public List<GatewayStats> GetStats()
        {
            var result = new List<GatewayStats>();

            foreach (var i in Directory.GetFiles(Context.StorageDirectory, Gateway.ToLower() + ".*.stats")
            .OrderBy(row => row))
            {
                var entry = new GatewayStats
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

                var stats = new List<LogStat>[] { entry.Logs, entry.Searches, entry.Errors, entry.CPU, entry.PVs, entry.Clients, entry.Servers, entry.MsgSecs };

                using (var reader = new BinaryReader(File.Open(i, FileMode.Open, FileAccess.Read)))
                {
                    try
                    {
                        var nbColsAvailable = reader.BaseStream.Length / (sizeof(long) * Stats.GetLength(0));

                        for (var s = 0; s < Math.Min(nbColsAvailable, Stats.GetLength(1)); s++)
                            for (var j = 0; j < Stats.GetLength(0); j++)
                                stats[s].Add(new LogStat { Date = DateOfFile(i).AddMinutes(10 * j), Value = reader.ReadInt64() });

                        // Fill with 0
                        for (var s = nbColsAvailable; s < Stats.GetLength(1); s++)
                            for (var j = 0; j < Stats.GetLength(0); j++)
                                stats[s].Add(new LogStat { Date = DateOfFile(i).AddMinutes(10 * j), Value = 0 });

                    }
                    catch
                    {
                    }
                }

                result.Add(entry);

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

            var dayStart = new DateTime(start.Year, start.Month, start.Day);

            foreach (var i in Directory.GetFiles(Context.StorageDirectory, Gateway.ToLower() + ".*.stats")
                .OrderBy(row => row)
                .Where(row => DateOfFile(row) >= dayStart && DateOfFile(row) <= end))
            {

                using (var reader = new BinaryReader(File.Open(i, FileMode.Open, FileAccess.Read)))
                {
                    try
                    {
                        var nbColsAvailable = reader.BaseStream.Length / (sizeof(long) * Stats.GetLength(0));

                        for (var s = 0; s < Math.Min(nbColsAvailable, Stats.GetLength(1)); s++)
                        {
                            for (var j = 0; j < Stats.GetLength(0); j++)
                            {
                                var t = DateOfFile(i).AddMinutes(10 * j);
                                var v = reader.ReadInt64();
                                if (t >= start && t <= end)
                                    stats[s].Add(new LogStat { Date = t, Value = v });
                            }
                        }

                        // Fill with 0
                        for (var s = nbColsAvailable; s < Stats.GetLength(1); s++)
                        {
                            for (var j = 0; j < Stats.GetLength(0); j++)
                            {
                                var t = DateOfFile(i).AddMinutes(10 * j);
                                if (t >= start && t <= end)
                                    stats[s].Add(new LogStat { Date = t, Value = 0 });
                            }
                        }
                    }
                    catch
                    {
                    }
                }
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
            if (lockObject == null)
                return;
            commandIndex?.Dispose();
            commandIndex = null;
            try
            {
                lockObject.Wait();
            }
            finally
            {
                lockObject.Release();
            }
            CloseFiles();
            lockObject.Dispose();
            lockObject = null;
        }
    }
}