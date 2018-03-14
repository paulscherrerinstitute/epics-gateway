// Main class...
class Main
{
    static CurrentGateway: string;
    static Sessions: GatewaySession[];
    static Stats: GatewayStats;
    static EndDate: Date;

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

    static GatewaySelected(): void
    {
        Main.LoadSessions();
        Main.LoadLogStats();
    }

    static LoadLogStats(): void
    {
        //var start = new Date((new Date()).getTime() - (24 * 3600 * 1000));
        var start = new Date(2018, 2, 12);
        var end = new Date();
        $.ajax({
            type: 'POST',
            url: 'DataAccess.asmx/GetStats',
            data: JSON.stringify({ "gatewayName": Main.CurrentGateway, start: Utils.FullDateFormat(start), end: Utils.FullDateFormat(end) }),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                Main.Stats = GatewayStats.CreateFromObject(msg.d);
                if (Main.Stats.Logs && Main.Stats.Logs.length > 0)
                    Main.EndDate = Main.Stats.Logs[Main.Stats.Logs.length - 1].Date;
                else
                    Main.EndDate = new Date();
                Main.DrawStats();
            },
            error: function (msg, textStatus)
            {
                console.log(msg.responseText);
            }
        });
    }

    static DrawStats(): void
    {
        var canvas = (<HTMLCanvasElement>$("#timeRangeCanvas")[0]);
        var ctx = canvas.getContext("2d");
        var width = $("#timeRange").width();
        var height = $("#timeRange").height();

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
        var maxNValue = 1;
        for (var i = 0; i < Main.Stats.Searches.length; i++)
            maxNValue = Math.max(maxNValue, Main.Stats.Searches[i].Value);

        /*ctx.fillStyle = "#FFFFFF00";
        ctx.fillRect(0, 100, width, 3);*/

        for (var i = 0; i < 145; i++)
        {
            var dt = new Date(Main.EndDate.getTime() - i * 10 * 60 * 1000);

            var val = Main.GetStat('Logs', dt);
            ctx.fillStyle = "#00FF00";
            ctx.strokeStyle = "#00E000";
            var bv = b * val / maxValue;
            ctx.fillRect(Math.round(width - (i + 1) * w), Math.round(b - bv), Math.ceil(w), Math.round(bv));
            ctx.beginPath();
            ctx.moveTo(Math.round(width - (i + 1) * w) + 1.5, Math.round(b - bv) + 0.5);
            ctx.lineTo(Math.round(width - i * w) + 0.5, Math.round(b - bv) + 0.5);
            ctx.lineTo(Math.round(width - i * w) + 0.5, Math.round(b) + 0.5);
            ctx.stroke();

            var val = Main.GetStat('Errors', dt);
            ctx.fillStyle = "#E00000";
            ctx.strokeStyle = "#800000";
            var bv = b * val / maxValue;
            ctx.fillRect(Math.round(width - (i + 1) * w), Math.round(b - bv), Math.ceil(w), Math.round(bv));
            ctx.beginPath();
            ctx.moveTo(Math.round(width - (i + 1) * w) + 1.5, Math.round(b - bv) + 0.5);
            ctx.lineTo(Math.round(width - i * w) + 0.5, Math.round(b - bv) + 0.5);
            ctx.lineTo(Math.round(width - i * w) + 0.5, Math.round(b) + 0.5);
            ctx.stroke();

            val = Main.GetStat('Searches', dt);
            ctx.fillStyle = "#00A000";
            var bv = bn * val / maxNValue;
            ctx.fillRect(Math.round(width - (i + 1) * w), Math.round(b), Math.ceil(w), Math.round(bv));
            ctx.beginPath();
            ctx.strokeStyle = "#006000";
            ctx.moveTo(Math.round(width - (i + 1) * w) + 1.5, Math.round(b + bv) + 0.5);
            ctx.lineTo(Math.round(width - i * w) + 0.5, Math.round(b + bv) + 0.5);
            ctx.lineTo(Math.round(width - i * w) + 0.5, Math.round(b) + 0.5);
            ctx.stroke();
        }

        ctx.fillStyle = "#E0E0E0";
        ctx.fillRect(0, Math.round(b), width, 1);
    }

    static GetStat(logType: string, when: Date): number
    {
        if (!Main.Stats || !Main.Stats[logType])
            return null;
        var s: LogStat[] = Main.Stats[logType];
        for (var i = 0; i < s.length; i++)
            if (s[i].Date.getTime() == when.getTime())
                return s[i].Value;
        return null;
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
                for (var i = 0; i < Main.Sessions.length; i++)
                {
                    html += "<tr><td>" + Utils.FullDateFormat(Main.Sessions[i].StartDate) + "</td>";
                    html += "<td>" + Utils.FullDateFormat(Main.Sessions[i].EndDate) + "</td>";
                    html += "<td>" + Main.Sessions[i].NbEntries + "</td></tr>";
                }
                html += "</table>";
                $("#gatewaySessions").html(html);
            },
            error: function (msg, textStatus)
            {
                console.log(msg.responseText);
            }
        });
    }

    static Resize(): void
    {
        Main.DrawStats();
    }

    static Init(): void
    {
        Main.LoadGateways();
        $("#gatewaySelector").on("change", Main.GatewaySelected);
        $(window).on("resize", Main.Resize);
    }
}

$(Main.Init); // Starting Main GUI tasks