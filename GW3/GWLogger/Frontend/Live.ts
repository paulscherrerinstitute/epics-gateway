/// <reference path="../scripts/typings/kendo/kendo.all.d.ts" />

interface GatewayShortInformation
{
    Name?: string;
    CPU: number;
    Mem?: number;
    Searches: number;
    Build?: string;
    State: number;
    Version?: string;
    RunningTime?: string;
    Direction?: string;
}

interface HistoricData
{
    Value: number;
    Date: Date;
}

var DebugChannels = {
    'CPU': '{0}:CPU', 'Mem': '{0}:MEM-FREE', 'Searches': '{0}:SEARCH-SEC',
    'Build': '{0}:BUILD', 'Version': '{0}:VERSION', 'Messages': '{0}:MESSAGES-SEC',
    'PVs': '{0}:PVTOTAL', 'RunningTime': '{0}:RUNNING-TIME', 'NbClients': '{0}:NBCLIENTS',
    'NbServers': '{0}:NBSERVERS', 'Network': '({0}:NET-IN + {0}:NET-OUT) / (1024 * 1024)',
    'NbGets': '{0}:NBCAGET', 'NbPuts': '{0}:NBCAPUT', 'NbNewMons': '{0}:NBNEWCAMON',
    'NbMons': '{0}:NBCAMON', 'NbCreates': '{0}:NBCREATECHANNEL'
};
var ChannelsUnits = {
    'CPU': '%', 'Mem': 'Mb', 'Searches': '/ Sec', 'Messages': '/ Sec', 'Network': 'MB/Sec', 'NbGets': '/ Sec',
    'NbPuts': '/ Sec', 'NbNewMons': '/ Sec', 'NbMons': '/ Sec', 'NbCreates': '/ Sec'
};
var GatewayStates = { 0: 'All Ok', 1: 'Minor', 2: 'Warning', 3: 'Error' };

class Live
{
    static shortInfo: GatewayShortInformation[] = [];
    static cpuChart: LineGraph;
    static searchesChart: LineGraph;
    static pvsChart: LineGraph;
    static networkChart: LineGraph;
    static mustUpdate: number = 0;
    static currentErrors: string[] = [];

    static InitShortDisplay()
    {
        $(".GWDisplay").each((idx, elem) =>
        {
            $(elem).on("click", (evt) =>
            {
                var elem = evt.currentTarget ? evt.currentTarget : evt.target;
                while ((elem.localName ? elem.localName : elem.tagName) != "div" && elem.className.indexOf("GWDisplay") == -1)
                    elem = elem.parentElement;
                var gwName = elem.id;
                Live.ShowDetails(gwName);
            }).html("<canvas id='canvas_" + elem.id + "' width='100' height='100'></canvas><br>" + elem.id);
        });

        $(".GWDisplay").on("mouseover", (e) =>
        {
            var target = e.target

            while (target.className != "GWDisplay")
                target = target.parentElement;

            var html = "<b>Gateway " + target.id + "</b><br>";
            var live = Live.Get(target.id);
            html += "<table style='text-align: left;'>";
            html += "<tr><td>Version</td><td>" + (live ? live.Version : "") + "</td></tr>";
            html += "<tr><td>State</td><td>" + (live ? GatewayStates[live.State] : "") + "</td></tr>";
            html += "</table>";
            html += "-- Click to view details --";

            ToolTip.Show(target, "bottom", html);
        });

        Live.cpuChart = new LineGraph("cpuGraph", { Values: [] }, {
            MinY: 0,
            MaxY: 100,
            XLabelWidth: 50,
            FontSize: 10,
            PlotColor: '#000080',
            ToolTip: true,
            LabelFormat: Utils.ShortGWDateFormat,
            TooltipLabelFormat: Utils.GWDateFormat,
            OnClick: (label: Date, value) =>
            {
                Main.Path = "GW";
                Main.CurrentTime = label.toUtc();
                Main.StartDate = new Date(label.getTime() - 12 * 3600000);
                Main.EndDate = new Date(label.getTime() + 12 * 3600000);
                State.Set(true);
                State.Pop(null);
            }
        });
        Live.searchesChart = new LineGraph("searchesGraph", { Values: [] }, {
            MinY: 0,
            XLabelWidth: 50,
            FontSize: 10,
            PlotColor: '#000080',
            ToolTip: true,
            LabelFormat: Utils.ShortGWDateFormat,
            TooltipLabelFormat: Utils.GWDateFormat,
            OnClick: (label: Date, value) =>
            {
                Main.Path = "GW";
                Main.CurrentTime = label.toUtc();
                Main.StartDate = new Date(label.getTime() - 12 * 3600000);
                Main.EndDate = new Date(label.getTime() + 12 * 3600000);
                State.Set(true);
                State.Pop(null);
            }
        });
        Live.pvsChart = new LineGraph("pvsGraph", { Values: [] }, {
            MinY: 0,
            XLabelWidth: 50,
            FontSize: 10,
            PlotColor: '#000080',
            ToolTip: true,
            LabelFormat: Utils.ShortGWDateFormat,
            TooltipLabelFormat: Utils.GWDateFormat,
            OnClick: (label: Date, value) =>
            {
                Main.Path = "GW";
                Main.CurrentTime = label.toUtc();
                Main.StartDate = new Date(Main.CurrentTime.getTime() - 12 * 3600000);
                Main.EndDate = new Date(Main.CurrentTime.getTime() + 12 * 3600000);
                State.Set(true);
                State.Pop(null);
            }
        });
        Live.networkChart = new LineGraph("networkGraph", { Values: [] }, {
            MinY: 0,
            XLabelWidth: 50,
            FontSize: 10,
            PlotColor: '#000080',
            ToolTip: true,
            LabelFormat: Utils.ShortGWDateFormat,
            TooltipLabelFormat: Utils.GWDateFormat,
            OnClick: (label: Date, value) =>
            {
                Main.Path = "GW";
                Main.CurrentTime = label.toUtc();
                Main.StartDate = new Date(Main.CurrentTime.getTime() - 12 * 3600000);
                Main.EndDate = new Date(Main.CurrentTime.getTime() + 12 * 3600000);
                State.Set(true);
                State.Pop(null);
            }
        });
    }

