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
        private Thread[] bufferFlusher = new Thread[5];
        private Thread performanceDisplay;
        long nbSaved = 0;
        long nbTotal = 0;

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

            for (var i = 0; i < bufferFlusher.Length; i++)
            {
                bufferFlusher[i] = new Thread(FlushLogAsync);
                bufferFlusher[i].Start();
            }

            performanceDisplay = new Thread(() =>
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

            nbTotal++;
            if (buffer.Count < 1000)
            {
                nbSaved++;
                buffer.Post(new LogMessage
                {
                    Details = fullDetails,
                    Gateway = gateway.Configuration.GatewayName,
                    MessageType = (int)messageType,
                    RemoteIpPoint = remoteIpPoint
                });
            }
        }

        private void FlushLogAsync()
        {
            DataAccessSoapClient logger;
            try
            {
                logger = new GWLoggerSoap.DataAccessSoapClient();
            }
            catch
            {
                logger = new GWLoggerSoap.DataAccessSoapClient(new System.ServiceModel.BasicHttpBinding(), new EndpointAddress("http://epics-gw-logger.psi.ch/DataAccess.asmx"));
            }

            LogMessage message = null;

            while (!shouldStop)
            {
                /*// Send bunch
                if (buffer.Count > 100)
                {
                    var toSend = new List<LogEntry>();
                    while (toSend.Count < 30)
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
                        logger.LogEntries(toSend.ToArray());
                    }
                    catch
                    {
                    }
                }
                else
                {*/

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
                        logger.LogEntry(message.Gateway, message.RemoteIpPoint,
                            message.MessageType, message.Details.Select(row => new LogEntryDetail
                            {
                                TypeId = (int)row.TypeId,
                                Value = row.Value
                            }).ToArray());
                    }
                    catch(Exception ex)
                    {
                    }
                }
            //}
        }

        public void Dispose()
        {
            shouldStop = true;
            cancelOperation.Cancel();
        }
    }
}
