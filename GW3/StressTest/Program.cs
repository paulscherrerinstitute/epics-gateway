using EpicsSharp.ChannelAccess.Client;
using EpicsSharp.ChannelAccess.Server;
using EpicsSharp.ChannelAccess.Server.RecordTypes;
using GatewayLogic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StressTest
{
    class Program
    {
        const int NB_SERVERS = 20;
        const int NB_CLIENTS = 30;
        const int NB_CHANNELS = 20;
        const int NB_LOOPS = 1;
        const int NB_CHECKED = 40;
        const int WAIT_TIMEOUT = 10000;

        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "server")
            {
                //Console.WriteLine("Start server " + args[1]);
                Console.Title = "Stress Test IOC";
                Server(int.Parse(args[1]));
                return;
            }

            if (args.Length > 0 && args[0] == "client")
            {
                //Console.WriteLine("Start client " + args[1]);
                Console.Title = "Stress Test Client";
                Environment.Exit(Client());
                return;
            }

            Console.SetWindowSize(130, 60);
            Console.SetBufferSize(130, 500);
            Console.WriteLine("Starting controller...");

            using (var gateway = new Gateway())
            {
                gateway.Configuration.SideA = "127.0.0.1:5432";
                var addrs = "";
                for (var i = 0; i < NB_SERVERS; i++)
                    addrs += (i != 0 ? ";" : "") + "127.0.0.1:" + (5056 + i);
                gateway.Configuration.RemoteSideB = addrs;
                gateway.Configuration.GatewayName = "STRESSGW";
                gateway.Configuration.SideB = "127.0.0.1:5055";
                //gateway.Log.Filter = (level) => { return level >= GatewayLogic.Services.LogLevel.Error; };
                gateway.Log.ClearHandlers();
                var logBuffer = new List<string>();
                gateway.Log.Handler += (level, source, message) =>
                 {
                     lock (logBuffer)
                     {
                         logBuffer.Add(DateTime.UtcNow.ToString("HH:mm:ss") + " - " + source + "\t" + message);
                         while (logBuffer.Count > 300)
                             logBuffer.RemoveAt(0);
                     }

                     if (message.Contains("Deadlock"))
                     {

                     }

                     if (level >= GatewayLogic.Services.LogLevel.Critical)
                     {
                         Console.Write(DateTime.UtcNow.ToString("HH:mm:ss"));
                         Console.Write(" - ");
                         Console.Write(source);
                         Console.Write("\t");
                         Console.WriteLine(message);
                     }
                 };
                gateway.Start();

                var current = Process.GetCurrentProcess();

                var clientTot = 0;
                var clientNb = 0;

                var lockReportObject = new object();

                EventHandler serverExit = (obj, evt) => { ((Process)obj).Start(); };
                EventHandler clientExit = (obj, evt) =>
                {
                    lock (lockReportObject)
                    {
                        var p = ((Process)obj);

                        var text = p.StandardOutput.ReadToEnd();
                        if (text.Contains("!! Wrong"))
                        {
                            Console.WriteLine("--------------------------------------------------");
                            lock (logBuffer)
                            {
                                logBuffer.ForEach(row => Console.WriteLine(row));
                                logBuffer.Clear();
                            }
                            Console.WriteLine("--------------------------------------------------");
                            Console.Write(text);
                            Console.WriteLine("--------------------------------------------------");
                        }
                        /*else if (text.Contains("!!"))
                        {
                            Console.WriteLine("--------------------------------------------------");
                            Console.Write(text);
                            lock (logBuffer)
                            {
                                logBuffer.ForEach(row => Console.WriteLine(row));
                                logBuffer.Clear();
                            }
                            Console.WriteLine("--------------------------------------------------");
                        }
                        else
                            Console.Write(text);*/

                        if (p.ExitCode >= 0)
                        {
                            clientTot += p.ExitCode;
                            clientNb++;
                        }
                        p.Start();
                    }
                };

                var servers = Enumerable.Range(0, NB_SERVERS)
                    .Select(i =>
                    {
                        var p = new Process();
                        p.StartInfo = new ProcessStartInfo(current.ProcessName, "server " + i)
                        {
                            UseShellExecute = false,
                        };
                        p.EnableRaisingEvents = true;
                        p.Exited += serverExit;
                        p.Start();
                        return p;
                    }).ToList();

                var clients = Enumerable.Range(0, NB_CLIENTS)
                    .Select(i =>
                    {
                        var p = new Process();
                        p.StartInfo = new ProcessStartInfo(current.ProcessName, "client " + i)
                        {
                            RedirectStandardOutput = true,
                            UseShellExecute = false
                        };
                        p.EnableRaisingEvents = true;
                        p.Exited += clientExit;
                        p.Start();
                        return p;
                    }).ToList();

                var rnd = new Random();
                var randomKiller = new Thread((obj) =>
                  {
                      while (true)
                      {
                          Thread.Sleep(rnd.Next(1000, 6000));
                          if (NB_CLIENTS < 10 && rnd.Next(0, 100) > 80)
                          {
                              var p = rnd.Next(0, clients.Count);
                              //Console.WriteLine("Killing client " + p);
                              try
                              {
                                  clients[p].Kill();
                              }
                              catch
                              {
                              }
                          }
                          else
                          {
                              var nb = rnd.Next(0, NB_CLIENTS / 10);
                              for (var j = 0; j < nb; j++)
                              {
                                  var p = rnd.Next(0, clients.Count);
                                  //Console.WriteLine("Killing client " + p);
                                  try
                                  {
                                      clients[p].Kill();
                                  }
                                  catch
                                  {
                                  }
                              }
                          }

                          //Console.WriteLine("Killing server " + p);
                          try
                          {
                              servers[rnd.Next(0, servers.Count)].Kill();
                          }
                          catch
                          {
                          }
                      }
                  });
                randomKiller.IsBackground = true;
                randomKiller.Start();

                EventHandler quitStressTest = (obj, evt) =>
                 {
                     servers.ForEach(row => row.Exited -= serverExit);
                     clients.ForEach(row => row.Exited -= clientExit);

                     servers.ForEach(row => { try { row.Kill(); } catch { } });
                     clients.ForEach(row => { try { row.Kill(); } catch { } });
                 };

                AppDomain.CurrentDomain.ProcessExit += quitStressTest;
                Console.Title = "Gateway Stress Test";
                Console.WriteLine("All started...");

                using (var client = new CAClient())
                {
                    client.Configuration.SearchAddress = "127.0.0.1:5432";
                    client.Configuration.WaitTimeout = WAIT_TIMEOUT;
                    var dbgSearchSec = client.CreateChannel<string>("STRESSGW:SEARCH-SEC");
                    var searchSec = "";
                    dbgSearchSec.MonitorChanged += (chan, val) =>
                     {
                         searchSec = val;
                     };
                    var dbgMsgSec = client.CreateChannel<string>("STRESSGW:MESSAGES-SEC");
                    var nbClients = "";
                    var dbgClients = client.CreateChannel<string>("STRESSGW:NBCLIENTS");
                    dbgClients.MonitorChanged += (chan, val) =>
                    {
                        nbClients = val;
                    };
                    var dbgTime = client.CreateChannel<string>("STRESSGW:RUNNING-TIME");
                    var nbMsg = "";
                    dbgMsgSec.MonitorChanged += (chan, val) =>
                      {
                          nbMsg = val;
                      };
                    dbgTime.MonitorChanged += (chan, val) =>
                    {
                        var qual = clientTot / (clientNb == 0 ? 1 : clientNb);
                        clientNb = 0;
                        clientTot = 0;
                        Console.Write(val + " - Msg/sec: " + nbMsg + ", Search/sec: " + searchSec + ", NB Clients: " + nbClients + ", Qual: " + qual + "                   \r");
                        Console.Out.Flush();
                    };

                    Console.CancelKeyPress += (obj, evt) =>
                    {
                        Console.WriteLine("Stopping...");
                        quitStressTest(null, null);
                        gateway.Dispose();
                        Environment.Exit(0);
                    };
                    Console.WriteLine("Ctrl+C to stop...");
                    while (true)
                        Console.ReadKey();
                }
            }
        }

        private static int Client()
        {
            var rnd = new Random();
            switch (rnd.Next(0, 3))
            {
                case 0:
                    //Console.WriteLine("Ten sec monitor");
                    return MonitorTenSecAction();
                case 1:
                    //Console.WriteLine("All monitors");
                    return MonitorOnceAction();
                case 2:
                    //Console.WriteLine("Get all");
                    return GetOnceAction();
            }
            return -1;
        }

        static int MonitorTenSecAction()
        {
            var rnd = new Random();
            var channelNames = Enumerable.Range(0, NB_SERVERS * NB_CHANNELS - 1)
                .Select(row => new KeyValuePair<string, double>("STRESS-TEST-" + (row + 1), rnd.Next()))
                .OrderBy(row => row.Value)
                .Select(row => row.Key)
                .Take(NB_CHECKED)
                .ToList();

            var tot = 0;
            var totOk = 0;

            for (var i = 0; i < NB_LOOPS; i++)
            {
                using (var client = new CAClient())
                {
                    client.Configuration.SearchAddress = "127.0.0.1:5432";
                    client.Configuration.WaitTimeout = WAIT_TIMEOUT;

                    var channels = channelNames.Select(row => client.CreateChannel<string>(row)).ToList();

                    var multiEvt = new CountdownEvent(channels.Count());

                    var nb = 0;
                    var res = new string[channels.Count];
                    channels.ForEach((row) =>
                    {
                        row.MonitorChanged += (chan, val) =>
                        {
                            for (var j = 0; j < channels.Count; j++)
                            {
                                if (channels[j].ChannelName == chan.ChannelName)
                                {
                                    res[j] = val;
                                    break;
                                }
                            }
                            multiEvt.Signal();
                            Interlocked.Increment(ref nb);
                        };
                    });
                    multiEvt.Wait(WAIT_TIMEOUT);

                    bool wrongData = false;
                    for (var j = 0; j < channels.Count; j++)
                    {
                        var id = channels[j].ChannelName.Split(new char[] { '-' }).Last();
                        if (res[j] != null && !((string)res[j]).Contains("- " + id + " -"))
                        {
                            wrongData = true;
                            Console.WriteLine("Was expecting id " + id + " found :" + res[j]);
                            break;
                        }
                    }
                    if (wrongData)
                        Console.WriteLine("!!! Wrong data");
                    tot += channels.Count;
                    totOk += nb;
                    if (nb != channels.Count)
                        Console.WriteLine("!!! NOT ALL is read: " + (channels.Count - nb) + " / " + channels.Count);
                    else
                        Console.WriteLine("Monitor complete");

                    Thread.Sleep(10000);
                }
            }
            return totOk * 100 / tot;
        }

        static int MonitorOnceAction()
        {
            var rnd = new Random();
            var channelNames = Enumerable.Range(0, NB_SERVERS * NB_CHANNELS - 1)
                .Select(row => new KeyValuePair<string, double>("STRESS-TEST-" + (row + 1), rnd.Next()))
                .OrderBy(row => row.Value)
                .Select(row => row.Key)
                .Take(NB_CHECKED)
                .ToList();

            var tot = 0;
            var totOk = 0;

            for (var i = 0; i < NB_LOOPS; i++)
            {
                using (var client = new CAClient())
                {
                    client.Configuration.SearchAddress = "127.0.0.1:5432";
                    client.Configuration.WaitTimeout = WAIT_TIMEOUT;

                    var channels = channelNames.Select(row => client.CreateChannel<string>(row)).ToList();

                    var multiEvt = new CountdownEvent(channels.Count());

                    var nb = 0;
                    var res = new string[channels.Count];
                    channels.ForEach((row) =>
                    {
                        row.MonitorChanged += (chan, val) =>
                            {
                                for (var j = 0; j < channels.Count; j++)
                                {
                                    if (channels[j].ChannelName == chan.ChannelName)
                                    {
                                        res[j] = val;
                                        break;
                                    }
                                }
                                multiEvt.Signal();
                                Interlocked.Increment(ref nb);
                            };
                    });
                    multiEvt.Wait(WAIT_TIMEOUT);

                    bool wrongData = false;
                    for (var j = 0; j < channels.Count; j++)
                    {
                        var id = channels[j].ChannelName.Split(new char[] { '-' }).Last();
                        if (res[j] != null && !((string)res[j]).Contains("- " + id + " -"))
                        {
                            wrongData = true;
                            Console.WriteLine("Was expecting id " + id + " found :" + res[j]);
                            break;
                        }
                    }
                    if (wrongData)
                        Console.WriteLine("!!! Wrong data");

                    tot += channels.Count;
                    totOk += nb;
                    if (nb != channels.Count)
                        Console.WriteLine("!!! NOT ALL is read: " + (channels.Count - nb) + " / " + channels.Count);
                    else
                        Console.WriteLine("Monitor complete");
                }
            }
            return totOk * 100 / tot;
        }

        static int GetOnceAction()
        {
            var rnd = new Random();
            var channelNames = Enumerable.Range(0, NB_SERVERS * NB_CHANNELS - 1)
                .Select(row => new KeyValuePair<string, double>("STRESS-TEST-" + (row + 1), rnd.Next()))
                .OrderBy(row => row.Value)
                .Select(row => row.Key)
                .Take(NB_CHECKED)
                .ToList();

            var tot = 0;
            var totOk = 0;

            for (var i = 0; i < NB_LOOPS; i++)
            {
                using (var client = new CAClient())
                {
                    client.Configuration.SearchAddress = "127.0.0.1:5432";
                    client.Configuration.WaitTimeout = WAIT_TIMEOUT;

                    var channels = channelNames.Select(row => client.CreateChannel<string>(row)).ToList();

                    var res = client.MultiGet<string>(channels);
                    var nb = res.Count(row => row == null);

                    bool wrongData = false;
                    for (var j = 0; j < channels.Count; j++)
                    {
                        var id = channels[j].ChannelName.Split(new char[] { '-' }).Last();
                        if (res[j] != null && !((string)res[j]).Contains("- " + id + " -"))
                        {
                            wrongData = true;
                            Console.WriteLine("Was expecting id " + id + " (" + channels[j].ChannelName + ") found: " + res[j]);
                            //break;
                        }
                    }
                    if (wrongData)
                        Console.WriteLine("!!! Wrong data");

                    tot += channels.Count;
                    totOk += channels.Count - nb;
                    if (nb != 0)
                        Console.WriteLine("!!! CAGET Incomplete: " + nb + " / " + channels.Count);
                    else
                        Console.WriteLine("CAGET Complete");
                }
            }
            return totOk * 100 / tot;
        }

        static void Server(int serverId)
        {
            using (var evt = new AutoResetEvent(false))
            {
                using (var server = new CAServer(IPAddress.Parse("127.0.0.1"), (5056 + serverId), (5056 + serverId)))
                {
                    var serverChannels = new List<CAStringRecord>();
                    for (var i = 0; i < NB_CHANNELS; i++)
                    {
                        var id = (i + (serverId * NB_CHANNELS));
                        var serverChannel = server.CreateRecord<CAStringRecord>("STRESS-TEST-" + id);
                        serverChannel.Value = "Works fine! - " + id + " - " + DateTime.UtcNow.ToLongTimeString();
                        /*serverChannel.PrepareRecord += (rec, obj) =>
                          {
                              var chanId = ((CARecord)rec).Name.Split(new char[] { '-' }).Last();
                              serverChannel.Value = "Works fine! - " + chanId + " - " + DateTime.UtcNow.ToLongTimeString();
                          };
                        serverChannel.Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.HZ10;*/
                        serverChannels.Add(serverChannel);
                    }
                    server.Start();
                    evt.WaitOne();
                }
            }
        }
    }
}
