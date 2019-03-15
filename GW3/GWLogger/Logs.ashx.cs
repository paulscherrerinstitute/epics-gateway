using GWLogger.Backend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        private Dictionary<int, string> Convertion;
        private Dictionary<int, string> DetailTypes;
        private object changeLock = new object();

        internal void RefreshLookup()
        {
            Convertion = Global.DataContext.MessageTypes.ToDictionary(key => key.Id, val => val.DisplayMask);
            DetailTypes = Global.DataContext.MessageDetailTypes.ToDictionary(key => key.Id, val => val.Value);
        }

        private string Convert(string remoteIpPoint, int messageType, IEnumerable<Backend.DataContext.LogEntryDetail> details)
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
            lock (changeLock)
            {
                RefreshLookup();

                var cancel = context.Response.ClientDisconnectedToken;
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

                long offset = 0;
                string startFile = null;
                if (!string.IsNullOrWhiteSpace(context.Request["offset"]))
                    offset = long.Parse(context.Request["offset"]);
                if (!string.IsNullOrWhiteSpace(context.Request["filename"]))
                    startFile = context.Request["filename"];

                //context.Request;
                var path = context.Request.Path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();
                IEnumerable<Backend.DataContext.LogEntry> logs = null;
                //var t = ctx.LogEntries.ToList();
                var gateway = path[0];
                if (path.Length == 1 || path[1] == "NaN")
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
                        if (context.Request["levels"] == "3,4") // Show errors
                            logs = Global.DataContext.GetLogs(gateway, start, end, query, null, true).Take(100).ToList();
                        else
                            logs = Global.DataContext.ReadLog(gateway, start, end, query, 100, msgTypes, startFile, offset, cancel);
                    }
                    else
                        if (context.Request["levels"] == "3,4") // Show errors
                        logs = Global.DataContext.GetLogs(gateway, start, start.AddMinutes(20), query, null, true).Take(100).ToList();
                    else
                        logs = Global.DataContext.ReadLog(gateway, start, start.AddMinutes(20), query, 100, msgTypes, startFile, offset, cancel);
                }

                foreach (var l in logs)
                    l.RemoteIpPoint = Hostname(l.RemoteIpPoint) + " (" + l.RemoteIpPoint + ")";

                context.Response.ContentType = "application/json";
                context.Response.CacheControl = "no-cache";
                context.Response.Expires = 0;
                context.Response.Write("[");

                if (logs == null)
                {
                    context.Response.Write("]");
                    return;
                }

                var messageDetails = Global.DataContext.MessageDetailTypes.ToDictionary(key => key.Id, val => val.Value);

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

                    context.Response.Write("\"Remote\":\"");
                    context.Response.Write(i.RemoteIpPoint?.JsEscape());
                    context.Response.Write("\",");

                    context.Response.Write("\"Type\":");
                    context.Response.Write(i.MessageTypeId);
                    context.Response.Write(",");

                    context.Response.Write("\"Level\":");
                    context.Response.Write(logLevels.ContainsKey(i.MessageTypeId) ? logLevels[i.MessageTypeId] : 0);
                    context.Response.Write(",");

                    context.Response.Write("\"Message\":\"");
                    context.Response.Write(Convert(i.RemoteIpPoint, i.MessageTypeId, i.LogEntryDetails).JsEscape());
                    context.Response.Write("\",");

                    context.Response.Write("\"Position\":");
                    context.Response.Write(i.Position);
                    context.Response.Write(",");

                    context.Response.Write("\"CurrentFile\":\"");
                    context.Response.Write(i.CurrentFile);
                    context.Response.Write("\",");

                    context.Response.Write("\"Details\":{");
                    context.Response.Write(string.Join(",", i.LogEntryDetails.Select(row => "\"" + (messageDetails.ContainsKey(row.DetailTypeId) ? messageDetails[row.DetailTypeId] : row.DetailTypeId.ToString()) + "\":\"" + row.Value.JsEscape() + "\"")));
                    context.Response.Write("}");

                    context.Response.Write("}");
                }
            }
            context.Response.Write("]");
        }

        private Dictionary<string, string> knownIps = new Dictionary<string, string>();
        private string Hostname(string remoteIpPoint)
        {
            var ip = remoteIpPoint.Split(new char[] { ':' }).First();
            lock (knownIps)
            {
                if (!knownIps.ContainsKey(ip))
                {
                    string host = ip;
                    try
                    {
                        host = Dns.GetHostEntry(ip).HostName;
                    }
                    catch
                    {
                    }
                    knownIps.Add(ip, host);
                }
                return knownIps[ip];
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