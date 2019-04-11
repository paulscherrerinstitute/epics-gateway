class StatusPage
{
    static shortInfo: GatewayShortInformation[] = [];
    static currentErrors: string[] = [];

    static Init()
    {
        $(".GWDisplay").each((idx, elem) =>
        {
            $(elem).on("click", (evt) =>
            {
                var elem = evt.currentTarget ? evt.currentTarget : evt.target;
                while ((elem.localName ? elem.localName : elem.tagName) != "div" && elem.className.indexOf("GWDisplay") == -1)
                    elem = elem.parentElement;
                var gwName = elem.id;
                DetailPage.Show(gwName);
            }).html("<canvas id='canvas_" + elem.id + "' width='100' height='100'></canvas><br>" + elem.id);
        });

        $(".GWDisplay").on("mouseover", (e) =>
        {
            var target = e.target

            while (target.className != "GWDisplay")
                target = target.parentElement;

            var html = "<b>Gateway " + target.id + "</b><br>";
            var live = StatusPage.Get(target.id);
            html += "<table style='text-align: left;'>";
            html += "<tr><td>Version</td><td>" + (live ? live.Version : "") + "</td></tr>";
            html += "<tr><td>State</td><td>" + (live ? GatewayStates[live.State] : "") + "</td></tr>";
            html += "</table>";
            html += "-- Click to view details --";

            ToolTip.Show(target, "bottom", html);
        });
    }

    public static Show(): void
    {
        Main.CurrentGateway = null;
        State.Set();
        $("#gatewayView").show();
        $("#gatewayDetails").hide();
    }

    private static DisplayShort()
    {
        for (var i = 0; i < StatusPage.shortInfo.length; i++)
            StatusGraph.DisplayPaint("#canvas_" + StatusPage.shortInfo[i].Name, StatusPage.shortInfo[i]);
    }


    public static async Refresh()
    {
        try
        {
            var msg = await Utils.Loader("GetShortInformation");

            StatusPage.shortInfo = msg.d;
            StatusPage.shortInfo.sort((a, b) =>
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
            for (var i = 0; i < StatusPage.shortInfo.length; i++)
            {
                MapPage.SetGatewayState(StatusPage.shortInfo[i].Name, StatusPage.shortInfo[i].State, StatusPage.shortInfo[i].Direction);
                if (ToolTip.currentElement && ToolTip.currentElement.id && ToolTip.currentElement.id.startsWith("svg_gw"))
                    $("#toolTip").html(MapPage.GetTooltipText());

                if (StatusPage.shortInfo[i].State < 3)
                    continue;
                if ((!StatusPage.currentErrors || StatusPage.currentErrors.indexOf(StatusPage.shortInfo[i].Name) == -1) && errorDisplayed == false)
                {
                    errorDisplayed = true;
                    Notifications.Show(StatusPage.shortInfo[i].Name + " is on error.");
                }
                newErrors.push(StatusPage.shortInfo[i].Name);
                html += "<a href='/Status/" + StatusPage.shortInfo[i].Name + "'>" + StatusPage.shortInfo[i].Name + "</a>";
            }
            StatusPage.currentErrors = newErrors;
            if (html == "")
                html = "<span>No errors</span>";
            $("#errorsContent").html(html);

            if (!Main.CurrentGateway && Main.Path != "GW")
                StatusPage.DisplayShort();

        }
        catch (ex)
        {
        }
    }

    public static Get(gwName: string): GatewayShortInformation
    {
        for (var i = 0; i < StatusPage.shortInfo.length; i++)
            if (StatusPage.shortInfo[i].Name.toUpperCase() == gwName.toUpperCase())
                return StatusPage.shortInfo[i];
        return null;
    }
}