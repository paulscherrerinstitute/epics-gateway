class ConfigurationPage
{
    static Config: XmlGatewayConfig;

    public static Show(gatewayName: string)
    {
        $("#frmCfgHost").val("");
        $("#frmCfgDirection").val("");
        $("#frmCfgLocA").val("");
        $("#frmCfgRemA").val("");
        $("#frmCfgLocB").val("");
        $("#frmCfgRemB").val("");
        $("#frmCfgDesc").val("");

        Utils.Loader("GetGatewayConfiguration", { hostname: gatewayName }).then(ConfigurationPage.ParseConfig);
    }

    public static ParseConfig(xmlConfig)
    {
        ConfigurationPage.Config = <XmlGatewayConfig>$.parseJSON(xmlConfig.d);
        ConfigurationPage.Config.Security.Groups.sort((a, b) =>
        {
            if (a.Name > b.Name)
                return 1;
            if (a.Name < b.Name)
                return -1;
            return 0;
        });

        $("#frmCfgHost").val(ConfigurationPage.Config.Name);
        $("#frmCfgDirection").val(ConfigurationPage.Config.Type);
        $("#frmCfgLocA").val(ConfigurationPage.Config.LocalAddressSideA);
        $("#frmCfgRemA").val(ConfigurationPage.Config.RemoteAddressSideA);
        $("#frmCfgLocB").val(ConfigurationPage.Config.LocalAddressSideB);
        $("#frmCfgRemB").val(ConfigurationPage.Config.RemoteAddressSideB);

        ConfigurationPage.ShowRules();
        ConfigurationPage.ShowRules();
        //$("#frmCfgDesc").val(config.Comment);
    }

    public static ShowRules()
    {
        var html = "";

        var sides = ["A", "B"];

        for (var g = 0; g <= ConfigurationPage.Config.Security.Groups.length; g++)
        {
            var group = (g < ConfigurationPage.Config.Security.Groups.length ? ConfigurationPage.Config.Security.Groups[g] : null);

            if(group)
                html += "Group " + group.Name;

            html += "<table class='sideConfigTable'>";
            html += "<tr><th>Side A</th><th>Side B</th></tr>";

            for (var s = 0; s < sides.length; s++)
            {
                var side = sides[s];
                var rules = <ConfigSecurityRule[]>ConfigurationPage.Config.Security["RulesSide" + side];
                if (s == 0)
                    html += "<tr>";
                html += "<td>";
                for (var i = 0; i < rules.length; i++)
                {
                    if (group == null && ConfigurationPage.ClassName(rules[i].Filter.$type) == "Group")
                        continue;
                    else if (group != null && (ConfigurationPage.ClassName(rules[i].Filter.$type) != "Group" || rules[i].Filter.Name != group.Name))
                        continue;
                    html += "<div class='colChannel'><input type='text' class='k-textbox' value='" + rules[i].Channel + "' /></div>";
                    html += "<div class='colAccess'><input type='text' class='k-textbox' value='" + rules[i].Access + "' /></div>";
                    if (group)
                    {
                        html += "<div class='colFilter'>&nbsp;</div>";
                        html += "<div class='colFilterParam'>&nbsp;</div>";
                    }
                    else
                    {
                        html += "<div class='colFilter'><input type='text' class='k-textbox' value='" + ConfigurationPage.ClassName(rules[i].Filter.$type) + "' /></div>";
                        if (typeof rules[i].Filter.IP !== "undefined")
                            html += "<div class='colFilterParam'><input type='text' class='k-textbox' value='" + rules[i].Filter.IP + "' /></div>";
                        else if (typeof rules[i].Filter.Name !== "undefined")
                            html += "<div class='colFilterParam'><input type='text' class='k-textbox' value='" + rules[i].Filter.Name + "' /></div>";
                        else
                            html += "<div class='colFilterParam'>&nbsp;</div>";
                    }
                }
                html += "</td>";
                if (s != 0)
                    html += "</tr>";
            }

            html += "</table>";
        }

        $("#configRulesView").html(html);
    }

    private static ClassName(source: string): string
    {
        return source.split(",")[0].substr(29).replace("Filter", "");
    }
}