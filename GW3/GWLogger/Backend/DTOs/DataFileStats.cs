namespace GWLogger.Backend.DTOs
{
    public class DataFileStats
    {
        public double LogsPerSeconds { get; set; }
        public long AverageEntryBytes { get; set; }
        public string Name { get; set; }
        public long TotalDataSize { get; set; }
    }
}