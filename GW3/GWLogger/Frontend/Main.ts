/// <reference path="query.ts" />

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
    static Path: string = "Status";

    static loadingLogs: JQueryXHR;


    static LoadGateways(callback: () => void): void
    {
        $.ajax({
            type: 'POST',
            url: 'DataAccess.asmx/GetGatewaysList',
            data: JSON.stringify({}),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                callback();

                var gateways: string[] = msg.d;
                var data = gateways.map((c) => { return { text: c, value: c }; });

                $("#gatewaySelector").kendoDropDownList({
                    dataTextField: "text",
                    dataValueField: "value",
                    dataSource: data,
                    value: Main.CurrentGateway
                });

                if (Main.CurrentGateway && gateways.indexOf(Main.CurrentGateway) == -1 && Main.Path == "GW")
                    Notifications.Alert("The selected gateway doesn't have logs.");
                if (Main.CurrentGateway && gateways.indexOf(Main.CurrentGateway) != -1)
                {
                    Main.GatewayChanged();
                }
                else
                {
                    $("#gatewaySelector").data("kendoDropDownList").dataSource.add({ text: "-- Select Gateway --", value: "" });
                    $("#gatewaySelector").data("kendoDropDownList").select((c) => { return c.value == ""; });
                    //$("#gatewaySelector").data("kendoDropDownList").value("");
                }
            },
            error: function (msg, textStatus)
            {
                Main.statsAreLoading = false;
                console.log(msg.responseText);
            }
        });
    }

    static GatewayChanged(): void
    {
        if (Main.Path != "GW")
            return;
        Main.CurrentGateway = $('#gatewaySelector').val();
        var dataSource = $("#gatewaySelector").data("kendoDropDownList").dataSource;
        if (dataSource.data()[dataSource.data().length - 1].value === "")
            dataSource.remove(dataSource.data()[dataSource.data().length - 1]);
        if (Main.CurrentGateway === "")
        {
            $("#gatewaySelector").data("kendoDropDownList").value(Main.CurrentGateway);
            return;
        }
        State.Set();

        Main.GatewaySelected();
    }

    static GatewaySelected(): void
    {
        Main.LoadSessions(() =>
        {
            Main.LoadLogStats();
        });
    }

    static LoadLogStats(refresh: boolean = false): void
    {
        var resetEndDate = refresh && ((Main.CurrentTime && Main.Stats && Main.Stats.Logs && Main.Stats.Logs.length > 0 && Main.CurrentTime == Main.Stats.Logs[Main.Stats.Logs.length - 1].Date) || !(Main.CurrentTime && Main.Stats))
        if (Main.StartDate === null)
            Main.StartDate = new Date((new Date()).getTime() - (24 * 3600 * 1000));

        if (refresh === true && (((new Date()).getTime() - Main.StartDate.getTime()) > (25 * 3600 * 1000)))
            return;

        //var end = new Date();
        var start = Main.StartDate;

        var params = State.Parameters();
        if (!params["c"] && !params["s"])
            start = new Date((new Date()).getTime() - 24 * 3600 * 1000);

        var end = new Date(start.getTime() + 24 * 3600 * 1000);

        $.ajax({
            type: 'POST',
            url: 'DataAccess.asmx/GetStats',
            data: JSON.stringify({
                "gatewayName": Main.CurrentGateway,
                start: Utils.FullUtcDateFormat(start),
                end: Utils.FullUtcDateFormat(end)
            }),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                if (!msg.d)
                    return;
                Main.Stats = GatewayStats.CreateFromObject(msg.d);

                if (Main.EndDate === null)
                {
                    $("#startDate").val(Utils.FullUtcDateFormat(start));
                    $("#endDate").val(Utils.FullUtcDateFormat(end));
                }

                StatsBarGraph.DrawStats();

                if (refresh && (!params["c"] && !params["s"]))
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

    static LoadSessions(callback: () => void): void
    {
        $.ajax({
            type: 'POST',
            url: 'DataAccess.asmx/GetGatewaySessionsList',
            data: JSON.stringify({ "gatewayName": Main.CurrentGateway }),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                callback();

                var restartTypes = [];
                for (var o in RestartType)
                    restartTypes.push({
                        text: o, value: (o == "0" || o == "Unknown" ? "" : RestartType[o])
                    });

                Main.Sessions = (msg.d ? (<object[]>msg.d).map(function (c) { return GatewaySession.CreateFromObject(c); }) : []);
                var grid = $("#gatewaySessions").kendoGrid({
                    columns: [{ title: "Start", field: "StartDate", format: "{0:MM/dd HH:mm:ss}" },
                    { title: "End", field: "EndDate", format: "{0:MM/dd HH:mm:ss}" },
                    { title: "NB&nbsp;Logs", field: "NbEntries", format: "{0:n0}", attributes: { style: "text-align:right;" } },
                    { title: "Reason", field: "RestartType", values: restartTypes }],
                    dataSource: { data: Main.Sessions },
                    selectable: "single cell",
                    change: (arg) =>
                    {
                        var selected = arg.sender.select()[0];
                        var txt = selected.innerText;
                        var content = selected.textContent;
                        var uid = $(selected).parent().attr("data-uid");
                        var row = grid.dataSource.getByUid(uid);
                        if (row)
                        {
                            if (kendo.format(grid.columns[0].format, row['StartDate']) == txt)
                                Main.CurrentTime = (<Date>row['StartDate']).toUtc();
                            else if (kendo.format(grid.columns[0].format, row['EndDate']) == txt)
                                Main.CurrentTime = (<Date>row['EndDate']).toUtc();
                            Main.StartDate = new Date(Main.CurrentTime.getTime() - 12 * 3600000);
                            Main.EndDate = new Date(Main.CurrentTime.getTime() + 12 * 3600000);
                            State.Set(true);
                            State.Pop(null);
                        }
                    }
                }).data("kendoGrid");

                $("#gatewaySessions tbody tr").on("mouseover", (e) =>
                {
                    var d: any = grid.dataItem(e.target);
                    if (!d)
                        d = grid.dataItem(e.target.parentElement);
                    if (!d || !d.EndDate)
                        return;

                    var html = "<table>";
                    html += "<tr><td>Start&nbsp;Date:</td><td>" + Utils.ShortGWDateFormat(d.StartDate) + "</td></tr>";
                    html += "<tr><td>End&nbsp;Date:</td><td>" + Utils.ShortGWDateFormat(d.EndDate) + "</td></tr>";
                    html += "<tr><td>Restart&nbsp;Reason:</td><td>" + (d.RestartType == 0 ? "" : RestartType[d.RestartType]) + "</td></tr>";
                    html += "<tr><td>Restart&nbsp;Comment:</td><td>" + (!<string>d.Description ? "" : <string>d.Description).htmlEntities() + "</td></tr>";
                    html += "</table>";

                    ToolTip.Show(e.target, "bottom", html);
                });
            },
            error: function (msg, textStatus)
            {
                Main.statsAreLoading = false;
                console.log(msg.responseText);
            }
        });
    }

    static ShowStats(refresh: boolean)
    {
        if (!Main.Stats || !Main.Stats.Logs || Main.Stats.Logs.length == 0)
        {
            $("#clientsContent").html("");
            $("#serversContent").html("");
            return;
        }

        var startDate = Main.Stats.Logs[Main.Stats.Logs.length - 1].Date;
        //var startDate = new Date((new Date()).getTime() - 10 * 60 * 1000);
        if (Main.CurrentTime)
            startDate = Main.CurrentTime;

        $.ajax({
            type: 'POST',
            url: 'DataAccess.asmx/ActiveClients',
            data: JSON.stringify({
                "gatewayName": Main.CurrentGateway,
                "datePoint": Utils.FullDateFormat(startDate)
            }),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                $.ajax({
                    type: 'POST',
                    url: 'DataAccess.asmx/ActiveServers',
                    data: JSON.stringify({
                        "gatewayName": Main.CurrentGateway,
                        "datePoint": Utils.FullDateFormat(startDate)
                    }),
                    contentType: 'application/json; charset=utf-8',
                    dataType: 'json',
                    success: function (msg)
                    {
                        if ($("#serversContent").data("kendoGrid"))
                            $("#serversContent").data("kendoGrid").destroy();
                        $("#serversContent").html("").kendoGrid({
                            columns: [
                                { title: "Server", field: "Key" },
                                { title: "Nb Actions", field: "Value" }],
                            dataSource:
                            {
                                data: msg.d
                            }
                        });
                    },
                    error: function (msg, textStatus)
                    {
                        $("#serversContent").html("");
                        console.log(msg.responseText);
                    }
                });

                if ($("#clientsContent").data("kendoGrid"))
                    $("#clientsContent").data("kendoGrid").destroy();
                $("#clientsContent").html("").kendoGrid({
                    columns: [
                        { title: "Client", field: "Key" },
                        { title: "Nb Actions", field: "Value" }],
                    dataSource:
                    {
                        data: msg.d
                    }
                });
            },
            error: function (msg, textStatus)
            {
                $("#clientsContent").html("");
                console.log(msg.responseText);
            }
        });
    }

    static LoadTimeInfo(refresh: boolean = false)
    {
        StatsBarGraph.DrawStats();

        if ($("#closeHelp, #closeOperation").css("display") == "none")
        {
            $("#helpView, #operationView").hide();
            $("#clients, #servers, #logs").show();
        }

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
            $("#showLastBtn").hide();
            var FirstColumn = {
                'SearchesPerformed': 'Client',
                'SearchesOnChannelsPerformed': 'Channel',
                'MostActiveClasses': 'Class',
                'MostConsumingChannel': 'Channel'
            };

            var SecondColumn = {
                'SearchesPerformed': 'Searches',
                'SearchesOnChannelsPerformed': 'Searches',
                'MostActiveClasses': 'Calls',
                'MostConsumingChannel': 'NbBytes'
            };

            Main.loadingLogs = $.ajax({
                type: 'POST',
                url: '/DataAccess.asmx/' + tab.getAttribute("report"),
                data: JSON.stringify({ "gatewayName": Main.CurrentGateway, "datePoint": Utils.FullDateFormat(Main.CurrentTime == null ? Main.StartDate : Main.CurrentTime) }),
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                success: function (msg)
                {
                    Main.ShowStats(refresh);

                    Main.loadingLogs = null;
                    var data = <KeyValuePair[]>msg.d;

                    if ($("#logsContent").data("kendoGrid"))
                        $("#logsContent").data("kendoGrid").destroy();
                    $("#logsContent").html("").kendoGrid({
                        columns: [
                            { title: (FirstColumn[tab.getAttribute("report")]), field: "Key" },
                            { title: (SecondColumn[tab.getAttribute("report")]), field: "Value", width: "80px", format: "{0:n0}", attributes: { style: "text-align:right;" } }],
                        dataSource:
                        {
                            data: data
                        }
                    });

                    $("#logsContent").data("kendoGrid").table.on("click", (evt) =>
                    {
                        var row = <KeyValuePair>(<object>$("#logsContent").data("kendoGrid").dataItem($(evt.target).parent()));
                        if ($("#queryField").val().trim() != "")
                            $("#queryField").val($("#queryField").val().trim() + " and ");

                        var query = tab.getAttribute("query")
                        var val = row.Key;
                        if (tab.getAttribute("split"))
                            val = val.split(tab.getAttribute("split"))[0];
                        $("#queryField").val($("#queryField").val() + query.replace(/'/g, "\"").replace(/\{0\}/g, val));

                        $("#logFilter > li")[0].click();
                    });
                },
                error: function (msg, textStatus)
                {
                    $("#logsContent").html("");

                    if (textStatus == "abort")
                        return;
                    if (textStatus)
                        console.log(textStatus)
                    if (msg.responseText)
                        console.log(msg.responseText);
                }
            });
            return;
        }
        Main.Levels = tab.getAttribute("level");

        var params = State.Parameters();
        var url: string;
        if (!params["c"] && !params["s"] && !Main.CurrentTime)
        {
            url = '/Logs/' + Main.CurrentGateway;
            $("#showLastBtn").hide();
        }
        else
        {
            $("#showLastBtn").show();
            var startDate = (Main.CurrentTime ? new Date(Main.CurrentTime.getTime()) : (Main.EndDate ? new Date(Main.EndDate.getTime() - 20 * 60 * 1000) : new Date((new Date()).getTime() - 20 * 60 * 1000)));
            var endDate = new Date(startDate.getTime() + 20 * 60 * 1000);

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
            url = '/Logs/' + Main.CurrentGateway + '/' + startDate.getTime() + '/' + endDate.getTime() + queryString;
        }

        Main.loadingLogs = $.ajax({
            type: 'GET',
            url: url,
            success: function (data)
            {
                Main.ShowStats(refresh);

                Main.loadingLogs = null;
                var tz = (new Date()).getTimezoneOffset() * 60000;

                if ($("#logsContent").data("kendoGrid"))
                    $("#logsContent").data("kendoGrid").destroy();

                if (data && data.length && !data[0].Details)
                {
                    var columns = [];
                    for (var j in data[0])
                    {
                        var c = { field: j, title: j.replace(/_/g, " ") };
                        if (typeof data[0][j] == "number" && Math.abs((new Date()).getTime() - new Date(data[0][j]).getTime()) < 4320000000)
                            c['format'] = "{0:MM/dd HH:mm:ss.fff}";
                        columns.push(c);
                    }

                    data.forEach((r) =>
                    {
                        columns.forEach((j) =>
                        {
                            if (j.format)
                                r[j.field] = new Date(r[j.field] + tz);
                        });
                    });

                    var options = {
                        columns: columns,
                        dataSource:
                        {
                            data: data
                        }
                    };

                    $("#logsContent").html("").kendoGrid(options);
                }
                else
                {
                    var logs: LogEntry[] = data;
                    Main.Logs = logs;
                    Main.Logs.forEach((r) =>
                    {
                        r.Date = new Date(<number>r.Date + tz);
                    });
                    $("#logsContent").html("").kendoGrid({
                        columns: [
                            { title: "Date", field: "Date", format: "{0:MM/dd HH:mm:ss.fff}", width: "160px" },
                            { title: "Type", field: "Type", width: "80px", attributes: { style: "text-align:right;" } },
                            { title: "Level", field: "Level", width: "80px", attributes: { style: "text-align:right;" } },
                            { title: "Message", field: "Message" }],
                        dataSource:
                        {
                            data: Main.Logs
                        }
                    });

                    $("#logsContent").data("kendoGrid").table.on("mouseover",
                        (evt) =>
                        {
                            if (Main.KeepOpenDetails)
                                return;
                            var row = <LogEntry>(<object>$("#logsContent").data("kendoGrid").dataItem($(evt.target).parent()));
                            var html = Main.DetailInfo(row);
                            $("#detailInfo").html(html).show();
                        }).on("mouseout", () =>
                        {
                            if (Main.KeepOpenDetails)
                                return;
                            $("#detailInfo").hide();
                        }).on("click", (evt) =>
                        {
                            var row = <LogEntry>(<object>$("#logsContent").data("kendoGrid").dataItem($(evt.target).parent()));
                            var html = Main.DetailInfo(row);

                            /*var rowId = parseInt(evt.target.parentElement.attributes["rowId"].value);
                            if (rowId == -1)
                            {
                                Main.Offset = Main.Logs[Main.Logs.length - 1].Position;
                                Main.OffsetFile = Main.Logs[Main.Logs.length - 1].CurrentFile;
                                Main.LoadTimeInfo();
                                return;
                            }*/
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
                }

                // Scroll to bottom
                //if (Main.IsLast)
                if (!params["c"] && !params["s"])
                    $("#logsContent div.k-grid-content").scrollTop($("#logsContent div.k-grid-content")[0].scrollHeight - $("#logsContent div.k-grid-content").height());
                else
                    $("#logsContent").scrollTop(0);
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

    static DetailInfo(row: LogEntry): string
    {
        if (!row)
            return "";
        var html = "";
        html += "<table>";
        html += "<tr><td>Message&nbsp;Type</td><td><span class='pseudoLink' onclick='Main.AddFilter(event,\"type\");'>" + Main.MessageTypes[row.Type] + "</span></td></tr>";
        html += "<tr><td>Date</td><td><span class='pseudoLink' onclick='Main.AddFilter(event,\"date\");'>" + Utils.GWDateFormatMilis(new Date(<number>row.Date)) + "</span></td>";
        html += "<td>Remote</td><td><span class='pseudoLink' onclick='Main.AddFilter(event,\"remote\");'>" + row.Remote + "</span></td></tr>";
        var n = 0;
        for (var i in row.Details)
        {
            if (typeof row[i] === "function" || typeof row[i] === "object" || i == "uid")
                continue;
            if (n == 0)
                html += "<tr>";
            else if (n % 2 == 0)
                html += "</tr><tr>";
            html += "<td>" + i.replace(/(\w)([A-Z][a-z])/g, "$1&nbsp;$2") + "</td><td><span class='pseudoLink' onclick='Main.AddFilter(event,\"" + i + "\");'>" + row.Details[i] + "</span></td>";
            n++;
        }
        html += "</tr></table>";
        return html;
    }

    static AddFilter(evt: MouseEvent, prop: string)
    {
        var value = (<HTMLElement>evt.target).innerText;

        if (prop == "remote")
            value = (value.split('(')[1]).replace(")", "");

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

    static lastRefresh: number = 0;
    static statsAreLoading = false;
    static Refresh()
    {
        var now = new Date();
        $("#currentTime").html(("" + now.getUTCHours()).padLeft("0", 2) + ":" + ("" + now.getUTCMinutes()).padLeft("0", 2) + ":" + ("" + now.getUTCSeconds()).padLeft("0", 2));
        if (Main.lastRefresh == 1)
        {
            Main.lastRefresh = 0;
            return;
        }
        Main.lastRefresh++;

        if (!this.statsAreLoading)
        {
            Main.statsAreLoading = true;
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

                    $.ajax({
                        type: 'POST',
                        url: 'DataAccess.asmx/GetBufferUsage',
                        data: JSON.stringify({}),
                        contentType: 'application/json; charset=utf-8',
                        dataType: 'json',
                        success: (msg) =>
                        {
                            $("#bufferSpace").html("" + msg.d + "%");

                            /*Main.LoadGateways(() =>
                            {*/
                                Live.RefreshShort(() =>
                                {
                                    if (Main.Path == "GW" && Main.CurrentGateway)
                                        Main.LoadSessions(() =>
                                        {
                                            Main.statsAreLoading = false;

                                            Main.LoadLogStats(true);
                                        });
                                    else
                                        Main.statsAreLoading = false;
                                });
                            //});
                        },
                        error: () =>
                        {
                            Main.statsAreLoading = false;
                        }
                    });
                },
                error: () =>
                {
                    Main.statsAreLoading = false;
                }
            });
        }
    }

    static CheckTouch(): boolean
    {
        return (('ontouchstart' in window)
            || ((<any>navigator).MaxTouchPoints > 0)
            || ((<any>navigator).msMaxTouchPoints > 0));
    }

    static Resize(): void
    {
        if (Math.abs(window.innerHeight - screen.height) < 10 && Main.CheckTouch())
            $("#fullScreenMode").attr("media", "");
        else
            $("#fullScreenMode").attr("media", "max-width: 1px");
        StatsBarGraph.DrawStats();
        $(".k-grid").each((idx, elem) =>
        {
            $(elem).data("kendoGrid").resize(true);
        });
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
                break;
            }
        }

        State.Set();
        Main.Levels = evt.target.getAttribute("level");
        Main.LoadTimeInfo();
    }

    static GWVersions(): void
    {
        $("#reportView").show();
        $('#helpView, #operationView').hide();
        $("#reportContent").removeClass().addClass("fixed").removeAttr('style').html("Loading...");

        if ($("#reportContent").data("kendoGrid"))
            $("#reportContent").data("kendoGrid").destroy();

        var lastVersion = null;
        for (var i = 0; i < Live.shortInfo.length; i++)
            if (lastVersion == null || Live.shortInfo[i].Version > lastVersion)
                lastVersion = Live.shortInfo[i].Version;

        $("#reportContent").html("").kendoGrid({
            columns: [
                { title: "Gateway", field: "Name" },
                { title: "Build", field: "Build" },
                { title: "Version", field: "Version" }],
            dataSource:
            {
                data: Live.shortInfo
            },
            sortable: true
        });

        var grid = $("#reportContent").data("kendoGrid");
        grid.bind("dataBound", (row) =>
        {
            var items = row.sender.items();
            items.each(function (index)
            {
                var dataItem = <GatewayShortInformation>(<any>grid.dataItem(this));
                if (dataItem.Version < lastVersion)
                    this.className += " oldGatewayVersion";
            })
        });
        grid.dataSource.fetch();
    }

    static LogStatistics(): void
    {
        $("#reportView").show();
        $('#helpView, #operationView').hide();
        $("#reportContent").removeClass().addClass("fixed").removeAttr('style').html("Loading...");

        if ($("#reportContent").data("kendoGrid"))
            $("#reportContent").data("kendoGrid").destroy();

        $.ajax({
            type: 'POST',
            url: '/DataAccess.asmx/GetDataFileStats',
            data: JSON.stringify({}),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                var vals = <DataFileStats[]>msg.d;

                var totSec = 0;
                var avgBytesTot = 0;
                var totSize = 0;
                for (var i = 0; i < vals.length; i++)
                {
                    totSec += vals[i].LogsPerSeconds;
                    avgBytesTot += vals[i].AverageEntryBytes;
                    totSize += vals[i].TotalDataSize;
                }

                vals.push(
                    {
                        Name: "Total",
                        AverageEntryBytes: Math.round(avgBytesTot / vals.length),
                        LogsPerSeconds: totSec,
                        TotalDataSize: totSize
                    });

                $("#reportContent").html("").kendoGrid({
                    columns: [
                        { title: "Gateway", field: "Name" },
                        { title: "Logs Per Sec.", field: "LogsPerSeconds", format: "{0:0.00}" },
                        { title: "Average Entry", field: "AverageEntryBytes" },
                        { title: "Size On Disk", field: "TotalDataSize", template: (row: DataFileStats) => Utils.HumanReadable(row.TotalDataSize) }],
                    dataSource:
                    {
                        data: vals
                    },
                    sortable: true
                });

                var grid = $("#reportContent").data("kendoGrid");
                grid.bind("dataBound", (row) =>
                {
                    var items = row.sender.items();
                    items.each(function (index)
                    {
                        var dataItem = <DataFileStats>(<any>grid.dataItem(this));
                        if (dataItem.Name == "Total")
                            this.className += " summary";
                    })
                });
                grid.dataSource.fetch();
            }
        });
    }

    static Init(): void
    {
        /*if (/Trident\/|MSIE /.test("" + window.navigator.userAgent))
            Notifications.Alert("Internet Explorer is not supported!");*/

        if (("" + document.location).startsWith("http://localhost"))
            $("#gatewayBeamlines").append(`<div class="GWDisplay" id="PBGW"></div>`);

        Main.LoadGateways(() => { });

        $(".helpMiddleContainer a").on("click", (elem) =>
        {
            var href: string = elem.target.attributes["href"].value;
            $(href)[0].scrollIntoView();
            return false;
        });

        $("*[tooltip]").each((idx, elem) =>
        {
            $(elem).on("mouseover", (e) =>
            {
                var text = $(e.target).attr("tooltip");
                var position = $(e.target).attr("tooltip-position");
                if (!position)
                    position = "bottom";
                //$(elem).kendoTooltip({ position: position, content: text, animation: false });
                ToolTip.Show(e.target, (<any>position), text);
            });
        });

        var currentUrl = (document.location + "");
        if (currentUrl.toLowerCase().startsWith("http://caesar"))
        {
            if (currentUrl.toLowerCase().startsWith("http://caesar/"))
                document.location.replace(currentUrl.toLowerCase().replace("http://caesar/", "https://caesar.psi.ch/"));
            else
                document.location.replace(currentUrl.toLowerCase().replace("http://caesar", "https://caesar"));
            return;
        }
        if (currentUrl.toLowerCase().startsWith("http://gfaepicslog"))
        {
            if (currentUrl.toLowerCase().startsWith("http://gfaepicslog/"))
                document.location.replace(currentUrl.toLowerCase().replace("http://gfaepicslog/", "https://caesar.psi.ch/"));
            else
                document.location.replace(currentUrl.toLowerCase().replace("http://gfaepicslog", "https://caesar"));
            return;
        }

        Live.InitShortDisplay();
        Main.QueriesHelp();
        Map.Init();

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

        $("#mainTabs li").click((evt) =>
        {
            if (evt.target.tagName != "INPUT")
            {
                var tab = evt.target.textContent;
                switch (tab)
                {
                    case "Status":
                        $("#reportView").hide();
                        if ($("#helpView").is(":visible"))
                            $("#helpView").hide();
                        if ($("#operationView").is(":visible"))
                            $("#operationView").hide();
                        if (Main.Path == "Status")
                            Main.CurrentGateway = null;
                        Main.Path = "Status";
                        State.Set(true);
                        State.Pop(null);
                        $(".inset").removeClass("inset");
                        break;
                    case "Logs":
                        $("#reportView").hide();
                        if ($("#helpView").is(":visible"))
                            $("#helpView").hide();
                        if ($("#operationView").is(":visible"))
                            $("#operationView").hide();
                        if (Main.Path == "GW")
                            break;
                        Main.Path = "GW";
                        State.Set(true);
                        State.Pop(null);
                        $(".inset").removeClass("inset");
                        break;
                    case "Map":
                        $("#reportView").hide();
                        if ($("#helpView").is(":visible"))
                            $("#helpView").hide();
                        if ($("#operationView").is(":visible"))
                            $("#operationView").hide();
                        if (Main.Path == "Map")
                            break;
                        Main.Path = "Map";
                        State.Set(true);
                        State.Pop(null);
                        $(".inset").removeClass("inset");
                        break;
                    case "Help":
                        window.open("/help.html", "help", "menubar=no,location=no,status=no,toolbar=no,width=800,height=600,scrollbars=yes");
                        /*$(".inset").removeClass("inset");
                        if ($("#operationView").is(":visible"))
                            $("#operationView").hide();
                        $("#reportView").hide();
                        if ($("#helpView").is(":visible"))
                            $("#helpView").hide();
                        else
                        {
                            $("#helpView").show();
                            $(evt.target).addClass("inset");
                        }*/
                        break;
                    case "Operator Help":
                        window.open("/sop_d.html", "help", "menubar=no,location=no,status=no,toolbar=no,width=800,height=600,scrollbars=yes");
                        /*$(".inset").removeClass("inset");
                        if ($("#helpView").is(":visible"))
                            $("#helpView").hide();
                        $("#reportView").hide();
                        if ($("#operationView").is(":visible"))
                            $("#operationView").hide();
                        else
                        {
                            $("#operationView").show();
                            $(evt.target).addClass("inset");
                        }*/
                        break;
                    case "": // Hambuger
                        $("#hamburgerMenu").toggleClass("visibleHamburger");
                        break;
                    default:
                        State.Set(true);
                        State.Pop(null);
                        break;
                }
            }
        });

        $("#hamburgerMenu div").on("click", () =>
        {
            $("#hamburgerMenu").toggleClass("visibleHamburger");
        });

        Main.BaseTitle = window.document.title;
        //$("#gatewaySelector").kendoDropDownList();
        $("#gatewaySelector").on("change", Main.GatewayChanged);
        $(window).on("resize", Main.Resize);
        $("#timeRangeCanvas").on("mousedown", StatsBarGraph.TimeLineSelected);
        window.setInterval(Main.Refresh, 1000);
        $(window).bind('popstate', State.Pop);
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
        var startDateChange = () =>
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
        };
        $("#startDate").kendoDateTimePicker({ format: "yyyy/MM/dd HH:mm:ss", change: startDateChange }).on("keyup", startDateChange);
        var endDateChange = () =>
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
        };
        $("#endDate").kendoDateTimePicker({ format: "yyyy/MM/dd MM:mm:ss", change: endDateChange }).on("keyup", endDateChange);
        $("#showLastBtn").on("click", () =>
        {
            Main.StartDate = null;
            Main.EndDate = null;
            Main.CurrentTime = null;
            $("#queryField").val("");
            State.Set(true);
            State.Pop(null);
        });
        /*$("#queryField").on("focus", () =>
        {
            $("#querySuggestions").show();
            Main.ShowSuggestion();
        });
        $("#queryField").on("blur", () =>
        {
            $("#querySuggestions").hide();
        });*/

        jQuery.fn.scrollTo = function (elem)
        {
            $(this).scrollTop($(this).scrollTop() - $(this).offset().top + $(elem).offset().top);
            return this;
        };
        $(document).on("keyup", (evt: JQueryEventObject) =>
        {
            if (evt.keyCode == 27)
            {
                $("#querySuggestions").hide();
            }
        });

        $("#btnPremadeQueries").on("click", () =>
        {
            if ($("#premadeQueries").is(":visible"))
            {
                $("#premadeQueries").hide();
            }
            else
            {
                $("#premadeQueries").show();
                setTimeout(() =>
                {
                    $("#premadeQueries").hide();
                }, 3000);
            }
        });

        $("#premadeQueries div").on("click", (evt: JQueryEventObject) =>
        {
            $("#premadeQueries").hide();
            $("#queryField").val(evt.target.getAttribute("query"));
            $("#queryField").keyup();
        });

        $("#queryField").on("click", (evt: JQueryEventObject) =>
        {
            $("#queryField").keyup();
        });

        $(document).on("click", (evt: JQueryEventObject) =>
        {
            if (evt.target != document.getElementById("queryField") && !evt.target.classList.contains("suggestion-item"))
            {
                $("#querySuggestions").hide();
            }
        });

        $("#queryField").on("keydown", (evt: JQueryEventObject) =>
        {
            var suggestionList = $("#suggestions");
            switch (evt.keyCode)
            {
                case 40:
                    var el = suggestionList.children(".selected").first();
                    if (el.next(".suggestion-item").length > 0)
                    {
                        el.next(".suggestion-item").addClass("selected");
                        el.removeClass("selected");
                        suggestionList.scrollTo(".selected");
                    }
                    break;
                case 38:
                    var el = suggestionList.children(".selected").first();
                    if (el.prev(".suggestion-item").length > 0)
                    {
                        el.prev(".suggestion-item").addClass("selected");
                        el.removeClass("selected");
                        suggestionList.scrollTo(".selected");
                    }
                    break;
            }
        });

        $("#queryField").on("keyup", (evt: JQueryEventObject) =>
        {
            var suggestionBox = $("#querySuggestions");
            var suggestionList = $("#suggestions");
            switch (evt.keyCode)
            {
                case 40:
                case 38:
                    break;
                case 13:
                    if (suggestionBox.is(":visible"))
                    {
                        $("#queryField").val(Main.completedText(suggestionList.children(".selected").clone().children("span").remove().end().text()) + " ");
                    }
                default:
                    suggestionList.empty();
                    var suggestions = QueryParser.getProposals($("#queryField").val());
                    if (suggestions.length > 0)
                    {
                        suggestions.forEach(sug =>
                        {
                            suggestionList.append(`<div class="suggestion-item">${sug.suggestion.replace(sug.input, '<b>' + sug.input + '</b>')}<span class="hint">${typeof (sug.hint) != "undefined" ? sug.hint: "" } ${typeof (sug.dataType) != "undefined" ? ": " + sug.dataType : ""}</span></div>`);
                        });
                        suggestionList.children().each(function ()
                        {
                            var element = this;
                            element.onclick = (evt: JQueryEventObject) =>
                            {
                                if (element.classList.contains("suggestion-item"))
                                {
                                    $("#queryField").val(Main.completedText($(element).clone().children("span").remove().end().text() + " "));
                                    var e = jQuery.Event("keyup");
                                    e.which = 32;
                                    $("#queryField").trigger(e);
                                    $("#queryField").focus();
                                }
                            }
                        });
                        if (suggestionList.children.length > 0)
                        {
                            suggestionList.children().first().addClass("selected");
                            suggestionBox.show();
                            suggestionList.scrollTo(".selected");
                        }
                    } else
                    {
                        suggestionBox.hide();
                    }
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

        var gateways: string[] = [];
        $('.GWDisplay').each(function ()
        {
            gateways.push($(this).attr('id'));
        });
        var autocomplete = $("#searchInput").kendoAutoComplete({
            minLength: 1,
            select: (e) => window.location.href = "Status/" + e.dataItem.toLowerCase(),
            dataSource: gateways,
            placeholder: "Search",
            filter: "contains"
        }).data("kendoAutoComplete");

        State.Pop(null);
    }

    static completedText(completionText: string): string
    {
        var previousText = $("#queryField").val();
        if (completionText == ")" || completionText == '"' || completionText == "'")
        {
            return previousText + completionText;
        }
        let brackets: number = previousText.lastIndexOf("(");
        let space: number = previousText.lastIndexOf(" ");
        return previousText.substring(0, Math.max(brackets, space) + 1) + completionText;
    }

    static QueriesHelp()
    {
        var html = "";
        html += "&lt;variable&gt; &lt;condition&gt; &lt;value&gt; [and/or &lt;...&gt;]";

        html += "<h3>Variables:</h3><table>";
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

        html += "<h3>Conditions:</h3><table>";
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

        $("#queriesHelp").html(html);
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
