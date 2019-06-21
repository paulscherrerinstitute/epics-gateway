using GWLogger.Backend;
using GWLogger.Backend.Controllers;
using GWLogger.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Services;
using System.Xml.Serialization;

namespace GWLogger
{
    public enum GatewayConfigurationType : int
    {
        UNIDIRECTIONAL = 0,
        BIDIRECTIONAL = 1
    }

    [Serializable]
    [XmlRoot("Config", IsNullable = false)]
    public class XmlGatewayConfig
    {
        public GatewayConfigurationType Type { get; set; }
        public string Name { get; set; }
        public string LocalAddressSideA { get; set; }
        public string RemoteAddressSideA { get; set; }
        public string LocalAddressSideB { get; set; }
        public string RemoteAddressSideB { get; set; }
        public ConfigSecurity Security { get; set; }
    }

    [Serializable]
    public class ConfigSecurity
    {
        [XmlArrayItemAttribute("Group", IsNullable = false)]
        public ConfigSecurityGroup[] Groups { get; set; }
        [XmlArrayItemAttribute("Rule", IsNullable = false)]
        public ConfigSecurityRule[] RulesSideA { get; set; }
        [XmlArrayItemAttribute("Rule", IsNullable = false)]
        public ConfigSecurityRule[] RulesSideB { get; set; }
    }

    [Serializable]
    public class ConfigSecurityGroup
    {
        [XmlArrayItemAttribute("Filter", IsNullable = false)]
        public SecurityFilter[] Filters { get; set; }
        [XmlAttributeAttribute]
        public string Name { get; set; }
    }

    [Serializable]
    [XmlInclude(typeof(AllFilter))]
    [XmlInclude(typeof(HostFilter))]
    [XmlInclude(typeof(IPFilter))]
    [XmlInclude(typeof(UserFilter))]
    [XmlInclude(typeof(GroupFilter))]
    public abstract class SecurityFilter
    {
    }

    [Serializable]
    public class AllFilter : SecurityFilter
    {
    }

    [Serializable]
    public class HostFilter : SecurityFilter
    {
        [XmlElement("Name")]
        public string Name { get; set; }
    }

    [Serializable]
    public class IPFilter : SecurityFilter
    {
        [XmlElement("IP")]
        public string IP { get; set; }
    }

    [Serializable]
    public class UserFilter : SecurityFilter
    {
        [XmlElement("Name")]
        public string Name { get; set; }
    }

    [Serializable]
    public class GroupFilter : SecurityFilter
    {
        [XmlElement("Name")]
        public string Name { get; set; }
    }

    [Serializable]
    public class ConfigSecurityRule
    {
        public SecurityFilter Filter { get; set; }
        [XmlAttributeAttribute]
        public string Channel { get; set; }
        [XmlAttributeAttribute]
        public string Access { get; set; }
    }

    public partial class DataAccess : WebService
    {
        [WebMethod]
        public void ImportInventoryConfiguration(string hostname)
        {
            XmlGatewayConfig config;
            using (var web = new WebClient())
            {
                var data = web.DownloadString("http://inventory.psi.ch/soap/gatewayConfig.aspx?gateway=" + System.Web.HttpUtility.UrlEncode(hostname));
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(XmlGatewayConfig));
                using (var memStream = new StringReader(data))
                {
                    config = (XmlGatewayConfig)serializer.Deserialize(memStream);
                }
            }

            // Cleanup
            using (var ctx = new CaesarContext())
            {
                ctx.Gateways.RemoveRange(ctx.Gateways.Where(row => row.GatewayName == hostname));
                ctx.SaveChanges();
            }

            // Insert
            using (var ctx = new CaesarContext())
            {
                ctx.Gateways.Add(new GatewayEntry
                {
                    GatewayName = hostname,
                    //Directions = (GatewayDirection)((int)config.ConfigurationType)
                    Directions = (GatewayDirection)((int)config.Type)
                });
            }
        }
    }
}