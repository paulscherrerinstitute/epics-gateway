using GWLogger.Backend;
using GWLogger.Backend.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Services;

namespace GWLogger
{
    public partial class DataAccess : System.Web.Services.WebService
    {
        [WebMethod]
        public List<string> GetGatewaysList()
        {
            return AnalyzeController.GetGatewaysList();
        }

        [WebMethod]
        public List<KeyValuePair<int, string>> GetMessageTypes()
        {
            return Global.DataContext.MessageTypes.Select(row => new KeyValuePair<int, string>(row.Id, row.Name)).ToList();
        }

        [WebMethod]
        public List<Backend.DTOs.GatewaySession> GetGatewaySessionsList(string gatewayName)
        {
            return AnalyzeController.GetGatewaySessionsList(gatewayName);
        }

        [WebMethod]
        public Backend.DTOs.GatewayStats GetStats(string gatewayName, DateTime start, DateTime end)
        {
            return AnalyzeController.GetStats(gatewayName, start, end);
        }

        [WebMethod]
        public bool CheckQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;
            try
            {
                Backend.DataContext.Query.QueryParser.Parse(query.Trim());
                return true;
            }
            catch
            {
            }
            return false;
        }

        [WebMethod]
        public List<KeyValuePair<string, int>> SearchesPerformed(string gatewayName, DateTime datePoint)
        {
            var searches = Global.DataContext.ReadLog(gatewayName, datePoint, datePoint.AddMinutes(5), "type = \"SearchRequest\"", 20000, null, null, 0, Context.Response.ClientDisconnectedToken);
            if (Context.Response.ClientDisconnectedToken.IsCancellationRequested)
                return null;
            var channelName = Global.DataContext.MessageDetailTypes.First(row => row.Value == "ChannelName").Id;
            var searchesData = searches.Select(row => new { Remote = row.RemoteIpPoint, Channel = row.LogEntryDetails.First(r2 => r2.DetailTypeId == channelName).Value });
            return searchesData.GroupBy(row => row.Remote).Select(row => new KeyValuePair<string, int>(row.Key + " (" + row.Key.Hostname() + ")", row.Count())).OrderByDescending(row => row.Value).ToList();
        }

        [WebMethod]
        public List<KeyValuePair<string, int>> SearchesOnChannelsPerformed(string gatewayName, DateTime datePoint)
        {
            var searches = Global.DataContext.ReadLog(gatewayName, datePoint, datePoint.AddMinutes(5), "type = \"SearchRequest\"", 20000, null, null, 0, Context.Response.ClientDisconnectedToken);
            if (Context.Response.ClientDisconnectedToken.IsCancellationRequested)
                return null;
            var channelName = Global.DataContext.MessageDetailTypes.First(row => row.Value == "ChannelName").Id;
            var searchesData = searches.Select(row => new { Remote = row.RemoteIpPoint, Channel = row.LogEntryDetails.First(r2 => r2.DetailTypeId == channelName).Value });
            return searchesData.GroupBy(row => row.Channel).Select(row => new KeyValuePair<string, int>(row.Key, row.Count())).OrderByDescending(row => row.Value).ToList();
        }

        [WebMethod]
        public List<KeyValuePair<string, int>> MostActiveClasses(string gatewayName, DateTime datePoint)
        {
            // Retrieves up to 20K lines of logs
            var searches = Global.DataContext.ReadLog(gatewayName, datePoint, datePoint.AddMinutes(5), null, 20000, null, null, 0, Context.Response.ClientDisconnectedToken);
            if (Context.Response.ClientDisconnectedToken.IsCancellationRequested)
                return null;

            // Find the different details IDs
            var srcFileId = Global.DataContext.MessageDetailTypes.FirstOrDefault(row => row.Value == "SourceFilePath")?.Id;
            var funcId = Global.DataContext.MessageDetailTypes.FirstOrDefault(row => row.Value == "SourceMemberName")?.Id;
            var lineId = Global.DataContext.MessageDetailTypes.FirstOrDefault(row => row.Value == "SourceLineNumber")?.Id;

            // Find the firsts msg IDs for each class&function
            var msgTypeIds = searches
                .Select(row => new
                {
                    MsgType = row.MessageTypeId,
                    Func = row.LogEntryDetails.FirstOrDefault(r2 => r2.DetailTypeId == srcFileId)?.Value + row.LogEntryDetails.FirstOrDefault(r2 => r2.DetailTypeId == funcId)?.Value,
                    Line = int.Parse(row.LogEntryDetails.FirstOrDefault(r2 => r2.DetailTypeId == lineId)?.Value)
                })
                .GroupBy(row => row.Func)
                .Select(row => row.OrderBy(r2 => r2.Line).Select(r2 => r2.MsgType).First())
                .ToList();

            // Returns the Class.Function & the number of time it was reached. Only the first message of a function will be displayed
            return searches.Where(row => msgTypeIds.Contains(row.MessageTypeId))
                .GroupBy(row => row.LogEntryDetails.FirstOrDefault(r2 => r2.DetailTypeId == srcFileId)?.Value + "@" + row.LogEntryDetails.FirstOrDefault(r2 => r2.DetailTypeId == funcId)?.Value)
                .Select(row => new KeyValuePair<string, int>(row.Key.Split('@')[0].Split('\\').Last().Replace(".cs", "") + "." + row.Key.Split('@')[1].Replace(".ctor", "Constructor"), row.Count()))
                .OrderByDescending(row => row.Value).ToList();
        }

