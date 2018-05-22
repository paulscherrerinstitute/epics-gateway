using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    public enum LogLevel : int
    {
        Detail = 0,
        Command = 1,
        Connection = 2,
        Error = 3,
        Critical = 4
    }

    public class TextLogger : IDisposable
    {
        public delegate void LogHandler(LogLevel level, string source, string message);
        public delegate bool LogFilter(LogLevel level);
        public static int? FilterLevel = null;

        //public event LogHandler Handler = TextLogger.DefaultHandler;
        public event LogHandler Handler;
        public LogFilter Filter = TextLogger.ShowAll;
        private SafeLock lockObject = new SafeLock();

        internal TextLogger()
        {

        }

        public static bool ShowAll(LogLevel level)
        {
            return true;
        }

        public static bool ShowUpToLevel(LogLevel level)
        {
            if (!FilterLevel.HasValue)
                FilterLevel = int.Parse(System.Configuration.ConfigurationManager.AppSettings["logLevel"] ?? "0");
            return (int)level >= FilterLevel;
        }

        public static void DefaultHandler(LogLevel level, string source, string message)
        {
            Console.Write(DateTime.UtcNow.ToString("HH:mm:ss"));
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
            if (this.Filter != null && !this.Filter(level))
                return;
            using (lockObject.Lock)
            {
                Handler?.Invoke(level, sourceFilePath.Split(new char[] { '\\' }).Last().Split(new char[] { '.' }).First() + "." + memberName + ":" + sourceLineNumber, message);
            }
        }

        public void ClearHandlers()
        {
            Handler = null;
        }

        public void Dispose()
        {
            lockObject.Dispose();
        }
    }
}
