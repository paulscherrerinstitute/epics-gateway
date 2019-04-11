/*class Patterns
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
        html += "<h2>Odd Gateway Pattern</h2>";
        html += "<table id='dbgEpicsTable'>";
        html += "<tr><td>EPICS Gateway</td><td><select id='dbgGateway'></select></td>";
        html += "</table><br/>";
        html += "<span class='button' onclick='Patterns.Check()' id='dbgRun'>Run Check</span>";
        html += "</center>";
        html += "<div id='wrgChannelSearch' class='fixed'></div>";

        $("#reportContent").html(html);

        var gateways = Live.shortInfo.map((c) => c.Name).sort();
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
            url: '/DataAccess.asmx/GetAllStats',
            data: JSON.stringify({ gatewayName: $("#dbgGateway").data("kendoDropDownList").value() }),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                $("#dbgRun").show();
                var statTypes = ['Logs', 'Searches', 'Errors', 'CPU', 'PVs', 'Clients', 'Servers', 'MsgSecs'];
                var data: GatewayStats[] = msg.d;

                var avgs = {};
                for (var i = 0; i < statTypes.length; i++)
                    for (var j = 0; j < data.length; j++)
                        avgs[statTypes[i]] = data[j][statTypes[i]].mean('Value');
            }
        });
    }
}*/