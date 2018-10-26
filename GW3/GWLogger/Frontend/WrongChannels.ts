class WrongChannels
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
        html += "<div id='wrgChannelSearch' class='fixed'>Not yet implemented sorry...</div>";

        $("#reportContent").html(html);

        var gateways = Live.shortInfo.map((c) => c.Name).sort();
        $("#dbgGateway").kendoDropDownList({
            dataSource: gateways,
            value: (Main.CurrentGateway ? Main.CurrentGateway.toUpperCase() : null)
        });
    }

    static Check()
    {
        $("#wrgChannelSearch").html("<b>Not yet implemented sorry...</b>");

        //$("#wrgChannelSearch");
    }
}