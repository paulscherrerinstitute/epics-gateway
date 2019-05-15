using GraphAnomalies.Types;
using System.Collections.Generic;

namespace GraphAnomalies.Processors
{
    /// <summary>
    /// This class implements the weighted moving average preprocessor according to https://en.wikipedia.org/wiki/Moving_average, Section "Weighted moving average"
    /// </summary>
    public class WeightedMovingAverage : Processor
    {
        private readonly LinkedList<TemporalValue> LastValues = new LinkedList<TemporalValue>();
        public readonly int AveragedValueCount;

        public WeightedMovingAverage(int averagedValueCount)
        {
            AveragedValueCount = averagedValueCount;
        }

        internal override void Update(TemporalValue value)
        {
            LastValues.AddLast(value);

            // Make sure that we have enough values to start sending out accurate averages
            if (LastValues.Count < AveragedValueCount)
                return;

            // Remove older values that are no longer needed
            while (LastValues.Count > AveragedValueCount)
                LastValues.RemoveFirst();

            var factor = 1;
            var numerator = 0.0;
            foreach (var l in LastValues)
            {
                numerator += l.Value * factor;
                factor++;
            }
            var denominator = AveragedValueCount * (AveragedValueCount + 1) / 2;

            Processed(new TemporalValue(value.Date, numerator / denominator));
        }
    }
}