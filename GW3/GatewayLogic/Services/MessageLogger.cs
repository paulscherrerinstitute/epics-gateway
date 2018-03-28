using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    class MessageLogger : IDisposable
    {
        private SoapLogger soapLogger;
        public LogMessageConverter MessageConverter { get; private set; }

        public MessageLogger(string gatewayName)
        {
            try
            {
                soapLogger = SoapLogger.CreateIfNeeded(gatewayName);
                if (soapLogger != null)
                    MessageHandler += soapLogger.MessageLogger_MessageHandler;
            }
            catch
            {
                Console.WriteLine("!!! SOAP Message logger not available even if requested");
                soapLogger = null;
            }
            this.MessageConverter = new LogMessageConverter(this);
        }

        public delegate void MessageEvent(string remoteIpPoint,
                            LogMessageType messageType,
                            IEnumerable<LogMessageDetail> details,
                            string memberName,
                            string sourceFilePath,
                            int sourceLineNumber = 0);

        public event MessageEvent MessageHandler;

        public void Write(string remoteIpPoint,
                            LogMessageType messageType,
                            IEnumerable<LogMessageDetail> details = null,
                            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            MessageHandler?.Invoke(remoteIpPoint, messageType, details, memberName, sourceFilePath, sourceLineNumber);
        }

        public void Dispose()
        {
            this.soapLogger?.Dispose();
            this.soapLogger = null;
            this.MessageConverter?.Dispose();
            this.MessageConverter = null;
        }
    }
}
