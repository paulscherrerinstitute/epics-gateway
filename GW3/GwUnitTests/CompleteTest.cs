﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GatewayLogic;
using EpicsSharp.ChannelAccess.Server;
using System.Net;
using EpicsSharp.ChannelAccess.Client;
using System.Threading;

namespace GwUnitTests
{
    [TestClass]
    public class CompleteTest
    {
        [TestMethod]
        [Timeout(1000)]
        public void CheckMonitor()
        {
            var gateway = new Gateway();
            gateway.Configuration.SideA = "127.0.0.1:5432";
            gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
            gateway.Configuration.SideB = "127.0.0.1:5055";
            gateway.Start();

            var autoReset = new AutoResetEvent(false);
            // Serverside
            var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5056, 5056);
            var serverChannel = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAStringRecord>("TEST-DATE");
            serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
            serverChannel.Value = "Works fine!";
            string valueFound = null;

            // Client

            var client = new CAClient();
            client.Configuration.SearchAddress = "127.0.0.1:5432";
            var clientChannel = client.CreateChannel<string>("TEST-DATE");
            clientChannel.MonitorChanged += (sender, newValue) =>
            {
                valueFound = newValue;
                autoReset.Set();
            };

            server.Start();

            autoReset.WaitOne();
            Assert.AreEqual("Works fine!", valueFound);

            gateway.Dispose();
            server.Dispose();
            client.Dispose();
            autoReset.Dispose();
        }

        [TestMethod]
        [Timeout(1000)]
        public void CheckGet()
        {
            var gateway = new Gateway();
            gateway.Configuration.SideA = "127.0.0.1:5432";
            gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
            gateway.Configuration.SideB = "127.0.0.1:5055";
            gateway.Start();

            // Serverside
            var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5056, 5056);
            var serverChannel = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAStringRecord>("TEST-DATE");
            serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
            serverChannel.Value = "Works fine!";

            // Client

            var client = new CAClient();
            client.Configuration.SearchAddress = "127.0.0.1:5432";
            var clientChannel = client.CreateChannel<string>("TEST-DATE");
            server.Start();

            Assert.AreEqual("Works fine!", clientChannel.Get());

            gateway.Dispose();
            server.Dispose();
            client.Dispose();
        }
    }
}
