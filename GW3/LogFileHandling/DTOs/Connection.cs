using System;

namespace GWLogger.Backend.DTOs
{
    public class Connection
    {
        public string RemoteIpPoint { get; set; }
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
    }
}