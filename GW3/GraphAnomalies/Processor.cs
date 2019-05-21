using GraphAnomalies.Types;
using System;

namespace GraphAnomalies
{
    public abstract class Processor
    {
        public delegate void ValueProcessedEvent(TemporalValue value);

        internal event ValueProcessedEvent ValueProcessed;

        public void Update(DateTime date, double value)
        {
            Update(new TemporalValue(date, value));
        }

        internal abstract void Update(TemporalValue value);

        protected void Processed(TemporalValue result)
        {
            ValueProcessed?.Invoke(result);
        }
    }
}