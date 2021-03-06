﻿using System;
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

    public class Log
    {
        public delegate void LogHandler(LogLevel level, string source, string message);
        public delegate bool LogFilter(LogLevel level);

        public event LogHandler Handler = Log.DefaultHandler;
        public LogFilter Filter = Log.ShowAll;
        private object lockObject = new object();

        private static bool ShowAll(LogLevel level)
        {
            return true;
        }

        private static void DefaultHandler(LogLevel level, string source, string message)
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
            lock (lockObject)
            {
                Handler?.Invoke(level, sourceFilePath.Split(new char[] { '\\' }).Last().Split(new char[] { '.' }).First() + "." + memberName + ":" + sourceLineNumber, message);
            }
        }

        public void ClearHandlers()
        {
            Handler = null;
        }
    }
}
