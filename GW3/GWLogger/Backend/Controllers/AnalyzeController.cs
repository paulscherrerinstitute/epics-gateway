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
        }

        public static List<DTOs.Connection> GetConnectedClients(string gatewayName, DateTime when)
        {
            return GetConnectedClientsBetween(gatewayName, when, when);
        }

        public static List<DTOs.Connection> GetConnectedServersBetween(string gatewayName, DateTime start, DateTime end)
        {
            return Global.DataContext.ReadServerSessions(gatewayName, start, end).Select(row => new DTOs.Connection
            {
                End = row.End,
                RemoteIpPoint = row.Remote,
                Start = row.Start
            }).ToList();
        }

        public static List<DTOs.Connection> GetConnectedServers(string gatewayName, DateTime when)
        {
            return Global.DataContext.ReadServerSessions(gatewayName, when, when).Select(row => new DTOs.Connection
            {
                End = row.End,
                RemoteIpPoint = row.Remote,
                Start = row.Start
            }).ToList();
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