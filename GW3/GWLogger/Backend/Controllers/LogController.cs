using EntityFramework.Utilities;
using GWLogger.Backend.Model;
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
        static Thread statFlusher;
        static LogStat logEntriesStats = new LogStat();
        static LogStat errorsStats = new LogStat();
        static LogStat searchesStats = new LogStat();
        static List<int> errorMessageTypes = null;
        static GatewaySessions gatewaySessions = new GatewaySessions();

        static LogController()
        {
            logEntryFlusher = new Thread(BulkSaver);
            logEntryFlusher.IsBackground = true;
            logEntryFlusher.Start();

            statFlusher = new Thread(StatUpdater);
            statFlusher.IsBackground = true;
            statFlusher.Start();
        }

        private static void StatUpdater()
        {
            while (true)
            {
                Thread.Sleep(1000);

                List<LogStat.StatEntry> logs;
                List<LogStat.StatEntry> errors;
                List<LogStat.StatEntry> searches;
                List<GatewaySessions.OpenSession> sessions;

                using (var ctx = new Model.LoggerContext())
                {
                    lock (logEntriesStats)
                    {
                        logs = logEntriesStats.GetListAndClear();
                        errors = errorsStats.GetListAndClear();
                        searches = searchesStats.GetListAndClear();
                        sessions = gatewaySessions.GetAndReset();
                    }

                    foreach (var i in logs)
                    {
                        var nbMsg = ctx.GatewayNbMessages.FirstOrDefault(row => row.Gateway == i.Gateway && row.Date == i.Date);
                        if (nbMsg == null)
                            ctx.GatewayNbMessages.Add(new GatewayNbMessage { Gateway = i.Gateway, NbMessages = i.Value, Date = i.Date });
                        else
                            nbMsg.NbMessages += i.Value;
                    }


                    foreach (var i in errors)
                    {
                        var nbErrs = ctx.GatewayErrors.FirstOrDefault(row => row.Gateway == i.Gateway && row.Date == i.Date);
                        if (nbErrs == null)
                            ctx.GatewayErrors.Add(new GatewayError { Gateway = i.Gateway, NbErrors = i.Value, Date = i.Date });
                        else
                            nbErrs.NbErrors += i.Value;
                    }

                    foreach (var i in searches)
                    {
                        var nbSrch = ctx.GatewaySearches.FirstOrDefault(row => row.Gateway == i.Gateway && row.Date == i.Date);
                        if (nbSrch == null)
                            ctx.GatewaySearches.Add(new GatewaySearch { Gateway = i.Gateway, NbSearches = i.Value, Date = i.Date });
                        else
                            nbSrch.NbSearches += i.Value;
                    }

                    foreach (var i in sessions)
                    {
                        var session = ctx.GatewaySessions.FirstOrDefault(row => row.Gateway == i.Gateway && row.StartDate == i.Start);
                        if (session == null)
                            ctx.GatewaySessions.Add(new GatewaySession { Gateway = i.Gateway, StartDate = i.Start, LastEntry = i.End, NbEntries = i.NbEntries });
                        else
                        {
                            session.LastEntry = i.End;
                            session.NbEntries = i.NbEntries;
                        }
                    }

                    ctx.SaveChanges();
                }
            }
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

            lock (logEntriesStats)
            {
                logEntriesStats[gateway][newEntry.EntryDate.Round()]++;
                if (newEntry.MessageTypeId == 2)
                    gatewaySessions[gateway].Restart();
                if(newEntry.MessageTypeId >= 2)
                    gatewaySessions[gateway].Log();
                if (errorMessageTypes == null)
                    using (var ctx = new LoggerContext())
                        errorMessageTypes = ctx.LogMessageTypes.Where(row => row.LogLevel >= 3).Select(row => row.MessageTypeId).ToList();
                if (errorMessageTypes.Contains(newEntry.MessageTypeId))
                    errorsStats[gateway][newEntry.EntryDate.Round()]++;
                if (newEntry.MessageTypeId == 39)
                    searchesStats[gateway][newEntry.EntryDate.Round()]++;
            }
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