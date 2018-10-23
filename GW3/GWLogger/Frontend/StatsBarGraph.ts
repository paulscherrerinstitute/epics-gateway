class StatsBarGraph
{
    static DrawStats(): void
    {
        if (!Main.Stats || !Main.Stats.Logs || !Main.Stats.Logs.length)
            return;

        var end = Main.Stats.Logs[Main.Stats.Logs.length - 1].Date;

        var canvas = (<HTMLCanvasElement>$("#timeRangeCanvas")[0]);
        var ctx = canvas.getContext("2d");
        var width = $("#timeRange").width() - 40;
        var height = $("#timeRange").height() - 18;

        canvas.width = width;
        canvas.height = height;

        //var w = width / 145;
        var w = width / Main.Stats.Logs.length;
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

        var prevErrorValY: number = null;

        //for (var i = 0; i < 145; i++)
        for (var i = 0; i < Main.Stats.Logs.length; i++)
        {
            var dt = new Date(end.getTime() - i * 10 * 60 * 1000);

            var val = StatsBarGraph.GetStat('Logs', dt);
            ctx.fillStyle = "#80d680";
            ctx.strokeStyle = "#528c52";
            var bv = b * val / maxValue;
            ctx.fillRect(Math.round(width - (i + 1) * w), Math.round(b - bv), Math.ceil(w), Math.round(bv));
            ctx.beginPath();
            ctx.moveTo(Math.round(width - (i + 1) * w) + 1.5, Math.round(b - bv) + 0.5);
            ctx.lineTo(Math.round(width - i * w) + 0.5, Math.round(b - bv) + 0.5);
            ctx.lineTo(Math.round(width - i * w) + 0.5, Math.round(b) + 0.5);
            ctx.stroke();

            var val = StatsBarGraph.GetStat('Errors', dt);
            var bv = b * val / maxEValue;
            if (prevErrorValY != null)
            {
                ctx.strokeStyle = "#cc8282";
                ctx.lineWidth = 2;
                ctx.beginPath();
                ctx.moveTo(Math.round(width - (i - 0.5) * w) + 0.5, Math.round(b - prevErrorValY) + 0.5);
                ctx.lineTo(Math.round(width - (i + 0.5) * w) + 0.5, Math.round(b - bv) + 0.5);
                ctx.stroke();
                ctx.lineWidth = 1;
            }
            prevErrorValY = bv;


            val = StatsBarGraph.GetStat('Searches', dt);
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

        var dts = Utils.ShortGWDateFormat(new Date(end.getTime() - end.getTimezoneOffset() * 60000));
        var tw = ctx.measureText(dts).width;
        ctx.fillStyle = "rgba(255,255,255,0.7)";
        ctx.fillRect(width - (tw + 5), b + 2, tw + 2, 16);
        ctx.fillStyle = "#000000";
        ctx.fillText(dts, width - (tw + 5), b + 14);

        var dts = Utils.ShortGWDateFormat(new Date(end.getTime() - end.getTimezoneOffset() * 60000 - Main.Stats.Logs.length / 2 * 10 * 60 * 1000));
        var tw = ctx.measureText(dts).width;
        ctx.fillStyle = "rgba(255,255,255,0.7)";
        ctx.fillRect(width / 2 - tw / 2, b + 2, tw + 2, 16);
        ctx.fillStyle = "#000000";
        ctx.fillText(dts, width / 2 - tw / 2, b + 14);

        var dts = Utils.ShortGWDateFormat(new Date(end.getTime() - end.getTimezoneOffset() * 60000 - Main.Stats.Logs.length * 10 * 60 * 1000));
        var tw = ctx.measureText(dts).width;
        ctx.fillStyle = "rgba(255,255,255,0.7)";
        ctx.fillRect(5, b + 2, tw + 2, 16);
        ctx.fillStyle = "#000000";
        ctx.fillText(dts, 5, b + 14);

        ctx.fillStyle = "#E0E0E0";
        ctx.fillRect(0, Math.round(b), width, 1);

        if (Main.CurrentTime)
        {
            //var tdiff = (end.getTime() - Main.CurrentTime.getTime()) + end.getTimezoneOffset() * 60000;
            //var tdiff = (end.getTime() + 10 * 60 * 1000 - Main.CurrentTime.getTime());
            var tdiff = (end.getTime() - Main.CurrentTime.getTime());
            var t = tdiff / (10 * 60 * 1000);
            var x = width - Math.floor(t * w + w / 2);
            //var x = width - Math.floor(t * w);

            ctx.lineWidth = 1;
            ctx.strokeStyle = "rgba(0,0,255,0.7)";
            ctx.beginPath();
            ctx.moveTo(x + 0.5, 0);
            ctx.lineTo(x + 0.5, height);
            ctx.stroke();
        }

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


    static TimeLineSelected(evt: JQueryMouseEventObject): void
    {
        StatsBarGraph.TimeLineMouse(evt);

        var up = () =>
        {
            $("#mouseCapture").hide().off("mousemove", StatsBarGraph.TimeLineMouse).off("mouseup").off("mouseleave");
        }

        $("#mouseCapture").show().on("mousemove", StatsBarGraph.TimeLineMouse).on("mouseup", up).on("mouseleave", up);
    }

    static TimeLineMouse(evt: JQueryMouseEventObject): void
    {
        var width = $("#timeRangeCanvas").width();
        var w = width / Main.Stats.Logs.length;
        //var x = evt.pageX - ($("#timeRange").position().left + $("#timeRange div").width());
        var x = evt.pageX - $("#timeRange").position().left - 10;

        var tx = Math.ceil((width - x) / w);
        if (tx < 0)
            tx = 0;
        if (tx >= Main.Stats.Logs.length)
            tx = Main.Stats.Logs.length - 1;
        //console.log(tx);


        if (!Main.EndDate)
        {
            Main.EndDate = new Date();
            Main.StartDate = new Date(Main.EndDate.getTime() - 24 * 3600 * 1000);
        }

        Main.CurrentTime = new Date(Main.Stats.Logs[Main.Stats.Logs.length - 1].Date.getTime() - tx * 10 * 60 * 1000);
        //Main.CurrentTime = new Date(Main.EndDate.getTime() - tx * 10 * 60 * 1000);
        //Main.CurrentTime = new Date((Main.EndDate.getTime() + Main.EndDate.getTimezoneOffset() * 60000) - tx * 10 * 60 * 1000);
        if (tx == 0 && ((new Date()).getTime() - Main.CurrentTime.getTime()) < 24 * 3600 * 1000)
            Main.IsLast = true;
        else
            Main.IsLast = false;
        State.Set();
        Main.Offset = null;
        Main.OffsetFile = null;

        Main.LoadTimeInfo();
    }
}