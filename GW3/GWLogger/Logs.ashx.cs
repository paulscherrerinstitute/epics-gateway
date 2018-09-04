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
            changeLock.EnterWriteLock();
            try
            {
                Convertion = Global.DataContext.MessageTypes.ToDictionary(key => key.Id, val => val.DisplayMask);
                DetailTypes = Global.DataContext.MessageDetailTypes.ToDictionary(key => key.Id, val => val.Value);
            }
            catch (Exception ex)
            {

            }
            finally
            {
                changeLock.ExitWriteLock();
            }
        }

        private static string Convert(string remoteIpPoint, int messageType, IEnumerable<Backend.DataContext.LogEntryDetail> details)
        {
            if (!Convertion.ContainsKey(messageType) || Convertion[messageType] == null)
            {
                var result = new StringBuilder();
                result.Append(messageType.ToString());
                foreach (var i in details)
                {
                    result.Append(",");
                    result.Append(DetailTypes.ContainsKey(i.DetailTypeId) ? DetailTypes[i.DetailTypeId] : i.DetailTypeId.ToString());
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
                        line = Regex.Replace(line, "\\{" + (DetailTypes.ContainsKey(i.DetailTypeId) ? DetailTypes[i.DetailTypeId] : i.DetailTypeId.ToString()) + "\\}", i.Value, RegexOptions.IgnoreCase);
                }
                if (remoteIpPoint != null)
                    line = Regex.Replace(line, "\\{endpoint\\}", remoteIpPoint, RegexOptions.IgnoreCase);
                return line;
            }
        }

        public void ProcessRequest(HttpContext context)
        {
            var logLevels = Global.DataContext.MessageTypes.ToDictionary(key => key.Id, val => val.LogLevel);

            List<int> msgTypes = null;
            if (!string.IsNullOrWhiteSpace(context.Request["levels"]))
            {
                var levelsRequested = context.Request["levels"];
                var levels = levelsRequested.Split(new char[] { ',' }).Select(row => int.Parse(row));
                msgTypes = logLevels.Where(row => levels.Contains(row.Value)).Select(row => row.Key).ToList();
            }
            string query = null;
            if (!string.IsNullOrWhiteSpace(context.Request["query"]))
                query = context.Request["query"];

            //context.Request;
            var path = context.Request.Path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();
            IEnumerable<Backend.DataContext.LogEntry> logs = null;
            //var t = ctx.LogEntries.ToList();
            var gateway = path[0];
            if (path.Length == 1)
            {
                logs = Global.DataContext.ReadLastLogs(gateway);
            }
            else
            {
                var start = long.Parse(path[1]).ToNetDate();
                var startTrim = start.Trim();

                if (path.Length > 2)
                {
                    var end = long.Parse(path[2]).ToNetDate().Trim();
                    logs = Global.DataContext.ReadLog(gateway, start, end, query, 100, msgTypes);
                }
                else
                    logs = Global.DataContext.ReadLog(gateway, start, start.AddMinutes(20), query, 100, msgTypes);
            }

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

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}