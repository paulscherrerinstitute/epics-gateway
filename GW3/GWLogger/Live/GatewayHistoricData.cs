using System.Collections.Generic;

namespace GWLogger.Live
{
    public class GatewayHistoricData
    {

        public List<HistoricData> CPU { get; set; }
        public List<HistoricData> Searches { get; set; }
        public List<HistoricData> PVs { get; set; }
        public List<HistoricData> Network { get; set; }

    }
}