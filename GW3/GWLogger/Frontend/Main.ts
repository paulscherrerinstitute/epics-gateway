// Main class...
class Main
{
    static CurrentGateway: string;
    static Sessions: GatewaySession[];

    static LoadGateways()
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
                $('#gatewaySelector').find('option').remove().end().append(options).val(gateways[0]);

                Main.CurrentGateway = gateways[0];
                Main.GatewaySelected();
            },
            error: function (msg, textStatus)
            {
                console.log(msg.responseText);
            }
        });
    }

    static GatewaySelected()
    {
        Main.LoadSessions();
        Main.LoadLogStats();
    }

    static LoadLogStats()
    {
        var start = new Date((new Date()).getTime() - (24 * 3600 * 1000));
        var end = new Date();
        $.ajax({
            type: 'POST',
            url: 'DataAccess.asmx/GetLogStats',
            data: JSON.stringify({ "gatewayName": Main.CurrentGateway, start: Utils.FullDateFormat(start), end: Utils.FullDateFormat(end) }),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                console.log("here");
            },
            error: function (msg, textStatus)
            {
                console.log(msg.responseText);
            }
        });
    }

    static LoadSessions()
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
                for (var i = 0; i < Main.Sessions.length; i++)
                {
                    html += "<tr><td>" + Utils.FullDateFormat(Main.Sessions[i].StartDate) + "</td>";
                    html += "<td>" + Utils.FullDateFormat(Main.Sessions[i].EndDate) + "</td>";
                    html += "<td>" + Main.Sessions[i].NbEntries + "</td></tr>";
                }
                html += "</table>";
                $("#gatewaySection").html(html);
            },
            error: function (msg, textStatus)
            {
                console.log(msg.responseText);
            }
        });
    }

    static Init()
    {
        Main.LoadGateways();
        $("#gatewaySelector").on("change", Main.GatewaySelected);
    }
}

$(Main.Init); // Starting Main GUI tasks