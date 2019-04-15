﻿class AnomaliesPage
{
    private static IsLoadingAnomalyInfos = false;
    private static IsLoadingAnomalyDetail = false;
    private static LastAnomalyInfos: GraphAnomalyInfo[] = [];
    private static CurrentPreview: string = null;
    private static PreviewGraph: LineGraph  = null;

    public static async Show() {
        if (Main.Path != "Anomalies")
            return;

        if (Main.DetailAnomaly == null) {
            await this.ShowOverviewSection();
        } else {
            this.CurrentPreview = null;
            await this.ShowDetailSection(Main.DetailAnomaly);
        }
    }

    public static async Refresh() {
        await this.Show();
    }

    private static async ShowOverviewSection() {
        if (this.IsLoadingAnomalyInfos)
            return;

        this.IsLoadingAnomalyInfos = true;

        var msg = null;
        try {
            msg = await Utils.Loader("GetGraphAnomalies", null);
        } catch{
            this.IsLoadingAnomalyInfos = false;
            return;
        }

        if (msg == null || msg.d == null)
            return;

        var anomalyInfos: GraphAnomalyInfo[] = msg.d.map(v => GraphAnomalyInfo.CreateFromObject(v)); // Fix dates
        this.LastAnomalyInfos = anomalyInfos;

        this.IsLoadingAnomalyInfos = false;
        if (Main.DetailAnomaly != null)
            return;



        var elem = $("#anomaly-overview-preview");
        if (elem == null || elem.length == 0) {
            $("#anomalyView").html(`
                <div id="anomaly-overview-preview"></div>
                <div id="anomaly-overview-grid"></div>
            `);
        }

        if (anomalyInfos.length > 0 && this.CurrentPreview == null) {
            this.ShowAnomalyPreview(anomalyInfos[0].FileName);
        }

        var prevGrid = $("#anomaly-overview-grid").data("kendoGrid");
        if (prevGrid)
            prevGrid.destroy();

        var scrollPosition = $("#anomaly-overview-grid").scrollTop();
        $("#anomaly-overview-grid").kendoGrid({
            columns: [
                {
                    title: "From",
                    field: "From",
                    format: "{0:MM/dd HH:mm:ss}",
                },
                {
                    title: "Gateway",
                    field: "Name",
                },
                {
                    title: "Duration",
                    field: "Duration",
                }
            ],
            sortable: false,
            selectable: "row",
            scrollable: true,
            height: "100%",
            rowTemplate: `
                <tr data-uid="#= uid #" onmouseover="AnomaliesPage.ShowAnomalyPreview('#: FileName #')">
                    <td>#: Utils.FullDateFormat(From) #</td>
                    <td>#: Name #</td>
                    <td>#: Duration #</td>
                </tr>`,
            dataSource: { data: anomalyInfos },
            change: (e: kendo.ui.GridChangeEvent) => {
                e.sender.select().each(function(i, e) {
                    var grid = $("#anomaly-overview-grid").data("kendoGrid");
                    var dataItem = <any>grid.dataItem(this);
                    Main.DetailAnomaly = dataItem.FileName;
                    State.Set(true);
                    State.Pop();
                    AnomaliesPage.Show();
                });
            },
        });
        $("#anomaly-overview-grid").scrollTop(scrollPosition);
    }

    private static async ShowAnomalyPreview(filename: string) {
        var anomalyInfo = this.LastAnomalyInfos.find(v => v.FileName == filename);
        if (anomalyInfo == null)
            return;
        if (this.CurrentPreview == filename)
            return;
        this.CurrentPreview = filename;

        var msg = await Utils.Loader("GetGraphAnomalyPreview", { filename: anomalyInfo.FileName });
        var data = msg.d ? msg.d.map(v => LogStat.CreateFromObject(v)) : [];
        var options: GraphOptions = {
            MaxY: 100,
            MinY: 0,
            XLabelWidth: 50,
            FontSize: 10,
            PlotColor: "#000080",
            LabelFormat: Utils.ShortGWDateFormat,
            HighlightSection: {
                StartLabel: anomalyInfo.From,
                EndLabel: anomalyInfo.To,
                HighlightColor: "#b60000",
            }
        };
        if (this.PreviewGraph) {
            this.PreviewGraph.Dispose();
        }
        this.PreviewGraph = new LineGraph("anomaly-overview-preview", {
            Values: data.map(v => <GraphPoint>{ Label: v.Date, Value: v.Value })
        }, options);
    }

    private static async ShowDetailSection(filename: string) {
        if (this.IsLoadingAnomalyDetail)
            return;

        this.IsLoadingAnomalyDetail = true;
        try {
            var msg = await Utils.Loader("GetGraphAnomaly", { filename: filename });

            this.IsLoadingAnomalyDetail = false;
            if (Main.DetailAnomaly == null)
                return;

            var anomaly = GraphAnomaly.CreateFromObject(msg.d); // Fix dates

            var html = `
                <div class="anomaly-detail">
                    <h2>${anomaly.Name}</h2>
                    <div style="float: left;">
                        <p>From: ${Utils.FullUtcDateFormat(anomaly.From)}</p>
                        <p>To: ${Utils.FullUtcDateFormat(anomaly.To)}</p>
                    </div>
                    <div style="float: right; padding: 5px;">
                        <div class="button" onclick="AnomaliesPage.DeleteDetail('${filename}')">Delete Anomaly</div>
                    </div>
                    <hr style="clear:both;"/>
                    <p>CPU</p>
                    <div id="anomaly-detail-cpu-graph"></div>
                    <p>PVs</p>
                    <div id="anomaly-detail-pv-graph"></div>
                    <p>Searches</p>
                    <div id="anomaly-detail-searches-graph"></div>
                    <p>Network</p>
                    <div id="anomaly-detail-network-graph"></div>
                </div>`;
            var scrolled = $("#anomalyView").scrollTop();
            $("#anomalyView").html(html);
            $("#anomalyView").scrollTop(scrolled);

            this.CreateGraph("anomaly-detail-cpu-graph", anomaly, anomaly.History.CPU, { MaxY: 100 });
            this.CreateGraph("anomaly-detail-pv-graph", anomaly, anomaly.History.PVs);
            this.CreateGraph("anomaly-detail-searches-graph", anomaly, anomaly.History.Searches);
            this.CreateGraph("anomaly-detail-network-graph", anomaly, anomaly.History.Network);
        } catch{
        }
    }

    public static async DeleteDetail(filename: string) {
        await Utils.Loader("DeleteGraphAnomaly", { filename: filename });
        Main.Path = "Anomalies";
        Main.DetailAnomaly = null;

        State.Set(true);
        State.Pop();
    }

    private static CreateGraph(id: string, anomaly: GraphAnomaly, data: LogStat[], opts?: { MaxY: number }) {
        if (data == null)
            return;

        var points = <GraphData>{
            Values: data.map(v => <GraphPoint>{
                Value: v.Value,
                Label: v.Date
            })
        };
        new LineGraph(id, points, {
            XLabelWidth: 50,
            MinY: 0,
            MaxY: opts ? opts.MaxY : null,
            FontSize: 10,
            PlotColor: "#000080",
            LabelFormat: Utils.ShortGWDateFormat,
            HighlightSection: {
                StartLabel: anomaly.From,
                EndLabel: anomaly.To,
                HighlightColor: "#b60000",
            }
        });
    }

}