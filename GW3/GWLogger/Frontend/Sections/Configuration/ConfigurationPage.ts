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

        html += "<table class='sideConfigTable'>";
        for (var s = 0; s < sides.length; s++)
        {
            var side = sides[s];
            var rules = <ConfigSecurityRule[]>ConfigurationPage.Config.Security["RulesSide" + side];
            if (s == 0)
                html += "<tr>";
            html += "<th>Side " + side + "</th>";
            if (s != 0)
                html += "</tr>";
        }

        for (var s = 0; s < sides.length; s++)
        {
            var side = sides[s];
            var rules = <ConfigSecurityRule[]>ConfigurationPage.Config.Security["RulesSide" + side];
            if (s == 0)
                html += "<tr>";
            html += "<td><table>";
            for (var i = 0; i < rules.length; i++)
            {
                if (ConfigurationPage.ClassName(rules[i].Filter.$type) == "Group")
                    continue;
                html += "<tr>";
                html += "<td><input type='text' class='k-textbox' value='" + rules[i].Channel + "' /></td>";
                html += "<td><input type='text' class='k-textbox' value='" + rules[i].Access + "' /></td>";
                html += "<td><input type='text' class='k-textbox' value='" + ConfigurationPage.ClassName(rules[i].Filter.$type) + "' /></td>";
                if (typeof rules[i].Filter.IP !== "undefined")
                    html += "<td><input type='text' class='k-textbox' value='" + rules[i].Filter.IP + "' /></td>";
                else if (typeof rules[i].Filter.Name !== "undefined")
                    html += "<td><input type='text' class='k-textbox' value='" + rules[i].Filter.Name + "' /></td>";
                else
                    html += "<td>&nbsp;</td>";
                html += "</tr>";
            }
            html += "</table></td>";
            if (s != 0)
                html += "</tr>";
        }
        $("#configRulesView").html(html);
    }

    private static ClassName(source: string): string
    {
        return source.split(",")[0].substr(29).replace("Filter", "");
    }
}