    static ShowDetails(gwName: string)
    {
        Main.CurrentGateway = gwName.toLowerCase();
        State.Set();
        //Live.RefreshShort();
        $("#gatewayView").hide();
        $("#gatewayDetails").show();
        Live.mustUpdate = 0;
        Live.RefreshShort(null);

        $("#currentGW").html("Loading...");
        $("#gwInfos").html("");
        /*$("#inventoryLink").attr("href", "https://inventory.psi.ch/#action=Part&system=" + encodeURIComponent(Main.CurrentGateway.toUpperCase()));
        $("#logLink").attr("href", "/GW/" + Main.CurrentGateway);*/

        Live.cpuChart.SetDataSource({ Values: [] });
        Live.searchesChart.SetDataSource({ Values: [] });
        Live.pvsChart.SetDataSource({ Values: [] });
    }

    static JumpLogs()
    {
        Main.Path = "GW";
        State.Set(true);
        State.Pop(null);
    }

    static JumpList()
    {
        Main.Path = "Status";
        Main.CurrentGateway = null;
        State.Set(true);
        State.Pop(null);
    }

    static Get(gwName: string): GatewayShortInformation
    {
        for (var i = 0; i < Live.shortInfo.length; i++)
            if (Live.shortInfo[i].Name.toUpperCase() == gwName.toUpperCase())
                return Live.shortInfo[i];
        return null;
    }

    static RefreshShort(callback: () => void)
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
                Live.shortInfo.sort((a, b) =>
                {
                    if (a.Name > b.Name)
                        return 1;
                    if (a.Name < b.Name)
                        return -1;
                    return 0;
                });

                var errorDisplayed = false;
                var newErrors: string[] = [];
                var html = "";
                for (var i = 0; i < Live.shortInfo.length; i++)
                {
                    Map.SetGatewayState(Live.shortInfo[i].Name, Live.shortInfo[i].State, Live.shortInfo[i].Direction);
                    if (ToolTip.currentElement && ToolTip.currentElement.id && ToolTip.currentElement.id.startsWith("svg_gw"))
                        $("#toolTip").html(Map.GetTooltipText());

                    if (Live.shortInfo[i].State < 3)
                        continue;
                    if ((!Live.currentErrors || Live.currentErrors.indexOf(Live.shortInfo[i].Name) == -1) && errorDisplayed == false)
                    {
                        errorDisplayed = true;
                        Notifications.Show(Live.shortInfo[i].Name + " is on error.");
                    }
                    newErrors.push(Live.shortInfo[i].Name);
                    html += "<a href='/Status/" + Live.shortInfo[i].Name + "'>" + Live.shortInfo[i].Name + "</a>";
                }
                Live.currentErrors = newErrors;
                if (html == "")
                    html = "<span>No errors</span>";
                $("#errorsContent").html(html);

