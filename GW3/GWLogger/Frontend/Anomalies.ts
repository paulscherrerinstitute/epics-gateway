class Anomalies
{
    private static IsLoadingAnomalyInfos = false;
    private static IsLoadingAnomalyDetail = false;

    public static Show(): void {
        Main.CurrentGateway = null;
        State.Set(true);

        if (Main.DetailAnomaly == null) {
            this.ShowOverviewSection();
        } else {
            this.ShowDetailSection(Main.DetailAnomaly);
        }
        
    }

    public static OpenDetailSection(filename: string) {
        Main.DetailAnomaly = filename;
        State.Set(true);
        this.Show();
    }

    private static ShowOverviewSection() {
        if (this.IsLoadingAnomalyInfos)
            return;

        this.IsLoadingAnomalyInfos = true;
        this.Load("GetGraphAnomalies", null, (anomalyInfos: GraphAnomalyInfo[]) => {
            this.IsLoadingAnomalyInfos = false;
            if (Main.DetailAnomaly != null)
                return;

            anomalyInfos = anomalyInfos.map(v => GraphAnomalyInfo.CreateFromObject(v)); // Fix dates

            var html = "";
            if (anomalyInfos.length == 0) {
                html += `<div class="no-anomalies">No anomalies</div>`;
            }

            for (var anomaly of anomalyInfos) {
                html += `<div class="anomaly-card" onclick="Anomalies.OpenDetailSection('${anomaly.FileName}')">${Utils.FullUtcDateFormat(anomaly.From)}: <b>${anomaly.Name}</b> ${this.DurationString(anomaly.To.getTime() - anomaly.From.getTime())}`;
                html += `<div id="anomaly_cpu_graph_${anomaly.FileName}"></div>`;
                html += `</div>`;
            }
            var scrolled = $("#anomalyView").scrollTop();
            $("#anomalyView").html(html);
            $("#anomalyView").scrollTop(scrolled);
        });
    }

    private static ShowDetailSection(filename: string) {
        if (this.IsLoadingAnomalyDetail)
            return;

        this.IsLoadingAnomalyDetail = true;
        this.Load("GetGraphAnomaly", { filename: filename }, (anomaly: GraphAnomaly) => {
            this.IsLoadingAnomalyDetail = false;
            if (Main.DetailAnomaly == null)
                return;

            anomaly = GraphAnomaly.CreateFromObject(anomaly); // Fix dates

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
        });
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

    private static DurationString(millis: number): string {
        var duration = "";

        var seconds = millis / 1000;
        if (seconds >= 1)
            duration = (seconds % 60).toFixed(0) + " s";

        var minutes = seconds / 60;
        if (minutes >= 1)
            duration = (minutes % 60).toFixed(0) + " min " + duration;

        var hours = minutes / 60;
        if (hours >= 1)
            duration = (hours % 24).toFixed(0) + " h " + duration;

        return duration;
    }

    private static Load(method: string, params: any, success: (data: any) => void): void {
        $.ajax({
            type: "POST",
            url: `/DataAccess.asmx/${method}`,
            data: JSON.stringify(params ? params : {}),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: (data) => { success(data.d) },
        });
    }

}