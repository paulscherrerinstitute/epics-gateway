using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    class LogFileWriter : IDisposable
    {
        Thread flusher;
        bool shouldStop = false;
        MemoryStream buffer = new MemoryStream();
        string path = null;
        Regex sourceFilter = null;
        StreamWriter bufferWriter;
        public static LogFileWriter CreateIfNeeded(Gateway gateway)
        {
            if (System.Configuration.ConfigurationManager.AppSettings["FileLogger"]?.ToLower() == "true")
                return new LogFileWriter(gateway);
            return null;
        }

        private LogFileWriter(Gateway gateway)
        {
            bufferWriter = new StreamWriter(buffer);
            gateway.Log.Handler += this.LogHandler;

            flusher = new Thread(FlushLog);
            path = System.Configuration.ConfigurationManager.AppSettings["FileLoggerPath"] ?? "C:\\TEMP\\Gateway.log";
            if (!string.IsNullOrWhiteSpace(System.Configuration.ConfigurationManager.AppSettings["FileLoggerClassFilter"]))
                sourceFilter = new Regex(System.Configuration.ConfigurationManager.AppSettings["FileLoggerClassFilter"]);
            flusher.Start();
        }

        private void FlushLog()
        {
            while (!shouldStop)
            {
                Thread.Sleep(1000);
                byte[] bytes;
                lock (buffer)
                {
                    bufferWriter.Flush();

                    bytes = buffer.ToArray();
                    buffer.Position = 0;
                    buffer.SetLength(0);                    
                }

                if (bytes.Length > 0)
                {
                    try
                    {
                        using (var stream = new FileStream(path, FileMode.Append))
                        {
                            stream.Write(bytes, 0, bytes.Length);
                        }
                    }
                    catch
                    {
                    }
                }
            }

            bufferWriter.Dispose();
            buffer.Dispose();
        }

        public void LogHandler(LogLevel level, string source, string message)
        {
            if (sourceFilter != null && !sourceFilter.IsMatch(source))
                return;
            lock (buffer)
            {
                bufferWriter.Write(DateTime.UtcNow.ToString("yyyy\\/MM\\/dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture));
                bufferWriter.Write("\t");
                bufferWriter.Write(source);
                bufferWriter.Write("\t");
                bufferWriter.WriteLine(message);
            }
        }

        public void Dispose()
        {
            shouldStop = true;
        }
    }
}
