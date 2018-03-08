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
        public List<Backend.DTOs.SearchRequest> GetSearchedChannels(string gatewayName, DateTime datePoint)
        {
            return AnalyzeController.GetSearchedChannels(gatewayName, datePoint);
        }
    }
}