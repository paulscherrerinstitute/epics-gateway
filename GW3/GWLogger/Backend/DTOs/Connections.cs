using System.Collections.Generic;

namespace GWLogger.Backend.DTOs
{
    public class Connections
    {
        public List<Connection> Clients { get; set; }
        public List<Connection> Servers { get; set; }
    }
}