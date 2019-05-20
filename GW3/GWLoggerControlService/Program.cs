using System.ServiceProcess;

namespace GWLoggerControlService
{
    internal static class Program
    {
        private static void Main()
        {
            ServiceBase.Run(new ControlService());
        }
    }
}