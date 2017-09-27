using GatewayLogic.Connections;
using GatewayLogic.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GatewayLogic
{
    public class Gateway : IDisposable
    {
        public const int BUFFER_SIZE = 8192 * 30;
        public const UInt16 CA_PROTO_VERSION = 11;

        public Configuration.Configuration Configuration { get; private set; } = new GatewayLogic.Configuration.Configuration();

        internal UdpReceiver udpSideA;
        internal UdpReceiver udpSideB;
        internal TcpClientListener tcpSideA;
        internal TcpClientListener tcpSideB;

        internal ChannelInformation ChannelInformation { get; }
        internal MonitorInformation MonitorInformation { get; private set; } = new MonitorInformation();
        internal ReadNotifyInformation ReadNotifyInformation { get; private set; } = new ReadNotifyInformation();
        internal WriteNotifyInformation WriteNotifyInformation { get; private set; } = new WriteNotifyInformation();
        internal SearchInformation SearchInformation { get; private set; } = new SearchInformation();
        internal ClientConnection ClientConnection { get; }
        internal ServerConnection ServerConnection { get; }
        public Log Log { get; } = new Log();

        internal event EventHandler OneSecUpdate;
        internal event EventHandler TenSecUpdate;

        bool isDiposed = false;

        Thread updaterThread;

        public Gateway()
        {
            ChannelInformation = new ChannelInformation(this);

            ClientConnection = new ClientConnection(this);
            ServerConnection = new ServerConnection(this);

            updaterThread = new Thread(Updater);
            updaterThread.IsBackground = true;
            updaterThread.Start();
        }

        void Updater()
        {
            int count = 0;
            while (!isDiposed)
            {
                Thread.Sleep(1000);
                try
                {
                    OneSecUpdate?.Invoke(this, null);
                    if (count >= 9)
                    {
                        count = 0;
                        TenSecUpdate?.Invoke(this, null);
                    }
                }
                catch (Exception ex)
                {
                    Log.Write(LogLevel.Error, ex.ToString());
                }
                count++;
            }
        }

        public void LoadConfig()
        {
            bool freshConfig = false;
            try
            {
                if (System.Configuration.ConfigurationManager.AppSettings["configURL"] == null || System.Configuration.ConfigurationManager.AppSettings["gatewayName"] == null)
                    throw new Exception("Direct config");
                using (var client = new System.Net.WebClient())
                {
                    string config = client.DownloadString(System.Configuration.ConfigurationManager.AppSettings["configURL"] + System.Configuration.ConfigurationManager.AppSettings["gatewayName"]);
                    Log.Write(LogLevel.Detail, "Loading configuration from");
                    Log.Write(LogLevel.Detail, System.Configuration.ConfigurationManager.AppSettings["configURL"] + System.Configuration.ConfigurationManager.AppSettings["gatewayName"]);
                    using (var txtReader = new System.IO.StringReader(config))
                    {
                        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(Configuration.Configuration));
                        Configuration = (Configuration.Configuration)serializer.Deserialize(txtReader);
                        txtReader.Close();
                    }
                }
                freshConfig = true;
            }
            catch
            {
                Log.Write(LogLevel.Detail, , "Loading configuration from gateway.xml");
                using (var txtReader = new System.IO.StreamReader("gateway.xml"))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(typeof(Configuration.Configuration));
                    Configuration = (Configuration.Configuration)serializer.Deserialize(txtReader);
                    txtReader.Close();
                }
            }

            if (freshConfig)
                SaveConfig();
        }

        public void SaveConfig()
        {
            using (var txtWriter = new System.IO.StreamWriter("gateway.xml"))
            {
                var serializer = new XmlSerializer(typeof(Configuration.Configuration));
                serializer.Serialize(txtWriter, Configuration);
                txtWriter.Close();
            }
        }

        public void Start()
        {
            tcpSideA = new TcpClientListener(this, this.Configuration.SideAEndPoint);
            tcpSideB = new TcpClientListener(this, this.Configuration.SideBEndPoint);

            udpSideA = new UdpReceiver(this, this.Configuration.SideAEndPoint);
            udpSideB = new UdpResponseReceiver(this, this.Configuration.SideBEndPoint);
        }

        public void Cleanup()
        {
            ChannelInformation.ForceDropUnused();
        }

        public void Dispose()
        {
            tcpSideA.Dispose();
            tcpSideB.Dispose();

            udpSideA.Dispose();
            udpSideB.Dispose();

            ChannelInformation.Dispose();
            MonitorInformation.Dispose();
            ReadNotifyInformation.Dispose();
            SearchInformation.Dispose();

            ClientConnection.Dispose();
            ServerConnection.Dispose();
        }

        internal void DropClient(TcpClientConnection tcpClientConnection)
        {
            ClientConnection.Remove(tcpClientConnection);
            ChannelInformation.DisconnectClient(tcpClientConnection);
        }
    }
}
