using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web;
using System.Xml.Serialization;
using GWLogger.Model;

namespace GWLogger.Backend.Controllers
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
        public ConfigSecurity Security { get; set; } = new ConfigSecurity();
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

    public static class ConfigController
    {
        private static readonly List<GatewayFilterType> filterTypes;
        private static Dictionary<string, ConstructorInfo> filterTypeClasses;

        static ConfigController()
        {
            using (var ctx = new CaesarContext())
            {
                filterTypes = ctx.GatewayFilterTypes.ToList();
            }

            filterTypeClasses = new Dictionary<string, ConstructorInfo>();
            filterTypeClasses.Add("AllFilter", typeof(AllFilter).GetConstructor(new Type[] { }));
            filterTypeClasses.Add("HostFilter", typeof(HostFilter).GetConstructor(new Type[] { }));
            filterTypeClasses.Add("IPFilter", typeof(IPFilter).GetConstructor(new Type[] { }));
            filterTypeClasses.Add("UserFilter", typeof(UserFilter).GetConstructor(new Type[] { }));
            filterTypeClasses.Add("GroupFilter", typeof(GroupFilter).GetConstructor(new Type[] { }));
        }

        public static void ImportInventoryConfiguration(string hostname)
        {
            XmlGatewayConfig config;
            using (var web = new WebClient())
            {
                config = XmlToConfig(web.DownloadString("http://inventory.psi.ch/soap/gatewayConfig.aspx?gateway=" + System.Web.HttpUtility.UrlEncode(hostname)));
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
                ctx.Gateways.Add(ConfigToDb(config));
                ctx.SaveChanges();
            }
        }

        private static string ConfigToXml(XmlGatewayConfig config)
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(XmlGatewayConfig));
            using (var memStream = new StringWriter())
            {
                serializer.Serialize(memStream, config);
                return memStream.ToString();
            }
        }

        private static XmlGatewayConfig XmlToConfig(string xml)
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(XmlGatewayConfig));
            using (var memStream = new StringReader(xml))
            {
                return (XmlGatewayConfig)serializer.Deserialize(memStream);
            }
        }

        private static XmlGatewayConfig DbToConfig(GatewayEntry entry)
        {
            var result = new XmlGatewayConfig
            {
                LocalAddressSideA = entry.LocalAddressA,
                LocalAddressSideB = entry.LocalAddressB,
                RemoteAddressSideA = entry.RemoteAddressA,
                RemoteAddressSideB = entry.RemoteAddressB,
                Name = entry.GatewayName
            };
            result.Security.RulesSideA = entry.GatewayRules.Where(row => row.Side == "A").Select(row => DbToRule(row)).ToArray();
            result.Security.RulesSideB = entry.GatewayRules.Where(row => row.Side == "B").Select(row => DbToRule(row)).ToArray();
            result.Security.Groups = entry.GatewayGroups.Select(row => new ConfigSecurityGroup
            {
                Name = row.Name,
                Filters = row.GatewayGroupMembers.Select(r2 => DbToSecurityFilter(r2)).ToArray()
            }).ToArray();
            return result;
        }

        private static SecurityFilter DbToSecurityFilter(GatewayGroupMember groupMember)
        {
            var result = (SecurityFilter)filterTypeClasses[groupMember.GatewayFilterType.ClassName].Invoke(new object[] { });
            if (!string.IsNullOrEmpty(groupMember.GatewayFilterType.FieldName))
                result.GetType().GetProperty(groupMember.GatewayFilterType.FieldName).SetValue(result, groupMember.Value1);
            return result;
        }

        private static ConfigSecurityRule DbToRule(GatewayRule rule)
        {
            var result = new ConfigSecurityRule
            {
                Access = rule.RuleAccess,
                Channel = rule.Channel,
            };
            result.Filter = (SecurityFilter)filterTypeClasses[rule.GwFilterType.ClassName].Invoke(new object[] { });
            if (!string.IsNullOrEmpty(rule.GwFilterType.FieldName))
                result.GetType().GetProperty(rule.GwFilterType.FieldName).SetValue(result, rule.Value1);
            return result;
        }

        private static GatewayEntry ConfigToDb(XmlGatewayConfig config)
        {
            return new GatewayEntry
            {
                GatewayName = config.Name,
                Directions = (GatewayDirection)((int)config.Type),
                LocalAddressA = config.LocalAddressSideA,
                LocalAddressB = config.LocalAddressSideB,
                RemoteAddressA = config.RemoteAddressSideA,
                RemoteAddressB = config.RemoteAddressSideB,
                GatewayGroups = config.Security.Groups.Select(row => new GatewayGroup
                {
                    Name = row.Name,
                    GatewayGroupMembers = row.Filters.Select(r2 => SecurityFilterToDb(r2)).ToList()
                }).ToList(),
                GatewayRules = config.Security.RulesSideA.Select(r2 => RuleToDb("A", r2))
                        .Union(config.Security.RulesSideB.Select(r2 => RuleToDb("B", r2)))
                        .ToList()
            };
        }

        private static GatewayGroupMember SecurityFilterToDb(SecurityFilter filter)
        {
            var typeName = filter.GetType().Name;
            var filterType = filterTypes.First(row => row.ClassName == typeName);
            var result = new GatewayGroupMember
            {
                FilterType = filterType.FilterId
            };
            if (!string.IsNullOrWhiteSpace(filterType.FieldName))
                result.Value1 = filter.GetType().GetProperty(filterType.FieldName).GetValue(filter)?.ToString();
            return result;
        }

        private static GatewayRule RuleToDb(string side, ConfigSecurityRule rule)
        {
            var result = new GatewayRule
            {
                Channel = rule.Channel,
                RuleAccess = rule.Access,
                Side = side
            };
            if (rule.Filter != null)
            {
                var filter = SecurityFilterToDb(rule.Filter);
                result.FilterType = filter.FilterType;
                result.Value1 = filter.Value1;
            }
            return result;
        }
    }
}