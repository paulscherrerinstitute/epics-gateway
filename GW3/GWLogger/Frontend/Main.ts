﻿var availableVariables = {
    "class": "Source class, path & function call",
    "line": "Source line number",
    "channel": "EPICS Channel Name",
    "sid": "Server ID (IOC ID)",
    "cid": "Client ID (EPICS client)",
    "gwid": "Gateway ID",
    "remote": "Remote IP & port (either client or IOC)",
    "cmd": "Command id",
    "ip": "3rd party IP (&lt;&gt; remote)",
    "exception": "Exception string",
    "datacount": "Channel data count",
    "gatewaymonitorid": "Gateway Monitor Id",
    "clientioid": "Client I/O ID",
    "version": "Channel Access protocol's version",
    "origin": "Origin",
    "type": "Log message type"
};

var availableConditions = {
    "!=": "Not equal",
    "=": "Equal",
    "&gt;": "Bigger than",
    "&lt;": "Smaller than",
    "&gt;=": "Bigger or equal than",
    "&lt;=": "Smaller or equal than",
    "contains": "Contains",
    "starts": "Starts with",
    "ends": "Ends with"
};

var availableOperators = [{ "and": "and" }, { "or": "or" }];

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

    static loadingLogs: JQueryXHR;

    static Path(): string[]
    {
        return document.location.pathname.split('/');
    }

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
        Main.SetState();

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
                Main.DrawStats();

                if (Main.IsLast && Main.CurrentTime)
                    Main.LoadTimeInfo(refresh);
            },
            error: function (msg, textStatus)
            {
                console.log(msg.responseText);
            }
        });
    }

    static DrawStats(): void
    {
        if (!Main.Stats || !Main.EndDate)
            return;

        var end = new Date(Math.floor((Main.EndDate.getTime() + Main.EndDate.getTimezoneOffset() * 60000) / (10 * 60 * 1000)) * 10 * 60 * 1000);

        var canvas = (<HTMLCanvasElement>$("#timeRangeCanvas")[0]);
        var ctx = canvas.getContext("2d");
        var width = $("#timeRange").width() - 40;
        var height = $("#timeRange").height() - 18;

        canvas.width = width;
        canvas.height = height;

        var w = width / 145;
        var b = height * 2 / 3;
        var bn = height - b;

        ctx.fillStyle = "rgba(255,255,255,0)";
        ctx.fillRect(0, 0, width, height);


        var maxValue = 1;
        for (var i = 0; i < Main.Stats.Logs.length; i++)
            maxValue = Math.max(maxValue, Main.Stats.Logs[i].Value);
        var maxEValue = 1;
        for (var i = 0; i < Main.Stats.Errors.length; i++)
            maxEValue = Math.max(maxEValue, Main.Stats.Errors[i].Value); var maxNValue = 1;
        for (var i = 0; i < Main.Stats.Searches.length; i++)
            maxNValue = Math.max(maxNValue, Main.Stats.Searches[i].Value);

        var prevErrorValY: number = null;

        for (var i = 0; i < 145; i++)
        {
            var dt = new Date(end.getTime() - i * 10 * 60 * 1000);

            var val = Main.GetStat('Logs', dt);
            ctx.fillStyle = "#80d680";
            ctx.strokeStyle = "#528c52";
            var bv = b * val / maxValue;
            ctx.fillRect(Math.round(width - (i + 1) * w), Math.round(b - bv), Math.ceil(w), Math.round(bv));
            ctx.beginPath();
            ctx.moveTo(Math.round(width - (i + 1) * w) + 1.5, Math.round(b - bv) + 0.5);
            ctx.lineTo(Math.round(width - i * w) + 0.5, Math.round(b - bv) + 0.5);
            ctx.lineTo(Math.round(width - i * w) + 0.5, Math.round(b) + 0.5);
            ctx.stroke();

            var val = Main.GetStat('Errors', dt);
            var bv = b * val / maxEValue;
            if (prevErrorValY != null)
            {
                ctx.strokeStyle = "#cc8282";
                ctx.lineWidth = 2;
                ctx.beginPath();
                ctx.moveTo(Math.round(width - (i - 0.5) * w) + 0.5, Math.round(b - prevErrorValY) + 0.5);
                ctx.lineTo(Math.round(width - (i + 0.5) * w) + 0.5, Math.round(b - bv) + 0.5);
                ctx.stroke();
                ctx.lineWidth = 1;
            }
            prevErrorValY = bv;


            val = Main.GetStat('Searches', dt);
            ctx.fillStyle = "#68a568";
            var bv = bn * val / maxNValue;
            ctx.fillRect(Math.round(width - (i + 1) * w), Math.round(b), Math.ceil(w), Math.round(bv));
            ctx.beginPath();
            ctx.strokeStyle = "#395639";
            ctx.moveTo(Math.round(width - (i + 1) * w) + 1.5, Math.floor(b + bv) + 0.5);
            ctx.lineTo(Math.round(width - i * w) + 0.5, Math.floor(b + bv) + 0.5);
            ctx.lineTo(Math.round(width - i * w) + 0.5, Math.floor(b) + 0.5);
            ctx.stroke();
        }

        ctx.strokeStyle = "#FFFFFF";
        ctx.font = "12px sans-serif";

        var dts = Utils.ShortGWDateFormat(Main.EndDate);
        var tw = ctx.measureText(dts).width;
        ctx.fillStyle = "rgba(255,255,255,0.7)";
        ctx.fillRect(width - (tw + 5), b + 2, tw + 2, 16);
        ctx.fillStyle = "#000000";
        ctx.fillText(dts, width - (tw + 5), b + 14);

        var dts = Utils.ShortGWDateFormat(new Date(Main.EndDate.getTime() - 72 * 10 * 60 * 1000));
        var tw = ctx.measureText(dts).width;
        ctx.fillStyle = "rgba(255,255,255,0.7)";
        ctx.fillRect(width / 2 - tw / 2, b + 2, tw + 2, 16);
        ctx.fillStyle = "#000000";
        ctx.fillText(dts, width / 2 - tw / 2, b + 14);

        var dts = Utils.ShortGWDateFormat(new Date(Main.EndDate.getTime() - 144 * 10 * 60 * 1000));
        var tw = ctx.measureText(dts).width;
        ctx.fillStyle = "rgba(255,255,255,0.7)";
        ctx.fillRect(5, b + 2, tw + 2, 16);
        ctx.fillStyle = "#000000";
        ctx.fillText(dts, 5, b + 14);

        ctx.fillStyle = "#E0E0E0";
        ctx.fillRect(0, Math.round(b), width, 1);

        if (Main.CurrentTime)
        {
            var tdiff = (Main.EndDate.getTime() - Main.CurrentTime.getTime()) + Main.EndDate.getTimezoneOffset() * 60000;
            var t = tdiff / (10 * 60 * 1000);
            var x = width - Math.floor(t * w + w / 2);

            ctx.lineWidth = 1;
            ctx.strokeStyle = "rgba(0,0,255,0.7)";
            ctx.beginPath();
            ctx.moveTo(x + 0.5, 0);
            ctx.lineTo(x + 0.5, height);
            ctx.stroke();
        }

    }

    static GetStat(logType: string, when: Date): number
    {
        if (!Main.Stats || !Main.Stats[logType])
            return 0;
        var s: LogStat[] = Main.Stats[logType];
        for (var i = 0; i < s.length; i++)
            if (s[i].Date.getTime() == when.getTime())
                return s[i].Value;
        return 0;
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

    static TimeLineSelected(evt: JQueryMouseEventObject): void
    {
        Main.TimeLineMouse(evt);

        var up = () =>
        {
            $("#mouseCapture").hide().off("mousemove", Main.TimeLineMouse).off("mouseup").off("mouseleave");
        }

        $("#mouseCapture").show().on("mousemove", Main.TimeLineMouse).on("mouseup", up).on("mouseleave", up);
    }

    static TimeLineMouse(evt: JQueryMouseEventObject): void
    {
        var width = $("#timeRangeCanvas").width();
        var w = width / 145;
        var x = evt.pageX - ($("#timeRange").position().left + $("#timeRange span").width());

        var tx = Math.floor((width - x) / w);
        if (tx < 0)
            tx = 0;
        if (tx > 144)
            tx = 144;
        Main.CurrentTime = new Date((Main.EndDate.getTime() + Main.EndDate.getTimezoneOffset() * 60000) - tx * 10 * 60 * 1000);
        if (tx == 144 && ((new Date()).getTime() - Main.CurrentTime.getTime()) < 24 * 3600 * 1000)
            Main.IsLast = true;
        Main.SetState();

        Main.LoadTimeInfo();
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

        var startDate = new Date(Main.CurrentTime.getTime() - 10 * 60 * 1000);
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
        Main.DrawStats();
        var startDate = new Date(Main.CurrentTime.getTime());
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

        if (Main.loadingLogs)
            Main.loadingLogs.abort();
        Main.loadingLogs = $.ajax({
            type: 'GET',
            url: '/Logs/' + Main.CurrentGateway + '/' + startDate.getTime() + '/' + endDate.getTime() + (Main.Levels ? "?levels=" + Main.Levels + "&query=" + $("#queryField").val() : "?query=" + $("#queryField").val()),
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
                html += "</tbody></table>";
                $("#logsContent").html(html);

                // Scroll to bottom
                $("#logsContent").scrollTop($("#logsContent")[0].scrollHeight - $("#logsContent").height());

                $("#logsContent tbody > tr").on("mouseover", (evt) =>
                {
                    var rowId = parseInt(evt.target.parentElement.attributes["rowId"].value);
                    var html = "";
                    html += "<table>";
                    html += "<tr><td>Remote</td><td>" + Main.Logs[rowId].Remote + "</td></tr>";
                    var n = 0;
                    for (var i in Main.Logs[rowId].Details)
                    {
                        if (n == 0)
                            html += "<tr>";
                        else if (n % 2 == 0)
                            html += "</tr><tr>";
                        html += "<td>" + i + "</td><td>" + Main.Logs[rowId].Details[i] + "</td>";
                        n++;
                    }
                    html += "</tr></table>";
                    $("#detailInfo").html(html).show();
                }).on("mouseout", () =>
                {
                    $("#detailInfo").hide();
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
        Main.DrawStats();
    }

    static PopState(jEvent: JQueryEventObject)
    {
        Main.CurrentGateway = null;
        Main.CurrentTime = null;
        Main.EndDate = null;

        $("#help").show();
        $("#clients, #servers, #logs").hide();

        var path = Main.Path();
        if (path.length > 2 && path[1] == "GW")
        {
            Main.CurrentGateway = path[2];
        }

        var url = "" + document.location;
        if (url.indexOf("#") == -1)
            url = "";
        else
            url = url.substr(url.indexOf("#") + 1);
        var parts = url.split("&");
        var queryString = {};
        parts.forEach(row => queryString[row.split("=")[0]] = decodeURIComponent(row.split("=")[1]));

        if (queryString["c"])
            Main.CurrentTime = new Date(parseInt(queryString["c"]));
        if (queryString["s"])
        {
            Main.StartDate = new Date(parseInt(queryString["s"]));
            $("#startDate").val(Utils.FullUtcDateFormat(Main.StartDate));
        }
        if (queryString["e"])
        {
            Main.EndDate = new Date(parseInt(queryString["e"]));
            $("#endDate").val(Utils.FullUtcDateFormat(Main.EndDate));
        }
        if (queryString["q"])
            $("#queryField").val(queryString["q"])

        Main.LoadGateways();
    }

    static SetState()
    {
        var params = "";
        if (Main.CurrentTime)
            params += (params != "" ? "&" : "#") + "c=" + Main.CurrentTime.getTime();
        if (Main.StartDate)
            params += (params != "" ? "&" : "#") + "s=" + Main.StartDate.getTime();
        if (Main.EndDate)
            params += (params != "" ? "&" : "#") + "e=" + Main.EndDate.getTime();
        if ($("#queryField").val())
            params += (params != "" ? "&" : "#") + "q=" + encodeURIComponent($("#queryField").val());
        window.history.pushState(null, Main.BaseTitle + " - " + Main.CurrentGateway, '/GW/' + Main.CurrentGateway + params);
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
        Main.Levels = evt.target.getAttribute("level");

        Main.LoadTimeInfo();
    }

    static Init(): void
    {
        Main.BaseTitle = window.document.title;
        $("#gatewaySelector").on("change", Main.GatewayChanged);
        $(window).on("resize", Main.Resize);
        $("#timeRangeCanvas").on("mousedown", Main.TimeLineSelected);
        window.setInterval(Main.Refresh, 1000);
        $(window).bind('popstate', Main.PopState);
        $("#clientsTabs li:nth-child(1)").click(Main.ShowStats);
        $("#clientsTabs li:nth-child(2)").click(Main.ShowSearches);
        $("#prevDay").click(() =>
        {
            Main.StartDate = new Date(Main.StartDate.getTime() - 24 * 3600 * 1000);
            Main.EndDate = new Date(Main.StartDate.getTime() + 24 * 3600 * 1000);
            $("#startDate").val(Utils.FullUtcDateFormat(Main.StartDate));
            $("#endDate").val(Utils.FullUtcDateFormat(Main.EndDate));
            Main.DelayedSearch(Main.LoadLogStats);
        });
        $("#nextDay").click(() =>
        {
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
            Main.DelayedSearch(Main.LoadTimeInfo);
        });

        $("#logFilter li").click(Main.LogFilter);

        $("#logHelp").click(Main.ShowHelp);
        $("#closeHelp").click(Main.HideHelp);

        Main.PopState(null);
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

    static DelayedSearch(cb)
    {
        if (Main.SearchTimeout !== null)
            clearTimeout(Main.SearchTimeout);
        Main.SearchTimeout = setTimeout(() =>
        {
            Main.SetState();
            Main.SearchTimeout = null;
            Main.IsLast = false;
            if (cb)
                cb();
        }, 500);
    }
}

$(Main.Init); // Starting Main GUI tasks