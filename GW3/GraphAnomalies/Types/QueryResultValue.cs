using System.Xml.Serialization;

namespace GraphAnomalies.Types
{
    public class QueryResultValue
    {
        public QueryResultValue()
        {
        }

        public QueryResultValue(object o)
        {
            var arr = (object[])o;
            Value = double.Parse(arr[0]?.ToString() ?? "0");
            Text = arr[1]?.ToString();
        }

        [XmlAttribute]
        public double Value { get; set; }

        [XmlAttribute]
        public string Text { get; set; }
    }
}