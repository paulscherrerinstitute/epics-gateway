class WrongChannelsMenu
{
    static Show()
    {
        $("#reportView").show();
        $("#reportContent").removeAttr('style').removeClass().css({ "overflow": "auto", "padding": "5px" });
        $('#helpView').hide();

        if ($("#reportContent").data("kendoGrid"))
            $("#reportContent").data("kendoGrid").destroy();

        var html = "";

        html += "<center>";
        html += "<h2>Wrong Channel Search</h2>";
        html += "<table id='dbgEpicsTable'>";
        html += "<tr><td>EPICS Gateway</td><td><select id='dbgGateway'></select></td>";
        html += "</table><br/>";
        html += "<span class='button' onclick='WrongChannels.Check()' id='dbgRun'>Run Check</span>";
        html += "</center>";
        html += "<div id='wrgChannelSearch' class='fixed'></div>";

        $("#reportContent").html(html);

        var gateways = StatusPage.shortInfo.map((c) => c.Name).sort();
        $("#dbgGateway").kendoDropDownList({
            dataSource: gateways,
            value: (Main.CurrentGateway ? Main.CurrentGateway.toUpperCase() : null)
        });
    }

    static Check()
    {
        $("#dbgRun").hide();
        $("#wrgChannelSearch").html("Please wait, loading...");

        $.ajax({
            type: 'POST',
            url: '/DataAccess.asmx/BadClientConfig',
            data: JSON.stringify({ gatewayName: $("#dbgGateway").data("kendoDropDownList").value() }),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                var order = 0;
                var orderString = null;
                for (var i = 0; i < msg.d.length; i++)
                {
                    if (orderString != msg.d[i].Hostname)
                    {
                        order++;
                        orderString = msg.d[i].Hostname;
                    }
                    msg.d[i].SortOrder = order;
                }

                /*for (var i = 0; i < msg.d.length; i++)
                    msg.d[i].SortOrder = i;*/

                $("#dbgRun").show();
                $("#wrgChannelSearch").html("").kendoGrid({
                    dataSource:
                    {
                        schema: {
                            model: {
                                fields: {
                                    SortOrder: { type: "number" },
                                    Hostname: { type: "string" },
                                    Channel: { type: "string" },
                                    Count: { type: "number" }
                                }
                            }
                        },
                        data: msg.d,
                        group: { field: "SortOrder", aggregates: [{ field: "Count", aggregate: "sum" }, { field: "Hostname", aggregate: "min" }] },
                        sort: [{ field: "Count", dir: "desc" }, { field: "Channel", dir: "asc" }]
                    },
                    sortable: true,
                    columns: [
                        { field: "SortOrder", hidden: true, groupHeaderTemplate: "Hostname: #= aggregates.Hostname.min #, Total Wrong Channels: #= aggregates.Count.sum#" },
                        { title: "Hostname", field: "Hostname" },
                        { title: "Channel", field: "Channel" },
                        { title: "Count", field: "Count" }],
                    dataBound: function (e)
                    {
                        var grid = this;
                        $(".k-grouping-row").each(function (e)
                        {
                            grid.collapseGroup(this);
                        });
                    }
                });
            },
            error: function (msg, textStatus)
            {
                $("#dbgRun").show();
                $("#wrgChannelSearch").html("Error: " + msg.responseText);
            }
        });
    }
}