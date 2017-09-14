using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    class Log
    {

        public void Write(string message,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            Console.WriteLine(sourceFilePath.Split(new char[] { '\\' }).Last().Split(new char[] { '.' }).First() + "." + memberName + ":" + sourceLineNumber + "\t" + message);
        }
    }
}
