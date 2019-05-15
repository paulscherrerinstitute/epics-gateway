using System;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace GraphAnomalies.Types
{
    [Serializable]
    public class HistoricData
    {
        [XmlAttribute(AttributeName = "Value")]
        [ScriptIgnore]
        public double XmlValue
        {
            get
            {
                return Value ?? -1;
            }
            set
            {
                Value = (value == -1 ? (double?)null : value);
            }
        }

        [XmlIgnore]
        public double? Value { get; set; }

        [XmlAttribute]
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}