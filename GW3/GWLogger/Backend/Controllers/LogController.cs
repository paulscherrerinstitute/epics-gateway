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
    }
}