using GWLogger.Backend.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;

namespace GWLogger.Backend.Controllers
{
    public static class LogController
    {
        static List<Model.LogEntry> entryToAdd = new List<Model.LogEntry>();
        static SemaphoreSlim lockEntry = new SemaphoreSlim(1);
        static Thread logEntryFlusher;
        static DateTime lastCleaning = DateTime.UtcNow.AddDays(-1);
        static long nextEntryId = 0;

        static LogController()
        {
            logEntryFlusher = new Thread(BulkSaver);
            logEntryFlusher.IsBackground = true;
            logEntryFlusher.Start();

            using (var ctx = new LoggerContext())
            {
                try
                {
                    nextEntryId = ctx.LogEntries.Max(row => row.EntryId);
                }
                catch
                {
                    nextEntryId = 0;
                }
            }
        }

        public static void BulkSaver()
        {
            while (true)
            {
                if ((DateTime.UtcNow - lastCleaning).TotalDays >= 0)
                {
                    CleanLogs();
                    lastCleaning = DateTime.UtcNow;
                }

                lockEntry.Wait();
                if (entryToAdd.Count == 0)
                {
                    lockEntry.Release();
                    Thread.Sleep(100);
                    continue;
                }

                var toAdd = entryToAdd.ToList();
                entryToAdd.Clear();
                lockEntry.Release();

                foreach (var i in toAdd)
                {
                    var nextId = System.Threading.Interlocked.Increment(ref nextEntryId);
                    i.EntryId = nextId;
                    foreach (var j in i.LogEntryDetails)
                        j.LogEntryId = nextId;
                }

                try
                {
                    using (var ctx = new Model.LoggerContext())
                    {
                        Bulk.Insert(ctx, ctx.LogEntries, toAdd);
                        Bulk.Insert(ctx, ctx.LogEntryDetails, toAdd.SelectMany(row => row.LogEntryDetails));
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }

        private static void CleanLogs()
        {
            using (var ctx = new Model.LoggerContext())
            {
                var toDeleteDate = DateTime.UtcNow.AddDays(-10);
                ctx.Database.Connection.Open();

                ctx.Database.ExecuteSqlCommand("DELETE FROM LogEntryDetails WHERE LogEntryId IN (SELECT EntryId FROM LogEntries WHERE TrimmedDate < @d1)", new System.Data.SqlClient.SqlParameter("@d1", toDeleteDate));
                ctx.Database.ExecuteSqlCommand("DELETE FROM LogEntries WHERE TrimmedDate < @d1", new System.Data.SqlClient.SqlParameter("@d1", toDeleteDate));
            }
        }

        public static void LogEntry(string gateway, string remoteIpPoint, int messageType, List<DTOs.LogEntryDetail> details)
        {
            var newEntry = new Model.LogEntry
            {
                EntryDate = DateTime.UtcNow,
                Gateway = gateway,
                MessageTypeId = messageType,
                RemoteIpPoint = remoteIpPoint
            };
            details
                .Select(row => new Model.LogEntryDetail
                {
                    DetailTypeId = row.TypeId,
                    Value = row.Value
                })
                .ToList()
                .ForEach(row => newEntry.LogEntryDetails.Add(row));

            lockEntry.Wait();
            while (entryToAdd.Count() > 30000)
            {
                lockEntry.Release();
                Thread.Sleep(100);
                lockEntry.Wait();
            }
            entryToAdd.Add(newEntry);
            lockEntry.Release();

            LogStatController.Record(newEntry);
        }

        public static void RegisterLogMessageType(List<DTOs.MessageType> types)
        {
            using (var ctx = new Model.LoggerContext())
            {
                var dbTypes = ctx.LogMessageTypes.ToList();
                var toAdd = types.Where(row => !dbTypes.Any(r2 => r2.MessageTypeId == row.Id));
                ctx.LogMessageTypes.AddRange(toAdd.Select(row => new Model.LogMessageType
                {
                    MessageTypeId = row.Id,
                    Name = row.Name,
                    DisplayMask = row.DisplayMask,
                    LogLevel = row.LogLevel
                }));
                try
                {
                    ctx.SaveChanges();
                }
                catch
                {
                }
            }
        }

        public static void RegisterLogMessageDetailType(List<DTOs.IdValue> types)
        {
            using (var ctx = new Model.LoggerContext())
            {
                var dbTypes = ctx.LogDetailItemTypes.ToList();
                var toAdd = types.Where(row => !dbTypes.Any(r2 => r2.ItemId == row.Id));
                ctx.LogDetailItemTypes.AddRange(toAdd.Select(row => new Model.LogDetailItemType { ItemId = row.Id, Name = row.Value }));
                try
                {
                    ctx.SaveChanges();
                }
                catch
                {
                }
            }
        }
    }
}