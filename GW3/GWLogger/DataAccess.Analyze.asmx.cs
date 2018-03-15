using GWLogger.Backend.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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
        public List<Backend.DTOs.LogStat> GetLogStats(string gatewayName, DateTime start, DateTime end)
        {
            return AnalyzeController.GetLogStats(gatewayName,start,end);
        }

        [WebMethod]
        public  List<Backend.DTOs.LogStat> GetErrorStats(string gatewayName, DateTime start, DateTime end)
        {
            return AnalyzeController.GetErrorStats(gatewayName, start, end);
        }

        [WebMethod]
        public  List<Backend.DTOs.LogStat> GetSearchesStats(string gatewayName, DateTime start, DateTime end)
        {
            return AnalyzeController.GetSearchesStats(gatewayName, start, end);
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
        public  List<Backend.DTOs.Connection> GetConnectedClients(string gatewayName, DateTime when)
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
    }
}