using System;
using System.Collections.Generic;

namespace GWLogger.Live
{
    /// <summary>
    /// Class that contains the most important info of a graph anomaly
    /// </summary>
    public class GraphAnomalyInfo
    {
        public string FileName { get; set; }
        public string Name { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }

    }
}