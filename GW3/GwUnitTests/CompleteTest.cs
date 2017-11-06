using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GatewayLogic;
using EpicsSharp.ChannelAccess.Server;
using System.Net;
using EpicsSharp.ChannelAccess.Client;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using GatewayDebugData;

namespace GwUnitTests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
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
        [Timeout(1000)]
        public void CheckCascadeGet()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.2:5045";
                gateway.Configuration.SideB = "127.0.0.1:5044";
                gateway.Start();

                using (var gateway2 = new Gateway())
                {
                    gateway2.Configuration.SideA = "127.0.0.2:5045";
                    gateway2.Configuration.RemoteSideB = "127.0.0.1:5056";
                    gateway2.Configuration.SideB = "127.0.0.2:5055";
                    gateway2.Start();

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
                    server.Start();

                    // Client
                    using (var client = new CAClient())
                    {
                        client.Configuration.WaitTimeout = 200;
                        client.Configuration.SearchAddress = "127.0.0.1:5432";
                        var clientChannel = client.CreateChannel<string>("TEST-DATE");

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
        [Timeout(5000)]
        public void ReconnectServer()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Configuration.SearchPreventionTimeout = 0;
                gateway.Start();


                // Client

                using (var client = new CAClient())
                {
                    client.Configuration.WaitTimeout = 1000;
                    client.Configuration.SearchAddress = "127.0.0.1:5432";
                    var clientChannel = client.CreateChannel<string>("TEST-DATE");
                    clientChannel.MonitorChanged += (channel, newValue) =>
                      {
                      };

                    using (var autoReset = new AutoResetEvent(false))
                    {

                        ChannelStatusDelegate disconnectFunction = (sender, newStatus) =>
                        {
                            if (newStatus == EpicsSharp.ChannelAccess.Constants.ChannelStatus.DISCONNECTED)
                                autoReset.Set();
                        };
                        clientChannel.StatusChanged += disconnectFunction;


                        // Serverside
                        using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5056, 5056))
                        {
                            var serverChannel = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAStringRecord>("TEST-DATE");
                            serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
                            serverChannel.Value = "Works fine!";
                            server.Start();

                            Assert.AreEqual("Works fine!", clientChannel.Get());
                        }

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
                        // Serverside
                        using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5056, 5056))
                        {
                            var serverChannel = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAStringRecord>("TEST-DATE");
                            serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
                            serverChannel.Value = "Works fine!";
                            server.Start();
                            autoReset.WaitOne();

                        }
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
        [Timeout(10000)]
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
                            clientA.Configuration.WaitTimeout = 500;
                            clientA.Configuration.SearchAddress = "127.0.0.1:5432";
                            var clientChannelA = clientA.CreateChannel<string>("TEST-DATE");

                            clientB.Configuration.WaitTimeout = 500;
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

        [TestMethod]
        [Timeout(10000)]
        public void CheckWriteNotify()
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
                        clientChannel.Put("New value");
                        Assert.AreEqual("New value", clientChannel.Get());
                    }
                }
            }
        }

        [TestMethod]
        [Timeout(10000)]
        public void ChangeServer()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056;127.0.0.1:5057";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Start();

                // Client
                using (var client = new CAClient())
                {
                    client.Configuration.SearchAddress = "127.0.0.1:5432";
                    var clientChannel = client.CreateChannel<string>("TEST-DATE");
                    client.Configuration.WaitTimeout = 2000;

                    // Serverside
                    using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5056, 5056))
                    {
                        var serverChannel = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAStringRecord>("TEST-DATE");
                        serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
                        serverChannel.Value = "Works fine!";
                        server.Start();

                        Assert.AreEqual("Works fine!", clientChannel.Get());
                    }

                    // Allows the gateway to drops the connection
                    try
                    {
                        clientChannel.Get();
                    }
                    catch
                    {
                    }

                    Console.WriteLine("--- Closing first one ----");
                    Console.WriteLine("" + clientChannel.Status);

                    // Serverside
                    using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5057, 5057))
                    {
                        var serverChannel = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAStringRecord>("TEST-DATE");
                        serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
                        serverChannel.Value = "Works fine2!";
                        server.Start();

                        Assert.AreEqual("Works fine2!", clientChannel.Get());
                    }
                }
            }
        }


        [TestMethod]
        [Timeout(5000)]
        public void CheckGetNotRead()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Configuration.Security.RulesSideA.Add(new GatewayLogic.Configuration.SecurityRule { ChannelPattern = "T*", Access = GatewayLogic.Configuration.SecurityAccess.NONE });
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
                        client.Configuration.WaitTimeout = 100;
                        server.Start();

                        var msg = "";
                        try
                        {
                            clientChannel.Get();
                        }
                        catch (Exception ex)
                        {
                            msg = ex.Message;
                        }
                        Assert.AreEqual("Connection timeout.", msg);
                    }
                }
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public void CheckChannelReadonly()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Configuration.Security.RulesSideA.Add(new GatewayLogic.Configuration.SecurityRule { ChannelPattern = "T*", Access = GatewayLogic.Configuration.SecurityAccess.READ });
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
                        client.Configuration.WaitTimeout = 1000;
                        server.Start();

                        clientChannel.Connect();

                        Assert.AreEqual(EpicsSharp.ChannelAccess.Constants.AccessRights.ReadOnly, clientChannel.AccessRight);
                    }
                }
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void CheckGetGatewayVersion()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.GatewayName = "TESTGW";
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
                        var clientChannel = client.CreateChannel<string>("TESTGW:VERSION");
                        server.Start();

                        Assert.AreEqual(Gateway.Version, clientChannel.Get());
                    }
                }
            }
        }

        [TestMethod]
        [Timeout(3000)]
        public void CheckSubArrayGet()
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
                    var serverChannel = server.CreateArrayRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAIntSubArrayRecord>("TEST-SUBARR", 20);
                    serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
                    for (var i = 0; i < 20; i++)
                        serverChannel.Value.Data[i] = i;
                    serverChannel.Value.SetSubArray(0, 5);

                    // Client
                    using (var client = new CAClient())
                    {
                        client.Configuration.SearchAddress = "127.0.0.1:5432";
                        var clientChannel = client.CreateChannel<int[]>("TEST-SUBARR");
                        server.Start();

                        var response = clientChannel.Get();
                        Assert.AreEqual(5, response.Length);
                        Assert.IsTrue(new int[] { 0, 1, 2, 3, 4 }.SequenceEqual(response));
                    }
                }
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public void CheckSubArrayMonitorVariableIndex()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Start();

                using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5056, 5056))
                {
                    using (var client = new CAClient())
                    {
                        var serverChannel = server.CreateArrayRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAIntSubArrayRecord>("TEST-SUBARR", 20);
                        for (var i = 0; i < 20; i++)
                            serverChannel.Value.Data[i] = i;
                        serverChannel.Value.SetSubArray(0, 0);
                        server.Start();

                        client.Configuration.SearchAddress = "127.0.0.1:5432";
                        var clientChannel = client.CreateChannel<int[]>("TEST-SUBARR");
                        clientChannel.WishedDataCount = 0;

                        using (AutoResetEvent arEvt = new AutoResetEvent(false))
                        {
                            ChannelValueDelegate<int[]> handler = (s, v) =>
                            {
                                Assert.IsTrue(serverChannel.SequenceEqual(v));
                                arEvt.Set();
                            };

                            clientChannel.MonitorChanged += handler;
                            serverChannel.Value.SetSubArray(0, 5);
                            arEvt.WaitOne();
                            serverChannel.Value.SetSubArray(1, 5);
                            arEvt.WaitOne();
                            serverChannel.Value.SetSubArray(2, 5);
                            arEvt.WaitOne();
                            serverChannel.Value.SetSubArray(3, 5);
                            arEvt.WaitOne();
                            serverChannel.Value.SetSubArray(7, 5);
                            arEvt.WaitOne();
                        }
                    }
                }
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public void CheckSubArrayMonitorVariableSize()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Start();

                using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), 5056, 5056))
                {
                    using (var client = new CAClient())
                    {
                        var serverChannel = server.CreateArrayRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAIntSubArrayRecord>("TEST-SUBARR", 20);
                        for (var i = 0; i < 20; i++)
                            serverChannel.Value.Data[i] = i;
                        serverChannel.Value.SetSubArray(0, 0);
                        server.Start();

                        client.Configuration.SearchAddress = "127.0.0.1:5432";
                        var clientChannel = client.CreateChannel<int[]>("TEST-SUBARR");
                        clientChannel.WishedDataCount = 0;

                        using (AutoResetEvent arEvt = new AutoResetEvent(false))
                        {
                            ChannelValueDelegate<int[]> handler = (s, v) =>
                            {
                                Assert.IsTrue(serverChannel.SequenceEqual(v));
                                arEvt.Set();
                            };

                            clientChannel.MonitorChanged += handler;
                            serverChannel.Value.SetSubArray(0, 1);
                            arEvt.WaitOne();
                            serverChannel.Value.SetSubArray(0, 2);
                            arEvt.WaitOne();
                            serverChannel.Value.SetSubArray(0, 3);
                            arEvt.WaitOne();
                            serverChannel.Value.SetSubArray(0, 10);
                            arEvt.WaitOne();
                            serverChannel.Value.SetSubArray(3, 9);
                            arEvt.WaitOne();
                        }
                    }
                }
            }
        }

        [TestMethod]
        [Timeout(3000)]
        public void CheckSubArrayPut()
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
                    var serverChannel = server.CreateArrayRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CAIntSubArrayRecord>("TEST-SUBARR", 20);
                    serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.SEC1;
                    for (var i = 0; i < 20; i++)
                        serverChannel.Value.Data[i] = i;
                    serverChannel.Value.SetSubArray(0, 5);

                    // Client
                    using (var client = new CAClient())
                    {
                        client.Configuration.SearchAddress = "127.0.0.1:5432";
                        var clientChannel = client.CreateChannel<int[]>("TEST-SUBARR");
                        server.Start();

                        var putArr = new int[] { 4, 3, 2, 1, 0 };
                        clientChannel.Put(putArr);
                        Assert.AreEqual(5, serverChannel.Value.Length);
                        for (var i = 0; i < 5; i++)
                        {
                            Assert.AreEqual(putArr[i], serverChannel.Value[i]);
                        }
                    }
                }
            }
        }

        [TestMethod]
        [Timeout(2000)]
        public void CheckGetRemoteDebugLog()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Configuration.GatewayName = "TEST";
                gateway.Start();

                System.Configuration.ConfigurationManager.AppSettings["gatewayDebugSearch"] = "127.0.0.1:5432";

                using (var evtWait = new AutoResetEvent(false))
                {
                    using (var dbg = new DebugContext("TEST"))
                    {
                        dbg.ConnectionState += (DebugContext ctx, System.Data.ConnectionState state) =>
                        {
                            evtWait.Set();
                        };
                        evtWait.WaitOne();
                        dbg.DebugLog += (string source, System.Diagnostics.TraceEventType eventType, int chainId, string message) =>
                        {
                            Console.WriteLine("--" + message);
                            if (message == "Read notify response on TEST-DATE")
                                evtWait.Set();
                        };
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
                        evtWait.WaitOne();
                    }
                }
            }
        }

        [TestMethod]
        [Timeout(7000)]
        public void CheckGetRemoteSearches()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Configuration.GatewayName = "TEST";
                gateway.Start();

                System.Configuration.ConfigurationManager.AppSettings["gatewayDebugSearch"] = "127.0.0.1:5432";

                using (var evtWait = new AutoResetEvent(false))
                {
                    using (var dbg = new DebugContext("TEST"))
                    {
                        dbg.ConnectionState += (DebugContext ctx, System.Data.ConnectionState state) =>
                        {
                            evtWait.Set();
                        };
                        evtWait.WaitOne();
                        dbg.SearchStats += (DebugContext ctx, System.Collections.Generic.List<SearchStat> stats) =>
                        {
                            foreach (var l in stats)
                                Console.WriteLine("=> " + l.Name + ": " + l.NumberOfSearches);
                            evtWait.Set();
                        };
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
                        evtWait.WaitOne();
                    }
                }
            }
        }

        [TestMethod]
        [Timeout(7000)]
        public void CheckGetRemoteIocs()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Configuration.GatewayName = "TEST";
                gateway.Start();

                System.Configuration.ConfigurationManager.AppSettings["gatewayDebugSearch"] = "127.0.0.1:5432";

                using (var evtWait = new AutoResetEvent(false))
                {
                    using (var dbg = new DebugContext("TEST"))
                    {
                        dbg.ConnectionState += (DebugContext ctx, System.Data.ConnectionState state) =>
                        {
                            evtWait.Set();
                        };
                        evtWait.WaitOne();
                        dbg.RefreshAllIocs += (DebugContext ctx) =>
                        {
                            foreach (var l in ctx.Iocs)
                                Console.WriteLine("=> " + l.Name);
                            evtWait.Set();
                        };
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
                        evtWait.WaitOne();
                    }
                }
            }
        }

        [TestMethod]
        [Timeout(7000)]
        public void CheckNewRemoteIocs()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Configuration.GatewayName = "TEST";
                gateway.Start();

                System.Configuration.ConfigurationManager.AppSettings["gatewayDebugSearch"] = "127.0.0.1:5432";

                using (var evtWait = new AutoResetEvent(false))
                {
                    using (var dbg = new DebugContext("TEST"))
                    {
                        dbg.ConnectionState += (DebugContext ctx, System.Data.ConnectionState state) =>
                        {
                            evtWait.Set();
                        };
                        evtWait.WaitOne();
                        dbg.NewIocChannel += (DebugContext ctx, string host, string channel) =>
                        {
                            Console.WriteLine("=> " + host + " " + channel);
                            if (host == "127.0.0.1:5056" && channel == "TEST-DATE")
                                evtWait.Set();
                        };
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
                        evtWait.WaitOne();
                    }
                }
            }
        }

        [TestMethod]
        [Timeout(7000)]
        public void CheckNewRemoteClient()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.1:5056";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                gateway.Configuration.GatewayName = "TEST";
                gateway.Start();

                System.Configuration.ConfigurationManager.AppSettings["gatewayDebugSearch"] = "127.0.0.1:5432";

                using (var evtWait = new AutoResetEvent(false))
                {
                    using (var dbg = new DebugContext("TEST"))
                    {
                        dbg.ConnectionState += (DebugContext ctx, System.Data.ConnectionState state) =>
                        {
                            evtWait.Set();
                        };
                        evtWait.WaitOne();
                        dbg.NewClientChannel += (DebugContext ctx, string host, string channel) =>
                        {
                            Console.WriteLine("=> " + host + " " + channel);
                            if (host.StartsWith("127.0.0.1:") && channel == "TEST-DATE")
                                evtWait.Set();
                        };
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
                        evtWait.WaitOne();
                    }
                }
            }
        }


        [TestMethod]
        [Timeout(3000)]
        public void CheckCascadeDebug()
        {
            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                gateway.Configuration.RemoteSideB = "127.0.0.2:5045";
                gateway.Configuration.SideB = "127.0.0.1:5044";
                gateway.Configuration.GatewayName = "T1";
                gateway.Start();

                using (var gateway2 = new Gateway())
                {
                    gateway2.Configuration.SideA = "127.0.0.2:5045";
                    gateway2.Configuration.RemoteSideB = "127.0.0.1:5056";
                    gateway2.Configuration.SideB = "127.0.0.2:5055";
                    gateway2.Configuration.GatewayName = "T2";
                    gateway2.Start();

                    System.Configuration.ConfigurationManager.AppSettings["gatewayDebugSearch"] = "127.0.0.1:5432";

                    using (var evtWait = new AutoResetEvent(false))
                    {
                        using (var dbg = new DebugContext("T2"))
                        {
                            dbg.ConnectionState += (DebugContext ctx, System.Data.ConnectionState state) =>
                            {
                                try
                                {
                                    evtWait.Set();
                                }
                                catch
                                {
                                }
                            };
                            evtWait.WaitOne();
                            dbg.DebugLog += (string source, System.Diagnostics.TraceEventType eventType, int chainId, string message) =>
                            {
                                Console.WriteLine("--" + message);
                                if (message == "Read notify response on TEST-DATE")
                                    evtWait.Set();
                            };
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
                            evtWait.WaitOne();
                        }
                    }
                }
            }
        }

    }
}
