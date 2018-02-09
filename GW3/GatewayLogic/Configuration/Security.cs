using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections.Concurrent;
using System.Net;

namespace GatewayLogic.Configuration
{
    [Serializable]
    public class Security
    {
        [XmlArray("Groups")]
        [XmlArrayItem(ElementName = "Group")]
        public List<Group> Groups = new List<Group>();

        [XmlArray("RulesSideA")]
        [XmlArrayItem(ElementName = "Rule")]
        public List<SecurityRule> RulesSideA = new List<SecurityRule>();

        [XmlArray("RulesSideB")]
        [XmlArrayItem(ElementName = "Rule")]
        public List<SecurityRule> RulesSideB = new List<SecurityRule>();

        Dictionary<string, string> ReverseIpLookup = new Dictionary<string, string>();

        public SecurityAccess EvaluateSideA(string channel, string username, string hostname, string ip)
        {
            SecurityAccess result = SecurityAccess.ALL;
            foreach (var i in RulesSideA)
            {
                if (i.Applies(channel, username, GetReverseLookup(ip), ip))
                    result = i.Access;
            }
            return result;
        }

        public SecurityAccess EvaluateSideB(string channel, string username, string hostname, string ip)
        {
            SecurityAccess result = SecurityAccess.ALL;
            foreach (var i in RulesSideB)
            {
                if (i.Applies(channel, username, GetReverseLookup(ip), ip))
                    result = i.Access;
            }
            return result;
        }

        public string GetReverseLookup(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return ip;

            lock (ReverseIpLookup)
            {
                if (!ReverseIpLookup.ContainsKey(ip))
                {
                    string hostname = null;
                    try
                    {
                        hostname = Dns.GetHostEntry(ip)?.HostName;
                        if (hostname == null)
                            hostname = ip;
                        else
                            hostname.Split(new char[] { '.' }).First();
                    }
                    catch
                    {
                        return hostname = ip;
                    }
                    ReverseIpLookup.Add(ip, hostname);
                }
                return ReverseIpLookup[ip];
            }
        }

        public void Init()
        {
            foreach (var i in RulesSideA)
            {
                i.Security = this;
                i.Init();
            }

            foreach (var i in RulesSideB)
            {
                i.Security = this;
                i.Init();
            }
        }
    }
}
