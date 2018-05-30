using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GatewayLogic.Configuration
{
    [Serializable]
    [XmlRoot("Config", IsNullable = false)]
    public class Configuration
    {
        public Configuration()
        {
            Security = new Security();
        }

        [XmlElement("Security")]
        public Security Security { get; set; }

        //public List<IPEndPoint> RemoteSideB { get { return ParseListAddress(RemoteAddressSideB); } }

        [XmlElement("Type")]
        public ConfigurationType ConfigurationType { get; set; }
        [XmlElement("Name")]
        public string GatewayName { get; set; }

        public int SearchPreventionTimeout { get; set; } = 100;


        private IPEndPoint sideA;
        [XmlElement("LocalAddressSideA")]
        public string SideA
        {
            get
            {
                return sideA.ToString();
            }
            set
            {
                sideA = ParseAddress(value);
            }
        }

        [XmlIgnore]
        public IPEndPoint SideAEndPoint => sideA;

        [XmlIgnore]
        internal List<IPEndPoint> remoteBEndPoints;
        private string remoteSideB;

        [XmlElement("RemoteAddressSideB")]
        public string RemoteSideB
        {
            get
            {
                return remoteSideB;
            }

            set
            {
                remoteSideB = value;
                remoteBEndPoints = ParseListAddress(value);
            }
        }

        [XmlIgnore]
        internal List<IPEndPoint> remoteAEndPoints;
        private string remoteSideA;

        [XmlElement("RemoteAddressSideA")]
        public string RemoteSideA
        {
            get
            {
                return remoteSideA;
            }

            set
            {
                remoteSideA = value;
                remoteAEndPoints = ParseListAddress(value);
            }
        }

        static private List<IPEndPoint> ParseListAddress(string list)
        {
            return list
                .Replace(";", ",")
                .Split(new char[] { ',' })
                .Select(ParseAddress)
                .ToList();
        }

        private IPEndPoint sideB;

        [XmlElement("LocalAddressSideB")]
        public string SideB
        {
            get
            {
                return sideB.ToString();
            }
            set
            {
                sideB = ParseAddress(value);
            }
        }

        [XmlIgnore]
        public IPEndPoint SideBEndPoint => sideB;

        static public IPEndPoint ParseAddress(string addr)
        {
            string[] parts = addr.Split(new char[] { ':' });
            try
            {
                return new IPEndPoint(IPAddress.Parse(parts[0].Trim()), int.Parse(parts[1].Trim()));
            }
            //catch (Exception ex)
            catch
            {
                try
                {
                    return new IPEndPoint(Dns.GetHostEntry(parts[0]).AddressList.First(), int.Parse(parts[1].Trim()));
                }
                catch (Exception ex2)
                {
                    //PBCaGw.Services.Log.TraceEvent(System.Diagnostics.TraceEventType.Critical, -1, "Wrong IP: " + addr);
                    throw ex2;
                }
            }
        }

        public int DiagnosticPort { get; set; } = 4890;
    }
}
