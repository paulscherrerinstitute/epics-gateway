using GWLogger.Backend.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Services;

namespace GWLogger
{
    public partial class DataAccess : System.Web.Services.WebService
    {
        [WebMethod]
        public List<string> GetGatewaysList()
        {
            return AnalyzeController.GetGatewaysList();
        }

        [WebMethod]
        public List<KeyValuePair<int, string>> GetMessageTypes()
        {
            return Global.DataContext.MessageTypes.Select(row => new KeyValuePair<int, string>(row.Id, row.Name)).ToList();
        }

        [WebMethod]
        public List<Backend.DTOs.GatewaySession> GetGatewaySessionsList(string gatewayName)
        {
            return AnalyzeController.GetGatewaySessionsList(gatewayName);
        }

        [WebMethod]
        public Backend.DTOs.GatewayStats GetStats(string gatewayName, DateTime start, DateTime end)
        {
            return AnalyzeController.GetStats(gatewayName, start, end);
        }

        [WebMethod]
        public Backend.DTOs.Connections GetConnectionsBetween(string gatewayName, DateTime start, DateTime end)
        {
            return AnalyzeController.GetConnectionsBetween(gatewayName, start, end);
        }

        [WebMethod]
        public List<Backend.DTOs.Connection> GetConnectedClientsBetween(string gatewayName, DateTime start, DateTime end)
        {
            return AnalyzeController.GetConnectedClientsBetween(gatewayName, start, end);
        }

        [WebMethod]
        public List<Backend.DTOs.Connection> GetConnectedClients(string gatewayName, DateTime when)
        {
            return AnalyzeController.GetConnectedClients(gatewayName, when);
        }

        [WebMethod]
        public List<Backend.DTOs.Connection> GetConnectedServersBetween(string gatewayName, DateTime start, DateTime end)
        {
            return AnalyzeController.GetConnectedServersBetween(gatewayName, start, end);
        }

        [WebMethod]
        public List<Backend.DTOs.Connection> GetConnectedServers(string gatewayName, DateTime when)
        {
            return AnalyzeController.GetConnectedServers(gatewayName, when);
        }

        [WebMethod]
        public List<Backend.DTOs.SearchRequest> GetSearchedChannels(string gatewayName, DateTime datePoint)
        {
            return AnalyzeController.GetSearchedChannels(gatewayName, datePoint);
        }

        [WebMethod]
        public bool CheckQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;
            try
            {
                Backend.DataContext.Query.QueryParser.Parse(query.Trim());
                return true;
            }
            catch
            {
            }
            return false;
        }

        [WebMethod]
        public List<KeyValuePair<string, int>> SearchesPerformed(string gatewayName, DateTime datePoint)
        {
            var searches = Global.DataContext.ReadLog(gatewayName, datePoint, datePoint.AddMinutes(5), "type = \"SearchRequest\"", -1, null, null, 0, Context.Response.ClientDisconnectedToken);
            if (Context.Response.ClientDisconnectedToken.IsCancellationRequested)
                return null;
            var channelName = Global.DataContext.MessageDetailTypes.First(row => row.Value == "ChannelName").Id;
            var searchesData = searches.Select(row => new { Remote = row.RemoteIpPoint, Channel = row.LogEntryDetails.First(r2 => r2.DetailTypeId == channelName).Value });
            var hosts = searchesData.Select(row => row.Remote.Split(':')[0]).Distinct().ToDictionary(key => key, val => Hostname(val));
            return searchesData.GroupBy(row => row.Remote).Select(row => new KeyValuePair<string, int>(row.Key + " (" + hosts[row.Key.Split(':')[0]] + ")", row.Count())).OrderByDescending(row => row.Value).ToList();
        }

        [WebMethod]
        public List<KeyValuePair<string, int>> SearchesOnChannelsPerformed(string gatewayName, DateTime datePoint)
        {
            var searches = Global.DataContext.ReadLog(gatewayName, datePoint, datePoint.AddMinutes(5), "type = \"SearchRequest\"", -1, null, null, 0, Context.Response.ClientDisconnectedToken);
            if (Context.Response.ClientDisconnectedToken.IsCancellationRequested)
                return null;
            var channelName = Global.DataContext.MessageDetailTypes.First(row => row.Value == "ChannelName").Id;
            var searchesData = searches.Select(row => new { Remote = row.RemoteIpPoint, Channel = row.LogEntryDetails.First(r2 => r2.DetailTypeId == channelName).Value });
            return searchesData.GroupBy(row => row.Channel).Select(row => new KeyValuePair<string, int>(row.Key, row.Count())).OrderByDescending(row => row.Value).ToList();
        }

        [WebMethod]
        public List<Backend.DTOs.DataFileStats> GetDataFileStats()
        {
            return Global.DataContext.GetDataFileStats();
        }

        private static string Hostname(string ip)
        {
            return System.Net.Dns.GetHostEntry(ip).HostName;
        }
    }
}