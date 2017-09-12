using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic
{
    [Flags]
    public enum SecurityAccess
    {
        NONE = 0x0,
        READ = 0x1,
        WRITE = 0x2,
        ALL = READ | WRITE
    }
}
