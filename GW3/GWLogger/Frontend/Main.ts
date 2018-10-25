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
                var data = gateways.map((c) => { return { text: c, value: c }; });

                $("#gatewaySelector").kendoDropDownList({
                    dataTextField: "text",
                    dataValueField: "value",
                    dataSource: data,
                    value: Main.CurrentGateway
                });

                if (Main.CurrentGateway)
                {
                    /*var items: any[] = $("#gatewaySelector").data("kendoDropDownList").items();
                    for (var i = 0; i < items[i].length; i++)
                    {
                        if (items[i].innerText == Main.CurrentGateway)
                        {
                            $("#gatewaySelector").data("kendoDropDownList").select(i);
                            break;
                        }
                    }*/
                    /*$("#gatewaySelector").data("kendoDropDownList").select((c) =>
                    {
                        return c.value == Main.CurrentGateway;
                    });*/
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
                    /*if (Main.Stats.Logs && Main.Stats.Logs.length > 0)
                        Main.EndDate = Main.Stats.Logs[Main.Stats.Logs.length - 1].Date;
                    else
                        Main.EndDate = new Date();*/
                    $("#startDate").val(Utils.FullUtcDateFormat(start));
                    $("#endDate").val(Utils.FullUtcDateFormat(end));
                }

                /*if (resetEndDate)
                {
                    if (Main.Stats.Logs && Main.Stats.Logs.length > 0)
                        Main.EndDate = Main.CurrentTime = Main.Stats.Logs[Main.Stats.Logs.length - 1].Date;
                    else
                        Main.EndDate = Main.CurrentTime = new Date();
                }*/
                StatsBarGraph.DrawStats();

                //if (refresh && Main.IsLast)
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
                $("#gatewaySessions").kendoGrid({
                    columns: [{ title: "Start", field: "StartDate", format: "{0:MM/dd HH:mm:ss}" },
                    { title: "End", field: "EndDate", format: "{0:MM/dd HH:mm:ss}" },
                    { title: "NB&nbsp;Logs", field: "NbEntries", format: "{0:n0}", attributes: { style: "text-align:right;" } }],
                    dataSource: { data: Main.Sessions }
                });
            },
            error: function (msg, textStatus)
            {
                console.log(msg.responseText);
            }
        });
    }

    static ShowStats()
    {
        if (!Main.Stats || !Main.Stats.Logs || Main.Stats.Logs.length == 0)
            return;

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
                console.log(msg.responseText);
            }
        });

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
                console.log(msg.responseText);
            }
        });
    }

    static LoadTimeInfo(refresh: boolean = false)
    {
        StatsBarGraph.DrawStats();

        if ($("#closeHelp").css("display") == "none")
        {
            $("#help").hide();
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
                'MostActiveClasses': 'Class'
            };

            Main.loadingLogs = $.ajax({
                type: 'POST',
                url: '/DataAccess.asmx/' + tab.getAttribute("report"),
                data: JSON.stringify({ "gatewayName": Main.CurrentGateway, "datePoint": Utils.FullDateFormat(Main.CurrentTime == null ? Main.StartDate : Main.CurrentTime) }),
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                success: function (msg)
                {
                    Main.ShowStats();

                    Main.loadingLogs = null;
                    var data = <KeyValuePair[]>msg.d;

                    if ($("#logsContent").data("kendoGrid"))
                        $("#logsContent").data("kendoGrid").destroy();
                    $("#logsContent").html("").kendoGrid({
                        columns: [
                            { title: (FirstColumn[tab.getAttribute("report")]), field: "Key" },
                            { title: "Searches", field: "Value", width: "80px", format: "{0:n0}", attributes: { style: "text-align:right;" } }],
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
                Main.ShowStats();

                Main.loadingLogs = null;
                var logs: LogEntry[] = data;
                Main.Logs = logs;
                var tz = (new Date()).getTimezoneOffset() * 60000;
                Main.Logs.forEach((r) =>
                {
                    r.Date = new Date(<number>r.Date + tz);
                });
                if ($("#logsContent").data("kendoGrid"))
                    $("#logsContent").data("kendoGrid").destroy();
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
        if (Main.Path == "GW")
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

        Live.RefreshShort();
    }

    static Resize(): void
    {
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
                //console.log("Tab " + i);
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
        $('#helpView').hide();
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
        $('#helpView').hide();
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
        Live.InitShortDisplay();
        Main.QueriesHelp();

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


        Live.DisplayPaint("#canvas_all_fine", { CPU: 10, Searches: 20, State: 0 });
        Live.DisplayPaint("#canvas_high_search", { CPU: 10, Searches: 100, State: 0 });
        Live.DisplayPaint("#canvas_high_cpu", { CPU: 90, Searches: 20, State: 0 });
        Live.DisplayPaint("#canvas_error", { CPU: 90, Searches: 20, State: 3 });

        $("#mainTabs li").click((evt) =>
        {
            var tab = evt.target.textContent;
            switch (tab)
            {
                case "Status":
                    $("#reportView").hide();
                    if ($("#helpView").is(":visible"))
                        $("#helpView").hide();
                    if (Main.Path == "Status")
                        Main.CurrentGateway = null;
                    Main.Path = "Status";
                    State.Set(true);
                    State.Pop(null);
                    break;
                case "Logs":
                    $("#reportView").hide();
                    if ($("#helpView").is(":visible"))
                        $("#helpView").hide();
                    if (Main.Path == "GW")
                        break;
                    Main.Path = "GW";
                    State.Set(true);
                    State.Pop(null);
                    break;
                case "Help":
                    $("#reportView").hide();
                    if ($("#helpView").is(":visible"))
                        $("#helpView").hide();
                    else
                        $("#helpView").show();
                    break;
                case "": // Hambuger
                    $("#hamburgerMenu").toggleClass("visibleHamburger");
                    break;
                default:
                    State.Set(true);
                    State.Pop(null);
                    break;
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
        window.setInterval(Main.Refresh, 2000);
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
        $("#queryField").on("keyup", (evt: JQueryEventObject) =>
        {
            switch (evt.keyCode)
            {
                case 27:
                    $("#queryField").blur();
                    break;
                case 13:
                    //$("#querySuggestions").hide();
                    break;
                default:
                    //$("#querySuggestions").show();
                    //Main.ShowSuggestion();
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

        State.Pop(null);
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

$(Main.Init); // Starting Main GUI tasks