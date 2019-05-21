using System.Collections.Generic;

namespace GraphAnomalies.Types
{
    public class GatewayHistoricData
    {
        public List<HistoricData> CPU { get; set; }
        public List<HistoricData> Searches { get; set; }
        public List<HistoricData> PVs { get; set; }
        public List<HistoricData> Network { get; set; }
    }
}