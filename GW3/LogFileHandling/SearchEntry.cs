using System;

namespace GWLogger.Backend.DataContext
{
    public class SearchEntry
    {
        public DateTime Date { get; set; }
        public string Remote { get; set; }
        public string Channel { get; set; }
    }
}