class Sections
{
    public static Init(): void
    {
        DetailPage.Init();
        StatusPage.Init();
        LogsPage.Init();
        MapPage.Init();
        Hamburger.Init();
        Sections.TopMenu();
    }

    private static TopMenu(): void
    {
        $("#login").click(Main.ShowLogin);

        $("#btnLoginCancel").click(() =>
        {
            $("#loginScreen").hide();
        });
        $("#btnLogin").click(() =>
        {
            $("#loginScreen").hide();
            Main.Login();
        });
        $("#frmUsername,#frmPassword").keydown((evt: JQueryKeyEventObject) =>
        {
            if (evt.keyCode == 27)
            {
                $("#loginScreen").hide();
            }
            else if (evt.keyCode == 13)
            {
                $("#loginScreen").hide();
                Main.Login();
            }
        });

        $("#mainTabs li").click((evt) =>
        {
            if (evt.target.id == "login")
                return;
            if (evt.target.tagName != "INPUT")
            {
                var tab = evt.target.textContent;
                switch (tab)
                {
                    case "Status":
                        $("#reportView").hide();
                        if ($("#helpView").is(":visible"))
                            $("#helpView").hide();
                        if ($("#operationView").is(":visible"))
                            $("#operationView").hide();
                        if (Main.Path == "Status")
                            Main.CurrentGateway = null;
                        Main.Path = "Status";
                        State.Set(true);
                        State.Pop();
                        $(".inset").removeClass("inset");
                        break;
                    case "Logs":
                        $("#reportView").hide();
                        if ($("#helpView").is(":visible"))
                            $("#helpView").hide();
                        if ($("#operationView").is(":visible"))
                            $("#operationView").hide();
                        if (Main.Path == "GW")
                            break;
                        Main.Path = "GW";
                        State.Set(true);
                        State.Pop();
                        $(".inset").removeClass("inset");
                        break;
                    case "Map":
                        $("#reportView").hide();
                        if ($("#helpView").is(":visible"))
                            $("#helpView").hide();
                        if ($("#operationView").is(":visible"))
                            $("#operationView").hide();
                        if (Main.Path == "Map")
                            break;
                        Main.Path = "Map";
                        State.Set(true);
                        State.Pop();
                        $(".inset").removeClass("inset");
                        break;
                    case "Anomalies":
                        $("#reportView").hide();
                        if ($("#helpView").is(":visible"))
                            $("#helpView").hide();
                        if ($("#operationView").is(":visible"))
                            $("#operationView").hide();
                        if (Main.DetailAnomaly)
                            Main.DetailAnomaly = null;
                        Main.Path = "Anomalies";
                        State.Set(true);
                        State.Pop();
                        $(".inset").removeClass("inset");
                        break;
                    case "Help":
                        window.open("/help.html", "help", "menubar=no,location=no,status=no,toolbar=no,width=800,height=600,scrollbars=yes");
                        break;
                    case "Operator Help":
                        window.open("/sop_d.html", "help", "menubar=no,location=no,status=no,toolbar=no,width=800,height=600,scrollbars=yes");
                        break;
                    case "": // Hambuger
                        if (evt.target.getAttribute("id") == "hamburgerDisplay" || evt.target.parentElement.getAttribute("id") == "hamburgerDisplay" || evt.target.parentElement.parentElement.getAttribute("id") == "hamburgerDisplay")
                            $("#hamburgerMenu").toggleClass("visibleHamburger");
                        break;
                    default:
                        State.Set(true);
                        State.Pop();
                        break;
                }
            }
        });

        $(".helpMiddleContainer a").on("click", (elem) =>
        {
            var href: string = elem.target.attributes["href"].value;
            $(href)[0].scrollIntoView();
            return false;
        });
    }
}