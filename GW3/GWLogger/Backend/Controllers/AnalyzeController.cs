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
            return Global.DataContext.GetGatewaySessions(gatewayName);
            //return new List<DTOs.GatewaySession>();
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

        public static DTOs.GatewayStats GetStats(string gatewayName, DateTime start, DateTime end)
        {
            return Global.DataContext.GetStats(gatewayName, start, end);
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
            return Global.DataContext.ReadSearches(gatewayName, datePoint, datePoint).
                GroupBy(row => row.Remote + "@" + row.Channel).Select(row => new DTOs.SearchRequest
                {
                    Channel = row.First().Channel,
                    Date = row.First().Date,
                    Client = row.First().Remote,
                    NbSearches = row.Count()
                }).ToList();
        }
    }
}