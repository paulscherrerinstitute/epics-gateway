﻿using GWLogger.Backend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
                        line = Regex.Replace(line, "\\{" + (DetailTypes.ContainsKey(i.DetailTypeId) ? DetailTypes[i.DetailTypeId] : i.DetailTypeId.ToString()) + "\\}", i.Value ?? "", RegexOptions.IgnoreCase);
                }
                if (remoteIpPoint != null)
                    line = Regex.Replace(line, "\\{endpoint\\}", remoteIpPoint, RegexOptions.IgnoreCase);
                return line;
            }
        }

        public void ProcessRequest(HttpContext context)
        {
            var continueMode = (context.Request["continue"] == "true");
            Backend.DataContext.LogPosition lastPosition = new Backend.DataContext.LogPosition();
            lock (changeLock)
            {
                RefreshLookup();

                var timeoutCancellation = new CancellationTokenSource();
                timeoutCancellation.CancelAfter(1000);
                var cancel = CancellationTokenSource.CreateLinkedTokenSource(context.Response.ClientDisconnectedToken, timeoutCancellation.Token);

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
                Backend.DataContext.Query.Statement.SelectNode queryNode = null;
                try
                {
                    queryNode = (Backend.DataContext.Query.Statement.SelectNode)Backend.DataContext.Query.QueryParser.Parse(query);
                }
                catch
                {
                }

                long offset = 0;
                string startFile = null;
                if (!string.IsNullOrWhiteSpace(context.Request["offset"]))
                    offset = long.Parse(context.Request["offset"]);
                if (!string.IsNullOrWhiteSpace(context.Request["filename"]))
                    startFile = context.Request["filename"];

                //context.Request;
                var path = context.Request.Path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();
                List<object> logs = null;
                //var t = ctx.LogEntries.ToList();
                var gateway = path[0];
                if (path.Length == 1 || path[1] == "NaN")
                {
                    logs = Global.DataContext.ReadLastLogs(gateway).Cast<object>().ToList();
                }
                else
                {
                    var start = long.Parse(path[1]).ToNetDate();
                    var startTrim = start.Trim();

                    var limit = (queryNode != null && queryNode.Group != null) ? 100000 : 100;
                    IEnumerable<object> raw = null;

                    if (path.Length > 2)
                    {
                        var end = long.Parse(path[2]).ToNetDate().Trim();
                        if (context.Request["levels"] == "3,4") // Show errors
                            raw = Global.DataContext.GetLogs(gateway, start, end, query, null, true, cancel.Token).Take(100);
                        else
                            raw = Global.DataContext.ReadLog(gateway, start, end, query, limit, msgTypes, startFile, offset, cancel.Token, lastPosition).Take(100);
                    }
                    else
                    {
                        if (context.Request["levels"] == "3,4") // Show errors
                            raw = Global.DataContext.GetLogs(gateway, start, start.AddMinutes(20), query, null, true, cancel.Token);
                        else
                            raw = Global.DataContext.ReadLog(gateway, start, start.AddMinutes(20), query, limit, msgTypes, startFile, offset, cancel.Token, lastPosition);
                    }
                    if (raw == null)
                        logs = new List<object>();
                    else
                        logs = raw.Where(row => row != null).Cast<object>().ToList();
                }

                var elementType = (logs.Count == 0 ? null : logs.First().GetType());

                context.Response.ContentType = "application/json";
                context.Response.CacheControl = "no-cache";
                context.Response.Expires = 0;
                if (continueMode)
                    context.Response.Write("{\"rows\":");
                context.Response.Write("[");

                if (logs == null || logs.Count == 0)
                {
                    context.Response.Write("]");
                    if (continueMode)
                    {
                        if (lastPosition.LogFile != null) // If we didn't reach the end yet
                            context.Response.Write(",\"lastPosition\":{\"file\":\"" + lastPosition.LogFile + "\",\"position\":" + lastPosition.Offset + "}");
                        context.Response.Write("}");
                    }
                    return;
                }

                var messageDetails = Global.DataContext.MessageDetailTypes.ToDictionary(key => key.Id, val => val.Value);

                if (elementType == typeof(Backend.DataContext.LogEntry))
                {
                    var entryLogs = logs.Cast<Backend.DataContext.LogEntry>();
                    var isFirst = true;
                    foreach (var i in entryLogs)
                    {
                        if (!isFirst)
                            context.Response.Write(",");
                        if (i.RemoteIpPoint != null)
                            i.RemoteIpPoint = Hostname(i.RemoteIpPoint) + " (" + i.RemoteIpPoint + ")";
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
                else
                {
                    var isFirst = true;

                    foreach (var i in logs.Cast<object[]>())
                    {
                        if (!isFirst)
                            context.Response.Write(",");
                        isFirst = false;
                        context.Response.Write("{");
                        for (var j = 0; j < i.Length; j++)
                        {
                            if (j != 0)
                                context.Response.Write(",");
                            context.Response.Write("\"" + (queryNode.Columns[j].DisplayTitle.Replace(" ", "_") ?? queryNode.Columns[j].Field.Name) + "\"");
                            context.Response.Write(":");
                            if (queryNode.Columns[j].Field.Name == "remote" && i[j] != null)
                                context.Response.Write((Hostname((string)i[j]) + " (" + i[j] + ")").ToJs());
                            else
                                context.Response.Write(i[j].ToJs());
                        }
                        context.Response.Write("}");
                    }
                }
            }
            context.Response.Write("]");
            if (continueMode)
            {
                if (lastPosition.LogFile != null) // If we didn't reach the end yet
                    context.Response.Write(",\"lastPosition\":{\"file\":\"" + lastPosition.LogFile + "\",\"position\":" + lastPosition.Offset + "}");
                context.Response.Write("}");
            }
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