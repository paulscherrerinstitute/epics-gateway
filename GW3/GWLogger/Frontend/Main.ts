/// <reference path="query.ts" />

// Main class...
class Main
{
    public static CurrentGateway: string;
    static Path: string = "Status";
    static StartDate: Date = null;
    static EndDate: Date = null;
    static CurrentTime: Date;
    static CurrentTab: number = 0;
    static IsLast: boolean = true;
    static BaseTitle: string;

    private static isLoading = false;

    public static Init(): void
    {
        Sections.Init();
        State.Init();

        Main.InitTooltips();
        Main.UrlRedirect();
        Main.InitSearchGateway();
        Main.BaseTitle = window.document.title;

        State.Pop(null);

        setInterval(Main.Refresh, 1000);
        $(window).on("resize", Main.Resize);

        setTimeout(() =>
        {
            $("#loadingScreen").animate({ left: "-100vw" }, "2s", () =>
            {
                $("#loadingScreen").hide();
            });
        }, 1000);
    }

    private static InitTooltips()
    {
        $("*[tooltip]").each((idx, elem) =>
        {
            $(elem).on("mouseover", (e) =>
            {
                var text = $(e.target).attr("tooltip");
                var position = $(e.target).attr("tooltip-position");
                if (!position)
                    position = "bottom";
                ToolTip.Show(e.target, (<any>position), text);
            });
        });
    }

    private static InitSearchGateway()
    {
        var gateways: string[] = [];
        $('.GWDisplay').each(function ()
        {
            gateways.push($(this).attr('id'));
        });
        $("#searchInput").kendoAutoComplete({
            minLength: 1,
            select: (e) => window.location.href = "Status/" + e.dataItem.toLowerCase(),
            dataSource: gateways,
            placeholder: "Search",
            filter: "contains"
        }).data("kendoAutoComplete");
    }

    private static UrlRedirect()
    {
        var currentUrl = (document.location + "");
        if (currentUrl.toLowerCase().startsWith("http://caesar"))
        {
            if (currentUrl.toLowerCase().startsWith("http://caesar/"))
                document.location.replace(currentUrl.toLowerCase().replace("http://caesar/", "https://caesar.psi.ch/"));
            else
                document.location.replace(currentUrl.toLowerCase().replace("http://caesar", "https://caesar"));
            return;
        }
        if (currentUrl.toLowerCase().startsWith("http://gfaepicslog"))
        {
            if (currentUrl.toLowerCase().startsWith("http://gfaepicslog/"))
                document.location.replace(currentUrl.toLowerCase().replace("http://gfaepicslog/", "https://caesar.psi.ch/"));
            else
                document.location.replace(currentUrl.toLowerCase().replace("http://gfaepicslog", "https://caesar"));
            return;
        }
    }

    private static CheckTouch(): boolean
    {
        return (('ontouchstart' in window)
            || ((<any>navigator).MaxTouchPoints > 0)
            || ((<any>navigator).msMaxTouchPoints > 0));
    }

    private static Resize(): void
    {
        if (Math.abs(window.innerHeight - screen.height) < 10 && Main.CheckTouch())
            $("#fullScreenMode").attr("media", "");
        else
            $("#fullScreenMode").attr("media", "max-width: 1px");
        StatsBarGraph.DrawStats();
        $(".k-grid").each((idx, elem) =>
        {
            $(elem).data("kendoGrid").resize(true);
        });
    }

    private static nbRefreshes: number = 0;
    public static async Refresh()
    {
        var now = new Date();
        $("#currentTime").html(("" + now.getUTCHours()).padLeft("0", 2) + ":" + ("" + now.getUTCMinutes()).padLeft("0", 2) + ":" + ("" + now.getUTCSeconds()).padLeft("0", 2));

        if (Main.isLoading)
            return;
        Main.isLoading = true;
        Main.nbRefreshes = (Main.nbRefreshes + 1) % 60;
        try
        {
            if (Main.nbRefreshes == 30)
                await Main.CheckJsCodeVersion();
            await Main.Status();
            await StatusPage.Refresh();
            await DetailPage.Refresh();
            await LogsPage.Refresh();
        }
        catch (ex)
        {
        }
        Main.isLoading = false;
    }

    // Stores the hash of the JS code served by the server.
    private static lastVersion: string = null;
    private static async CheckJsCodeVersion()
    {
        var msg = await Utils.Loader("JsHash");
        var version = <string>msg.d;
        if (Main.lastVersion && Main.lastVersion != version)
            window.location.reload(true);
        Main.lastVersion = version;
    }

    private static async Status()
    {
        try
        {
            var msg = await Utils.Loader("GetFreeSpace");
            var free = <FreeSpace>msg.d;
            $("#freeSpace").html("" + (Math.round(free.FreeMB * 1000 / free.TotMB) / 10) + "%");

            var msg = await Utils.Loader("GetBufferUsage");
            $("#bufferSpace").html("" + msg.d + "%");
        }
        catch (ex)
        {
        }
    }
}
