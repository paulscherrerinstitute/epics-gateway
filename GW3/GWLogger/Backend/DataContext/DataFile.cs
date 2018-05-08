﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace GWLogger.Backend.DataContext
{
    public class DataFile : IDisposable
    {
        private string gateway;
        public SemaphoreSlim LockObject { get; private set; } = new SemaphoreSlim(1);
        FileStream file;
        private BinaryWriter DataWriter { get; set; }
        public BinaryReader DataReader { get; private set; }
        public long[] Index = new long[24 * 6];
        string currentFile;

        FileStream clientSession;
        string currentClientSession;
        Dictionary<string, SessionLocation> openClientSessions = new Dictionary<string, SessionLocation>();

        FileStream serverSession;
        string currentServerSession;
        Dictionary<string, SessionLocation> openServerSessions = new Dictionary<string, SessionLocation>();

        FileStream searches;
        string currentSearches;

        private bool isAtEnd = true;
        private bool mustFlush = false;

        public static string StorageDirectory => System.Configuration.ConfigurationManager.AppSettings["storageDirectory"];

        public static List<string> Gateways
        {
            get
            {
                return Directory.GetFiles(StorageDirectory, "*.data")
                    .Select(row => row.Substring(StorageDirectory.Length + 1).Split(new char[] { '.' }))
                    .First()
                    .Distinct()
                    .OrderBy(row => row)
                    .ToList();
            }
        }

        public static void DeleteFiles(string gateway)
        {
            // Delete all the data files
            foreach (var i in Directory.GetFiles(StorageDirectory, gateway.ToLower() + ".*.data"))
                File.Delete(i);
            // Delete all the index files
            foreach (var i in Directory.GetFiles(StorageDirectory, gateway.ToLower() + ".*.idx"))
                File.Delete(i);
            // Delete all the clientSessions files
            foreach (var i in Directory.GetFiles(StorageDirectory, gateway.ToLower() + ".*.clientSessions"))
                File.Delete(i);
            // Delete all the serverSessions files
            foreach (var i in Directory.GetFiles(StorageDirectory, gateway.ToLower() + ".*.serverSessions"))
                File.Delete(i);
            // Delete all the searches files
            foreach (var i in Directory.GetFiles(StorageDirectory, gateway.ToLower() + ".*.searches"))
                File.Delete(i);
        }

        public DataFile(string gateway)
        {
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
                if (!isAtEnd)
                    file.Seek(0, SeekOrigin.End);

                DataWriter.Dispose();
                DataReader.Dispose();
                file.Dispose();
            }

            currentFile = filename ?? FileName();
            file = System.IO.File.Open(currentFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            DataWriter = new BinaryWriter(file, System.Text.Encoding.UTF8, true);
            DataReader = new BinaryReader(file, System.Text.Encoding.UTF8, true);

            file.Seek(0, SeekOrigin.End);
            isAtEnd = true;

            ReadIndex();
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

        public string FileName(DateTime? forDate = null, string extention = ".data")
        {
            if (!forDate.HasValue)
                forDate = DateTime.UtcNow;

            return StorageDirectory + "\\" + gateway.ToLower() + "." + forDate.Value.Year + ("" + forDate.Value.Month).PadLeft(2, '0') + ("" + forDate.Value.Day).PadLeft(2, '0') + extention;
        }

        public DateTime CurrentDate
        {
            get
            {
                var dt = currentFile.Split(new char[] { '.' }).Reverse().Take(2).Last();
                return new DateTime(int.Parse(dt.Substring(0, 4)), int.Parse(dt.Substring(4, 2)), int.Parse(dt.Substring(6, 2)));
            }
        }

        int IndexPosition(DateTime date)
        {
            return date.Minute / 10 + date.Hour * 6;
        }

        public void Save(LogEntry entry)
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

                DataWriter.Write(entry.EntryDate.ToBinary());
                DataWriter.Write((byte)entry.MessageTypeId);
                DataWriter.Write(entry.RemoteIpPoint);

                DataWriter.Write((byte)entry.LogEntryDetails.Count);
                foreach (var i in entry.LogEntryDetails)
                {
                    DataWriter.Write((byte)i.DetailTypeId);
                    DataWriter.Write(i.Value);
                }

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

        static int SerializedStringLength(int length)
        {
            var nbBits = (Math.Log(length) / Math.Log(2)) / 8.0;
            var sizePrefix = (int)Math.Ceiling(Math.Max(1, nbBits));
            if ((length & (1 << (sizePrefix * 8 - 1))) != 0)
                sizePrefix++;
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
                DataWriter.Flush();
            }
            finally
            {
                LockObject.Release();
            }
        }

        public List<LogEntry> ReadLog(DateTime start, DateTime end)
        {
            try
            {
                var result = new List<LogEntry>();
                LockObject.Wait();

                var currentDate = new DateTime(start.Year, start.Month, start.Day);
                var firstLoop = true;

                while (currentDate < end)
                {
                    var fileToUse = FileName(currentDate);
                    if (File.Exists(fileToUse))
                    {
                        if (fileToUse != currentFile)
                            SetFile(fileToUse);

                        if (firstLoop)
                            Seek(Index[IndexPosition(start)]);
                        else
                            Seek(0);

                        while (DataReader.BaseStream.Position < DataReader.BaseStream.Length)
                        {
                            var entry = ReadEntry(DataReader);

                            if (entry != null && entry.EntryDate >= start && entry.EntryDate <= end)
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

        LogEntry ReadEntry(BinaryReader stream)
        {
            var result = new LogEntry
            {
                EntryDate = DateTime.FromBinary(stream.ReadInt64()),
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
        }

        public List<LogSession> ReadClientSessions(DateTime start, DateTime end)
        {
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

                foreach (var i in Directory.GetFiles(StorageDirectory, gateway.ToLower() + ".*.clientSessions"))
                {
                    using (var reader = new BinaryReader(File.Open(i, FileMode.Open, FileAccess.Read), System.Text.Encoding.UTF8))
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
            finally
            {
                LockObject.Release();
            }

            return result.OrderBy(row => row.Start).ThenBy(row => row.Remote).ToList();
        }

        public List<LogSession> ReadServerSessions(DateTime start, DateTime end)
        {
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

                foreach (var i in Directory.GetFiles(StorageDirectory, gateway.ToLower() + ".*.serverSessions"))
                {
                    using (var reader = new BinaryReader(File.Open(i, FileMode.Open, FileAccess.Read), System.Text.Encoding.UTF8))
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
            finally
            {
                LockObject.Release();
            }

            return result.OrderBy(row => row.Start).ThenBy(row => row.Remote).ToList();
        }

        public List<SearchEntry> ReadSearches(DateTime start, DateTime end)
        {
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

                foreach (var i in Directory.GetFiles(StorageDirectory, gateway.ToLower() + ".*.searches"))
                {
                    using (var reader = new BinaryReader(File.Open(i, FileMode.Open, FileAccess.Read), System.Text.Encoding.UTF8))
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
            finally
            {
                LockObject.Release();
            }

            return result.OrderBy(row => row.Channel).ToList();
        }

        public void Dispose()
        {
            try
            {
                LockObject.Wait();

                file.Seek(0, SeekOrigin.End);

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
            finally
            {
                LockObject.Release();
            }
            LockObject.Dispose();
        }
    }
}