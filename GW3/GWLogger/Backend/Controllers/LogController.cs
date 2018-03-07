using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GWLogger.Backend.Controllers
{
    public static class LogController
    {
        public static void LogEntry(string gateway, string remoteIpPoint, int messageType, List<DTOs.LogEntryDetail> details)
        {
            using (var ctx = new Model.LoggerContext())
            {
                var newEntry = new Model.LogEntry
                {
                    EntryDate = DateTime.UtcNow,
                    Gateway = gateway,
                    MessageTypeId = messageType
                };
                details
                    .Select(row => new Model.LogEntryDetail
                    {
                        DetailTypeId = row.TypeId,
                        Value = row.Value
                    })
                    .ToList()
                    .ForEach(row => newEntry.LogEntryDetails.Add(row));

                ctx.LogEntries.Add(newEntry);
                ctx.SaveChanges();
            }
        }

        public static void RegisterLogMessageType(List<DTOs.MessageType> types)
        {
            using (var ctx = new Model.LoggerContext())
            {
                var dbTypes = ctx.LogMessageTypes.ToList();
                var toAdd = types.Where(row => !dbTypes.Any(r2 => r2.MessageTypeId == row.Id));
                ctx.LogMessageTypes.AddRange(toAdd.Select(row => new Model.LogMessageType { MessageTypeId = row.Id, Name = row.Name, DisplayMask = row.DisplayMask }));
                ctx.SaveChanges();
            }
        }

        public static void RegisterLogMessageDetailType(List<DTOs.IdValue> types)
        {
            using (var ctx = new Model.LoggerContext())
            {
                var dbTypes = ctx.LogDetailItemTypes.ToList();
                var toAdd = types.Where(row => !dbTypes.Any(r2 => r2.ItemId == row.Id));
                ctx.LogDetailItemTypes.AddRange(toAdd.Select(row => new Model.LogDetailItemType { ItemId = row.Id, Name = row.Value }));
                ctx.SaveChanges();
            }
        }
    }
}