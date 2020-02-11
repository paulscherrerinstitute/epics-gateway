namespace GWLogger.Backend.DataContext
{
    public class LogPosition
    {
        public string LogFile { get; set; } = null;
        public long Offset { get; set; } = -1;
    }
}