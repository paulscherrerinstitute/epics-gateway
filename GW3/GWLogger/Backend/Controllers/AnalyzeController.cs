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
            return Global.DataContext.Gateways;
        }

        public static List<DTOs.GatewaySession> GetGatewaySessionsList(string gatewayName)
        {
            return new List<DTOs.GatewaySession>();
            /*using (var ctx = new LoggerContext())
            {
                return ctx.GatewaySessions.Where(row => row.Gateway == gatewayName).OrderByDescending(row => row.StartDate)
                .Select(row => new DTOs.GatewaySession
                {
                    StartDate = row.StartDate,
                    EndDate = row.LastEntry,
                    NbEntries = row.NbEntries
                }).ToList();
            }*/
        }

        public static List<DTOs.LogStat> GetLogStats(string gatewayName, DateTime start, DateTime end)
        {
            return new List<DTOs.LogStat>();
            /*using (var ctx = new LoggerContext())
            {
                return ctx.GatewayNbMessages.Where(row => row.Gateway == gatewayName && row.Date >= start && row.Date <= end)
                    .Select(row => new DTOs.LogStat { Date = row.Date, Value = row.NbMessages }).ToList();
            }*/
        }

        public static List<DTOs.LogStat> GetErrorStats(string gatewayName, DateTime start, DateTime end)
        {
            return new List<DTOs.LogStat>();
            /*using (var ctx = new LoggerContext())
            {
                return ctx.GatewayErrors.Where(row => row.Gateway == gatewayName && row.Date >= start && row.Date <= end)
                    .Select(row => new DTOs.LogStat { Date = row.Date, Value = row.NbErrors }).ToList();
            }*/
        }

        public static List<DTOs.LogStat> GetSearchesStats(string gatewayName, DateTime start, DateTime end)
        {
            return new List<DTOs.LogStat>();
            /*using (var ctx = new LoggerContext())
            {
                return ctx.GatewaySearches.Where(row => row.Gateway == gatewayName && row.Date >= start && row.Date <= end)
                    .Select(row => new DTOs.LogStat { Date = row.Date, Value = row.NbSearches }).ToList();
            }*/
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

        public static DTOs.Connections GetConnectionsBetween(string gatewayName, DateTime start, DateTime end)
        {
            return new DTOs.Connections
            {
                Clients = GetConnectedClientsBetween(gatewayName, start, end),
                Servers = GetConnectedServersBetween(gatewayName, start, end)
            };
        }

        public static List<DTOs.Connection> GetConnectedClientsBetween(string gatewayName, DateTime start, DateTime end)
        {
            return Global.DataContext.ReadClientSessions(gatewayName, start, end).Select(row => new DTOs.Connection
            {
                End = row.End,
                RemoteIpPoint = row.Remote,
                Start = row.Start
            }).ToList();
            /*using (var ctx = new LoggerContext())
            {
                return ctx.ConnectedClients.Where(row => row.Gateway == gatewayName
                    && (!(row.EndConnection < start || row.StartConnection > end)
                    || (row.EndConnection == null && end >= row.StartConnection)))
                    .OrderBy(row => row.RemoteIpPoint)
                    .Select(row => new DTOs.Connection
                    {
                        RemoteIpPoint = row.RemoteIpPoint,
                        Start = row.StartConnection,
                        End = row.EndConnection
                    }).Take(1000).ToList();
            }*/
        }

        public static List<DTOs.Connection> GetConnectedClients(string gatewayName, DateTime when)
        {
            return GetConnectedClientsBetween(gatewayName, when, when);
            /*using (var ctx = new LoggerContext())
            {
                return ctx.ConnectedClients.Where(row => row.Gateway == gatewayName
                    && row.StartConnection <= when && row.EndConnection >= when)
                    .OrderBy(row => row.RemoteIpPoint)
                    .Select(row => new DTOs.Connection
                    {
                        RemoteIpPoint = row.RemoteIpPoint,
                        Start = row.StartConnection,
                        End = row.EndConnection
                    }).Take(1000).ToList();
            }*/
        }

        public static List<DTOs.Connection> GetConnectedServersBetween(string gatewayName, DateTime start, DateTime end)
        {
            return Global.DataContext.ReadServerSessions(gatewayName, start, end).Select(row => new DTOs.Connection
            {
                End = row.End,
                RemoteIpPoint = row.Remote,
                Start = row.Start
            }).ToList();
            /*using (var ctx = new LoggerContext())
            {
                return ctx.ConnectedServers.Where(row => row.Gateway == gatewayName
                    && (!(row.EndConnection < start || row.StartConnection > end)
                    || (row.EndConnection == null && end >= row.StartConnection)))
                    .OrderBy(row => row.RemoteIpPoint)
                    .Select(row => new DTOs.Connection
                    {
                        RemoteIpPoint = row.RemoteIpPoint,
                        Start = row.StartConnection,
                        End = row.EndConnection
                    }).Take(1000).ToList();
            }*/
        }

        public static List<DTOs.Connection> GetConnectedServers(string gatewayName, DateTime when)
        {
            return Global.DataContext.ReadServerSessions(gatewayName, when, when).Select(row => new DTOs.Connection
            {
                End = row.End,
                RemoteIpPoint = row.Remote,
                Start = row.Start
            }).ToList();
            /*using (var ctx = new LoggerContext())
            {
                return ctx.ConnectedServers.Where(row => row.Gateway == gatewayName
                    && row.StartConnection <= when && row.EndConnection >= when)
                    .OrderBy(row => row.RemoteIpPoint)
                    .Select(row => new DTOs.Connection
                    {
                        RemoteIpPoint = row.RemoteIpPoint,
                        Start = row.StartConnection,
                        End = row.EndConnection
                    }).Take(1000).ToList();
            }*/
        }

        public static List<DTOs.SearchRequest> GetSearchedChannels(string gatewayName, DateTime datePoint)
        {
            return Global.DataContext.ReadSearches(gatewayName, datePoint, datePoint).Select(row => new DTOs.SearchRequest
            {
                Channel = row.Channel,
                Client = row.Remote,
                Date = row.Date
            }).ToList();
            /*using (var ctx = new LoggerContext())
            {
                var start = datePoint.AddSeconds(-1);
                var end = datePoint.AddSeconds(1);

                return ctx.SearchedChannels.Where(row => row.Gateway == gatewayName && row.SearchDate == datePoint)
                //return ctx.LogEntries.Where(row => row.MessageTypeId == 39 && row.Gateway == gatewayName)
                    .OrderBy(row => row.Channel)
                    .ThenBy(row => row.Client)
                    .Select(row => new DTOs.SearchRequest
                    {
                        Channel = row.Channel,
                        Date = row.SearchDate,
                        Client = row.Client,
                        NbSearches = row.NbSearches
                    }).Take(1000).ToList();
            }*/
        }
    }
}