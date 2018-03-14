using GWLogger.Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GWLogger.Backend.Controllers
{
    public static class AnalyzeController
    {
        public static List<string> GetGatewaysList()
        {
            using (var ctx = new LoggerContext())
            {
                return ctx.GatewaySessions.GroupBy(row => row.Gateway).Select(row => row.Key).ToList();
            }
        }

        public static List<DTOs.GatewaySession> GetGatewaySessionsList(string gatewayName)
        {
            using (var ctx = new LoggerContext())
            {
                return ctx.GatewaySessions.Where(row => row.Gateway == gatewayName).OrderByDescending(row => row.StartDate)
                .Select(row => new DTOs.GatewaySession
                {
                    StartDate = row.StartDate,
                    EndDate = row.LastEntry,
                    NbEntries = row.NbEntries
                }).ToList();
            }
        }

        public static List<DTOs.LogStat> GetLogStats(string gatewayName, DateTime start, DateTime end)
        {
            using (var ctx = new LoggerContext())
            {
                return ctx.GatewayNbMessages.Where(row => row.Gateway == gatewayName && row.Date >= start && row.Date <= end)
                    .Select(row => new DTOs.LogStat { Date = row.Date, Value = row.NbMessages }).ToList();
            }
        }

        public static List<DTOs.LogStat> GetErrorStats(string gatewayName, DateTime start, DateTime end)
        {
            using (var ctx = new LoggerContext())
            {
                return ctx.GatewayErrors.Where(row => row.Gateway == gatewayName && row.Date >= start && row.Date <= end)
                    .Select(row => new DTOs.LogStat { Date = row.Date, Value = row.NbErrors }).ToList();
            }
        }

        public static List<DTOs.LogStat> GetSearchesStats(string gatewayName, DateTime start, DateTime end)
        {
            using (var ctx = new LoggerContext())
            {
                return ctx.GatewaySearches.Where(row => row.Gateway == gatewayName && row.Date >= start && row.Date <= end)
                    .Select(row => new DTOs.LogStat { Date = row.Date, Value = row.NbSearches }).ToList();
            }
        }

        public static DTOs.GatewayStats GetStats(string gatewayName, DateTime start, DateTime end)
        {
            return new DTOs.GatewayStats
            {
                Logs = GetLogStats(gatewayName, start, end),
                Searches = GetSearchesStats(gatewayName, start, end),
                Errors = GetErrorStats(gatewayName, start, end)
            };
        }

        public static List<DTOs.Connection> GetConnectedClients(string gatewayName, DateTime when)
        {
            using (var ctx = new LoggerContext())
            {
                return ctx.ConnectedClients.Where(row => row.Gateway == gatewayName
                    && row.StartConnection <= when && row.EndConnection >= when)
                    .OrderBy(row => row.RemoteIpPoint)
                    .Select(row => new DTOs.Connection
                    {
                        RemoteIpPoint = row.RemoteIpPoint,
                        Start = row.StartConnection,
                        End = row.EndConnection
                    }).ToList();
            }
        }

        public static List<DTOs.Connection> GetConnectedServers(string gatewayName, DateTime when)
        {
            using (var ctx = new LoggerContext())
            {
                return ctx.ConnectedServers.Where(row => row.Gateway == gatewayName
                    && row.StartConnection <= when && row.EndConnection >= when)
                    .OrderBy(row => row.RemoteIpPoint)
                    .Select(row => new DTOs.Connection
                    {
                        RemoteIpPoint = row.RemoteIpPoint,
                        Start = row.StartConnection,
                        End = row.EndConnection
                    }).ToList();
            }
        }

        public static List<DTOs.SearchRequest> GetSearchedChannels(string gatewayName, DateTime datePoint)
        {
            using (var ctx = new LoggerContext())
            {
                var start = datePoint.AddSeconds(-1);
                var end = datePoint.AddSeconds(1);

                return ctx.LogEntries.Where(row => row.MessageTypeId == 39 && row.Gateway == gatewayName && row.EntryDate >= start && row.EntryDate <= end)
                //return ctx.LogEntries.Where(row => row.MessageTypeId == 39 && row.Gateway == gatewayName)
                    .OrderBy(row => row.EntryDate)
                    .Select(row => new DTOs.SearchRequest
                    {
                        Channel = row.LogEntryDetails.Where(r2 => r2.DetailTypeId == 7).FirstOrDefault().Value,
                        Date = row.EntryDate,
                        Client = row.RemoteIpPoint
                    }).ToList();
            }
        }
    }
}