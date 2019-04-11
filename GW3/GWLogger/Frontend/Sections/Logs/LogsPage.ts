class LogsPage
{
    static Sessions: GatewaySession[];
    static Logs: LogEntry[] = null;
    static Offset: number = null;
    static OffsetFile: string = null;
    static MessageTypes: string[] = [];
    static Stats: GatewayStats;
    private static loadingLogs: JQueryXHR = null;

    public static Init(): void
    {
        LogsPage.LoadGateways();
    }

    static async LoadGateways()
    {
        try
        {
            var msg = await Utils.Loader("GetGatewaysList");

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
                LogsPage.GatewayChanged();
            else
            {
                $("#gatewaySelector").data("kendoDropDownList").dataSource.add({ text: "-- Select Gateway --", value: "" });
                $("#gatewaySelector").data("kendoDropDownList").select((c) => { return c.value == ""; });
            }
        }
        catch (ex)
        {
        }
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

        LogsPage.GatewaySelected();
    }

    static async GatewaySelected()
    {
        await LogsPage.LoadSessions();
        await LogsPage.LoadLogStats();
    }

    static async LoadSessions()
    {
        try
        {
            var msg = await Utils.Loader("GetGatewaySessionsList", { "gatewayName": Main.CurrentGateway });

            var restartTypes = [];
            for (var o in RestartType)
                restartTypes.push({
                    text: o, value: (o == "0" || o == "Unknown" ? "" : RestartType[o])
                });

            LogsPage.Sessions = (msg.d ? (<object[]>msg.d).map(function (c) { return GatewaySession.CreateFromObject(c); }) : []);
            var grid = $("#gatewaySessions").kendoGrid({
                columns: [{ title: "Start", field: "StartDate", format: "{0:MM/dd HH:mm:ss}" },
                { title: "End", field: "EndDate", format: "{0:MM/dd HH:mm:ss}" },
                { title: "NB&nbsp;Logs", field: "NbEntries", format: "{0:n0}", attributes: { style: "text-align:right;" } },
                { title: "Reason", field: "RestartType", values: restartTypes }],
                dataSource: { data: LogsPage.Sessions },
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
        }
        catch (ex)
        {
        }
    }

    static LoadLogStats(refresh: boolean = false): void
    {
        var resetEndDate = refresh && ((Main.CurrentTime && LogsPage.Stats && LogsPage.Stats.Logs && LogsPage.Stats.Logs.length > 0 && Main.CurrentTime == LogsPage.Stats.Logs[LogsPage.Stats.Logs.length - 1].Date) || !(Main.CurrentTime && LogsPage.Stats))
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
                LogsPage.Stats = GatewayStats.CreateFromObject(msg.d);

                if (Main.EndDate === null)
                {
                    $("#startDate").val(Utils.FullUtcDateFormat(start));
                    $("#endDate").val(Utils.FullUtcDateFormat(end));
                }

                StatsBarGraph.DrawStats();

                if (refresh && (!params["c"] && !params["s"]))
                    LogsPage.LoadTimeInfo(refresh);
                else if (!refresh)
                    LogsPage.LoadTimeInfo();
            },
            error: function (msg, textStatus)
            {
                console.log(msg.responseText);
            }
        });
    }

    static async LoadTimeInfo(refresh: boolean = false)
    {
        StatsBarGraph.DrawStats();

        if ($("#closeHelp, #closeOperation").css("display") == "none")
        {
            $("#helpView, #operationView").hide();
            $("#clients, #servers, #logs").show();
        }

        if (refresh && LogsPage.loadingLogs)
            return;
        else if (LogsPage.loadingLogs)
            LogsPage.loadingLogs.abort();

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

            LogsPage.loadingLogs = Utils.LoaderXHR(tab.getAttribute("report"), { "gatewayName": Main.CurrentGateway, "datePoint": Utils.FullDateFormat(Main.CurrentTime == null ? Main.StartDate : Main.CurrentTime) });
            try
            {
                var msg = await LogsPage.loadingLogs.promise();


                LogsPage.ShowStats(refresh);

                LogsPage.loadingLogs = null;
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
            }
            catch (ex)
            {
                    $("#logsContent").html("");
            }
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

} 