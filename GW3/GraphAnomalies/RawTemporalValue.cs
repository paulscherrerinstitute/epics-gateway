using System;

namespace GraphAnomalies
{
    public struct RawTemporalValue
    {
        public RawTemporalValue(DateTime date, double value)
        {
            Date = date;
            Value = value;
        }

        public DateTime Date { get; set; }
        public double Value { get; set; }
    }
}