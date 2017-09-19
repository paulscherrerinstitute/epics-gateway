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
        public delegate void LogHandler(string source, string message);
        public delegate bool LogFilter(LogLevel level);

        public event LogHandler Handler = Log.DefaultHandler;
        public event LogFilter Filter = Log.ShowAll;

        private static bool ShowAll(LogLevel level)
        {
            return true;
        }

        private static void DefaultHandler(string source, string message)
        {
            Console.Write(DateTime.Now.ToString("HH:mm:ss"));
            Console.Write(" - ");
            Console.Write(source);
            Console.Write("\t");
            Console.WriteLine(message);
        }

        public void Write(LogLevel level, string message,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            Handler?.Invoke(sourceFilePath.Split(new char[] { '\\' }).Last().Split(new char[] { '.' }).First() + "." + memberName + ":" + sourceLineNumber, message);
        }
    }
}
