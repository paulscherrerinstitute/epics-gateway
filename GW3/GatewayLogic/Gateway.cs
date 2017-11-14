﻿using GatewayLogic.Connections;
using GatewayLogic.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GatewayLogic
{
    public delegate void NewIocChannelDelegate(string ioc, string channel);
    public delegate void NewClientChannelDelegate(string client, string channel);
    public delegate void DropIocDelegate(string ioc);
    public delegate void DropClientDelegate(string client);

    public class Gateway : IDisposable
    {
        public const int BUFFER_SIZE = 8192 * 30;
        public const UInt16 CA_PROTO_VERSION = 13;

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
        internal DiagnosticServer DiagnosticServer { get; private set; }
        public Log Log { get; } = new Log();

        public event NewIocChannelDelegate NewIocChannel;
        public event NewClientChannelDelegate NewClientChannel;
        public event DropIocDelegate DropedIoc;
        public event DropClientDelegate DropedClient;
        public event EventHandler UpdateSearch;

        internal event EventHandler OneSecUpdate;
        internal event EventHandler TenSecUpdate;

        private Dictionary<string, int> searches = new Dictionary<string, int>();
        private Dictionary<string, int> searchers = new Dictionary<string, int>();

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
                    Log.Write(LogLevel.Critical, ex.ToString());
                }
                count++;
            }
        }

        public Dictionary<string, List<string>> KnownIocs
        {
            get
            {
                return ServerConnection.ToDictionary(key => key.Name, val => val.Channels);
            }
        }

        public Dictionary<string, List<string>> KnownClients
        {
            get
            {
                return ChannelInformation.KnownClients;
            }
        }

        public void LoadConfig()
        {
            if (System.Configuration.ConfigurationManager.AppSettings["gatewayName"] == null)
                throw new Exception("Direct config");
            LoadConfig(System.Configuration.ConfigurationManager.AppSettings["configURL"] ?? "https://inventory.psi.ch/soap/gatewayConfig.aspx?gateway=", System.Configuration.ConfigurationManager.AppSettings["gatewayName"]);
        }

        public void LoadConfig(string configUrl, string gatewayName)
        {
            bool freshConfig = false;
            try
            {
                using (var client = new System.Net.WebClient())
                {
                    string config = client.DownloadString(configUrl + gatewayName);
                    Log.Write(LogLevel.Detail, "Loading configuration from");
                    Log.Write(LogLevel.Detail, configUrl + gatewayName);
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
                Log.Write(LogLevel.Detail, "Loading configuration from gateway.xml");
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
            DiagnosticServer = new DiagnosticServer(this, Configuration.SideBEndPoint.Address);

            if (this.Configuration.ConfigurationType == GatewayLogic.Configuration.ConfigurationType.UNIDIRECTIONAL)
            {
                tcpSideA = new TcpClientListener(this, this.Configuration.SideAEndPoint);
                tcpSideB = new TcpClientListener(this, this.Configuration.SideBEndPoint);

                udpSideA = new UdpReceiver(this, this.Configuration.SideAEndPoint);
                udpSideB = new UdpResponseReceiver(this, this.Configuration.SideBEndPoint);
            }
            else
            {
                tcpSideA = new TcpClientListener(this, this.Configuration.SideAEndPoint);
                tcpSideB = new TcpClientListener(this, this.Configuration.SideBEndPoint);

                udpSideA = new UdpReceiver(this, this.Configuration.SideAEndPoint);
                udpSideB = new UdpReceiver(this, this.Configuration.SideBEndPoint);
            }

            this.TenSecUpdate += UpdateSearchInformation;
            Configuration.Security.Init();
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

            DiagnosticServer.Dispose();
        }

        internal void DropClient(TcpClientConnection tcpClientConnection)
        {
            ClientConnection.Remove(tcpClientConnection);
            ChannelInformation.DisconnectClient(tcpClientConnection);
        }

        public static string Version
        {
            get
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        internal void Search(string channelName, string endpoint)
        {
            lock (searches)
            {
                if (!searches.ContainsKey(channelName))
                    searches.Add(channelName, 0);
                searches[channelName]++;

                if (!searchers.ContainsKey(endpoint))
                    searchers.Add(endpoint, 0);
                searchers[endpoint]++;
            }
        }

        private void UpdateSearchInformation(object sender, EventArgs e)
        {
            GotUpdateSearch();
            lock (searches)
            {
                searches.Clear();
                searchers.Clear();
            }
        }

        public List<KeyValuePair<string, int>> Searches
        {
            get
            {
                lock (searches)
                {
                    return searches.Select(row => new KeyValuePair<string, int>(row.Key, row.Value)).ToList();
                }
            }
        }

        public List<KeyValuePair<string, int>> Searchers
        {
            get
            {
                lock (searches)
                {
                    return searchers.Select(row => new KeyValuePair<string, int>(row.Key, row.Value)).ToList();
                }
            }
        }

        public void GotNewIocChannel(string ioc, string channel)
        {
            try
            {
                NewIocChannel?.Invoke(ioc, channel);
            }
            catch
            {
            }
        }

        public void GotNewClientChannel(string client, string channel)
        {
            try
            {
                NewClientChannel?.Invoke(client, channel);
            }
            catch
            {
            }
        }

        public void GotDropedIoc(string ioc)
        {
            try
            {
                DropedIoc?.Invoke(ioc);
            }
            catch
            {
            }
        }

        public void GotDropedClient(string client)
        {
            try
            {
                DropedClient?.Invoke(client);
            }
            catch
            {
            }
        }

        public void GotUpdateSearch()
        {
            try
            {
                UpdateSearch?.Invoke(this, null);
            }
            catch
            {
            }
        }
    }
}
