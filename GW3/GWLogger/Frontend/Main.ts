﻿/// <reference path="query.ts" />

// Main class...
class Main
{
    static BaseTitle: string;
    static CurrentGateway: string;
    static Sessions: GatewaySession[];
    static Stats: GatewayStats;
    static StartDate: Date = null;
    static EndDate: Date = null;
    static CurrentTime: Date;
    static Levels: string;
    static IsLast: boolean = true;
    static SearchTimeout: number = null;
    static Logs: LogEntry[] = null;
    static KeepOpenDetails: boolean = false;
    static Offset: number = null;
    static OffsetFile: string = null;
    static MessageTypes: string[] = [];
    static CurrentTab: number = 0;

    static loadingLogs: JQueryXHR;


    static LoadGateways(): void
    {
        $.ajax({
            type: 'POST',
            url: 'DataAccess.asmx/GetGatewaysList',
            data: JSON.stringify({}),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                var gateways: string[] = msg.d;
                var options = "<option>" + gateways.join("</option><option>") + "</option>";
                if (Main.CurrentGateway)
                {
                    $('#gatewaySelector').find('option').remove().end().append(options).val(Main.CurrentGateway);
                    Main.GatewaySelected();
                }
                else
                {
                    options = "<option value=''>-- Select a gateway --</option>" + options;
                    $('#gatewaySelector').find('option').remove().end().append(options).val("");
                    //Main.GatewayChanged();
                }
            },
            error: function (msg, textStatus)
            {
                console.log(msg.responseText);
            }
        });
    }

    static GatewayChanged(): void
    {
        Main.CurrentGateway = $('#gatewaySelector').val();
        State.Set();

        Main.GatewaySelected();
    }

    static GatewaySelected(): void
    {
        Main.LoadSessions();
        Main.LoadLogStats();
    }

    static LoadLogStats(refresh: boolean = false): void
    {
        var resetEndDate = refresh && ((Main.CurrentTime && Main.Stats && Main.Stats.Logs && Main.Stats.Logs.length > 0 && Main.CurrentTime == Main.Stats.Logs[Main.Stats.Logs.length - 1].Date) || !(Main.CurrentTime && Main.Stats))
        if (Main.StartDate === null)
            Main.StartDate = new Date((new Date()).getTime() - (24 * 3600 * 1000));

        if (refresh === true && (((new Date()).getTime() - Main.StartDate.getTime()) > (25 * 3600 * 1000)))
            return;

        //var end = new Date();
        var end = new Date(Main.StartDate.getTime() + 24 * 3600 * 1000);
        $.ajax({
            type: 'POST',
            url: 'DataAccess.asmx/GetStats',
            data: JSON.stringify({
                "gatewayName": Main.CurrentGateway,
                start: Utils.FullUtcDateFormat(Main.StartDate),
                end: Utils.FullUtcDateFormat(end)
            }),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                Main.Stats = GatewayStats.CreateFromObject(msg.d);

                if (Main.EndDate === null)
                {
                    if (Main.Stats.Logs && Main.Stats.Logs.length > 0)
                        Main.EndDate = Main.Stats.Logs[Main.Stats.Logs.length - 1].Date;
                    else
                        Main.EndDate = new Date();
                    $("#startDate").val(Utils.FullUtcDateFormat(Main.StartDate));
                    $("#endDate").val(Utils.FullUtcDateFormat(Main.EndDate));
                }

                if (resetEndDate)
                {
                    if (Main.Stats.Logs && Main.Stats.Logs.length > 0)
                        Main.EndDate = Main.CurrentTime = Main.Stats.Logs[Main.Stats.Logs.length - 1].Date;
                    else
                        Main.EndDate = Main.CurrentTime = new Date();
                }
                MainGraph.DrawStats();

                if (refresh && Main.IsLast)
                    Main.LoadTimeInfo(refresh);
                else if (!refresh)
                    Main.LoadTimeInfo();
            },
            error: function (msg, textStatus)
            {
                console.log(msg.responseText);
            }
        });
    }

    static LoadSessions(): void
    {
        $.ajax({
            type: 'POST',
            url: 'DataAccess.asmx/GetGatewaySessionsList',
            data: JSON.stringify({ "gatewayName": Main.CurrentGateway }),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                Main.Sessions = (msg.d ? (<object[]>msg.d).map(function (c) { return GatewaySession.CreateFromObject(c); }) : []);
                var html = "";
                html += "<table>";
                html += "<thead><tr><td>Start</td><td>End</td><td>NB&nbsp;Logs</td></tr></thead>";
                html += "<tbody>";
                for (var i = 0; i < Main.Sessions.length; i++)
                {
                    html += "<tr><td>" + Utils.GWDateFormat(Main.Sessions[i].StartDate) + "</td>";
                    html += "<td>" + Utils.GWDateFormat(Main.Sessions[i].EndDate) + "</td>";
                    html += "<td>" + Main.Sessions[i].NbEntries + "</td></tr>";
                }
                html += "</tbody></table>";
                $("#gatewaySessions").html(html);
            },
            error: function (msg, textStatus)
            {
                console.log(msg.responseText);
            }
        });
    }

    static ShowStats(showClientConnections: boolean = true)
    {
        if (showClientConnections)
        {
            $("#clientsTabs li").removeClass("activeTab");
            $("#clientsTabs li:nth-child(1)").addClass("activeTab");

            var prefs = Utils.Preferences;
            prefs['showSearches'] = false
            Utils.Preferences = prefs;
        }

        //var startDate = new Date(Main.CurrentTime.getTime() - 10 * 60 * 1000);
        var startDate = (Main.CurrentTime ? new Date(Main.CurrentTime.getTime()) : new Date(Main.EndDate.getTime() - 20 * 60 * 1000));
        var endDate = new Date(startDate.getTime() + 20 * 60 * 1000);

        $.ajax({
            type: 'POST',
            url: 'DataAccess.asmx/GetConnectionsBetween',
            data: JSON.stringify({
                "gatewayName": Main.CurrentGateway,
                "start": Utils.FullDateFormat(startDate),
                "end": Utils.FullDateFormat(endDate)
            }),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                var connections = Connections.CreateFromObject(msg.d);
                if (showClientConnections)
                {
                    var html = "";
                    html += "<table>";
                    html += "<thead><tr><td>Client</td><td>Start</td><td>End</td></tr></thead>";
                    html + "<tbody>";
                    for (var i = 0; i < connections.Clients.length; i++)
                    {
                        html += "<tr>";
                        html += "<td>" + connections.Clients[i].RemoteIpPoint + "</td>";
                        html += "<td>" + Utils.FullDateFormat(connections.Clients[i].Start) + "</td>";
                        html += "<td>" + (connections.Clients[i].End ? Utils.FullDateFormat(connections.Clients[i].End) : "&nbsp;") + "</td>";
                        html + "</tr>";
                    }
                    html += "</tbody></table>";
                    $("#clientsContent").html(html);
                }

                html = "<table>";
                html += "<thead><tr><td>Server</td><td>Start</td><td>End</td></tr></thead>";
                html + "<tbody>";
                for (var i = 0; i < connections.Servers.length; i++)
                {
                    html += "<tr>";
                    html += "<td>" + connections.Servers[i].RemoteIpPoint + "</td>";
                    html += "<td>" + Utils.FullDateFormat(connections.Servers[i].Start) + "</td>";
                    html += "<td>" + (connections.Servers[i].End ? Utils.FullDateFormat(connections.Servers[i].End) : "&nbsp;") + "</td>";
                    html + "</tr>";
                }
                html += "</table>";
                html += "</tbody></table>";
                $("#serversContent").html(html);
            },
            error: function (msg, textStatus)
            {
                console.log(msg.responseText);
            }
        });
    }

    static LoadTimeInfo(refresh: boolean = false)
    {
        MainGraph.DrawStats();
        var startDate = (Main.CurrentTime ? new Date(Main.CurrentTime.getTime()) : new Date(Main.EndDate.getTime() - 20 * 60 * 1000));
        var endDate = new Date(startDate.getTime() + 20 * 60 * 1000);

        if ($("#closeHelp").css("display") == "none")
        {
            $("#help").hide();
            $("#clients, #servers, #logs").show();
        }

        if (Utils.Preferences['showSearches'] === true)
        {
            Main.ShowSearches();
            Main.ShowStats(false);
        }
        else
            Main.ShowStats();

        if (refresh && Main.loadingLogs)
            return;
        else if (Main.loadingLogs)
            Main.loadingLogs.abort();

        if (!refresh)
            $("#logsContent").html("Loading...");

        var tab = $("#logFilter li")[Main.CurrentTab];
        if (tab.getAttribute("report") && refresh == true)
        {
            return;
        }
        else if (tab.getAttribute("report"))
        {
            Main.loadingLogs = $.ajax({
                type: 'POST',
                url: '/DataAccess.asmx/' + tab.getAttribute("report"),
                data: JSON.stringify({ "gatewayName": Main.CurrentGateway, "datePoint": Utils.FullDateFormat(Main.CurrentTime == null ? Main.StartDate : Main.CurrentTime) }),
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                success: function (msg)
                {
                    Main.loadingLogs = null;
                    var data = <KeyValuePair[]>msg.d;
                    var html = "";
                    html += "<table>";
                    //html += "<thead><tr><td>Channel</td><td>Client</td><td>NB</td><td>Date</td></tr></thead>";
                    html + "<tbody>";
                    for (var i = 0; i < data.length; i++)
                    {
                        html += "<tr>";
                        html += "<td>" + data[i].Key + "</td>";
                        html += "<td>" + data[i].Value + "</td>";
                        html + "</tr>";
                    }
                    html += "</tbody></table>";
                    $("#logsContent").html(html);
                },
                error: function (msg, textStatus)
                {
                    console.log(msg.responseText);
                }
            });
            return;
        }
        Main.Levels = tab.getAttribute("level");

        var queryString = "";
        if (Main.Levels)
            queryString += (queryString != "" ? "&" : "?") + "levels=" + Main.Levels;
        if ($("#queryField").val())
            queryString += (queryString != "" ? "&" : "?") + "query=" + $("#queryField").val();
        if (Main.Offset !== null)
            queryString += (queryString != "" ? "&" : "?") + "offset=" + this.Offset;
        if (Main.OffsetFile !== null)
            queryString += (queryString != "" ? "&" : "?") + "filename=" + this.OffsetFile;
        //(Main.Levels ? "?levels=" + Main.Levels + "&query=" + $("#queryField").val() : "?query=" + $("#queryField").val()

        Main.loadingLogs = $.ajax({
            type: 'GET',
            url: '/Logs/' + Main.CurrentGateway + '/' + startDate.getTime() + '/' + endDate.getTime() + queryString,
            success: function (data)
            {
                Main.loadingLogs = null;
                var logs: LogEntry[] = data;
                Main.Logs = logs;

                var html = "";
                html += "<table>";
                html += "<thead><tr><td>Date</td><td>Type</td><td>Level</td><td>Message</td></tr></thead>";
                html + "<tbody>";
                for (var i = 0; i < logs.length; i++)
                {
                    html += "<tr rowId=\"" + i + "\">";
                    html += "<td>" + Utils.GWDateFormatMilis(new Date(logs[i].Date)) + "</td>";
                    html += "<td>" + logs[i].Type + "</td>";
                    html += "<td>" + logs[i].Level + "</td>";
                    html += "<td>" + logs[i].Message + "</td>";
                    html + "</tr>";
                }

                html += "<tr rowId=\"-1\"><td class='pseudoLink'>Next entries</td></tr>";
                html += "</tbody></table>";
                $("#logsContent").html(html);

                // Scroll to bottom
                if (Main.IsLast)
                    $("#logsContent").scrollTop($("#logsContent")[0].scrollHeight - $("#logsContent").height());
                else
                    $("#logsContent").scrollTop(0);

                $("#logsContent tbody > tr").on("mouseover", (evt) =>
                {
                    if (Main.KeepOpenDetails)
                        return;
                    var rowId = parseInt(evt.target.parentElement.attributes["rowId"].value);
                    if (rowId == -1)
                        return;
                    var html = Main.DetailInfo(rowId);
                    $("#detailInfo").html(html).show();
                }).on("mouseout", () =>
                {
                    if (Main.KeepOpenDetails)
                        return;
                    $("#detailInfo").hide();
                }).on("click", (evt) =>
                {
                    var rowId = parseInt(evt.target.parentElement.attributes["rowId"].value);
                    if (rowId == -1)
                    {
                        Main.Offset = Main.Logs[Main.Logs.length - 1].Position;
                        Main.OffsetFile = Main.Logs[Main.Logs.length - 1].CurrentFile;
                        Main.LoadTimeInfo();
                        return;
                    }
                    var html = Main.DetailInfo(rowId);
                    if (Main.KeepOpenDetails)
                    {
                        Main.KeepOpenDetails = false;
                        $("#detailInfo").html(html);
                        return;
                    }
                    Main.KeepOpenDetails = true;
                    html += "<div class='closeDetails' onclick='Main.CloseDetails()'></div>";
                    $("#detailInfo").html(html).show();
                });
            },
            error: function (msg, textStatus)
            {
                if (msg.statusText == "abort")
                    return;
                Main.loadingLogs = null;
                console.log(msg.responseText);
            }
        });

        if (!refresh)
        {
            $("#logsContent").html("Loading...");
            $("#serversContent").html("Loading...");
            $("#clientsContent").html("Loading...");
        }
    }

    static DetailInfo(rowId: number): string
    {
        var html = "";
        html += "<table>";
        html += "<tr><td>Message&nbsp;Type</td><td><span class='pseudoLink' onclick='Main.AddFilter(event,\"type\");'>" + Main.MessageTypes[Main.Logs[0].Type] + "</span></td></tr>";
        html += "<tr><td>Date</td><td><span class='pseudoLink' onclick='Main.AddFilter(event,\"date\");'>" + Utils.GWDateFormatMilis(new Date(Main.Logs[rowId].Date)) + "</span></td>";
        html += "<td>Remote</td><td><span class='pseudoLink' onclick='Main.AddFilter(event,\"remote\");'>" + Main.Logs[rowId].Remote + "</span></td></tr>";
        var n = 0;
        for (var i in Main.Logs[rowId].Details)
        {
            if (n == 0)
                html += "<tr>";
            else if (n % 2 == 0)
                html += "</tr><tr>";
            html += "<td>" + i.replace(/(\w)([A-Z][a-z])/g, "$1&nbsp;$2") + "</td><td><span class='pseudoLink' onclick='Main.AddFilter(event,\"" + i + "\");'>" + Main.Logs[rowId].Details[i] + "</span></td>";
            n++;
        }
        html += "</tr></table>";
        return html;
    }

    static AddFilter(evt: MouseEvent, prop: string)
    {
        var value = (<HTMLElement>evt.target).innerText;
        if ($("#queryField").val().trim() != "")
            $("#queryField").val($("#queryField").val() + " and ");
        if (prop == "date")
            $("#queryField").val($("#queryField").val() + prop + " >= \"" + value + "\"").trigger("keyup");
        else
            $("#queryField").val($("#queryField").val() + prop + " = \"" + value + "\"").trigger("keyup");
        $("#queryField").blur();
    }

    static CloseDetails()
    {
        Main.KeepOpenDetails = false;
        $("#detailInfo").hide();
    }

    static Refresh()
    {
        var now = new Date();
        $("#utcTime").html(("" + now.getUTCHours()).padLeft("0", 2) + ":" + ("" + now.getUTCMinutes()).padLeft("0", 2) + ":" + ("" + now.getUTCSeconds()).padLeft("0", 2));

        $.ajax({
            type: 'POST',
            url: 'DataAccess.asmx/GetFreeSpace',
            data: JSON.stringify({}),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                var free = <FreeSpace>msg.d;
                $("#freeSpace").html("" + (Math.round(free.FreeMB * 1000 / free.TotMB) / 10) + "%");
            }
        });

        $.ajax({
            type: 'POST',
            url: 'DataAccess.asmx/GetBufferUsage',
            data: JSON.stringify({}),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                $("#bufferSpace").html("" + msg.d + "%");
            }
        });

        if (Main.CurrentGateway)
        {
            Main.LoadSessions();
            Main.LoadLogStats(true);
        }
    }

    static Resize(): void
    {
        MainGraph.DrawStats();
    }

    static ShowSearches()
    {
        var current = Main.CurrentTime.getTime();
        var startDate = new Date(current);

        var prefs = Utils.Preferences;
        prefs['showSearches'] = true
        Utils.Preferences = prefs;

        $("#clientsTabs li").removeClass("activeTab");
        $("#clientsTabs li:nth-child(2)").addClass("activeTab");

        $.ajax({
            type: 'POST',
            url: '/DataAccess.asmx/GetSearchedChannels',
            data: JSON.stringify({ "gatewayName": Main.CurrentGateway, "datePoint": Utils.FullDateFormat(startDate) }),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                var data: SearchRequest[];
                if (msg.d)
                    data = (<object[]>msg.d).map(c => SearchRequest.CreateFromObject(c));
                else
                    data = [];
                var html = "";
                html += "<table>";
                html += "<thead><tr><td>Channel</td><td>Client</td><td>NB</td><td>Date</td></tr></thead>";
                html + "<tbody>";
                for (var i = 0; i < data.length; i++)
                {
                    html += "<tr>";
                    html += "<td>" + data[i].Channel + "</td>";
                    html += "<td>" + data[i].Client + "</td>";
                    html += "<td>" + data[i].NbSearches + "</td>";
                    html += "<td>" + Utils.FullDateFormat(data[i].Date) + "</td>";
                    html + "</tr>";
                }
                html += "</tbody></table>";
                $("#clientsContent").html(html);
            },
            error: function (msg, textStatus)
            {
                console.log(msg.responseText);
            }
        });
    }

    static ShowHelp()
    {
        $("#help").show();
        $("#clients, #servers, #logs").hide();
        $("#closeHelp").show();
    }

    static HideHelp()
    {
        $("#help").hide();
        $("#clients, #servers, #logs").show();
        $("#closeHelp").hide();
    }

    static LogFilter(evt: JQueryEventObject)
    {
        $("#logFilter li").removeClass("activeTab");
        $(evt.target).addClass("activeTab");

        var tabs = $("#logFilter li");
        for (var i = 0; i < tabs.length; i++)
        {
            if (tabs[i] == evt.target)
            {
                Main.CurrentTab = i;
                console.log("Tab " + i);
                break;
            }
        }

        State.Set();
        Main.Levels = evt.target.getAttribute("level");
        Main.LoadTimeInfo();
    }

    static Init(): void
    {
        $.ajax({
            type: 'POST',
            url: '/DataAccess.asmx/GetMessageTypes',
            data: JSON.stringify({}),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                var vals = <KeyValuePair[]>msg.d;
                for (var i = 0; i < vals.length; i++)
                    Main.MessageTypes[vals[i].Key] = vals[i].Value;
            }
        });

        Main.BaseTitle = window.document.title;
        $("#gatewaySelector").on("change", Main.GatewayChanged);
        $(window).on("resize", Main.Resize);
        $("#timeRangeCanvas").on("mousedown", MainGraph.TimeLineSelected);
        window.setInterval(Main.Refresh, 1000);
        $(window).bind('popstate', State.Pop);
        $("#clientsTabs li:nth-child(1)").click(Main.ShowStats);
        $("#clientsTabs li:nth-child(2)").click(Main.ShowSearches);
        $("#prevDay").click(() =>
        {
            Main.Offset = null;
            Main.OffsetFile = null;
            Main.StartDate = new Date(Main.StartDate.getTime() - 24 * 3600 * 1000);
            Main.EndDate = new Date(Main.StartDate.getTime() + 24 * 3600 * 1000);
            $("#startDate").val(Utils.FullUtcDateFormat(Main.StartDate));
            $("#endDate").val(Utils.FullUtcDateFormat(Main.EndDate));
            Main.DelayedSearch(Main.LoadLogStats);
        });
        $("#nextDay").click(() =>
        {
            Main.Offset = null;
            Main.OffsetFile = null;
            Main.StartDate = new Date(Main.StartDate.getTime() + 24 * 3600 * 1000);
            Main.EndDate = new Date(Main.StartDate.getTime() + 24 * 3600 * 1000);
            $("#startDate").val(Utils.FullUtcDateFormat(Main.StartDate));
            $("#endDate").val(Utils.FullUtcDateFormat(Main.EndDate));
            Main.DelayedSearch(Main.LoadLogStats);
        });
        $("#startDate").on("keyup", () =>
        {
            try
            {
                var dt = Utils.ParseDate($("#startDate").val());
                if (isNaN(dt.getTime()) || dt.getFullYear() == 1970)
                    return;
                Main.StartDate = dt;
                Main.EndDate = new Date(Main.StartDate.getTime() + 24 * 3600 * 1000);
                $("#endDate").val(Utils.FullUtcDateFormat(Main.EndDate));
                Main.Offset = null;
                Main.OffsetFile = null;
                Main.DelayedSearch(Main.LoadLogStats);
            }
            catch (ex)
            {
            }
        });
        $("#endDate").on("keyup", () =>
        {
            try
            {
                var dt = Utils.ParseDate($("#endDate").val());
                if (isNaN(dt.getTime()) || dt.getFullYear() == 1970)
                    return;
                Main.EndDate = dt;
                Main.StartDate = new Date(Main.EndDate.getTime() - 24 * 3600 * 1000);
                $("#startDate").val(Utils.FullUtcDateFormat(Main.StartDate));
                Main.Offset = null;
                Main.OffsetFile = null;
                Main.DelayedSearch(Main.LoadLogStats);
            }
            catch (ex)
            {
            }
        });
        $("#queryField").on("focus", () =>
        {
            $("#querySuggestions").show();
            Main.ShowSuggestion();
        });
        $("#queryField").on("blur", () =>
        {
            $("#querySuggestions").hide();
        });
        $("#queryField").on("keyup", (evt: JQueryEventObject) =>
        {
            switch (evt.keyCode)
            {
                case 27:
                    $("#queryField").blur();
                    break;
                case 13:
                    $("#querySuggestions").hide();
                    break;
                default:
                    $("#querySuggestions").show();
                    Main.ShowSuggestion();
                    break;
            }

            $.ajax({
                type: 'POST',
                url: '/DataAccess.asmx/CheckQuery',
                data: JSON.stringify({ "query": $("#queryField").val() }),
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                success: function (msg)
                {
                    if (msg.d === false)
                        $("#queryField").css("color", "red");
                    else
                        $("#queryField").css("color", "black");
                }
            });
            Main.Offset = null;
            Main.OffsetFile = null;
            Main.DelayedSearch(Main.LoadTimeInfo);
        });

        $("#logFilter li").click(Main.LogFilter);

        $("#logHelp").click(Main.ShowHelp);
        $("#closeHelp").click(Main.HideHelp);

        State.Pop(null);
    }

    static ShowSuggestion()
    {
        var html = "";
        html += "<h1>Queries:</h1>";
        html += "&lt;variable&gt; &lt;condition&gt; &lt;value&gt; [and/or &lt;...&gt;]";

        html += "<h1>Variables:</h1><table>";
        var n = 0;
        for (var i in availableVariables)
        {
            if (n % 2 == 0 && n != 0)
                html += "</tr><tr>";
            else if (n % 2 == 0)
                html += "<tr>";
            html += "<td>" + i + "</td><td>" + availableVariables[i] + "</td>";
            n++;
        }
        html += "</tr></table>";

        html += "<h1>Conditions:</h1><table>";
        n = 0;
        for (var i in availableConditions)
        {
            if (n % 2 == 0 && n != 0)
                html += "</tr><tr>";
            else if (n % 2 == 0)
                html += "<tr>";
            html += "<td>" + i + "</td><td>" + availableConditions[i] + "</td>";
            n++;
        }
        html += "</tr></table>";

        $("#querySuggestions").html(html);
    }

    static DelayedSearch(cb, fromPop: boolean = false)
    {
        if (Main.SearchTimeout !== null)
            clearTimeout(Main.SearchTimeout);
        Main.SearchTimeout = setTimeout(() =>
        {
            if (!fromPop)
                State.Set();
            Main.SearchTimeout = null;
            Main.IsLast = false;
            if (cb)
                cb();
        }, 500);
    }
}

$(Main.Init); // Starting Main GUI tasks