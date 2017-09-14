using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    internal enum LogLevel : int
    {
        Detail = 0,
        Command,
        Connection,
        Error
    }

    internal class Log
    {

        public void Write(LogLevel level, string message,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            Console.Write(DateTime.Now.ToString("HH:mm:ss"));
            Console.Write(" - ");
            Console.Write(sourceFilePath.Split(new char[] { '\\' }).Last().Split(new char[] { '.' }).First() + "." + memberName + ":" + sourceLineNumber);
            Console.Write("\t");
            Console.WriteLine(message);
        }
    }
}
