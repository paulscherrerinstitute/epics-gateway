using GatewayLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GWConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var gateway = new Gateway();
            gateway.LoadConfig("https://inventory.psi.ch/soap/gatewayConfig.aspx?gateway=", "PBGW");

            /*Console.WriteLine("Starting...");
            var gateway = new Gateway();
            gateway.Configuration.SideA = "129.129.130.45:5055";
            gateway.Configuration.SideB = "129.129.130.45:5056";
            gateway.Start();
            Console.ReadKey();*/
        }
    }
}
