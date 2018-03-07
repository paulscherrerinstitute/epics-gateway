using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using GatewayLogic.GWLoggerSoap;

namespace GatewayLogic.Services
{
    class SoapLogger : IDisposable
    {
        private static DataAccessSoapClient soapLogger;
        private bool shouldStop;
        private CancellationTokenSource cancelOperation = new CancellationTokenSource();

        BufferBlock<LogMessage> buffer = new BufferBlock<LogMessage>();
        private Gateway gateway;
        private Thread bufferFlusher;

        static SoapLogger()
        {
            try
            {
                soapLogger = new GWLoggerSoap.DataAccessSoapClient();
            }
            catch
            {
                soapLogger = new GWLoggerSoap.DataAccessSoapClient(new System.ServiceModel.BasicHttpBinding(), new EndpointAddress("http://epics-gw-logger.psi.ch/DataAccess.asmx"));
            }

            soapLogger.RegisterLogMessageType(Enum.GetValues(typeof(LogMessageType))
                .AsQueryable()
                .OfType<LogMessageType>()                
                .Select(row => new MessageType
                {
                    Id = (int)row,
                    Name = row.ToString(),
                    DisplayMask = ((MessageDisplayAttribute)(typeof(LogMessageType).GetMember(row.ToString())[0].GetCustomAttributes(typeof(MessageDisplayAttribute), false)).FirstOrDefault()).LogDisplay
                }).ToArray());

            soapLogger.RegisterLogMessageDetailType(Enum.GetValues(typeof(MessageDetail))
                .AsQueryable()
                .OfType<MessageDetail>()
                .Select(row => new IdValue
                {
                    Id = (int)row,
                    Value = row.ToString()
                }).ToArray());
        }


        public static SoapLogger CreateIfNeeded(Gateway gateway)
        {
            if (System.Configuration.ConfigurationManager.AppSettings["soapLogger"]?.ToLower() == "true")
                return new SoapLogger(gateway);
            return null;
        }

        private SoapLogger(Gateway gateway)
        {
            gateway.MessageLogger.MessageHandler += MessageLogger_MessageHandler;
            this.gateway = gateway;
            bufferFlusher = new Thread(FlushLogAsync);
            bufferFlusher.Start();
        }

        private void MessageLogger_MessageHandler(string remoteIpPoint,
            LogMessageType messageType,
            IEnumerable<LogMessageDetail> details,
            string memberName,
            string sourceFilePath,
            int sourceLineNumber = 0)
        {
            var fullDetails = details == null ? new List<LogMessageDetail>() : new List<LogMessageDetail>(details);
            fullDetails.Add(new LogMessageDetail { TypeId = MessageDetail.SourceMemberName, Value = memberName });
            fullDetails.Add(new LogMessageDetail { TypeId = MessageDetail.SourceFilePath, Value = sourceFilePath });
            fullDetails.Add(new LogMessageDetail { TypeId = MessageDetail.SourceLineNumber, Value = sourceLineNumber.ToString() });

            if (buffer.Count < 100)
                buffer.Post(new LogMessage
                {
                    Details = fullDetails,
                    Gateway = gateway.Configuration.GatewayName,
                    MessageType = (int)messageType,
                    RemoteIpPoint = remoteIpPoint
                });
        }

        private void FlushLogAsync()
        {
            while (!shouldStop)
            {
                LogMessage message;
                try
                {
                    message = buffer.Receive(cancelOperation.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    throw;
                }

                try
                {
                    soapLogger.LogEntry(message.Gateway, message.RemoteIpPoint,
                        message.MessageType, message.Details.Select(row => new LogEntryDetail
                        {
                            TypeId = (int)row.TypeId,
                            Value = row.Value
                        }).ToArray());
                }
                catch
                {
                }
            }
        }

        public void Dispose()
        {
            shouldStop = true;
            cancelOperation.Cancel();
        }
    }
}
