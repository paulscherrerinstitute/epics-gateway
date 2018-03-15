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
        var maxEValue = 1;
        for (var i = 0; i < Main.Stats.Errors.length; i++)
            maxEValue = Math.max(maxEValue, Main.Stats.Errors[i].Value); var maxNValue = 1;
        for (var i = 0; i < Main.Stats.Searches.length; i++)
            maxNValue = Math.max(maxNValue, Main.Stats.Searches[i].Value);

        /*ctx.fillStyle = "#FFFFFF00";
        ctx.fillRect(0, 100, width, 3);*/

        var prevErrorValY: number = null;

        for (var i = 0; i < 145; i++)
        {
            var dt = new Date(Main.EndDate.getTime() - i * 10 * 60 * 1000);

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
                ctx.moveTo(Math.round(width - (i - 0.5) * w) + 0.5, Math.round(prevErrorValY) + 0.5);
                ctx.lineTo(Math.round(width - (i + 0.5) * w) + 0.5, Math.round(bv) + 0.5);
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
        var w = ctx.measureText(dts).width;
        ctx.fillStyle = "rgba(255,255,255,0.7)";
        ctx.fillRect(width - (w + 5), b + 2, w + 2, 16);
        ctx.fillStyle = "#000000";
        ctx.fillText(dts, width - (w + 5), b + 14);

        var dts = Utils.ShortGWDateFormat(new Date(Main.EndDate.getTime() - 72 * 10 * 60 * 1000));
        var w = ctx.measureText(dts).width;
        ctx.fillStyle = "rgba(255,255,255,0.7)";
        ctx.fillRect(width / 2 - w / 2, b + 2, w + 2, 16);
        ctx.fillStyle = "#000000";
        ctx.fillText(dts, width / 2 - w / 2, b + 14);

        var dts = Utils.ShortGWDateFormat(new Date(Main.EndDate.getTime() - 144 * 10 * 60 * 1000));
        var w = ctx.measureText(dts).width;
        ctx.fillStyle = "rgba(255,255,255,0.7)";
        ctx.fillRect(5, b + 2, w + 2, 16);
        ctx.fillStyle = "#000000";
        ctx.fillText(dts, 5, b + 14);


        ctx.fillStyle = "#E0E0E0";
        ctx.fillRect(0, Math.round(b), width, 1);
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
                for (var i = 0; i < Main.Sessions.length; i++)
                {
                    html += "<tr><td>" + Utils.GWDateFormat(Main.Sessions[i].StartDate) + "</td>";
                    html += "<td>" + Utils.GWDateFormat(Main.Sessions[i].EndDate) + "</td>";
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