﻿using System;
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
        SafeLock bufferLock = new SafeLock();
        MemoryStream buffer = new MemoryStream();
        string path = null;
        Regex sourceFilter = null;
        StreamWriter bufferWriter;
        bool logRotation = false;
        int logKeepDays;
        string lastLogDate = null;
        string currentLogFilename = null;

        public static LogFileWriter CreateIfNeeded(TextLogger logger)
        {
            if (System.Configuration.ConfigurationManager.AppSettings["fileLogger"]?.ToLower() == "true")
                return new LogFileWriter(logger);
            return null;
        }

        private LogFileWriter(TextLogger logger)
        {
            bufferWriter = new StreamWriter(buffer);

            bufferWriter.WriteLine("=============================================================================");
            bufferWriter.WriteLine(" Start loggin at " + DateTime.UtcNow.ToString("yyyy\\/MM\\/dd HH:mm:ss.fff"));
            bufferWriter.WriteLine("=============================================================================");

            logger.Handler += this.LogHandler;

            flusher = new Thread(FlushLog);
            path = System.Configuration.ConfigurationManager.AppSettings["fileLoggerPath"] ?? "C:\\TEMP\\Gateway.log";
            currentLogFilename = path;
            if (!string.IsNullOrWhiteSpace(System.Configuration.ConfigurationManager.AppSettings["fileLoggerClassFilter"]))
                sourceFilter = new Regex(System.Configuration.ConfigurationManager.AppSettings["fileLoggerClassFilter"]);
            logRotation = (System.Configuration.ConfigurationManager.AppSettings["fileLoggerRotation"]?.ToLower() == "true");
            logKeepDays = int.Parse(System.Configuration.ConfigurationManager.AppSettings["fileLoggerKeepDays"] ?? "5");
            //flusher.IsBackground = true;
            flusher.Start();
        }

        private void FlushLog()
        {
            while (!shouldStop)
            {
                Thread.Sleep(1000);
                byte[] bytes;
                using (bufferLock.Aquire())
                {
                    bufferWriter.Flush();
                    bytes = buffer.ToArray();
                    if (bytes.Length == 0)
                        continue;
                    buffer.Position = 0;
                    buffer.SetLength(0);
                }

                // Day changed, we must rotate
                if (logRotation && lastLogDate != DateTime.UtcNow.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture))
                    RotateLogs();

                if (bytes.Length > 0)
                {
                    try
                    {
                        using (var stream = new FileStream(currentLogFilename, FileMode.Append))
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

        private void RotateLogs()
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                var logDate = DateTime.UtcNow.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                var filename = Path.GetFileName(path);

                if (logKeepDays > 0)
                {
                    var iLogDate = DateTime.UtcNow.AddDays(-logKeepDays).ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);

                    var logs = Directory.GetFiles(dir, "*." + filename);
                    foreach (var f in logs.Where(row => Path.GetFileName(row).Substring(0, 8).CompareTo(iLogDate) < 0))
                        File.Delete(f);
                }

                lastLogDate = logDate;
                currentLogFilename = dir + "\\" + lastLogDate + "." + filename;
            }
            catch
            {
            }
        }

        public void LogHandler(LogLevel level, string source, string message)
        {
            if (sourceFilter != null && !sourceFilter.IsMatch(source))
                return;
            using (bufferLock.Aquire())
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
            bufferLock.Dispose();
        }
    }
}
