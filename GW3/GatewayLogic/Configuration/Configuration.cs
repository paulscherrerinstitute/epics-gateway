using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Configuration
{
    public class Configuration
    {
        IPEndPoint sideA;
        public string SideA
        {
            get
            {
                return sideA.ToString();
            }
            set
            {
                var ip = value.Split(new char[] { ':' }).First();
                var port = 5064;
                try
                {
                    port = int.Parse(value.Split(new char[] { ':' })[1]);
                }
                catch
                {
                }
                sideA = new IPEndPoint(IPAddress.Parse(ip), port);
            }
        }
        public IPEndPoint SideAEndPoint => sideA;

        internal List<IPEndPoint> remoteBEndPoints;
        private string remoteSideB;
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

        internal List<IPEndPoint> remoteAEndPoints;
        private string remoteSideA;
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

        static public IPEndPoint ParseAddress(string addr)
        {
            string[] parts = addr.Split(new char[] { ':' });
            try
            {
                return new IPEndPoint(IPAddress.Parse(parts[0].Trim()), int.Parse(parts[1].Trim()));
            }
            catch (Exception ex)
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

        IPEndPoint sideB;
        public string SideB
        {
            get
            {
                return sideB.ToString();
            }
            set
            {
                var ip = value.Split(new char[] { ':' }).First();
                var port = 5064;
                try
                {
                    port = int.Parse(value.Split(new char[] { ':' })[1]);
                }
                catch
                {
                }
                sideB = new IPEndPoint(IPAddress.Parse(ip), port);
            }
        }

        public IPEndPoint SideBEndPoint => sideB;
    }
}
