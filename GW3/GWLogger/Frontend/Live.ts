interface GatewayShortInformation
{
    Name: string;
    Cpu: number;
    Mem: number;
    Searches: number;
    Build: string;
}

interface HistoricData
{
    Value: number;
    Date: Date;
}

class Live
{
    static Detail: string = null;
    static shortInfo: GatewayShortInformation[] = [];
    static cpuChart: LineGraph;
    static searchesChart: LineGraph;
    static pvsChart: LineGraph;
    static mustUpdate: number = 0;

    static InitShortDisplay()
    {
        $(".GWDisplay").each((idx, elem) =>
        {
            $(elem).on("click", (evt) =>
            {
                var elem = evt.target;
                while (elem.tagName != "div" && elem.className != "GWDisplay")
                    elem = elem.parentElement;
                var gwName = elem.id;
                Live.ShowDetails(gwName);
            }).html("<canvas id='canvas_" + elem.id + "' width='100' height='100'></canvas><br>" + elem.id);
        });

        Live.cpuChart = new LineGraph("cpuGraph", { Values: [] }, { MinY: 0, MaxY: 100 });
        Live.searchesChart = new LineGraph("searchesGraph", { Values: [] }, { MinY: 0 });
        Live.pvsChart = new LineGraph("pvsGraph", { Values: [] }, { MinY: 0 });
    }

    static ShowDetails(gwName: string)
    {
        Live.Detail = gwName;
        Live.RefreshShort();
        $("#gatewayView").hide();
        $("#gatewayDetails").show();
        Live.mustUpdate = 0;
    }

    static RefreshShort()
    {
        if (Live.Detail)
        {
            this.mustUpdate--;
            if (this.mustUpdate >= 0)
                return;
            this.mustUpdate = 5;
            $.ajax({
                type: 'POST',
                url: 'DataAccess.asmx/GetHistoricData',
                data: JSON.stringify({ gatewayName: Live.Detail }),
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                success: function (msg)
                {
                    var data: HistoricData[] = null;
                    for (var i = 0; i < msg.d.length; i++)
                    {
                        switch (msg.d[i].Key)
                        {
                            case "CPU":
                                data = msg.d[i].Value;
                                Live.cpuChart.SetDataSource({
                                    Values: data.map((c) =>
                                    {
                                        return { Label: Utils.ShortGWDateFormat(Utils.DateFromNet(c.Date)), Value: c.Value };
                                    })
                                });
                                break;
                            case "Searches":
                                data = msg.d[i].Value;
                                Live.searchesChart.SetDataSource({
                                    Values: data.map((c) =>
                                    {
                                        return { Label: Utils.ShortGWDateFormat(Utils.DateFromNet(c.Date)), Value: c.Value };
                                    })
                                });
                                break;
                            case "PVs":
                                data = msg.d[i].Value;
                                Live.pvsChart.SetDataSource({
                                    Values: data.map((c) =>
                                    {
                                        return { Label: Utils.ShortGWDateFormat(Utils.DateFromNet(c.Date)), Value: c.Value };
                                    })
                                });
                                break;
                            default:
                                break;
                        }
                    }
                }
            });
        }
        else
        {
            $.ajax({
                type: 'POST',
                url: 'DataAccess.asmx/GetShortInformation',
                data: JSON.stringify({}),
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                success: function (msg)
                {
                    Live.shortInfo = msg.d;
                    Live.DisplayShort();
                },
                error: function (msg, textStatus)
                {
                    console.log(msg.responseText);
                }
            });
        }
    }

    static DisplayDetails()
    {
    }

    static DisplayShort()
    {
        for (var i = 0; i < Live.shortInfo.length; i++)
        {
            var canvas = <HTMLCanvasElement>$("#canvas_" + Live.shortInfo[i].Name)[0];
            if (!canvas)
                continue;
            var ctx = canvas.getContext("2d");

            // Clear rect
            ctx.clearRect(0, 0, 100, 100);

            if (Live.shortInfo[i].Cpu > 70 || Live.shortInfo[i].Cpu === null || Live.shortInfo[i].Cpu === undefined)
                ctx.fillStyle = "#FFE0E0";
            else if (Live.shortInfo[i].Cpu > 50)
                ctx.fillStyle = "#ffca93";
            else if (Live.shortInfo[i].Cpu > 30)
                ctx.fillStyle = "#fff6af";
            else
                ctx.fillStyle = "#FFFFFF";

            ctx.beginPath();
            ctx.lineWidth = 15;
            ctx.arc(50, 50, 42, 0, Math.PI * 2);
            ctx.fill();

            ctx.fillStyle = "#000000";

            // Background arc
            ctx.beginPath();
            ctx.lineWidth = 15;
            ctx.strokeStyle = "#C0C0C0";
            ctx.arc(50, 50, 42, 0, Math.PI * 2);
            ctx.stroke();

            // Searches
            ctx.beginPath();
            ctx.lineWidth = 5;
            if (Live.shortInfo[i].Searches > 90)
                ctx.strokeStyle = "#E00000";
            else if (Live.shortInfo[i].Searches > 50)
                ctx.strokeStyle = "#ff8300";
            else if (Live.shortInfo[i].Searches > 30)
                ctx.strokeStyle = "#ffe500";
            else
                ctx.strokeStyle = "#00E000";
            ctx.arc(50, 50, 39, 1.5 * Math.PI, 1.5 * Math.PI + (2 * Math.PI) * Math.min(100, (Live.shortInfo[i].Searches ? Live.shortInfo[i].Searches : 0)) / 100);
            ctx.stroke();

            // CPU
            ctx.beginPath();
            ctx.lineWidth = 5;
            if (Live.shortInfo[i].Cpu > 70 || Live.shortInfo[i].Cpu === null || Live.shortInfo[i].Cpu === undefined)
                ctx.strokeStyle = "#E00000";
            else if (Live.shortInfo[i].Cpu > 50)
                ctx.strokeStyle = "#ff8300";
            else if (Live.shortInfo[i].Cpu > 30)
                ctx.strokeStyle = "#ffe500";
            else
                ctx.strokeStyle = "#00E000";
            ctx.arc(50, 50, 45, 1.5 * Math.PI, 1.5 * Math.PI + (2 * Math.PI) * (Live.shortInfo[i].Cpu ? Live.shortInfo[i].Cpu : 0) / 100);
            ctx.stroke();

            // CPU info
            var str = "" + (Live.shortInfo[i].Cpu ? Math.round(Live.shortInfo[i].Cpu) : "-");
            ctx.font = "20px Sans-serif";
            var w = ctx.measureText(str).width;
            ctx.lineWidth = 1;
            ctx.fillStyle = "#000000";
            ctx.fillText(str, 50 - w / 2, 50);
            ctx.font = "10px Sans-serif";
            ctx.fillStyle = "#A0A0A0";
            ctx.fillText(" %", 50 + w / 2, 50);
            ctx.fillText("CPU", 50 - w / 2 - ctx.measureText("CPU").width, 50);

            // searches info
            var str = "Srch " + (Live.shortInfo[i].Searches ? Live.shortInfo[i].Searches : "-");
            ctx.font = "10px Sans-serif";
            var w = ctx.measureText(str).width;
            ctx.lineWidth = 1;
            ctx.fillStyle = "#A0A0A0";
            ctx.fillText(str, 50 - w / 2, 60);
        }
    }
}