using GatewayLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GWConsole
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    class Program
    {
        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            try
            {
                Console.SetWindowSize(130, 60);
            }
            catch
            {
            }
            try
            {
                Console.SetBufferSize(130, 500);
            }
            catch
            {
            }
            Console.Clear();
            Console.WriteLine("Starting...");

            var gateway = new Gateway();
            gateway.LoadConfig();
            var levelToLog = int.Parse(System.Configuration.ConfigurationManager.AppSettings["log"] ?? "0");
            gateway.Log.Filter = (level) =>
             {
                 return ((int)level >= levelToLog);
             };
            gateway.Log.ClearHandlers();
            gateway.Log.Handler += (level, source, message) =>
              {
                  switch(level)
                  {
                      case GatewayLogic.Services.LogLevel.Command:
                          Console.ForegroundColor = ConsoleColor.DarkGray;
                          break;
                      case GatewayLogic.Services.LogLevel.Connection:
                          Console.ForegroundColor = ConsoleColor.Green;
                          break;
                      case GatewayLogic.Services.LogLevel.Critical:
                          Console.ForegroundColor = ConsoleColor.Red;
                          break;
                      case GatewayLogic.Services.LogLevel.Detail:
                          Console.ForegroundColor = ConsoleColor.Gray;
                          break;
                      case GatewayLogic.Services.LogLevel.Error:
                          Console.ForegroundColor = ConsoleColor.Yellow;
                          break;
                  }

                  Console.Write(DateTime.UtcNow.ToString("HH:mm:ss"));
                  Console.Write(" - ");
                  Console.Write(source);
                  Console.Write("\t");
                  Console.WriteLine(message);

                  Console.ForegroundColor = ConsoleColor.Gray;
              };

            Console.Title = "Gateway " + gateway.Configuration.GatewayName;
            Console.CancelKeyPress += (obj, evt) =>
            {
                Console.WriteLine("Stopping...");
                gateway.Dispose();
                Environment.Exit(0);
            };
            gateway.Start();
            Console.WriteLine("Ctrl+C to quit...");
            while (true)
                Console.ReadKey();
        }
    }
}