        [WebMethod]
        public List<KeyValuePair<string, int>> ActiveClients(string gatewayName, DateTime datePoint)
        {
            // Retrieves up to 20K lines of logs
            var searches = Global.DataContext.ReadLog(gatewayName, datePoint, datePoint.AddMinutes(5), null, 20000, null, null, 0, Context.Response.ClientDisconnectedToken);
            if (Context.Response.ClientDisconnectedToken.IsCancellationRequested)
                return null;

            // Find the different details IDs
            var srcFileId = Global.DataContext.MessageDetailTypes.FirstOrDefault(row => row.Value == "SourceFilePath")?.Id;
            var funcId = Global.DataContext.MessageDetailTypes.FirstOrDefault(row => row.Value == "SourceMemberName")?.Id;
            var lineId = Global.DataContext.MessageDetailTypes.FirstOrDefault(row => row.Value == "SourceLineNumber")?.Id;

            // Find the firsts msg IDs for each class&function
            var msgTypeIds = searches
                .Select(row => new
                {
                    MsgType = row.MessageTypeId,
                    Func = row.LogEntryDetails.FirstOrDefault(r2 => r2.DetailTypeId == srcFileId)?.Value + row.LogEntryDetails.FirstOrDefault(r2 => r2.DetailTypeId == funcId)?.Value,
                    Line = int.Parse(row.LogEntryDetails.FirstOrDefault(r2 => r2.DetailTypeId == lineId)?.Value)
                })
                .GroupBy(row => row.Func)
                .Select(row => row.OrderBy(r2 => r2.Line).Select(r2 => r2.MsgType).First())
                .ToList();

            return searches
                .Where(row => msgTypeIds.Contains(row.MessageTypeId) && row.LogEntryDetails.FirstOrDefault(r2 => r2.DetailTypeId == funcId)?.Value == "DoRequest" && !(row.LogEntryDetails.FirstOrDefault(r2 => r2.DetailTypeId == srcFileId)?.Value.Contains("Echo") ?? false))
                .GroupBy(row => row.RemoteIpPoint.Hostname())
                .Select(row => new KeyValuePair<string, int>(row.Key, row.Count()))
                .OrderByDescending(row => row.Value)
                .ToList();
        }

        [WebMethod]
        public List<KeyValuePair<string, int>> ActiveServers(string gatewayName, DateTime datePoint)
        {
            // Retrieves up to 20K lines of logs
            var searches = Global.DataContext.ReadLog(gatewayName, datePoint, datePoint.AddMinutes(5), null, 20000, null, null, 0, Context.Response.ClientDisconnectedToken);
            if (Context.Response.ClientDisconnectedToken.IsCancellationRequested)
                return null;

            // Find the different details IDs
            var srcFileId = Global.DataContext.MessageDetailTypes.FirstOrDefault(row => row.Value == "SourceFilePath")?.Id;
            var funcId = Global.DataContext.MessageDetailTypes.FirstOrDefault(row => row.Value == "SourceMemberName")?.Id;
            var lineId = Global.DataContext.MessageDetailTypes.FirstOrDefault(row => row.Value == "SourceLineNumber")?.Id;

            // Find the firsts msg IDs for each class&function
            var msgTypeIds = searches
                .Select(row => new
                {
                    MsgType = row.MessageTypeId,
                    Func = row.LogEntryDetails.FirstOrDefault(r2 => r2.DetailTypeId == srcFileId)?.Value + row.LogEntryDetails.FirstOrDefault(r2 => r2.DetailTypeId == funcId)?.Value,
                    Line = int.Parse(row.LogEntryDetails.FirstOrDefault(r2 => r2.DetailTypeId == lineId)?.Value)
                })
                .GroupBy(row => row.Func)
                .Select(row => row.OrderBy(r2 => r2.Line).Select(r2 => r2.MsgType).First())
                .ToList();

            return searches
                .Where(row => msgTypeIds.Contains(row.MessageTypeId) && row.LogEntryDetails.FirstOrDefault(r2 => r2.DetailTypeId == funcId)?.Value == "DoResponse" && !(row.LogEntryDetails.FirstOrDefault(r2 => r2.DetailTypeId == srcFileId)?.Value.Contains("Echo") ?? false))
                .GroupBy(row => row.RemoteIpPoint.Hostname())
                .Select(row => new KeyValuePair<string, int>(row.Key, row.Count()))
                .OrderByDescending(row => row.Value)
                .ToList();
        }

