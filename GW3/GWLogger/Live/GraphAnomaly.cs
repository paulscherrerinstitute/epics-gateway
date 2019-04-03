using System;
using System.Xml.Serialization;

namespace GWLogger.Live
{
    public class GraphAnomaly
    {
        [XmlIgnore]
        public bool IsDirty { get; set; } = false;

        private DateTime _From = DateTime.MinValue;
        public DateTime From {

            get {
                return _From;
            }
            set {
                if (_From.Equals(value))
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
                if (_To.Equals(value))
                    return;
                _To = value;
                IsDirty = true;
            }
        }

    }
}