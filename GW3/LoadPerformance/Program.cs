using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace LoadPerformance
{
    internal class Program
    {
        static public int ArraySize = 1024 * 4;
        static public string ServerAddress = "127.0.0.1";
        static public string ClientSearchAddress = "127.0.0.1:5064";
        static public int NbServers = 40;
        static public int NbClients = 1;
        static public int ServerPort = 5064;

        public const int CA_PROTO_VERSION = 13;

        private static void Main(string[] args)
        {
            ServerAddress = System.Configuration.ConfigurationManager.AppSettings["serverAddress"];
            ClientSearchAddress = System.Configuration.ConfigurationManager.AppSettings["clientSearchAddress"];

            var threads = new List<Thread>();
            var cancel = new CancellationTokenSource();

            LoadServer server = null;
            LoadClient client = null;

            if (args.Length < 1)
            {
                ShowHelp();
                Environment.Exit(1);
            }

            var nbMons = 100;
            int nbSteps = 105;

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "--nbmon")
                {
                    i++;
                    nbMons = int.Parse(args[i]);
                }
                else if (args[i] == "--nbsteps")
                {
                    i++;
                    nbSteps = int.Parse(args[i]);
                }
                else if (args[i] == "--size")
                {
                    i++;
                    ArraySize = int.Parse(args[i]);
                }
                else if (args[i] == "--nbclients")
                {
                    i++;
                    NbClients = int.Parse(args[i]);
                }
                else if (args[i] == "--saddr")
                {
                    i++;
                    ServerAddress = args[i];
                }
                else if (args[i] == "--caddr")
                {
                    i++;
                    ClientSearchAddress = args[i];
                }
                else if (args[i] == "--sport")
                {
                    i++;
                    ServerPort = int.Parse(args[i]);
                }
            }

            var didSomething = false;


            Action waitInput = () =>
              {
                  Console.WriteLine("Press any key to stop...");
                  Console.ReadKey();
                  cancel.Cancel();
                  client?.Dispose();
                  server?.Dispose();
                  Console.WriteLine("Stopping...");
              };


            if (args[0] == "--server" || args[0] == "--both")
            {
                didSomething = true;
                Console.WriteLine("Starting server...");
                server = new LoadServer(ServerAddress, ServerPort, NbServers);

                if (args[0] == "--server")
                    waitInput();
            }
            if (args[0] == "--report")
            {
                didSomething = true;
                Console.WriteLine("Starting server...");
                server = new LoadServer(ServerAddress, ServerPort, NbServers);

                using (var writer = new StreamWriter(File.Create(args[1])))
                {

                    for (var i = 0; i < nbSteps; i++)
                    {
                        var nb = Math.Max(1, i * 10);
                        long dataPerSec = 0;
                        long expectedPerSec = 0;

                        for (var j = 0; j < 4; j++)
                        {
                            Console.WriteLine("Starting client... (will monitor " + nb + " channels of " + ArraySize + ")");
                            using (client = new LoadClient(ClientSearchAddress, nb, NbClients, 5066))
                            {
                                Thread.Sleep(3000);
                                client.ResetCounter();
                                Thread.Sleep(3000);
                                dataPerSec = client.DataPerSeconds;
                                expectedPerSec = client.ExpectedDataPerSeconds;
                            }

                            // Data is valid?
                            if (dataPerSec > 0 && dataPerSec >= expectedPerSec * 4 / 10)
                                break;
                        }
                        writer.WriteLine(nb + ";" + dataPerSec + ";" + expectedPerSec);
                    }
                }
                Console.WriteLine("Run report done");
                cancel.Cancel();
                client?.Dispose();
                server?.Dispose();
            }
            if (args[0] == "--client" || args[0] == "--both")
            {
                didSomething = true;
                Console.WriteLine("Starting client... (will monitor " + nbMons + " channels of " + ArraySize + ")");
                client = new LoadClient(ClientSearchAddress, nbMons, NbClients, 5066);

                Thread.Sleep(1000);
                var t = new Thread(() =>
                {
                    var resetCounter = 10;
                    while (!cancel.IsCancellationRequested)
                    {
                        Thread.Sleep(1000);
                        try
                        {
                            Console.Write("Data: " + HumanSize(client.DataPerSeconds) + " / " + HumanSize(client.ExpectedDataPerSeconds) + " (" + (client.DataPerSeconds * 100 / client.ExpectedDataPerSeconds) + "%, conn " + client.NbConnected + ")                          \r");
                        }
                        catch (DivideByZeroException)
                        {
                        }
                        resetCounter--;
                        if (resetCounter < 1)
                        {
                            resetCounter = 10;
                            client.ResetCounter();
                        }
                    }
                });
                t.Start();
                waitInput();
            }
            if (!didSomething)
            {
                ShowHelp();
                Environment.Exit(1);
            }

        }

        private static void ShowHelp()
        {
            Console.WriteLine("One of the following switch must be used as first argument:");
            Console.WriteLine("--client               starts the clients");
            Console.WriteLine("--server               starts the servers");
            Console.WriteLine("--both                 starts both the clients and the servers");
            Console.WriteLine("--report <filename>    starts both the clients and the servers and run a reporting test (csv)");
            Console.WriteLine("");
            Console.WriteLine("Can optionally specifiy the following options:");
            Console.WriteLine("--nbmon <nb>           specifies how many monitors must be started");
            Console.WriteLine("--nbclients <nb>       specifies how many monitors must be started [default 1]");
            Console.WriteLine("--nbsteps <nb>         specifies how steps for the report");
            Console.WriteLine("--size <nb>            specifies the array size");
            Console.WriteLine("--saddr <addr>         specifies the server address");
            Console.WriteLine("--sport <port>         specifies the server port when serving");
            Console.WriteLine("--caddr <addr:port>    specifies the client search address & port");
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