        [WebMethod]
        public List<Backend.DTOs.DataFileStats> GetDataFileStats()
        {
            return Global.DataContext.GetDataFileStats();
        }

        [WebMethod]
        public List<KeyValuePair<string, string>> GetGatewayNetworks(string gatewayName)
        {
            var partInfo = Global.Inventory.GetPartBySystem(gatewayName);
            var attributes = Global.Inventory.GetPartAttributes(partInfo.PSILabel);
            string[] toPick;
            if (attributes.First(row => row.Name == "Directions").Value == "BIDIRECTIONAL")
                toPick = new string[] { "Local Address Side A", "Remote Address Side A", "Local Address Side B", "Remote Address Side B" };
            else
                toPick = new string[] { "Local Address Side A", "Remote Address Side B" };
            return attributes.Where(row => toPick.Contains(row.Name)).Select(row => new KeyValuePair<string, string>(row.Name, row.Value)).ToList();
        }

        [WebMethod]
        public string EpicsCheck(string gatewayName, string config, string channel)
        {
            return Global.DirectCommands.SendEpicsCommand(config, channel, gatewayName.ToUpper());
        }

        [WebMethod]
        public List<HostChannelCount> BadClientConfig(string gatewayName)
        {
            // Take the last 10 min slot
            var datePoint = DateTime.UtcNow.Trim();

            var lastEntries = Global.DataContext.ReadLastLogs(gatewayName, 100);
            // No data, let's try to get the last entries instead
            if (lastEntries.Last().EntryDate < datePoint)
                datePoint = lastEntries.Last().EntryDate.Trim();

            var searchData = new List<KeyValuePair<string, string>>();
            for (var i = 0; i < 3; i++)
            {
                if (Context.Response.ClientDisconnectedToken.IsCancellationRequested)
                    return null;
                List<Backend.DataContext.LogEntry> searches = null;
                try
                {
                    searches = Global.DataContext.ReadLog(gatewayName, datePoint, datePoint.AddMinutes(5), "type = \"SearchRequestTooNew\"", 10000, null, null, 0, Context.Response.ClientDisconnectedToken);
                }
                catch
                {
                }
                if (searches == null || searches.Count == 0)
                    continue;

                var channelName = Global.DataContext.MessageDetailTypes.First(row => row.Value == "ChannelName").Id;
                searchData.AddRange(searches
                    .Select(row => new KeyValuePair<string, string>
                    (
                        row.RemoteIpPoint.Hostname(),
                        row.LogEntryDetails.First(r2 => r2.DetailTypeId == channelName).Value
                    )));

                // scroll back in time
                datePoint = datePoint.AddMinutes(-10);
            }

            // Extract the data
            var result = searchData.GroupBy(row => row.Key + ";" + row.Value)
                .Select(row => new HostChannelCount
                {
                    Hostname = row.First().Key,
                    Channel = row.First().Value,
                    Count = row.Count()
                }).ToList();
            // Find how many faults per hosts
            var d = result.GroupBy(row => row.Hostname).ToDictionary(key => key.Key, val => val.Sum(r2 => r2.Count));
            // Order list by faults, then by host,then by channel count, then by channel name
            result.Sort((a, b) =>
            {
                if (d[a.Hostname] > d[b.Hostname])
                    return -1;
                if (d[a.Hostname] < d[b.Hostname])
                    return 1;
                var h = string.Compare(a.Hostname, b.Hostname, true);
                if (h != 0)
                    return h;
                if (a.Count != b.Count)
                    return b.Count - a.Count;
                return string.Compare(a.Channel, b.Channel);
            });
            return result;
        }

        [WebMethod]
        public List<Backend.DTOs.GatewayStats> GetAllStats(string gatewayName)
        {
            return Global.DataContext.GetStats(gatewayName);
        }
    }

    public class HostChannelCount
    {
        public string Hostname { get; set; }
        public string Channel { get; set; }
        public int Count { get; set; }
    }
}