using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using GWLogger.Backend.DTOs;

namespace GWLogger.Backend.Controllers
{
    public static class LogController
    {
        static Thread cleanupThread;
        public static int NBDaysToKeep => int.Parse(System.Configuration.ConfigurationManager.AppSettings["storageKeepDays"] ?? "5");

        static LogController()
        {
            // Every hours check if we changed day, if yes call the data cleanup
            cleanupThread = new Thread((obj) =>
              {
                  var lastCleanup = DateTime.UtcNow.ToShortDateString();
                  while (true)
                  {
                      Thread.Sleep(TimeSpan.FromHours(1));
                      if (DateTime.UtcNow.ToShortDateString() != lastCleanup)
                      {
                          lastCleanup = DateTime.UtcNow.ToShortDateString();
                          CleanLogs();
                      }
                  }
              });
            cleanupThread.IsBackground = true;
            cleanupThread.Start();
        }

        internal static void CleanLogs()
        {
            Global.DataContext.CleanOlderThan(NBDaysToKeep);
        }

        public static void LogEntry(string gateway, string remoteIpPoint, int messageType, List<DTOs.LogEntryDetail> details)
        {
            Global.DataContext.Save(new DataContext.LogEntry
            {
                Gateway = gateway,
                RemoteIpPoint = remoteIpPoint,
                MessageTypeId = messageType,
                EntryDate = DateTime.UtcNow,
                LogEntryDetails = details.Select(row => new DataContext.LogEntryDetail
                {
                    DetailTypeId = row.TypeId,
                    Value = row.Value
                }).ToList()
            });
        }

        public static void RegisterLogMessageType(List<DTOs.MessageType> types)
        {
            Global.DataContext.MessageTypes = types;
        }

        public static void RegisterLogMessageDetailType(List<DTOs.IdValue> types)
        {
            Global.DataContext.MessageDetailTypes = types;
        }

        public static void UpdateLastGatewaySessionInformation(string gateway, RestartType restartType, string comment)
        {
            Global.DataContext.UpdateLastGatewaySessionInformation(gateway, restartType, comment);
        }
    }
}