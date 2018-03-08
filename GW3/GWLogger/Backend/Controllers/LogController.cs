using EntityFramework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace GWLogger.Backend.Controllers
{
    public static class LogController
    {
        static List<Model.LogEntry> entryToAdd = new List<Model.LogEntry>();
        static SemaphoreSlim lockEntry = new SemaphoreSlim(1);
        static Thread logEntryFlusher;

        static LogController()
        {
            logEntryFlusher = new Thread(BulkSaver);
            logEntryFlusher.IsBackground = true;
            logEntryFlusher.Start();
        }

        public static void BulkSaver()
        {
            while (true)
            {
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
                    i.EntryId = Guid.NewGuid();
                    foreach (var j in i.LogEntryDetails)
                    {
                        j.EntryDetailId = Guid.NewGuid();
                        j.LogEntryId = i.EntryId;
                    }
                }

                try
                {
                    using (var ctx = new Model.LoggerContext())
                    {
                        EFBatchOperation.For(ctx, ctx.LogEntries).InsertAll(toAdd);
                        EFBatchOperation.For(ctx, ctx.LogEntryDetails).InsertAll(toAdd.SelectMany(row => row.LogEntryDetails));
                    }
                }
                catch
                {
                }
            }
        }

        public static void LogEntry(string gateway, string remoteIpPoint, int messageType, List<DTOs.LogEntryDetail> details)
        {

            var newEntry = new Model.LogEntry
            {
                EntryDate = DateTime.UtcNow,
                Gateway = gateway,
                MessageTypeId = messageType
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
        }

        public static void RegisterLogMessageType(List<DTOs.MessageType> types)
        {
            using (var ctx = new Model.LoggerContext())
            {
                var dbTypes = ctx.LogMessageTypes.ToList();
                var toAdd = types.Where(row => !dbTypes.Any(r2 => r2.MessageTypeId == row.Id));
                ctx.LogMessageTypes.AddRange(toAdd.Select(row => new Model.LogMessageType { MessageTypeId = row.Id, Name = row.Name, DisplayMask = row.DisplayMask }));
                ctx.SaveChanges();
            }
        }

        public static void RegisterLogMessageDetailType(List<DTOs.IdValue> types)
        {
            using (var ctx = new Model.LoggerContext())
            {
                var dbTypes = ctx.LogDetailItemTypes.ToList();
                var toAdd = types.Where(row => !dbTypes.Any(r2 => r2.ItemId == row.Id));
                ctx.LogDetailItemTypes.AddRange(toAdd.Select(row => new Model.LogDetailItemType { ItemId = row.Id, Name = row.Value }));
                ctx.SaveChanges();
            }
        }
    }
}