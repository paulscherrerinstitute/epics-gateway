using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GWLogger.Live
{
    public class GraphAnomaly
    {
        [XmlIgnore]
        public bool IsDirty { get; set; } = false;

        [XmlIgnore]
        public string Filename { get; set; }

        private DateTime _From = DateTime.MinValue;
        public DateTime From {

            get {
                return _From;
            }
            set {
                if (Equals(_From, value))
                    return;
                _From = value;
                IsDirty = true;
            }
        }

        private DateTime _To = DateTime.MinValue;
        public DateTime To {
            get
            {
                return _To;
            }
            set
            {
                if (Equals(_To, value))
                    return;
                _To = value;
                IsDirty = true;
            }
        }

        public string Name { get; set; }

        public List<InterestingEventType> InterestingEventTypeRemotes { get; set; }

        public List<QueryResultValue> BeforeRemoteCounts { get; set; }
        public List<QueryResultValue> DuringRemoteCounts { get; set; }


        public List<QueryResultValue> BeforeEventTypes { get; set; }
        public List<QueryResultValue> DuringEventTypes { get; set; }

        public GatewayHistory History { get; set; }


    }

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

    public class InterestingEventType
    {
        public QueryResultValue EventType { get; set; }
        public List<QueryResultValue> TopRemotes { get; set; }

    }

}