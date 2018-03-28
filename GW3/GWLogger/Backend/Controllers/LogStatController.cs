using GWLogger.Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace GWLogger.Backend.Controllers
{
    public static class LogStatController
    {
        static Thread statFlusher;
        static LogStat logEntriesStats = new LogStat();
        static LogStat errorsStats = new LogStat();
        static LogStat searchesStats = new LogStat();
        static List<int> errorMessageTypes = null;
        static GatewaySessions gatewaySessions = new GatewaySessions();
        static List<Search> searchList = new List<Search>();

        static LogStatController()
        {
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
                List<Search> allSearches;

                using (var ctx = new Model.LoggerContext())
                {
                    lock (logEntriesStats)
                    {
                        logs = logEntriesStats.GetListAndClear();
                        errors = errorsStats.GetListAndClear();
                        searches = searchesStats.GetListAndClear();
                        sessions = gatewaySessions.GetAndReset();
                        allSearches = searchList.ToList();
                        searchList.Clear();
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
                            ctx.GatewaySessions.Add(new GatewaySession
                            {
                                Gateway = i.Gateway,
                                StartDate = i.Start,
                                LastEntry = i.End,
                                NbEntries = i.NbEntries
                            });
                        else
                        {
                            session.LastEntry = i.End;
                            session.NbEntries = i.NbEntries;
                        }
                    }

                    foreach (var i in allSearches.GroupBy(row => row,
                        new EqualityComparer<Search>((a, b) =>
                            a.Channel == b.Channel && a.Client == b.Client && a.Gateway == b.Gateway && a.Date == b.Date
                        )))
                    {
                        var s = ctx.SearchedChannels.FirstOrDefault(row => row.Gateway == i.Key.Gateway && row.Client == i.Key.Client && row.Channel == i.Key.Channel && row.SearchDate == i.Key.Date);
                        if (s == null)
                        {
                            ctx.SearchedChannels.Add(new SearchedChannel
                            {
                                Channel = i.Key.Channel,
                                Client = i.Key.Client,
                                Gateway = i.Key.Gateway,
                                NbSearches = i.Count(),
                                SearchDate = i.Key.Date
                            });
                        }
                        else
                            s.NbSearches += i.Count();
                    }

                    ctx.SaveChanges();
                }
            }
        }

        public static void Record(Model.LogEntry newEntry)
        {
            lock (logEntriesStats)
            {
                if (errorMessageTypes == null)
                    using (var ctx = new LoggerContext())
                        errorMessageTypes = ctx.LogMessageTypes.Where(row => row.LogLevel >= 3).Select(row => row.MessageTypeId).ToList();

                var gateway = newEntry.Gateway;
                logEntriesStats[gateway][newEntry.EntryDate.Trim()]++;
                switch (newEntry.MessageTypeId)
                {
                    case 4: // Start client session
                        ClientSessions.Connect(gateway, newEntry.RemoteIpPoint);
                        gatewaySessions[gateway].Log();
                        break;
                    case 6: // Ends client session
                        ClientSessions.Disconnect(gateway, newEntry.RemoteIpPoint);
                        gatewaySessions[gateway].Log();
                        break;
                    case 2: // Starts GW
                        gatewaySessions[gateway].Restart();
                        gatewaySessions[gateway].Log();
                        break;
                    case 39: //Search
                        searchesStats[gateway][newEntry.EntryDate.Trim()]++;
                        searchList.Add(new Search
                        {
                            Client = newEntry.RemoteIpPoint.Split(new char[] { ':' })[0],
                            Gateway = gateway,
                            Date = newEntry.EntryDate.Trim(),
                            Channel = newEntry
                                .LogEntryDetails
                                .Where(row => row.DetailTypeId == 7)
                                .FirstOrDefault()?.Value
                        });
                        gatewaySessions[gateway].Log();
                        break;
                    case 55: // Starts server TCP
                        ServerSessions.Connect(gateway, newEntry.RemoteIpPoint);
                        gatewaySessions[gateway].Log();
                        break;
                    case 53: // Ends server TCP
                        ServerSessions.Disconnect(gateway, newEntry.RemoteIpPoint);
                        gatewaySessions[gateway].Log();
                        break;
                    // Skip
                    case 0:
                    case 1:
                        break;
                    default:
                        if (errorMessageTypes.Contains(newEntry.MessageTypeId))
                            errorsStats[gateway][newEntry.EntryDate.Trim()]++;
                        gatewaySessions[gateway].Log();
                        break;
                }
            }
        }
    }
}