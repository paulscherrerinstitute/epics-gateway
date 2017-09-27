using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace  GatewayLogic.Configuration
{
    [Serializable]
    public class AllFilter : SecurityFilter
    {
        public override bool Applies(string username, string hostname, string ip)
        {
            return true;
        }

        public override void Init()
        {
        }
    }
}
