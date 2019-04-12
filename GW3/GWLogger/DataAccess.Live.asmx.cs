using GWLogger.Live;
using System.Collections.Generic;
using System.Web.Services;

namespace GWLogger
{
    public partial class DataAccess : WebService
    {
        [WebMethod]
        public List<GatewayShortInformation> GetShortInformation()
        {
            return Global.LiveInformation.GetShortInformation();
        }

        [WebMethod]
        public GatewayInformation GetGatewayInformation(string gatewayName)
        {
            return Global.LiveInformation.GetGatewayInformation(gatewayName);
        }

        [WebMethod]
        public List<HistoricData> CpuHistory(string gatewayName)
        {
            return Global.LiveInformation.CpuHistory(gatewayName);
        }

        [WebMethod]
        public List<HistoricData> SearchHistory(string gatewayName)
        {
            return Global.LiveInformation.SearchHistory(gatewayName);
        }

        [WebMethod]
        public List<HistoricData> PVsHistory(string gatewayName)
        {
            return Global.LiveInformation.PVsHistory(gatewayName);
        }

        [WebMethod]
        public List<HistoricData> NetworkHistory(string gatewayName)
        {
            return Global.LiveInformation.NetworkHistory(gatewayName);
        }

        [WebMethod]
        public GatewayHistoricData GetHistoricData(string gatewayName)
        {
            return new GatewayHistoricData()
            {
                CPU = CpuHistory(gatewayName),
                Searches = SearchHistory(gatewayName),
                PVs = PVsHistory(gatewayName),
                Network = NetworkHistory(gatewayName),
            };
        }
    }
}