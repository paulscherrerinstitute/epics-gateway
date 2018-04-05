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
        private string gatewayName;
        private Thread bufferFlusher;
        long nbSaved = 0;
        long nbTotal = 0;

        static SoapLogger()
        {
            if (System.Configuration.ConfigurationManager.AppSettings["soapLogger"]?.ToLower() != "true")
                return;

            // Build SOAP connection
            try
            {
                soapLogger = new GWLoggerSoap.DataAccessSoapClient();
            }
            catch
            {
                soapLogger = new GWLoggerSoap.DataAccessSoapClient(new System.ServiceModel.BasicHttpBinding(), new EndpointAddress("http://epics-gw-logger.psi.ch/DataAccess.asmx"));
            }

            // Sends all message types to the server
            soapLogger.RegisterLogMessageType(Enum.GetValues(typeof(LogMessageType))
                .AsQueryable()
                .OfType<LogMessageType>()
                .Select(row => new MessageType
                {
                    Id = (int)row,
                    Name = row.ToString(),
                    DisplayMask = ((MessageDisplayAttribute)(typeof(LogMessageType).GetMember(row.ToString())[0].GetCustomAttributes(typeof(MessageDisplayAttribute), false)).FirstOrDefault()).LogDisplay,
                    LogLevel = (int)((MessageDisplayAttribute)(typeof(LogMessageType).GetMember(row.ToString())[0].GetCustomAttributes(typeof(MessageDisplayAttribute), false)).FirstOrDefault()).LogLevel
                }).ToArray());

            // Sends all message type details to the server
            soapLogger.RegisterLogMessageDetailType(Enum.GetValues(typeof(MessageDetail))
                .AsQueryable()
                .OfType<MessageDetail>()
                .Select(row => new IdValue
                {
                    Id = (int)row,
                    Value = row.ToString()
                }).ToArray());
        }


        public static SoapLogger CreateIfNeeded(string gatewayName)
        {
            if (System.Configuration.ConfigurationManager.AppSettings["soapLogger"]?.ToLower() == "true")
                return new SoapLogger(gatewayName);
            return null;
        }

        private SoapLogger(string gatewayName)
        {
            this.gatewayName = gatewayName;

            bufferFlusher = new Thread(FlushLogAsync);
            //bufferFlusher.IsBackground = true;
            bufferFlusher.Start();

            Thread performanceDisplay = new Thread(() =>
              {
                  while (true)
                  {
                      Thread.Sleep(1000);
                      if (nbTotal > 0)
                      {
                          Console.Write("Messages saved: " + (nbSaved * 100L / nbTotal) + " %              \r");
                          nbTotal = 0;
                          nbSaved = 0;
                      }
                      else
                      {
                          Console.Write("Messages saved: 100 %              \r");
                      }
                  }
              });
            performanceDisplay.IsBackground = true;
            performanceDisplay.Start();
        }

        public void MessageLogger_MessageHandler(string remoteIpPoint,
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

            nbTotal++;
            if (buffer.Count < 8000)
            {
                nbSaved++;
                buffer.Post(new LogMessage
                {
                    Details = fullDetails,
                    Gateway = gatewayName,
                    MessageType = (int)messageType,
                    RemoteIpPoint = remoteIpPoint
                });
            }
        }

        private void FlushLogAsync()
        {
            LogMessage message = null;

            while (!shouldStop)
            {
                // Send bunch if there is multiple entries waiting
                if (buffer.Count > 1)
                {
                    var toSend = new List<LogEntry>();
                    while (toSend.Count < 100 && buffer.Count > 0)
                    {
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

                        toSend.Add(new LogEntry
                        {
                            Gateway = message.Gateway,
                            Details = message.Details.Select(row => new LogEntryDetail
                            {
                                TypeId = (int)row.TypeId,
                                Value = row.Value
                            }).ToArray(),
                            MessageType = message.MessageType,
                            RemoteIpPoint = message.RemoteIpPoint
                        });
                    }

                    try
                    {
                        soapLogger.LogEntries(toSend.ToArray());
                    }
                    catch
                    {
                    }
                }
                else
                {

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
        }

        public void Dispose()
        {
            shouldStop = true;
            cancelOperation.Cancel();
        }
    }
}
