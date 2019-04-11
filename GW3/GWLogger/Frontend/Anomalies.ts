class Anomalies
{
    private static IsLoadingAnomalyInfos = false;
    private static IsLoadingAnomalyDetail = false;
    private static AllAnomalyInfos: GraphAnomalyInfo[] = null;

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
            anomalyInfos = anomalyInfos.map(v => GraphAnomalyInfo.CreateFromObject(v)); // Fix dates
            this.IsLoadingAnomalyInfos = false;

            this.AllAnomalyInfos = anomalyInfos;
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
            anomaly = GraphAnomaly.CreateFromObject(anomaly); // Fix dates
            this.IsLoadingAnomalyDetail = false;

            var html = `<div>${anomaly.Name}</div>`;



            $("#anomalyView").html(html);
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