﻿using EpicsSharp.ChannelAccess.Server;
using EpicsSharp.ChannelAccess.Server.RecordTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic
/// <summary>
/// Handles all the diagnostic channels
/// </summary>
{
    public class DiagnosticServer : IDisposable
    {
        readonly CADoubleRecord channelCpu;
        readonly CADoubleRecord channelMem;
        readonly CADoubleRecord channelAverageCpu;
        readonly CAIntRecord channelNbClientConn;
        readonly CAIntRecord channelNbServerConn;
        readonly CAIntRecord channelKnownChannels;
        readonly CAIntRecord channelOpenMonitor;
        readonly CAIntRecord channelNbSearchPerSec;
        readonly CAIntRecord channelNbMessagesPerSec;
        readonly CAIntRecord channelNbCreatedPacketPerSec;
        readonly CAIntRecord channelNbPooledPacket;
        readonly CAIntRecord channelNbTcpCreated;
        readonly CAIntRecord channelRestartGateway;
        readonly CAIntRecord channelMaxCid;
        readonly CAIntRecord channelFreeCid;
        readonly CAIntRecord channelHeartBeat;
        readonly CAStringRecord channelVersion;
        readonly CAStringRecord channelBuild;
        readonly CAStringRecord runningTime;
        readonly DateTime startTime = DateTime.Now;

        public int NbSearches = 0;
        public int NbMessages = 0;
        public int NbNewData = 0;
        public int NbPooledPacket = 0;
        public int NbTcpCreated = 0;
        bool disposed = false;

        readonly CAServer diagServer;
        readonly Gateway gateway;

        public DiagnosticServer(Gateway gateway, IPAddress address)
        {
            this.gateway = gateway;
            // Starts the diagnostic server
            // using the CAServer library
            diagServer = new CAServer(address, 7890, 7890);
            gateway.Log.Write(Services.LogLevel.Connection, "Starting debug server on " + 7890);
            // CPU usage
            channelCpu = diagServer.CreateRecord<CADoubleRecord>(gateway.Configuration.GatewayName + ":CPU");
            channelCpu.EngineeringUnits = "%";
            channelCpu.CanBeRemotlySet = false;
            channelCpu.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC5;
            channelCpu.PrepareRecord += new EventHandler(channelCPU_PrepareRecord);

            // Mem free
            channelMem = diagServer.CreateRecord<CADoubleRecord>(gateway.Configuration.GatewayName + ":MEM-FREE");
            channelMem.CanBeRemotlySet = false;
            channelMem.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC5;
            channelMem.EngineeringUnits = "Mb";
            channelMem.PrepareRecord += new EventHandler(channelMEM_PrepareRecord);

            // NB Client connections
            channelNbClientConn = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":NBCLIENTS");
            channelNbClientConn.CanBeRemotlySet = false;
            channelNbClientConn.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC5;
            channelNbClientConn.PrepareRecord += new EventHandler(channelNbClientConn_PrepareRecord);
            // NB Server connections
            channelNbServerConn = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":NBSERVERS");
            channelNbServerConn.CanBeRemotlySet = false;
            channelNbServerConn.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC5;
            channelNbServerConn.PrepareRecord += new EventHandler(channelNbServerConn_PrepareRecord);
            // Known channels (PV keept)
            channelKnownChannels = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":PVTOTAL");
            channelKnownChannels.CanBeRemotlySet = false;
            channelKnownChannels.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC5;
            channelKnownChannels.PrepareRecord += new EventHandler(channelKnownChannels_PrepareRecord);
            // Open monitors
            channelOpenMonitor = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":MONITORS");
            channelOpenMonitor.CanBeRemotlySet = false;
            channelOpenMonitor.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC5;
            channelOpenMonitor.PrepareRecord += new EventHandler(channelOpenMonitor_PrepareRecord);
            // Searches per sec
            channelNbSearchPerSec = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":SEARCH-SEC");
            channelNbSearchPerSec.CanBeRemotlySet = false;
            channelNbSearchPerSec.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC5;
            channelNbSearchPerSec.PrepareRecord += new EventHandler(channelNbSearchPerSec_PrepareRecord);
            // Messages per sec
            channelNbMessagesPerSec = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":MESSAGES-SEC");
            channelNbMessagesPerSec.CanBeRemotlySet = false;
            channelNbMessagesPerSec.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC5;
            channelNbMessagesPerSec.PrepareRecord += new EventHandler(channelNbMessagesPerSec_PrepareRecord);
            // DataPacket sent per sec
            channelNbCreatedPacketPerSec = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":NEWDATA-SEC");
            channelNbCreatedPacketPerSec.CanBeRemotlySet = false;
            channelNbCreatedPacketPerSec.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC5;
            channelNbCreatedPacketPerSec.PrepareRecord += new EventHandler(channelNbCreatedPacketPerSec_PrepareRecord);
            // Number of package ready to be re-used
            channelNbPooledPacket = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":POOLED-DATA");
            channelNbPooledPacket.CanBeRemotlySet = false;
            channelNbPooledPacket.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC5;
            channelNbPooledPacket.PrepareRecord += new EventHandler(channelNbPooledPacket_PrepareRecord);
            // TCP Created from startup
            channelNbTcpCreated = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":COUNT-TCP");
            channelNbTcpCreated.CanBeRemotlySet = false;
            channelNbTcpCreated.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC5;
            channelNbTcpCreated.PrepareRecord += new EventHandler(channelNbTcpCreated_PrepareRecord);
            // MAX CID
            channelMaxCid = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":MAX-CID");
            channelMaxCid.CanBeRemotlySet = false;
            channelMaxCid.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC5;
            channelMaxCid.PrepareRecord += new EventHandler(channelMaxCID_PrepareRecord);
            // FREE CID (not reused)
            channelFreeCid = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":FREE-CID");
            channelFreeCid.CanBeRemotlySet = false;
            channelFreeCid.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC5;
            channelFreeCid.PrepareRecord += new EventHandler(channelFreeCID_PrepareRecord);
            // Average CPU usage
            channelAverageCpu = diagServer.CreateRecord<CADoubleRecord>(gateway.Configuration.GatewayName + ":AVG-CPU");
            channelAverageCpu.CanBeRemotlySet = false;
            channelAverageCpu.EngineeringUnits = "%";
            channelAverageCpu.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC5;
            channelAverageCpu.PrepareRecord += new EventHandler(channelAverageCpu_PrepareRecord);

            runningTime = diagServer.CreateRecord<CAStringRecord>(gateway.Configuration.GatewayName + ":RUNNING-TIME");
            runningTime.CanBeRemotlySet = false;
            runningTime.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
            runningTime.PrepareRecord += runningTime_PrepareRecord;

            // Restart channel
            channelRestartGateway = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":RESTART");
            channelRestartGateway.Value = 0;
            channelRestartGateway.PropertySet += new EventHandler<PropertyDelegateEventArgs>(channelRestartGateway_PropertySet);
            // Gateway Version channel
            channelVersion = diagServer.CreateRecord<CAStringRecord>(gateway.Configuration.GatewayName + ":VERSION");
            channelVersion.CanBeRemotlySet = false;
            channelVersion.Value = Gateway.Version;
            // Gateway build date channel
            channelBuild = diagServer.CreateRecord<CAStringRecord>(gateway.Configuration.GatewayName + ":BUILD");
            channelBuild.CanBeRemotlySet = false;
            channelBuild.Value = BuildTime.ToString(CultureInfo.InvariantCulture);

            channelHeartBeat = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":BEAT");
            channelHeartBeat.Value = 0;
            channelHeartBeat.PrepareRecord += channelHeartBeat_PrepareRecord;
            channelHeartBeat.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
            diagServer.Start();
        }

        void channelHeartBeat_PrepareRecord(object sender, EventArgs e)
        {
            channelHeartBeat.Value = 1 - channelHeartBeat.Value;
        }

        void runningTime_PrepareRecord(object sender, EventArgs e)
        {
            runningTime.Value = (DateTime.Now - startTime).ToString();
        }

        // ReSharper disable InconsistentNaming
        void channelAverageCpu_PrepareRecord(object sender, EventArgs e)
        {
            TimeSpan diff = (DateTime.Now - startTime);
            channelAverageCpu.Value = (Process.GetCurrentProcess().TotalProcessorTime.TotalSeconds / diff.TotalSeconds) * 100 / Environment.ProcessorCount;
        }

        void channelMaxCID_PrepareRecord(object sender, EventArgs e)
        {
            //channelMaxCid.Value = (int)CidGenerator.Peek();
        }

        void channelFreeCID_PrepareRecord(object sender, EventArgs e)
        {
            //channelFreeCid.Value = (int)CidGenerator.freeNbCid;
        }

        void channelNbTcpCreated_PrepareRecord(object sender, EventArgs e)
        {
            channelNbTcpCreated.Value = NbTcpCreated;
        }

        void channelNbPooledPacket_PrepareRecord(object sender, EventArgs e)
        {
            channelNbPooledPacket.Value = NbPooledPacket;
        }

        void channelNbCreatedPacketPerSec_PrepareRecord(object sender, EventArgs e)
        {
            channelNbCreatedPacketPerSec.Value = NbNewData / 5;
            NbNewData = 0;
        }

        void channelNbMessagesPerSec_PrepareRecord(object sender, EventArgs e)
        {
            channelNbMessagesPerSec.Value = NbMessages / 5;
            NbMessages = 0;
        }

        void channelRestartGateway_PropertySet(object sender, PropertyDelegateEventArgs e)
        {
            /*if ((int)e.NewValue == 1)
            {
                e.CancelEvent = true;
                gateway.StopGateway();
                Thread.Sleep(2000);
                gateway.LoadConfig();
                gateway.StartGateway();
            }
            else if ((int)e.NewValue == 2)
            {
                e.CancelEvent = true;
                gateway.StopGateway();
                TcpManager.DisposeAll();
                Thread.Sleep(2000);
                gateway.LoadConfig();
                gateway.StartGateway();
            }*/
        }

        void channelNbSearchPerSec_PrepareRecord(object sender, EventArgs e)
        {
            channelNbSearchPerSec.Value = NbSearches / 5;
            NbSearches = 0;
        }

        void channelOpenMonitor_PrepareRecord(object sender, EventArgs e)
        {
            channelOpenMonitor.Value = gateway.MonitorInformation.Count;
        }

        void channelKnownChannels_PrepareRecord(object sender, EventArgs e)
        {
            channelKnownChannels.Value = gateway.ChannelInformation.Count;
        }

        void channelNbServerConn_PrepareRecord(object sender, EventArgs e)
        {
            channelNbServerConn.Value = gateway.ServerConnection.Count;
        }

        void channelNbClientConn_PrepareRecord(object sender, EventArgs e)
        {
            channelNbClientConn.Value = gateway.ClientConnection.Count;
        }

        void channelMEM_PrepareRecord(object sender, EventArgs e)
        {
            try
            {
                UInt64 total, free;
                DiagnosticInfo.GetMemoryUsage(out total, out free);
                channelMem.Value = free;
            }
            catch
            {
            }
        }

        void channelCPU_PrepareRecord(object sender, EventArgs e)
        {
            try
            {
                channelCpu.Value = DiagnosticInfo.GetCPUUsage();
            }
            catch
            {
            }
        }
        // ReSharper restore InconsistentNaming

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            diagServer.Dispose();
        }

        public static DateTime BuildTime
        {
            get
            {
                string filePath = System.Reflection.Assembly.GetCallingAssembly().Location;
                const int cPeHeaderOffset = 60;
                const int cLinkerTimestampOffset = 8;
                byte[] b = new byte[2048];
                using (System.IO.Stream s = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    s.Read(b, 0, 2048);
                }

                int i = System.BitConverter.ToInt32(b, cPeHeaderOffset);
                int secondsSince1970 = System.BitConverter.ToInt32(b, i + cLinkerTimestampOffset);
                DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
                dt = dt.AddSeconds(secondsSince1970);
                dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
                return dt;
            }
        }
    }
}