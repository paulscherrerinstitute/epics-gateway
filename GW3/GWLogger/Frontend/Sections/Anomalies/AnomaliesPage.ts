class AnomaliesPage
{
    private static IsLoadingAnomalyInfos = false;
    private static IsLoadingAnomalyDetail = false;

    public static async Show() {
        if (Main.Path != "Anomalies")
            return;

        if (Main.DetailAnomaly == null) {
            await this.ShowOverviewSection();
        } else {
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
            console.error("Call to GetGraphAnomalies failed");
            return;
        }

        if (msg == null || msg.d == null)
            return;

        var anomalyInfos = msg.d.map(v => GraphAnomalyInfo.CreateFromObject(v)); // Fix dates

        this.IsLoadingAnomalyInfos = false;
        if (Main.DetailAnomaly != null)
            return;

        $("#anomalyView").html(`<div id="anomaly-overview-grid"></div>`);
        var grid = $("#anomaly-overview-grid").kendoGrid({
            columns: [
                {
                    title: "From",
                    field: "From",
                    format: "{0:MM/dd HH:mm:ss}",
                },
                {
                    title: "To",
                    field: "To",
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
            sortable: true,
            selectable: "row",
            scrollable: false,
            dataSource: { data: anomalyInfos },
            change: (e: kendo.ui.GridChangeEvent) => {
                e.sender.select().each(function(v) {
                    var grid = $("#anomaly-overview-grid").data("kendoGrid");
                    var dataItem = <any>grid.dataItem(this);
                    Main.DetailAnomaly = dataItem.FileName;
                    State.Set(true);
                    State.Pop();
                    AnomaliesPage.Show();
                });
            },
        });
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
                    <p>From: ${Utils.FullUtcDateFormat(anomaly.From)}</p>
                    <p>To: ${Utils.FullUtcDateFormat(anomaly.To)}</p>
                    <hr />
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