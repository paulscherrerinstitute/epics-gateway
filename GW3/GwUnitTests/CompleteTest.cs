using System;
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
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Start();

                using (var autoReset = new AutoResetEvent(false))
                {
                    // Serverside
                    using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5056, 5056))
                    {
                        var serverChannel = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAStringRecord>("TEST-DATE");
                        serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
                        serverChannel.Value = "Works fine!";
                        string valueFound = null;

                        // Client

                        using (var client = new CAClient())
                        {
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
                        }
                    }
                }
            }
        }

        [TestMethod]
        [Timeout(1000)]
        public void CheckGet()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Start();

                // Serverside
                using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5056, 5056))
                {
                    var serverChannel = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAStringRecord>("TEST-DATE");
                    serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
                    serverChannel.Value = "Works fine!";

                    // Client

                    using (var client = new CAClient())
                    {
                        client.Configuration.SearchAddress = "127.0.0.1:5432";
                        var clientChannel = client.CreateChannel<string>("TEST-DATE");
                        server.Start();

                        Assert.AreEqual("Works fine!", clientChannel.Get());
                    }
                }
            }
        }

        [TestMethod]
        [Timeout(2000)]
        public void DisconnectServer()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Start();

                // Serverside
                using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5056, 5056))
                {
                    var serverChannel = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAStringRecord>("TEST-DATE");
                    serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
                    serverChannel.Value = "Works fine!";

                    // Client

                    using (var client = new CAClient())
                    {
                        client.Configuration.WaitTimeout = 200;
                        client.Configuration.SearchAddress = "127.0.0.1:5432";
                        var clientChannel = client.CreateChannel<string>("TEST-DATE");
                        server.Start();

                        Assert.AreEqual("Works fine!", clientChannel.Get());

                        using (var autoReset = new AutoResetEvent(false))
                        {
                            clientChannel.StatusChanged += (sender, newStatus) =>
                                {
                                    if (newStatus == EpicsSharp.ChannelAccess.Constants.ChannelStatus.DISCONNECTED)
                                        autoReset.Set();
                                };
                            server.Dispose();
                            // Need to do something with the channel to trigger the server and see the disconnection
                            try
                            {
                                clientChannel.Get();
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
        }

        [TestMethod]
        [Timeout(1000)]
        public void ReconnectServer()
        {
            using (var gateway = new Gateway())
            {
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

                using (var client = new CAClient())
                {
                    client.Configuration.WaitTimeout = 200;
                    client.Configuration.SearchAddress = "127.0.0.1:5432";
                    var clientChannel = client.CreateChannel<string>("TEST-DATE");
                    clientChannel.MonitorChanged += (channel, newValue) =>
                      {
                      };
                    server.Start();

                    Assert.AreEqual("Works fine!", clientChannel.Get());

                    using (var autoReset = new AutoResetEvent(false))
                    {
                        ChannelStatusDelegate disconnectFunction = (sender, newStatus) =>
                         {
                             if (newStatus == EpicsSharp.ChannelAccess.Constants.ChannelStatus.DISCONNECTED)
                                 autoReset.Set();
                         };
                        clientChannel.StatusChanged += disconnectFunction;
                        server.Dispose();
                        // Need to do something with the channel to trigger the server and see the disconnection
                        try
                        {
                            clientChannel.Get();
                        }
                        catch
                        {
                        }
                        autoReset.WaitOne();

                        clientChannel.StatusChanged -= disconnectFunction;
                        ChannelStatusDelegate connectFunction = (sender, newStatus) =>
                        {
                            if (newStatus == EpicsSharp.ChannelAccess.Constants.ChannelStatus.CONNECTED)
                                autoReset.Set();
                        };
                        clientChannel.StatusChanged += connectFunction;

                        server = new CAServer(IPAddress.Parse("127.0.0.1"), 5056, 5056);
                        serverChannel = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAStringRecord>("TEST-DATE");
                        serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
                        serverChannel.Value = "Works fine!";
                        server.Start();

                        autoReset.WaitOne();

                        server.Dispose();
                    }
                }
            }
        }

        [TestMethod]
        [Timeout(2000)]
        public void CancelMonitorAndRebuild()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Start();

                // Serverside
                using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5056, 5056))
                {
                    var serverChannel = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAStringRecord>("TEST-DATE");
                    serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
                    serverChannel.Value = "Works fine!";

                    // Client

                    using (var client = new CAClient())
                    {
                        client.Configuration.WaitTimeout = 200;
                        client.Configuration.SearchAddress = "127.0.0.1:5432";
                        var clientChannel = client.CreateChannel<string>("TEST-DATE");
                        server.Start();

                        using (var autoReset = new AutoResetEvent(false))
                        {
                            ChannelValueDelegate<string> monitorFunction = (sender, newValue) =>
                            {
                                Console.WriteLine("==> " + newValue);
                                autoReset.Set();
                            };

                            clientChannel.MonitorChanged += monitorFunction;
                            autoReset.WaitOne();
                            clientChannel.MonitorChanged -= monitorFunction;
                            clientChannel.MonitorChanged += monitorFunction;
                            autoReset.WaitOne();
                        }

                    }
                }
            }
        }

        [TestMethod]
        [Timeout(2000)]
        public void DoubleMonitor()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Start();

                // Serverside
                using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5056, 5056))
                {
                    var serverChannel = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAStringRecord>("TEST-DATE");
                    serverChannel.Value = "Works fine!";
                    server.Start();

                    // Client

                    using (var clientA = new CAClient())
                    {
                        using (var clientB = new CAClient())
                        {
                            clientA.Configuration.WaitTimeout = 200;
                            clientA.Configuration.SearchAddress = "127.0.0.1:5432";
                            var clientChannelA = clientA.CreateChannel<string>("TEST-DATE");

                            clientB.Configuration.WaitTimeout = 200;
                            clientB.Configuration.SearchAddress = "127.0.0.1:5432";
                            var clientChannelB = clientB.CreateChannel<string>("TEST-DATE");

                            using (var autoResetA = new AutoResetEvent(false))
                            {
                                using (var autoResetB = new AutoResetEvent(false))
                                {
                                    Console.WriteLine("Add first monitor");
                                    clientChannelA.MonitorChanged += (chan, newValue) =>
                                      {
                                          Console.WriteLine("== A > " + newValue);
                                          autoResetA.Set();
                                      };

                                    Console.WriteLine("Add second monitor");
                                    clientChannelB.MonitorChanged += (chan, newValue) =>
                                    {
                                        Console.WriteLine("== B > " + newValue);
                                        autoResetB.Set();
                                    };

                                    autoResetA.WaitOne();

                                    autoResetB.WaitOne();
                                }
                            }
                        }
                    }
                }
            }
        }

        [TestMethod]
        [Timeout(2000)]
        public void DoubleDifferedMonitor()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Start();

                // Serverside
                using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5056, 5056))
                {
                    var serverChannel = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAStringRecord>("TEST-DATE");
                    serverChannel.Value = "Works fine!";
                    server.Start();

                    // Client

                    using (var clientA = new CAClient())
                    {
                        using (var clientB = new CAClient())
                        {
                            clientA.Configuration.WaitTimeout = 200;
                            clientA.Configuration.SearchAddress = "127.0.0.1:5432";
                            var clientChannelA = clientA.CreateChannel<string>("TEST-DATE");

                            clientB.Configuration.WaitTimeout = 200;
                            clientB.Configuration.SearchAddress = "127.0.0.1:5432";
                            var clientChannelB = clientB.CreateChannel<string>("TEST-DATE");

                            using (var autoResetA = new AutoResetEvent(false))
                            {
                                using (var autoResetB = new AutoResetEvent(false))
                                {
                                    Console.WriteLine("Add first monitor");
                                    clientChannelA.MonitorChanged += (chan, newValue) =>
                                    {
                                        Console.WriteLine("== A > " + newValue);
                                        autoResetA.Set();
                                    };

                                    autoResetA.WaitOne();

                                    Console.WriteLine("Add second monitor");
                                    clientChannelB.MonitorChanged += (chan, newValue) =>
                                    {
                                        Console.WriteLine("== B > " + newValue);
                                        autoResetB.Set();
                                    };


                                    autoResetB.WaitOne();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
