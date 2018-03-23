using System;

namespace GWLogger.Backend.Controllers
{
    public class Search
    {
        public string Client { get; set; }
        public string Gateway { get; set; }
        public DateTime Date { get; set; }
        public string Channel { get; set; }
    }
}