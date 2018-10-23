using System;

namespace GWLogger.Backend.DataContext
{
    public class LogSession
    {
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
        public string Remote { get; set; }
    }
}