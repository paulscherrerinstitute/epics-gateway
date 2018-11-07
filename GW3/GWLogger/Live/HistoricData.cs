using System;

namespace GWLogger.Live
{
    [Serializable]
    public class HistoricData
    {
        public double? Value { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}