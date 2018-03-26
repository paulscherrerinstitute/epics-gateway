using GWLogger.Backend;
using GWLogger.Backend.Model;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace GWLogger
{
    /// <summary>
    /// Summary description for Logs
    /// </summary>
    public class Logs : IHttpHandler
    {
        static Dictionary<int, string> Convertion;
        static Dictionary<int, string> DetailTypes;

        static Logs()
        {
            using (var ctx = new LoggerContext())
            {
                Convertion = ctx.LogMessageTypes.ToDictionary(key => key.MessageTypeId, val => val.DisplayMask);
                DetailTypes = ctx.LogDetailItemTypes.ToDictionary(key => key.ItemId, val => val.Name);
            }
        }

        private static string Convert(string remoteIpPoint, int messageType, IEnumerable<LogEntryDetail> details)
        {
            if (Convertion[messageType] == null)
            {
                var result = new StringBuilder();
                result.Append(messageType.ToString());
                foreach (var i in details)
                {
                    result.Append(",");
                    result.Append(i.LogDetailItemType.Name.ToString());
                    result.Append("=");
                    result.Append(i.Value);
                }

                return result.ToString();
            }
            else
            {
                var line = Convertion[messageType];
                var typesId = DetailTypes.Keys.ToList();
                if (details != null)
                {
                    foreach (var i in details.Where(row => typesId.Contains(row.DetailTypeId)))
                        line = Regex.Replace(line, "\\{" + DetailTypes[i.DetailTypeId] + "\\}", i.Value, RegexOptions.IgnoreCase);
                }
                if (remoteIpPoint != null)
                    line = Regex.Replace(line, "\\{endpoint\\}", remoteIpPoint, RegexOptions.IgnoreCase);
                return line;
            }
        }

        public void ProcessRequest(HttpContext context)
        {
            using (var ctx = new LoggerContext())
            {
                //context.Request;
                var path = context.Request.Path.Split(new char[] { '/' }).Skip(2).ToArray();
                IQueryable<LogEntry> logs = ctx.LogEntries;
                //var t = ctx.LogEntries.ToList();
                var gateway = path[0];
                logs = logs.Where(row => row.Gateway == gateway);
                if (path.Length == 1)
                {
                    logs = logs.OrderByDescending(row => row.EntryId).Take(100).OrderBy(row=>row.EntryId);
                }
                else
                {
                    var start = long.Parse(path[1]).ToNetDate();
                    var startTrim = start.Trim();
                    logs = logs.Where(row => row.TrimmedDate >= startTrim);
                    logs = logs.Where(row => row.EntryDate >= start);

                    if (path.Length > 2)
                    {
                        var end = long.Parse(path[2]).ToNetDate();
                        var endTrim = end.Trim();
                        logs = logs.Where(row => row.TrimmedDate <= endTrim);
                        logs = logs.Where(row => row.EntryDate <= end);
                    }

                    logs = logs.OrderBy(row=>row.EntryId).Take(100);
                }
                //logs.Include(row => row.LogMessageType);

                context.Response.ContentType = "application/json";
                context.Response.CacheControl = "no-cache";
                context.Response.Expires = 0;
                context.Response.Write("[");

                var isFirst = true;
                foreach (var i in logs)
                {
                    if (!isFirst)
                        context.Response.Write(",");
                    isFirst = false;
                    context.Response.Write("{");

                    context.Response.Write("\"Date\":");
                    context.Response.Write(i.EntryDate.ToUniversalTime().ToJsDate());
                    context.Response.Write(",");

                    context.Response.Write("\"Type\":");
                    context.Response.Write(i.MessageTypeId.ToString());
                    context.Response.Write(",");

                    context.Response.Write("\"Level\":");
                    context.Response.Write((i.LogMessageType?.LogLevel) ?? 0);
                    context.Response.Write(",");

                    context.Response.Write("\"Message\":\"");
                    context.Response.Write(Convert(i.RemoteIpPoint, i.MessageTypeId, i.LogEntryDetails).JsEscape());
                    context.Response.Write("\"");

                    context.Response.Write("}");
                }
                context.Response.Write("]");
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}