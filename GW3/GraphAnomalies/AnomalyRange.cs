using System;

namespace GraphAnomalies
{
    public class AnomalyRange
    {
        public AnomalyRange(AnomalyRange range) : this(range.From, range.To)
        {
        }

        public AnomalyRange(DateTime from, DateTime to)
        {
            From = from;
            To = to;
        }

        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
}