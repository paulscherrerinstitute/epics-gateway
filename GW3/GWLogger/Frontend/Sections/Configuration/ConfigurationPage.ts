class ConfigurationPage
{
    static Config: XmlGatewayConfig;
    static Expanded: boolean[] = [];

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
        //$("#frmCfgDesc").val(config.Comment);
    }

    public static ExpandGroup(groupId: number)
    {
        if ($("#grp_" + groupId).is(":visible"))
        {
            $("#grp_" + groupId).hide();
            delete ConfigurationPage.Expanded[groupId];
        }
        else
        {
            $("#grp_" + groupId).show();
            ConfigurationPage.Expanded[groupId] = true;
        }
    }

    public static ChangeField(evt: Event)
    {
        evt = evt || window.event;
        var elemId = <string>(evt.target["id"]);
        var p = elemId.split("_");
        var val = $("#" + elemId).val();

        if (p[1] == "grpfltr")
        {
            ConfigurationPage.Config.Security.Groups[parseInt(p[2])].Filters[parseInt(p[3])][p[4]] = val;
        }
        else
        {
            var side = p[2];
            var field = p[3];
            var id = parseInt(p[4]);

            var rules = <ConfigSecurityRule[]>ConfigurationPage.Config.Security["RulesSide" + side];
            switch (field)
            {
                case "channel":
                    rules[id].Channel = val;
                    break;
                case "access":
                    rules[id].Access = val;
                    break;
                case "filter":
                    if (val == "Host")
                        rules[id].Filter = { $type: "GWLogger.Backend.Controllers." + val + "Filter, GWLogger", Name: "" }
                    else if (val == "IP")
                        rules[id].Filter = { $type: "GWLogger.Backend.Controllers." + val + "Filter, GWLogger", IP: "" }
                    else if (val == "All")
                        rules[id].Filter = { $type: "GWLogger.Backend.Controllers." + val + "Filter, GWLogger" }
                    ConfigurationPage.ShowRules();
                    break;
                case "ipfilter":
                    rules[id].Filter.IP = val;
                    break;
                case "namefilter":
                    rules[id].Filter.Name = val;
                    break;
                default:
                    break;
            }
        }
    }

    public static AddGrpFilter(evt: Event)
    {
        evt = evt || window.event;
        var elemId = <string>(evt.target["id"]);
        var p = elemId.split("_");
        var val = $("#" + elemId).val();

        var group = ConfigurationPage.Config.Security.Groups[parseInt(p[2])];
        switch (val)
        {
            case "Host":
                group.Filters.push({ $type: "GWLogger.Backend.Controllers." + val + "Filter, GWLogger", Name: "" });
                break;
            case "IP":
                group.Filters.push({ $type: "GWLogger.Backend.Controllers." + val + "Filter, GWLogger", IP: "" });
                break;
            default:
                break;
        }
        ConfigurationPage.ShowRules();
    }

    public static ShowRules()
    {
        var html = "";

        var sides = ["A", "B"];

        for (var g = 0; g <= ConfigurationPage.Config.Security.Groups.length; g++)
        {
            var group = (g < ConfigurationPage.Config.Security.Groups.length ? ConfigurationPage.Config.Security.Groups[g] : null);

            if (group)
            {
                html += "<div class='groupTitle' onclick='ConfigurationPage.ExpandGroup(" + g + ")'>Group " + group.Name + "</div>";
                html += "<div class='groupRules' id='grp_" + g + "'" + (ConfigurationPage.Expanded[g] === true ? " style='display: block;'" : "") + ">";
                html += "<b>Filters:</b><br>";
                for (var i = 0; i < group.Filters.length; i++)
                {
                    html += ConfigurationPage.ClassName(group.Filters[i].$type) + ":";
                    if (typeof group.Filters[i].IP !== "undefined")
                        html += "<input type='text' class='k-textbox' value='" + group.Filters[i].IP + "' id='frm_grpfltr_" + g + "_" + i + "_IP' onkeyup='ConfigurationPage.ChangeField(event);' />";
                    else if (typeof group.Filters[i].Name !== "undefined")
                        html += "<input type='text' class='k-textbox' value='" + group.Filters[i].Name + "' id='frm_grpfltr_" + g + "_" + i + "_Name' onkeyup='ConfigurationPage.ChangeField(event);' />";
                }
                html += Utils.Dropdown({ frmId: "frm_newgrpfltr_" + g, values: ["-- Add new filter --", "IP", "Host"], onchange:"ConfigurationPage.AddGrpFilter(event)" })+"<br>";
                html += "<b>Rules:</b><br>";
            }
            else
                html += "<div class='groupTitle'>Generic rules</div>";

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
                    html += "<div class='colChannel'><input type='text' id='frm_rule_" + side + "_channel_" + i + "' onkeyup='ConfigurationPage.ChangeField(event);' class='k-textbox' value='" + rules[i].Channel + "' /></div>";
                    html += "<div class='colAccess'>" + Utils.Dropdown({
                        frmId: "frm_rule_" + side + "_access_" + i, onchange: "ConfigurationPage.ChangeField(event)", values: { "READ": "Read", "ALL": "Read Write", "NONE": "None" }, cssClass: 'drpToConvert', currentValue: rules[i].Access
                    }) + "</div>";
                    if (group)
                    {
                        html += "<div class='colFilter'>&nbsp;</div>";
                        html += "<div class='colFilterParam'>&nbsp;</div>";
                    }
                    else
                    {
                        html += "<div class='colFilter'>" + Utils.Dropdown({ frmId: "frm_rule_" + side + "_filter_" + i, onchange: "ConfigurationPage.ChangeField(event)", values: ["All", "Host", "IP"], cssClass: 'drpToConvert', currentValue: ConfigurationPage.ClassName(rules[i].Filter.$type) }) + "</div>";
                        if (typeof rules[i].Filter.IP !== "undefined")
                            html += "<div class='colFilterParam'><input type='text' class='k-textbox' value='" + rules[i].Filter.IP + "' id='frm_rule_" + side + "_ipfilter_" + i + "' onkeyup='ConfigurationPage.ChangeField(event);' /></div>";
                        else if (typeof rules[i].Filter.Name !== "undefined")
                            html += "<div class='colFilterParam'><input type='text' class='k-textbox' value='" + rules[i].Filter.Name + "' id='frm_rule_" + side + "_namefilter_" + i + "' onkeyup='ConfigurationPage.ChangeField(event);' /></div>";
                        else
                            html += "<div class='colFilterParam'>&nbsp;</div>";
                    }
                }
                html += "</td>";
                if (s != 0)
                    html += "</tr>";
            }

            html += "</table>";
            if (group)
                html += "</div>";
        }

        $("#configRulesView").html(html);
        //$(".drpToConvert").kendoDropDownList();
    }

    private static ClassName(source: string): string
    {
        return source.split(",")[0].substr(29).replace("Filter", "");
    }
}