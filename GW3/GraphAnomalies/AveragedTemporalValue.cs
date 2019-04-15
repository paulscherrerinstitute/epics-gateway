using System;

namespace GraphAnomalies
{
    public struct AveragedTemporalValue
    {
        public AveragedTemporalValue(AveragedTemporalValue v)
        {
            Date = v.Date;
            Value = v.Value;
            StandardDeviation = v.StandardDeviation;
        }

        public DateTime Date { get; set; }
        public double Value { get; set; }
        public double StandardDeviation { get; set; }
    }
}