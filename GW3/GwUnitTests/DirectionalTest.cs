using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EpicsSharp.ChannelAccess.Client;
using EpicsSharp.ChannelAccess.Constants;
using EpicsSharp.ChannelAccess.Server;
using EpicsSharp.ChannelAccess.Server.RecordTypes;
using System.Net;
using GatewayLogic;
using GatewayLogic.Configuration;

namespace GwUnitTests
{
    [TestClass]
    public class DirectionalTest
    {
        [TestMethod]
        [Timeout(10000)]
        [ExpectedExceptionWithMessage(typeof(Exception),ExpectedMessage = "Connection timeout.")]
        public void TestUnidirectionalGet()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Configuration.RemoteSideA = "127.0.0.1:5057";
                gateway.Configuration.ConfigurationType = ConfigurationType.UNIDIRECTIONAL;
                gateway.Configuration.DelayStartup = 0;
                gateway.Start();

                // A to B
                using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5056, 5056))
                {
                    var serverChannel = server.CreateRecord<CAStringRecord>("TEST-SERVER-A");
                    serverChannel.Value = "A works!";
                    server.Start();
                    using (var client = new CAClient())
                    {
                        client.Configuration.SearchAddress = "127.0.0.1:5432";
                        var clientChannel = client.CreateChannel<string>("TEST-SERVER-A");

                        Assert.AreEqual("A works!", clientChannel.Get());
                    }
                }

                // B to A
                using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5057, 5057))
                {
                    var serverChannel = server.CreateRecord<CAStringRecord>("TEST-SERVER-B");
                    serverChannel.Value = "B works!";
                    server.Start();

                    using (var client = new CAClient())
                    {
                        client.Configuration.WaitTimeout = 200;
                        client.Configuration.SearchAddress = "127.0.0.1:5055";
                        var clientChannel = client.CreateChannel<string>("TEST-SERVER-B");

                        clientChannel.Get();

                        /*Exception e = Assert.ThrowsException<Exception>(() => { clientChannel.Get(); });
                        Assert.AreEqual("Connection timeout.", e.Message);*/
                    }
                }
            }
        }

        [TestMethod]
        [Timeout(10000)]
        public void TestBidirectionalGet()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Configuration.RemoteSideA = "127.0.0.1:5057";
                gateway.Configuration.ConfigurationType = ConfigurationType.BIDIRECTIONAL;
                gateway.Configuration.DelayStartup = 0;
                gateway.Start();

                // A to B
                using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5056, 5056))
                {
                    var serverChannel = server.CreateRecord<CAStringRecord>("TEST-SERVER-A");
                    serverChannel.Value = "A works!";
                    server.Start();

                    using (var client = new CAClient())
                    {
                        client.Configuration.SearchAddress = "127.0.0.1:5432";
                        var clientChannel = client.CreateChannel<string>("TEST-SERVER-A");

                        Assert.AreEqual("A works!", clientChannel.Get());
                    }
                }

                // B to A
                using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5057, 5057))
                {
                    var serverChannel = server.CreateRecord<CAStringRecord>("TEST-SERVER-B");
                    serverChannel.Value = "B works!";
                    server.Start();

                    using (var client = new CAClient())
                    {
                        client.Configuration.SearchAddress = "127.0.0.1:5055";
                        var clientChannel = client.CreateChannel<string>("TEST-SERVER-B");

                        Assert.AreEqual("B works!", clientChannel.Get());
                    }
                }
            }
        }

        [TestMethod]
        [Timeout(10000)]
        [ExpectedExceptionWithMessage(typeof(Exception), ExpectedMessage = "Connection timeout.")]
        public void TestUnidirectionalDiagnostic()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.GatewayName = "TEST-UDGW";
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Configuration.RemoteSideA = "127.0.0.1:5057";
                gateway.Configuration.ConfigurationType = ConfigurationType.UNIDIRECTIONAL;
                gateway.Configuration.DelayStartup = 0;
                gateway.Start();

                // A
                using (var client = new CAClient())
                {
                    client.Configuration.SearchAddress = "127.0.0.1:5432";
                    var clientChannel = client.CreateChannel<double>("TEST-UDGW:MEM-FREE");

                    Assert.AreNotEqual(0, clientChannel.Get());
                }

                // B
                using (var client = new CAClient())
                {
                    client.Configuration.SearchAddress = "127.0.0.1:5055";
                    client.Configuration.WaitTimeout = 200;
                    var clientChannel = client.CreateChannel<double>("TEST-UDGW:MEM-FREE");

                    clientChannel.Get();
                    /*Exception e = Assert.ThrowsException<Exception>(() => { clientChannel.Get(); });
                        Assert.AreEqual("Connection timeout.", e.Message);*/
                }
            }
        }

        [TestMethod]
        [Timeout(10000)]
        public void TestBidirectionalDiagnostic()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.GatewayName = "TEST-BDGW";
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Configuration.RemoteSideA = "127.0.0.1:5057";
                gateway.Configuration.ConfigurationType = ConfigurationType.BIDIRECTIONAL;
                gateway.Configuration.DelayStartup = 0;
                gateway.Start();

                // A
                using (var client = new CAClient())
                {
                    client.Configuration.SearchAddress = "127.0.0.1:5432";
                    var clientChannel = client.CreateChannel<double>("TEST-BDGW:MEM-FREE");

                    Assert.AreNotEqual(0, clientChannel.Get());
                }

                // B
                using (var client = new CAClient())
                {
                    client.Configuration.SearchAddress = "127.0.0.1:5055";
                    var clientChannel = client.CreateChannel<double>("TEST-BDGW:MEM-FREE");

                    Assert.AreNotEqual(0, clientChannel.Get());
                }
            }
        }
    }
}
