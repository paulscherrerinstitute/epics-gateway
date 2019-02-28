using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LoadPerformance
{
    internal class Program
    {
        static public int NbArrays = 300;
        static public int ArraySize = 1024 * 2;
        static public int NbClients = 15;
        static public int NbServers = 15;
        static public string ServerAddress = "127.0.0.1";
        static public string ClientSearchAddress = "127.0.0.1:5064";

        public const int CA_PROTO_VERSION = 13;

        private static void Main(string[] args)
        {
            ServerAddress = System.Configuration.ConfigurationManager.AppSettings["serverAddress"];
            ClientSearchAddress = System.Configuration.ConfigurationManager.AppSettings["clientSearchAddress"];
            NbClients = int.Parse(System.Configuration.ConfigurationManager.AppSettings["nbClients"]);
            NbServers = int.Parse(System.Configuration.ConfigurationManager.AppSettings["nbServers"]); ;
            ArraySize = int.Parse(System.Configuration.ConfigurationManager.AppSettings["arraySize"]); ;
            NbArrays = int.Parse(System.Configuration.ConfigurationManager.AppSettings["nbArrays"]);

            var threads = new List<Thread>();
            var cancel = new CancellationTokenSource();

            var servers = new LoadServer[NbServers];
            var clients = new LoadClient[NbClients];

            if (args.Length < 1)
            {
                ShowHelp();
                Environment.Exit(1);
            }

            var nbMons = NbArrays;

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "--nbmon")
                {
                    i++;
                    nbMons = int.Parse(args[i]);
                }
            }

            var didSomething = false;

            if (args[0] == "--server" || args[0] == "--both")
            {
                didSomething = true;
                Console.WriteLine("Starting servers...");
                for (var i = 0; i < servers.Length; i++)
                {
                    var thread = new Thread((obj) =>
                      {
                          var idx = (int)obj;
                          var nb = NbArrays / NbServers;
                          if (idx == servers.Length - 1 && idx != 0) // Lets take all the left overs
                              nb += NbArrays % NbServers;
                          servers[idx] = new LoadServer(ServerAddress, NbArrays * idx / servers.Length, nb, idx * 2);
                          cancel.Token.WaitHandle.WaitOne();
                      });
                    thread.Start(i);
                    threads.Add(thread);
                }
            }
            if (args[0] == "--client" || args[0] == "--both")
            {
                didSomething = true;
                Console.WriteLine("Starting clients... (will monitor " + nbMons + " channels of " + ArraySize + ")");
                for (var i = 0; i < clients.Length; i++)
                {
                    var thread = new Thread((obj) =>
                      {
                          var idx = (int)obj;
                          var nb = nbMons / NbClients;
                          if (idx == clients.Length - 1 && idx != 0) // Lets take all the left overs
                              nb += nbMons % clients.Length;
                          clients[idx] = new LoadClient(ClientSearchAddress, NbArrays * idx / clients.Length, nb, idx + 100);
                          cancel.Token.WaitHandle.WaitOne();
                      });
                    thread.Start(i);
                    threads.Add(thread);
                }

                Thread.Sleep(1000);
                var totIdeal = 0L;
                var startTime = DateTime.UtcNow;
                var t = new Thread(() =>
                {
                    while (!cancel.IsCancellationRequested)
                    {
                        Thread.Sleep(1000);
                        try
                        {
                            totIdeal += nbMons * ArraySize * 4 * 10;
                            var idealPerSec = (long)(totIdeal / (DateTime.UtcNow - startTime).TotalSeconds);
                            var connected = clients.Sum(r => (r?.Connected) ?? 0);
                            Console.Write("" + HumanSize(clients.Sum(r => r.NbBytesSec)) + " / " + HumanSize(idealPerSec) + " (" + (clients.Sum(r => r.NbBytesSec) * 100 / idealPerSec) + " %, connected: " + connected + ")                              \r");
                            //Console.Write("" + HumanSize(clients.Sum(r => r.NbBytesSec)) + " / " + HumanSize(servers.Sum(r => r.NbChangedPerSec)) + " (" + (clients.Sum(r => r.NbBytesSec) * 100 / servers.Sum(r => r.NbChangedPerSec)) + " %)                              \r");
                        }
                        catch (DivideByZeroException)
                        {
                        }
                    }
                });
                t.Start();
            }
            if (!didSomething)
            {
                ShowHelp();
                Environment.Exit(1);
            }

            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();
            cancel.Cancel();
            Console.WriteLine("Stopping...");
        }

        private static void ShowHelp()
        {
            Console.WriteLine("One of the following switch must be used as first argument:");
            Console.WriteLine("--client               starts the clients");
            Console.WriteLine("--server               starts the servers");
            Console.WriteLine("--both                 starts both the clients and the servers");
            Console.WriteLine("");
            Console.WriteLine("Can optionally specifiy the following options:");
            Console.WriteLine("--nbmon <nb>           specifies how many monitors must be started");
        }

        private static string HumanSize(long nb)
        {
            if (nb > 1024l * 1024 * 1024 * 1024)
                return (nb / (1024l * 1024 * 1024 * 1024)) + "TB";
            else if (nb > 1024 * 1024 * 1024)
                return (nb / (1024 * 1024 * 1024)) + "GB";
            else if (nb > 1024 * 1024)
                return (nb / (1024 * 1024)) + "MB";
            else if (nb > 1024)
                return (nb / 1024) + "KB";
            return nb + "Bytes";
        }
    }
}
