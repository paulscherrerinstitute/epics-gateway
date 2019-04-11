class StatusGraph
{
    static DisplayPaint(element: string, info: GatewayShortInformation)
    {
        var canvas = <HTMLCanvasElement>$(element)[0];
        if (!canvas)
            return;
        var ctx = canvas.getContext("2d");

        // Clear rect
        ctx.clearRect(0, 0, 100, 100);

        switch (info.State)
        {
            case 3:
                ctx.fillStyle = "#FFE0E0";
                break;
            case 2:
                ctx.fillStyle = "#ffca93";
                break;
            case 1:
                ctx.fillStyle = "#fff6af";
                break;
            case 0:
            default:
                ctx.fillStyle = "#FFFFFF";
        }

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
        if (info.Searches > 3000)
            ctx.strokeStyle = "#E00000";
        else if (info.Searches > 2000)
            ctx.strokeStyle = "#ff8300";
        else if (info.Searches > 1000)
            ctx.strokeStyle = "#ffe500";
        else
            ctx.strokeStyle = "#00E000";
        ctx.lineCap = "round";
        ctx.arc(50, 50, 39, 1.5 * Math.PI, 1.5 * Math.PI + (2 * Math.PI) * Math.min(4000, (info.Searches ? info.Searches : 0)) / 4000);
        ctx.stroke();

        // CPU
        ctx.beginPath();
        ctx.lineWidth = 5;
        if (info.CPU > 70 || info.CPU === null || info.CPU === undefined)
            ctx.strokeStyle = "#E00000";
        else if (info.CPU > 50)
            ctx.strokeStyle = "#ff8300";
        else if (info.CPU > 30)
            ctx.strokeStyle = "#ffe500";
        else
            ctx.strokeStyle = "#00E000";
        ctx.lineCap = "round";
        ctx.arc(50, 50, 45, 1.5 * Math.PI, 1.5 * Math.PI + (2 * Math.PI) * (info.CPU ? info.CPU : 0) / 100);
        ctx.stroke();

        ctx.lineCap = "butt";

        // CPU info
        var str = "" + (info.CPU ? Math.round(info.CPU) : "-");
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
        var str = "Srch " + (info.Searches ? info.Searches : "-");
        ctx.font = "10px Sans-serif";
        var w = ctx.measureText(str).width;
        ctx.lineWidth = 1;
        ctx.fillStyle = "#A0A0A0";
        ctx.fillText(str, 50 - w / 2, 60);
    }
}