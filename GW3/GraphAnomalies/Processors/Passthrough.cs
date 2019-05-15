using GraphAnomalies.Types;

namespace GraphAnomalies.Processors
{
    public class Passthrough : Processor
    {
        public Passthrough()
        {
        }

        internal override void Update(TemporalValue value)
        {
            Processed(new TemporalValue(value));
        }
    }
}