                if (!Main.CurrentGateway && Main.Path != "GW")
                    Live.DisplayShort();

                if (!Main.CurrentGateway)
                {
                    callback();
                    return;
                }

                $("#inventoryLink").attr("href", "https://inventory.psi.ch/#action=Part&system=" + encodeURIComponent(Main.CurrentGateway.toUpperCase()));
                $("#logLink").attr("href", "/GW/" + Main.CurrentGateway);

                $.ajax({
                    type: 'POST',
                    url: 'DataAccess.asmx/GetGatewayInformation',
                    data: JSON.stringify({ gatewayName: Main.CurrentGateway.toUpperCase() }),
                    contentType: 'application/json; charset=utf-8',
                    dataType: 'json',
                    success: function (msg)
                    {
                        $.ajax({
                            type: 'POST',
                            url: 'DataAccess.asmx/GetHistoricData',
                            data: JSON.stringify({ gatewayName: Main.CurrentGateway.toUpperCase() }),
                            contentType: 'application/json; charset=utf-8',
                            dataType: 'json',
                            success: function (msg)
                            {
                                if (callback)
                                    callback();

                                $("#currentGW").html(Main.CurrentGateway.toUpperCase());

                                var data: HistoricData[] = null;
                                try
                                {
                                    for (var i = 0; i < msg.d.length; i++)
                                    {
                                        switch (msg.d[i].Key)
                                        {
                                            case "CPU":
                                                data = msg.d[i].Value;
                                                Live.cpuChart.SetDataSource({
                                                    Values: data.map((c) =>
                                                    {
                                                        return { Label: Utils.DateFromNet(c.Date), Value: c.Value };
                                                    })
                                                });
                                                break;
                                            case "Searches":
                                                data = msg.d[i].Value;
                                                Live.searchesChart.SetDataSource({
                                                    Values: data.map((c) =>
                                                    {
                                                        return { Label: Utils.DateFromNet(c.Date), Value: c.Value };
                                                    })
                                                });
                                                break;
                                            case "PVs":
                                                data = msg.d[i].Value;
                                                Live.pvsChart.SetDataSource({
                                                    Values: data.map((c) =>
                                                    {
                                                        return { Label: Utils.DateFromNet(c.Date), Value: c.Value };
                                                    })
                                                });
                                                break;
                                            case "Network":
                                                data = msg.d[i].Value;
                                                Live.networkChart.SetDataSource({
                                                    Values: data.map((c) =>
                                                    {
                                                        return { Label: Utils.DateFromNet(c.Date), Value: c.Value / (1024 * 1024) };
                                                    })
                                                });
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                }
                                catch (ex)
                                {
                                }
                            },
                            error: () =>
                            {
                                Main.statsAreLoading = false;
                            }
                        });
                        var data: GatewayInformation = msg.d;
                        try
                        {
                            data.CPU = Math.round(data.CPU * 100) / 100;
                            data.RunningTime = data.RunningTime ? data.RunningTime.substr(0, data.RunningTime.lastIndexOf('.')).replace(".", " day(s) ") : "";

                            var html = "";
                            for (var i in data)
                            {
                                if (i == "__type" || i == "Name")
                                    continue;
                                html += "<tr><td>" + i + "</td><td" + (ChannelsUnits[i] ? "" : " colspan='2'") + ">" + data[i] + "</td>";
                                html += ChannelsUnits[i] ? "<td>" + ChannelsUnits[i] + "</td>" : "";
                                html += "</tr>";
                            }
                            $("#gwInfos").html(html);

                            $("#gwInfos tr").on("mouseenter", null, (e) =>
                            {
                                var target = e.target;
                                if (target.nodeName == "TD")
                                    target = target.parentElement;
                                ToolTip.Show(target, "bottom", "EPICS Channel Name:<br>" + DebugChannels[target.children[0].innerHTML].replace(/\{0\}/g, Main.CurrentGateway.toUpperCase()));
                            });
                        }
                        catch (ex)
                        {
                        }
                    },
                    error: () =>
                    {
                        Main.statsAreLoading = false;
                    }
                });
            },
            error: function (msg, textStatus)
            {
                Main.statsAreLoading = false;
                console.log(msg.responseText);
            }
        });
    }

    static DisplayDetails()
    {
    }

    static DisplayShort()
    {
        for (var i = 0; i < Live.shortInfo.length; i++)
        {
            Live.DisplayPaint("#canvas_" + Live.shortInfo[i].Name, Live.shortInfo[i]);
        }
    }

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