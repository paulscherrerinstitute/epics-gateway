using GraphAnomalies.Types;
using System;

namespace GraphAnomalies.Processors
{
    public class Rounding : Processor
    {
        public Rounding()
        {
        }

        internal override void Update(TemporalValue value)
        {
            Processed(new TemporalValue(value.Date, Math.Round(value.Value)));
        }
    }
}