class LogsPage
{
    static Sessions: GatewaySession[];
    static Logs: LogEntry[] = null;
    static Offset: number = null;
    static OffsetFile: string = null;
    static MessageTypes: string[] = [];
    static Stats: GatewayStats;
    private static loadingLogs: JQueryXHR = null;
    static Levels: string;
    static KeepOpenDetails: boolean = false;
    static SearchTimeout: number = null;

    public static Init(): void
    {
        LogsPage.LoadGateways();
        LogsPage.LoadMessageTypes();
        LogsPage.QueriesHelp();

        $("#gatewaySelector").on("change", LogsPage.GatewayChanged);
        $("#timeRangeCanvas").on("mousedown", StatsBarGraph.TimeLineSelected);

        $("#prevDay").click(() =>
        {
            LogsPage.Offset = null;
            LogsPage.OffsetFile = null;
            Main.StartDate = new Date(Main.StartDate.getTime() - 24 * 3600 * 1000);
            Main.EndDate = new Date(Main.StartDate.getTime() + 24 * 3600 * 1000);
            $("#startDate").val(Utils.FullUtcDateFormat(Main.StartDate));
            $("#endDate").val(Utils.FullUtcDateFormat(Main.EndDate));
            LogsPage.DelayedSearch(LogsPage.LoadLogStats);
        });
        $("#nextDay").click(() =>
        {
            LogsPage.Offset = null;
            LogsPage.OffsetFile = null;
            Main.StartDate = new Date(Main.StartDate.getTime() + 24 * 3600 * 1000);
            Main.EndDate = new Date(Main.StartDate.getTime() + 24 * 3600 * 1000);
            $("#startDate").val(Utils.FullUtcDateFormat(Main.StartDate));
            $("#endDate").val(Utils.FullUtcDateFormat(Main.EndDate));
            LogsPage.DelayedSearch(LogsPage.LoadLogStats);
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
                LogsPage.Offset = null;
                LogsPage.OffsetFile = null;
                LogsPage.DelayedSearch(LogsPage.LoadLogStats);
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
                LogsPage.Offset = null;
                LogsPage.OffsetFile = null;
                LogsPage.DelayedSearch(LogsPage.LoadLogStats);
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
                        $("#queryField").val(LogsPage.completedText(suggestionList.children(".selected").clone().children("span").remove().end().text()) + " ");
                    }
                default:
                    suggestionList.empty();
                    var suggestions = QueryParser.getProposals($("#queryField").val());
                    if (suggestions.length > 0)
                    {
                        suggestions.forEach(sug =>
                        {
                            suggestionList.append(`<div class="suggestion-item">${sug.suggestion.replace(sug.input, '<b>' + sug.input + '</b>')}<span class="hint">${typeof (sug.hint) != "undefined" ? sug.hint : ""} ${typeof (sug.dataType) != "undefined" ? ": " + sug.dataType : ""}</span></div>`);
                        });
                        suggestionList.children().each(function ()
                        {
                            var element = this;
                            element.onclick = (evt: JQueryEventObject) =>
                            {
                                if (element.classList.contains("suggestion-item"))
                                {
                                    $("#queryField").val(LogsPage.completedText($(element).clone().children("span").remove().end().text() + " "));
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
            LogsPage.Offset = null;
            LogsPage.OffsetFile = null;
            LogsPage.DelayedSearch(LogsPage.LoadTimeInfo);
        });

        $("#logFilter li").click(LogsPage.LogFilter);
    }

    public static async Refresh()
    {
        if (Main.Path == "GW" && Main.CurrentGateway)
        {
            await LogsPage.LoadSessions();
            await LogsPage.LoadLogStats(true);
        }
    }

    static async LoadMessageTypes()
    {
        try
        {
            var msg = await Utils.Loader("GetMessageTypes");
            var vals = <KeyValuePair[]>msg.d;
            for (var i = 0; i < vals.length; i++)
                LogsPage.MessageTypes[vals[i].Key] = vals[i].Value;
        }
        catch (ex)
        {
        }
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

    static async LoadLogStats(refresh: boolean = false)
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

        try
        {
            var msg = await Utils.Loader("GetStats", { "gatewayName": Main.CurrentGateway, start: Utils.FullUtcDateFormat(start), end: Utils.FullUtcDateFormat(end) });
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
        }
        catch (ex)
        {
        }
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
        {
            $("#logsContent").html("Loading...");
        }

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


                await LogsPage.ShowStats(refresh);

                LogsPage.loadingLogs = null;
                var data = <any[]>msg.d;

                if ($("#logsContent").data("kendoGrid"))
                {
                    $("#logsContent").data("kendoGrid").destroy();
                    $("#logsContent").empty();
                }
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
        LogsPage.Levels = tab.getAttribute("level");

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
            if (LogsPage.Levels)
                queryString += (queryString != "" ? "&" : "?") + "levels=" + LogsPage.Levels;
            if ($("#queryField").val())
                queryString += (queryString != "" ? "&" : "?") + "query=" + $("#queryField").val();
            if (LogsPage.Offset !== null)
                queryString += (queryString != "" ? "&" : "?") + "offset=" + this.Offset;
            if (LogsPage.OffsetFile !== null)
                queryString += (queryString != "" ? "&" : "?") + "filename=" + this.OffsetFile;
            url = '/Logs/' + Main.CurrentGateway + '/' + startDate.getTime() + '/' + endDate.getTime() + queryString;
        }


        if (!refresh)
        {
            $("#logsContent").html("Loading...");
            $("#serversContent").html("Loading...");
            $("#clientsContent").html("Loading...");
        }

        LogsPage.loadingLogs = Utils.LoaderXHR(url);
        try
        {
            var data = <any[]>(await LogsPage.loadingLogs.promise());
            await LogsPage.ShowStats(refresh);

            LogsPage.loadingLogs = null;
            var tz = (new Date()).getTimezoneOffset() * 60000;

            if ($("#logsContent").data("kendoGrid"))
            {
                $("#logsContent").data("kendoGrid").destroy();
                $("#logsContent").empty();
            }

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
                LogsPage.Logs = logs;
                LogsPage.Logs.forEach((r) =>
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
                        data: LogsPage.Logs
                    }
                });

                $("#logsContent").data("kendoGrid").table.on("mouseover",
                    (evt) =>
                    {
                        if (LogsPage.KeepOpenDetails)
                            return;
                        var row = <LogEntry>(<object>$("#logsContent").data("kendoGrid").dataItem($(evt.target).parent()));
                        var html = LogsPage.DetailInfo(row);
                        $("#detailInfo").html(html).show();
                    }).on("mouseout", () =>
                    {
                        if (LogsPage.KeepOpenDetails)
                            return;
                        $("#detailInfo").hide();
                    }).on("click", (evt) =>
                    {
                        var row = <LogEntry>(<object>$("#logsContent").data("kendoGrid").dataItem($(evt.target).parent()));
                        var html = LogsPage.DetailInfo(row);

                        if (LogsPage.KeepOpenDetails)
                        {
                            LogsPage.KeepOpenDetails = false;
                            $("#detailInfo").html(html);
                            return;
                        }
                        LogsPage.KeepOpenDetails = true;
                        html += "<div class='closeDetails' onclick='LogsPage.CloseDetails()'></div>";
                        $("#detailInfo").html(html).show();
                    });
            }

            // Scroll to bottom
            //if (Main.IsLast)
            if (!params["c"] && !params["s"])
                $("#logsContent div.k-grid-content").scrollTop($("#logsContent div.k-grid-content")[0].scrollHeight - $("#logsContent div.k-grid-content").height());
            else
                $("#logsContent").scrollTop(0);
        }
        catch (ex)
        {
        }
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

    static DetailInfo(row: LogEntry): string
    {
        if (!row)
            return "";
        var html = "";
        html += "<table>";
        html += "<tr><td>Message&nbsp;Type</td><td><span class='pseudoLink' onclick='LogsPage.AddFilter(event,\"type\");'>" + LogsPage.MessageTypes[row.Type] + "</span></td></tr>";
        html += "<tr><td>Date</td><td><span class='pseudoLink' onclick='LogsPage.AddFilter(event,\"date\");'>" + Utils.GWDateFormatMilis(new Date(<number>row.Date)) + "</span></td>";
        html += "<td>Remote</td><td><span class='pseudoLink' onclick='LogsPage.AddFilter(event,\"remote\");'>" + row.Remote + "</span></td></tr>";
        var n = 0;
        for (var i in row.Details)
        {
            if (typeof row[i] === "function" || typeof row[i] === "object" || i == "uid")
                continue;
            if (n == 0)
                html += "<tr>";
            else if (n % 2 == 0)
                html += "</tr><tr>";
            html += "<td>" + i.replace(/(\w)([A-Z][a-z])/g, "$1&nbsp;$2") + "</td><td><span class='pseudoLink' onclick='LogsPage.AddFilter(event,\"" + i + "\");'>" + row.Details[i] + "</span></td>";
            n++;
        }
        html += "</tr></table>";
        return html;
    }

    static CloseDetails()
    {
        LogsPage.KeepOpenDetails = false;
        $("#detailInfo").hide();
    }

    static async ShowStats(refresh: boolean)
    {
        if (!LogsPage.Stats || !LogsPage.Stats.Logs || LogsPage.Stats.Logs.length == 0)
        {
            $("#clientsContent").html("");
            $("#serversContent").html("");
            return;
        }

        var startDate = LogsPage.Stats.Logs[LogsPage.Stats.Logs.length - 1].Date;
        if (Main.CurrentTime)
            startDate = Main.CurrentTime;

        try
        {
            var clientsMsg = await Utils.Loader("ActiveClients", { "gatewayName": Main.CurrentGateway, "datePoint": Utils.FullDateFormat(startDate) });
            var serversMsg = await Utils.Loader("ActiveServers", { "gatewayName": Main.CurrentGateway, "datePoint": Utils.FullDateFormat(startDate) });

            if ($("#serversContent").data("kendoGrid"))
            {
                $("#serversContent").data("kendoGrid").destroy();
                $("#serversContent").empty();
            }
            $("#serversContent").html("").kendoGrid({
                columns: [
                    { title: "Server", field: "Key" },
                    { title: "Nb Actions", field: "Value" }],
                dataSource:
                {
                    data: serversMsg.d
                }
            });

            if ($("#clientsContent").data("kendoGrid"))
            {
                $("#clientsContent").data("kendoGrid").destroy();
                $("#clientsContent").empty();
            }
            $("#clientsContent").html("").kendoGrid({
                columns: [
                    { title: "Client", field: "Key" },
                    { title: "Nb Actions", field: "Value" }],
                dataSource:
                {
                    data: clientsMsg.d
                }
            });
        }
        catch (ex)
        {
        }
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
        LogsPage.Levels = evt.target.getAttribute("level");
        LogsPage.LoadTimeInfo();
    }

    static DelayedSearch(cb, fromPop: boolean = false)
    {
        if (LogsPage.SearchTimeout !== null)
            clearTimeout(LogsPage.SearchTimeout);
        LogsPage.SearchTimeout = setTimeout(() =>
        {
            if (!fromPop)
                State.Set();
            LogsPage.SearchTimeout = null;
            Main.IsLast = false;
            if (cb)
                cb();
        }, 500);
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
} 