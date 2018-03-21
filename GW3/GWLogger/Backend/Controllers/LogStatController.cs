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
                logEntriesStats[gateway][newEntry.EntryDate.Round()]++;
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
                        searchesStats[gateway][newEntry.EntryDate.Round()]++;
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
                            errorsStats[gateway][newEntry.EntryDate.Round()]++;
                        gatewaySessions[gateway].Log();
                        break;
                }
            }
        }
    }
}