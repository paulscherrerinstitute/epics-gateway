using GWLoggerSoap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace GatewayLogic.Services
{
    internal class SoapLogger : IDisposable
    {
        private static DataAccessSoapClient soapLogger;
        private bool shouldStop;
        private CancellationTokenSource cancelOperation = new CancellationTokenSource();
        private static Thread reconnecter;
        private BufferBlock<LogMessage> buffer = new BufferBlock<LogMessage>();
        private string gatewayName;
        private Thread bufferFlusher;
        private long nbSaved = 0;
        private long nbTotal = 0;

        static SoapLogger()
        {
            if (System.Configuration.ConfigurationManager.AppSettings["soapLogger"]?.ToLower() != "true")
                return;

            reconnecter = new Thread(() =>
              {
                  while (true)
                  {
                      if (soapLogger != null)
                      {
                          Thread.Sleep(30000);
                          continue;
                      }

                      // Build SOAP connection
                      try
                      {
                          if (System.Configuration.ConfigurationManager.AppSettings["soapURL"] != null)
                              soapLogger = new DataAccessSoapClient(new System.ServiceModel.BasicHttpBinding(), new EndpointAddress(System.Configuration.ConfigurationManager.AppSettings["soapURL"]));
                          else
                              soapLogger = new DataAccessSoapClient(new System.ServiceModel.BasicHttpBinding(), new EndpointAddress("http://caesar.psi.ch/DataAccess.asmx"));
                      }
                      catch
                      {
                          try
                          {
                              soapLogger = new GWLoggerSoap.DataAccessSoapClient(new System.ServiceModel.BasicHttpBinding(), new EndpointAddress("http://epics-gw-logger.psi.ch/DataAccess.asmx"));
                          }
                          catch
                          {
                              soapLogger = null;
                          }
                      }
                      if (soapLogger == null)
                      {
                          Thread.Sleep(30000);
                          continue;
                      }


                      try
                      {
                          // Sends all message types to the server
                          soapLogger.RegisterLogMessageTypeAsync(Enum.GetValues(typeof(LogMessageType))
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
                          soapLogger.RegisterLogMessageDetailTypeAsync(Enum.GetValues(typeof(MessageDetail))
                              .AsQueryable()
                              .OfType<MessageDetail>()
                              .Select(row => new IdValue
                              {
                                  Id = (int)row,
                                  Value = row.ToString()
                              }).ToArray());
                      }
                      catch
                      {
                          soapLogger = null;
                      }

                      Thread.Sleep(30000);
                  }
              });
            reconnecter.IsBackground = true;
            reconnecter.Start();
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
            bufferFlusher.Priority = ThreadPriority.Lowest;
            bufferFlusher.Start();

            /*Thread performanceDisplay = new Thread(() =>
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
            performanceDisplay.Start();*/
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
                // Not connected, we wait
                if (soapLogger == null)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                // Send bunch if there is multiple entries waiting
                using (var mem = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(mem))
                    {

                        var nb = (uint)Math.Max(1, Math.Min(1000, buffer.Count));
                        writer.Write(nb);

                        for (var i = 0; i < nb; i++)
                        {
                            try
                            {
                                message = buffer.Receive(cancelOperation.Token);
                            }
                            catch (OperationCanceledException)
                            {
                                return;
                            }
                            catch
                            {
                                throw;
                            }

                            if (message.RemoteIpPoint == null)
                                writer.Write(new byte[] { 0, 0, 0, 0, 0, 0 });
                            else
                            {
                                try
                                {
                                    writer.Write(System.Net.IPAddress.Parse(message.RemoteIpPoint.Split(':')[0]).GetAddressBytes());
                                    writer.Write(ushort.Parse(message.RemoteIpPoint.Split(':')[1]));
                                }
                                catch
                                {
                                    writer.Write(new byte[] { 0, 0, 0, 0, 0, 0 });
                                }
                            }
                            writer.Write((ushort)message.MessageType);
                            writer.Write((byte)message.Details.Count());
                            foreach (var detail in message.Details)
                            {
                                writer.Write((ushort)detail.TypeId);
                                writer.Write((detail?.Value) ?? "");
                            }
                        }
                        try
                        {
                            soapLogger.BinaryLogEntriesAsync(gatewayName, mem.ToArray()).Wait(10000);

                            //soapLogger.LogEntries(toSend.ToArray());
                        }
                        catch
                        {
                            soapLogger = null;
                        }
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
