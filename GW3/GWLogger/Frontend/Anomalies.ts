class Anomalies {

    public static Show(): void {
        this.GetGraphAnomalies((anomalies) => {
            console.dir(anomalies);
            var html = "";
            for (var anomaly of anomalies) {
                html += "<div class=\"anomaly-card\">" + anomaly.Filename + "</div>"
            }
            $("#anomalyView").html(html);
        });
    }

    private static GetGraphAnomalies(callback: (a: GraphAnomaly[]) => void): void {
        $.ajax({
            type: 'POST',
            url: '/DataAccess.asmx/GetGraphAnomalies',
            data: {},
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: (data) => {
                var list = data.d;
                if (list) {
                    var anomalies = (<Array<any>>list).map(v => GraphAnomaly.CreateFromObject(v));
                    callback(anomalies);
                } else {
                    callback(null);
                }
            },
        });
    }

}