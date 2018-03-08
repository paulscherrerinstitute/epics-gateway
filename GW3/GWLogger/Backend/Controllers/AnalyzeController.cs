using GWLogger.Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GWLogger.Backend.Controllers
{
    public static class AnalyzeController
    {
        public static List<string> GetGatewaysList()
        {
            using (var ctx = new LoggerContext())
            {
                return ctx.LogEntries.GroupBy(row => row.Gateway).Select(row => row.Key).ToList();
            }
        }

        public static List<DTOs.GatewaySession> GetGatewaySessionsList(string gatewayName)
        {
            using (var ctx = new LoggerContext())
            {
                var startDates = ctx.LogEntries.Where(row => row.Gateway == gatewayName && row.MessageTypeId == 2).OrderBy(row => row.EntryDate).Select(row => row.EntryDate).ToList();
                var endDates = new List<DateTime>();
                foreach (var i in startDates.Skip(1))
                {
                    endDates.Add(ctx.LogEntries.OrderByDescending(row => row.EntryDate)
                        .Where(row => row.Gateway == gatewayName && row.EntryDate < i
                        && row.MessageTypeId != 0 && row.MessageTypeId != 1).First().EntryDate);
                }
                endDates.Add(ctx.LogEntries.OrderByDescending(row => row.EntryDate).Where(row => row.Gateway == gatewayName).First().EntryDate);

                var result = new List<DTOs.GatewaySession>();
                for (var i = 0; i < endDates.Count; i++)
                    result.Add(new DTOs.GatewaySession { StartDate = startDates[i], EndDate = endDates[i] });
                return result;
            }
        }

        public static List<DTOs.SearchRequest> GetSearchedChannels(string gatewayName, DateTime datePoint)
        {
            using (var ctx = new LoggerContext())
            {
                var start = datePoint.AddSeconds(-1);
                var end = datePoint.AddSeconds(1);

                return ctx.LogEntries.Where(row => row.MessageTypeId == 39 && row.Gateway == gatewayName && row.EntryDate >= start && row.EntryDate <= end)
                //return ctx.LogEntries.Where(row => row.MessageTypeId == 39 && row.Gateway == gatewayName)
                    .OrderBy(row => row.EntryDate)
                    .Select(row => new DTOs.SearchRequest
                    {
                        Channel = row.LogEntryDetails.Where(r2 => r2.DetailTypeId == 7).FirstOrDefault().Value,
                        Date = row.EntryDate,
                        Client = row.RemoteIpPoint
                    }).ToList();
            }
        }
    }
}