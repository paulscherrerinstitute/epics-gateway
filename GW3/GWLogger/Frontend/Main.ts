/// <reference path="query.ts" />

// Main class...
class Main
{
    public static CurrentGateway: string;
    public static DetailAnomaly: string;
    static Path: string = "Status";
    static StartDate: Date = null;
    static EndDate: Date = null;
    static CurrentTime: Date;
    static CurrentTab: number = 0;
    static IsLast: boolean = true;
    static BaseTitle: string;
    static CurrentUser: string = null;
    static Token: string = null;

    private static isLoading = false;

    public static async Init()
    {
        if (Utils.Preferences['LoggedAs'])
        {
            $("#frmUsername").val(Utils.Preferences['LoggedAs']);
            $("#frmPassword").val(Utils.Preferences['Password']);
            Main.Login();
        }
        else
        {
            $(".req-login").hide();
        }

        var toDisplay: GatewayToDisplay[] = (await Utils.Loader("GatewaysToDisplay")).d;
        for (var i = 0; i < toDisplay.length; i++)
        {
            $(toDisplay[i].IsMain ? "#gatewayMains" : "#gatewayBeamlines").append("<div class=\"GWDisplay\" id=\"" + toDisplay[i].Name + "\"></div>");
        }

        Sections.Init();
        State.Init();

        Main.InitTooltips();
        Main.UrlRedirect();
        Main.InitSearchGateway();
        Main.BaseTitle = window.document.title;

        State.Pop();

        setInterval(Main.Refresh, 1000);
        $(window).on("resize", Main.Resize);

        setTimeout(() =>
        {
            $("#loadingScreen").animate({ left: "-100vw" }, "2s", () =>
            {
                $("#loadingScreen").hide();
            });
        }, 1000);

        setInterval(Main.RenewToken, 10000);
    }

    public static RenewToken()
    {
        if (Main.Token)
            Utils.Loader("/AuthAccess/AuthService.asmx/RenewToken", { tokenId: Main.Token });
    }

    public static ShowLogin()
    {
        if ($("#loginScreen").is(":visible"))
        {
            $("#loginScreen").hide();
            return;
        }

        if (Main.CurrentUser)
        {
            $("#login").text("Not Logged In").removeClass("loggedIn");
            Main.CurrentUser = null;
            var prefs = Utils.Preferences;
            delete prefs['LoggedAs'];
            Utils.Preferences = prefs;
        }
        else
        {
            $("#loginScreen").show();
            $("#frmUsername").focus();
        }
    }

    public static async Login()
    {
        try
        {
            var user = $("#frmUsername").val();
            var token: string = (await Utils.Loader('/AuthAccess/AuthService.asmx/Login', {
                username: user, password: $("#frmPassword").val()
            }))['d'];
            $("#login").text(user).addClass("loggedIn");
            var prefs = Utils.Preferences;
            prefs['LoggedAs'] = user;
            prefs['Password'] = $("#frmPassword").val();
            Utils.Preferences = prefs;
            Main.CurrentUser = user;
            Main.Token = token;

            if (await Utils.Loader('/AuthAccess/AuthService.asmx/HasAdminRole', {
                tokenId: token
            }))
            {
                $("#hamburgerMenu").append("<div onclick=\"Main.CreateNewGateway()\">Create Gateway</div>");
            }
        }
        catch (e)
        {
            $("#login").text("Not Logged In").removeClass("loggedIn");
            var prefs = Utils.Preferences;
            delete prefs['LoggedAs'];
            delete prefs['Password'];
            Utils.Preferences = prefs;
        }

        if (Main.CurrentUser)
            $(".req-login").show();
        else
            $(".req-login").hide();
        State.Pop();
    }

    private static async CreateNewGateway()
    {
        var newGateway = await kendo.prompt("New gateway name", "");
        if (newGateway)
        {
            await Utils.Loader('/AuthAccess/AuthService.asmx/CreateNewGateway', {
                tokenId: Main.Token,
                gatewayName: newGateway
            });
            document.location.reload();
        }
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
            if (Main.nbRefreshes % 30 == 0)
                await Main.CheckJsCodeVersion();
            if (Main.nbRefreshes % 5 == 0)
                await Main.CPUUpdate();
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

    private static async CPUUpdate()
    {
        var msg = await Utils.Loader("GetCpu");
        var cpu = <number>msg.d;
        $("#cpuSpace").html("" + Math.round(cpu) + " %");
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