using GWLogger.Backend.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;

namespace GWLogger.Backend.Controllers
{
    public static class LogController
    {
        static DataContext.DataContext context = new DataContext.DataContext();

        internal static void CleanLogs()
        {
#warning must implement the cleaning
        }

        public static void LogEntry(string gateway, string remoteIpPoint, int messageType, List<DTOs.LogEntryDetail> details)
        {
            context.Save(new DataContext.LogEntry
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
        }

        public static void RegisterLogMessageDetailType(List<DTOs.IdValue> types)
        {
        }
    }
}