using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GraphAnomalies.Types
{
    public class GraphAnomaly
    {
        [XmlIgnore]
        public string FileName { get; set; }

        public DateTime From { get; set; } = DateTime.MinValue;

        public DateTime To { get; set; } = DateTime.MinValue;

        public string Name { get; set; }

        public List<InterestingEventType> InterestingEventTypeRemotes { get; set; }

        public List<QueryResultValue> BeforeRemoteCounts { get; set; }
        public List<QueryResultValue> DuringRemoteCounts { get; set; }

        public List<QueryResultValue> BeforeEventTypes { get; set; }
        public List<QueryResultValue> DuringEventTypes { get; set; }

        public GatewayHistoricData History { get; set; }
    }
}