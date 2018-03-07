using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    internal class MessageDisplayAttribute : Attribute
    {
        public string LogDisplay { get; }
        public LogLevel LogLevel { get; }

        internal MessageDisplayAttribute(string logDisplay, LogLevel logLevel)
        {
            this.LogDisplay = logDisplay;
            this.LogLevel = logLevel;
        }
    }

    class LogMessageConverter : IDisposable
    {
        public static Dictionary<LogMessageType, string> Convertion { get; private set; }

        public TextLogger TextLogger { get; private set; }

        public LogFileWriter LogFileWriter { get; }

        static LogMessageConverter()
        {
            Convertion = Enum.GetValues(typeof(LogMessageType))
                .AsQueryable()
                .OfType<LogMessageType>()
                .ToDictionary(key => key, val => ((MessageDisplayAttribute)(typeof(LogMessageType).GetMember(val.ToString())[0].GetCustomAttributes(typeof(MessageDisplayAttribute), false)).FirstOrDefault())?.LogDisplay);
        }

        internal LogMessageConverter(MessageLogger messageLogger)
        {
            messageLogger.MessageHandler += MessageLogger_MessageHandler;
            this.TextLogger = new TextLogger();
            this.LogFileWriter = LogFileWriter.CreateIfNeeded(this.TextLogger);
        }

        private void MessageLogger_MessageHandler(string remoteIpPoint,
            LogMessageType messageType,
            IEnumerable<LogMessageDetail> details,
            string memberName,
            string sourceFilePath,
            int sourceLineNumber = 0)
        {
            if (Convertion[messageType] == null)
            {
                var result = new StringBuilder();
                result.Append(messageType.ToString());
                foreach (var i in details)
                {
                    result.Append(",");
                    result.Append(i.TypeId.ToString());
                    result.Append("=");
                    result.Append(i.Value);
                }

                this.TextLogger.Write(LogLevel.Detail, result.ToString(), memberName, sourceFilePath, sourceLineNumber);
            }
            else
            {
                var line = Convertion[messageType];
                foreach (var i in details)
                    line = Regex.Replace(line, "\\{" + i.TypeId.ToString() + "\\}", i.Value, RegexOptions.IgnoreCase);
                this.TextLogger.Write(LogLevel.Detail, line, memberName, sourceFilePath, sourceLineNumber);
            }
        }

        public void Dispose()
        {
            LogFileWriter?.Dispose();
        }
    }
}
