using System;

namespace GraphAnomalies
{
    public struct TemporalValue
    {
        public TemporalValue(TemporalValue v) : this(v.Date, v.Value)
        {
        }

        public TemporalValue(DateTime date, double value)
        {
            Date = date;
            Value = value;
        }

        public DateTime Date { get; set; }
        public double Value { get; set; }
    }
}