using GWLogger.Backend;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

        static ReaderWriterLockSlim changeLock = new ReaderWriterLockSlim();

        static Logs()
        {
            RefreshLookup();
        }

        static internal void RefreshLookup()
        {
            using (var ctx = new LoggerContext())
            {
                changeLock.EnterWriteLock();
                try
                {
                    Convertion = ctx.LogMessageTypes.ToDictionary(key => key.MessageTypeId, val => val.DisplayMask);
                    DetailTypes = ctx.LogDetailItemTypes.ToDictionary(key => key.ItemId, val => val.Name);
                }
                finally
                {
                    changeLock.ExitWriteLock();
                }
            }
        }

        private static string Convert(string remoteIpPoint, int messageType, IEnumerable<Backend.DTOs.LogEntryDetail> details)
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
                var logLevels = ctx.LogMessageTypes.ToDictionary(key => key.MessageTypeId, val => val.LogLevel);

                //context.Request;
                var path = context.Request.Path.Split(new char[] { '/' }).Skip(2).ToArray();
                IQueryable<LogEntry> logs = ctx.LogEntries;
                //var t = ctx.LogEntries.ToList();
                var gateway = path[0];
                logs = logs.Where(row => row.Gateway == gateway);
                if (path.Length == 1)
                {
                    logs = logs.OrderByDescending(row => row.EntryId);
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

                    logs = logs.OrderByDescending(row => row.EntryId);
                }

                var levelsRequested = context.Request["levels"];
                if (!string.IsNullOrWhiteSpace(levelsRequested))
                {
                    var levels = levelsRequested.Split(new char[] { ',' }).Select(row => int.Parse(row));
                    var msgTypes = logLevels.Where(row => levels.Contains(row.Value)).Select(row => row.Key).ToList();
                    logs = logs.Where(row => msgTypes.Contains(row.MessageTypeId));
                }
                //logs.Include(row => row.LogMessageType);

                logs = logs.Take(100).OrderBy(row => row.EntryId);

                context.Response.ContentType = "application/json";
                context.Response.CacheControl = "no-cache";
                context.Response.Expires = 0;
                context.Response.Write("[");

                changeLock.EnterReadLock();
                try
                {
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
                        context.Response.Write(i.MessageTypeId);
                        context.Response.Write(",");

                        context.Response.Write("\"Level\":");
                        context.Response.Write(logLevels.ContainsKey(i.MessageTypeId) ? logLevels[i.MessageTypeId] : 0);
                        context.Response.Write(",");

                        context.Response.Write("\"Message\":\"");
                        context.Response.Write(Convert(i.RemoteIpPoint, i.MessageTypeId, i.LogEntryDetails).JsEscape());
                        context.Response.Write("\"");

                        context.Response.Write("}");
                    }
                }
                finally
                {
                    changeLock.ExitReadLock();
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