using GatewayLogic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace GWService
{
    public partial class GatewayService : ServiceBase
    {
        private Gateway gateway;

        public GatewayService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            gateway = new Gateway();
            gateway.Log.ClearHandlers();
            var levelToLog = int.Parse(System.Configuration.ConfigurationManager.AppSettings["log"] ?? "2");
            gateway.Log.Filter = (level) =>
            {
                return ((int)level >= levelToLog);
            };
            gateway.LoadConfig();

            gateway.Start();
        }

        protected override void OnStop()
        {
            if (gateway != null)
                gateway.Dispose();
            gateway = null;
        }
    }
}
