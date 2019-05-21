using System.Collections.Generic;

namespace GraphAnomalies.Types
{
    public class InterestingEventType
    {
        public QueryResultValue EventType { get; set; }
        public List<QueryResultValue> TopRemotes { get; set; }
    }
}