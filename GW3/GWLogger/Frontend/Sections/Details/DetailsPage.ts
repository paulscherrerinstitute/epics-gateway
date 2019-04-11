﻿/// <reference path="../../../scripts/typings/promise/promise.d.ts" />
/// <reference path="../../../scripts/typings/kendo/kendo.all.d.ts" />

interface HistoricData
{
    Value: number;
    Date: Date;
}

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

class DetailPage
{
    static cpuChart: LineGraph;
    static searchesChart: LineGraph;
    static pvsChart: LineGraph;
    static networkChart: LineGraph;
    static mustUpdate: number = 0;
    static currentErrors: string[] = [];

    public static Init()
    {
        DetailPage.cpuChart = new LineGraph("cpuGraph", { Values: [] }, {
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
        DetailPage.searchesChart = new LineGraph("searchesGraph", { Values: [] }, {
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
        DetailPage.pvsChart = new LineGraph("pvsGraph", { Values: [] }, {
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
        DetailPage.networkChart = new LineGraph("networkGraph", { Values: [] }, {
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

    public static Show(gwName: string)
    {
        Main.CurrentGateway = gwName.toLowerCase();
        State.Set();
        $("#gatewayView").hide();
        $("#gatewayDetails").show();
        DetailPage.mustUpdate = 0;

        $("#currentGW").html("Loading...");
        $("#gwInfos").html("");

        DetailPage.cpuChart.SetDataSource({ Values: [] });
        DetailPage.searchesChart.SetDataSource({ Values: [] });
        DetailPage.pvsChart.SetDataSource({ Values: [] });
    }

    public static async LoadDetails()
    {
        $("#inventoryLink").attr("href", "https://inventory.psi.ch/#action=Part&system=" + encodeURIComponent(Main.CurrentGateway.toUpperCase()));
        $("#logLink").attr("href", "/GW/" + Main.CurrentGateway);

        await DetailPage.GetInformation();
        await DetailPage.GetHistoricData();
    }

    private static async GetInformation()
    {
        try
        {
            var msg = await Utils.Loader('GetGatewayInformation', { gatewayName: Main.CurrentGateway.toUpperCase() });
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
        }
        catch (ex)
        {
        }
    }

    private static async GetHistoricData()
    {
        try
        {
            var msg = await Utils.Loader("GetHistoricData", { gatewayName: Main.CurrentGateway.toUpperCase() });
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
                            DetailPage.cpuChart.SetDataSource({
                                Values: data.map((c) =>
                                {
                                    return { Label: Utils.DateFromNet(c.Date), Value: c.Value };
                                })
                            });
                            break;
                        case "Searches":
                            data = msg.d[i].Value;
                            DetailPage.searchesChart.SetDataSource({
                                Values: data.map((c) =>
                                {
                                    return { Label: Utils.DateFromNet(c.Date), Value: c.Value };
                                })
                            });
                            break;
                        case "PVs":
                            data = msg.d[i].Value;
                            DetailPage.pvsChart.SetDataSource({
                                Values: data.map((c) =>
                                {
                                    return { Label: Utils.DateFromNet(c.Date), Value: c.Value };
                                })
                            });
                            break;
                        case "Network":
                            data = msg.d[i].Value;
                            DetailPage.networkChart.SetDataSource({
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
        }
        catch (ex)
        {
        }
    }
}
