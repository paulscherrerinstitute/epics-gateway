using System;

namespace GWLogger.Live
{
    public class HistoricData
    {
        public double? Value { get; set; }
        public DateTime Date { get; } = DateTime.UtcNow;
    }
}