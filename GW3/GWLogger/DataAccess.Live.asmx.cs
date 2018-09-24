using GWLogger.Live;
using System.Collections.Generic;
using System.Web.Services;

namespace GWLogger
{
    public partial class DataAccess : System.Web.Services.WebService
    {
        [WebMethod]
        public List<GatewayShortInformation> GetShortInformation()
        {
            return Global.LiveInformation.GetShortInformation();
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
        public List<KeyValuePair<string, List<HistoricData>>> GetHistoricData(string gatewayName)
        {
            return new List<KeyValuePair<string, List<HistoricData>>>()
            {
                new KeyValuePair<string, List<HistoricData>>("CPU", CpuHistory(gatewayName)),
                new KeyValuePair<string, List<HistoricData>>("Searches", SearchHistory(gatewayName)),
                new KeyValuePair<string, List<HistoricData>>("PVs", PVsHistory(gatewayName)),
            };
        }
    }
}