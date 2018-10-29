using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GWLogger.AuthAccess
{
    public class AlertSubscription
    {
        public string EMail { get; set; }
        public List<string> Gateways { get; set; } = new List<string>();
    }
}