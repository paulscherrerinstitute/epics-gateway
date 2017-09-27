using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace  GatewayLogic.Configuration
{
    [Serializable]
    public class HostFilter : SecurityFilter
    {
        [XmlElement("Name")]
        public string Name { get; set; }

        public override bool Applies(string username, string hostname, string ip)
        {
            if (hostname == null)
                return false;
            return String.Compare(hostname, Name, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public override void Init()
        {
        }
    }
}
