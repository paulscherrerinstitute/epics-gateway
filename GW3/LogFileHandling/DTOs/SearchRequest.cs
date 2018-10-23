using System;

namespace GWLogger.Backend.DTOs
{
    public class SearchRequest
    {
        public string Channel { get; set; }
        public DateTime Date { get; set; }
        public string Client { get; set; }
        public int NbSearches { get; set; }
    }
}