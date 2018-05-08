using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;

namespace GWLogger.Backend.Controllers
{
    public static class LogController
    {
        internal static void CleanLogs()
        {
#warning must implement the cleaning
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
#warning must implement storage of message types
        }

        public static void RegisterLogMessageDetailType(List<DTOs.IdValue> types)
        {
#warning must implement storage of details types
        }
    }
}