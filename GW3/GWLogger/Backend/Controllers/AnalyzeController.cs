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
    }
}