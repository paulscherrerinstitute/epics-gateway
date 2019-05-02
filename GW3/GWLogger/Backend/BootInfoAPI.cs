using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web;

namespace GWLogger.Backend
{
    [DataContract]
    public class BootInfoChannel
    {
        [DataMember]
        public string Channel { get; set; }

        [DataMember]
        public string RecordType { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string IOC { get; set; }

        [DataMember]
        public string Facility { get; set; }

        [DataMember]
        public string SubmittedOn { get; set; }

        public DateTime SubmittedDate => DateTime.Parse(SubmittedOn);
    }

    public static class BootInfoAPI
    {
        private static List<TType> RetreiveList<TType>(string url)
        {
            using (var client = new WebClient())
            {
                var stringData = client.DownloadString(url);
                using (var mem = new MemoryStream(Encoding.Unicode.GetBytes(stringData)))
                {
                    var serializer = new DataContractJsonSerializer(typeof(List<TType>));
                    return (List<TType>)serializer.ReadObject(mem);
                }
            }
        }

        public static List<BootInfoChannel> FindChannels(string query)
        {
            return RetreiveList<BootInfoChannel>("http://epics-boot-info.psi.ch/find-channel.aspx/" + HttpUtility.UrlEncode(query) + "?match=regex");
        }

        public static List<BootInfoChannel> IocChannels(string ioc, string facility)
        {
            return RetreiveList<BootInfoChannel>("http://epics-boot-info.psi.ch/find-channel.aspx/?limit=0&ioc=" + HttpUtility.UrlEncode(ioc) + "&facility=" + HttpUtility.UrlEncode(facility));
        }
    }
}