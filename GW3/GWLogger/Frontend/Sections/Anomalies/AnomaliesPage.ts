class AnomaliesPage
{
    private static IsLoadingAnomalyInfos = false;
    private static IsLoadingAnomalyDetail = false;

    public static async Show() {
        Main.CurrentGateway = null;
        State.Set(true);

        if (Main.DetailAnomaly == null) {
            await this.ShowOverviewSection();
        } else {
            await this.ShowDetailSection(Main.DetailAnomaly);
        }
    }

    public static OpenDetailSection(filename: string) {
        Main.DetailAnomaly = filename;
        State.Set(true);
        this.Show().then(() => { });
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
            console.error("Call to GetGraphAnomalies failed");
            return;
        }

        if (msg == null || msg.d == null)
            return;

        var anomalyInfos = msg.d.map(v => GraphAnomalyInfo.CreateFromObject(v)); // Fix dates

        this.IsLoadingAnomalyInfos = false;
        if (Main.DetailAnomaly != null)
            return;

        var html = "";
        if (anomalyInfos.length == 0) {
            html += `<div class="no-anomalies">No anomalies</div>`;
        }

        for (var anomaly of anomalyInfos) {
            html += `<div class="anomaly-card" onclick="AnomaliesPage.OpenDetailSection('${anomaly.FileName}')">${Utils.FullUtcDateFormat(anomaly.From)}: <b>${anomaly.Name}</b> ${Utils.DurationString(anomaly.To.getTime() - anomaly.From.getTime())}`;
            html += `<div id="anomaly_cpu_graph_${anomaly.FileName}"></div>`;
            html += `</div>`;
        }
        var scrolled = $("#anomalyView").scrollTop();
        $("#anomalyView").html(html);
        $("#anomalyView").scrollTop(scrolled);

        //var grid = $("#gatewaySessions").kendoGrid({
        //    columns: [{ title: "Start", field: "StartDate", format: "{0:MM/dd HH:mm:ss}" },
        //    { title: "End", field: "EndDate", format: "{0:MM/dd HH:mm:ss}" },
        //    { title: "NB&nbsp;Logs", field: "NbEntries", format: "{0:n0}", attributes: { style: "text-align:right;" } },
        //    { title: "Reason", field: "RestartType", values: restartTypes }],
        //    dataSource: { data: LogsPage.Sessions },
        //    selectable: "single cell",
        //    change: (arg) => {
        //        var selected = arg.sender.select()[0];
        //        var txt = selected.innerText;
        //        var content = selected.textContent;
        //        var uid = $(selected).parent().attr("data-uid");
        //        var row = grid.dataSource.getByUid(uid);
        //        if (row) {
        //            if (kendo.format(grid.columns[0].format, row['StartDate']) == txt)
        //                Main.CurrentTime = (<Date>row['StartDate']).toUtc();
        //            else if (kendo.format(grid.columns[0].format, row['EndDate']) == txt)
        //                Main.CurrentTime = (<Date>row['EndDate']).toUtc();
        //            Main.StartDate = new Date(Main.CurrentTime.getTime() - 12 * 3600000);
        //            Main.EndDate = new Date(Main.CurrentTime.getTime() + 12 * 3600000);
        //            State.Set(true);
        //            State.Pop(null);
        //        }
        //    }
        //}).data("kendoGrid");
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