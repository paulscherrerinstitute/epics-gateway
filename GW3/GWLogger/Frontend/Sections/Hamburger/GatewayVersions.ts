class GatewayVersions
{
    static Show(): void
    {
        $("#reportView").show();
        $('#helpView, #operationView').hide();
        $("#reportContent").removeClass().addClass("fixed").removeAttr('style').html("Loading...");

        if ($("#reportContent").data("kendoGrid"))
            $("#reportContent").data("kendoGrid").destroy();

        var lastVersion = null;
        for (var i = 0; i < StatusPage.shortInfo.length; i++)
            if (lastVersion == null || StatusPage.shortInfo[i].Version > lastVersion)
                lastVersion = StatusPage.shortInfo[i].Version;

        $("#reportContent").html("").kendoGrid({
            columns: [
                { title: "Gateway", field: "Name" },
                { title: "Build", field: "Build" },
                { title: "Version", field: "Version" }],
            dataSource:
            {
                data: StatusPage.shortInfo
            },
            sortable: true
        });

        var grid = $("#reportContent").data("kendoGrid");
        grid.bind("dataBound", (row) =>
        {
            var items = row.sender.items();
            items.each(function (index)
            {
                var dataItem = <GatewayShortInformation>(<any>grid.dataItem(this));
                if (dataItem.Version < lastVersion)
                    this.className += " oldGatewayVersion";
            })
        });
        grid.dataSource.fetch();
    }
}