using System.Collections.Generic;
using GraphAnomalies.Types;

namespace GraphAnomalies.Processors
{
    public class ValueCollector : Processor
    {
        public List<TemporalValue> CollectedValues = new List<TemporalValue>();

        internal override void Update(TemporalValue value)
        {
            CollectedValues.Add(new TemporalValue(value));
            Processed(value);
        }
    }
}