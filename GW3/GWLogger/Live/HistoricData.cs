using System;

namespace GWLogger.Live
{
    public class HistoricData
    {
        public double? Value { get; }
        public DateTime Date { get; }

        public HistoricData(double? value)
        {
            Value = value;
            Date = DateTime.UtcNow;
        }
    }
}