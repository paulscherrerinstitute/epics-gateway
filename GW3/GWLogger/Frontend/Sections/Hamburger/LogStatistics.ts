class LogStatistics
{
    public static Show()
    {
        $("#reportView").show();
        $('#helpView, #operationView').hide();
        $("#reportContent").removeClass().addClass("fixed").removeAttr('style').html("Loading...");

        if ($("#reportContent").data("kendoGrid"))
            $("#reportContent").data("kendoGrid").destroy();

        LogStatistics.LoadData();
    }

    private static async LoadData()
    {
        try
        {
            var msg = await Utils.Loader("GetDataFileStats");
            var vals = <DataFileStats[]>msg.d;

            var totSec = 0;
            var avgBytesTot = 0;
            var totSize = 0;
            for (var i = 0; i < vals.length; i++)
            {
                totSec += vals[i].LogsPerSeconds;
                avgBytesTot += vals[i].AverageEntryBytes;
                totSize += vals[i].TotalDataSize;
            }

            vals.push(
                {
                    Name: "Total",
                    AverageEntryBytes: Math.round(avgBytesTot / vals.length),
                    LogsPerSeconds: totSec,
                    TotalDataSize: totSize
                });

            $("#reportContent").html("").kendoGrid({
                columns: [
                    { title: "Gateway", field: "Name" },
                    { title: "Logs Per Sec.", field: "LogsPerSeconds", format: "{0:0.00}" },
                    { title: "Average Entry", field: "AverageEntryBytes" },
                    { title: "Size On Disk", field: "TotalDataSize", template: (row: DataFileStats) => Utils.HumanReadable(row.TotalDataSize) }],
                dataSource:
                {
                    data: vals
                },
                sortable: true
            });

            var grid = $("#reportContent").data("kendoGrid");
            grid.bind("dataBound", (row) =>
            {
                var items = row.sender.items();
                items.each(function (index)
                {
                    var dataItem = <DataFileStats>(<any>grid.dataItem(this));
                    if (dataItem.Name == "Total")
                        this.className += " summary";
                })
            });
            grid.dataSource.fetch();
        }
        catch (ex)
        {
        }
    }
}