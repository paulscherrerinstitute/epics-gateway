class ConfigurationPage
{
    static HasEditRole: boolean;
    static Config: XmlGatewayConfig;
    static Expanded: boolean[] = [];

    public static Show(gatewayName: string)
    {
        if (!gatewayName)
            return;
        gatewayName = gatewayName.toUpperCase();
        $("#configOverlay").show();
        $("#frmCfg_Name").val("");
        $("#frmCfg_Type").val(0);
        $("#frmCfg_LocalAddressSideA").val("");
        $("#frmCfg_RemoteAddressSideA").val("");
        $("#frmCfg_LocalAddressSideB").val("");
        $("#frmCfg_RemoteAddressSideB").val("");
        $("#frmCfg_Comment").val("");

        if (Main.Token)
        {
            Utils.Loader("/AuthAccess/AuthService.asmx/HasEditConfigRole", { tokenId: Main.Token, gatewayName: gatewayName }).then((hasEditRole) =>
            {
                ConfigurationPage.HasEditRole = hasEditRole;
                Utils.Loader("GetGatewayConfiguration", { hostname: gatewayName }).then(ConfigurationPage.ParseConfig);
            });
        }
        else
        {
            ConfigurationPage.HasEditRole = false;
            Utils.Loader("GetGatewayConfiguration", { hostname: gatewayName }).then(ConfigurationPage.ParseConfig);
        }
    }

    public static ParseConfig(xmlConfig)
    {
        ConfigurationPage.Config = <XmlGatewayConfig>$.parseJSON(xmlConfig.d);
        $("#configOverlay").hide();
        ConfigurationPage.SortGroups();

        $("#frmCfg_Name").val(ConfigurationPage.Config.Name);
        $("#frmCfg_Type").val(ConfigurationPage.Config.Type);
        $("#frmCfg_LocalAddressSideA").val(ConfigurationPage.Config.LocalAddressSideA);
        $("#frmCfg_RemoteAddressSideA").val(ConfigurationPage.Config.RemoteAddressSideA);
        $("#frmCfg_LocalAddressSideB").val(ConfigurationPage.Config.LocalAddressSideB);
        $("#frmCfg_RemoteAddressSideB").val(ConfigurationPage.Config.RemoteAddressSideB);
        $("#frmCfg_Comment").val(ConfigurationPage.Config.Comment);

        ConfigurationPage.ShowRules();
        if (ConfigurationPage.HasEditRole)
        {
            $(".genCommands").show();
            $("#configurationView input").prop("disabled", false);
            $("#configurationView select").prop("disabled", false);
            $("#configurationView textarea").prop("disabled", false);
        }
        else
        {
            $(".genCommands").hide();
            $("#configurationView input").prop("disabled", true);
            $("#configurationView input").prop("disabled", true);
            $("#configurationView select").prop("disabled", true);
            $("#configurationView textarea").prop("disabled", true);
        }
    }

    public static Save()
    {
        $("#configOverlay").show();
        Utils.Loader("/AuthAccess/AuthService.asmx/SaveGatewayConfiguration", { json: JSON.stringify(ConfigurationPage.Config), tokenId: Main.Token }).then(() =>
        {
            Notifications.Popup("Configuration saved", "info");
            Utils.Loader("GetGatewayConfiguration", { hostname: ConfigurationPage.Config.Name }).then(ConfigurationPage.ParseConfig);
        });
    }

    public static ChangeConfigField(evt: Event)
    {
        evt = evt || window.event;
        var elemId = <string>(evt.target["id"]);
        var p = elemId.split("_");
        var val = $("#" + elemId).val();

        ConfigurationPage.Config[p[1]] = val;
    }

    static SortGroups()
    {
        ConfigurationPage.Config.Security.Groups.sort((a, b) =>
        {
            if (a.Name > b.Name)
                return 1;
            if (a.Name < b.Name)
                return -1;
            return 0;
        });
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

    public static AddRule(side: string, groupName: string)
    {
        var rules = <ConfigSecurityRule[]>ConfigurationPage.Config.Security["RulesSide" + side];
        rules.push({
            Access: "NONE", Channel: "", Filter: (groupName ? { $type: "GWLogger.Backend.Controllers.GroupFilter, GWLogger", Name: groupName } : { $type: "GWLogger.Backend.Controllers.AllFilter, GWLogger" })
        });


        ConfigurationPage.ShowRules();
    }

    public static DeleteRule(side: string, id: number)
    {
        var rules = <ConfigSecurityRule[]>ConfigurationPage.Config.Security["RulesSide" + side];
        rules.splice(id, 1);
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
                html += "<div class='groupTitle' onclick='ConfigurationPage.ExpandGroup(" + g + ")'>Group " + group.Name;
                if (ConfigurationPage.HasEditRole)
                    html += "<span class='fa_times' onclick='ConfigurationPage.DeleteGroup(event, " + g + ")'></span><span class='pencilIcon' onclick='ConfigurationPage.RenameGroup(event, " + g + ")'></span>";
                html += "</div>";
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
                if (ConfigurationPage.HasEditRole)
                    html += Utils.Dropdown({ frmId: "frm_newgrpfltr_" + g, values: ["-- Add new filter --", "IP", "Host"], onchange: "ConfigurationPage.AddGrpFilter(event)" }) + "<br>";
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
                var currRow = 0;
                for (var i = 0; i < rules.length; i++)
                {
                    if (group == null && ConfigurationPage.ClassName(rules[i].Filter.$type) == "Group")
                        continue;
                    else if (group != null && (ConfigurationPage.ClassName(rules[i].Filter.$type) != "Group" || rules[i].Filter.Name != group.Name))
                        continue;
                    if (ConfigurationPage.HasEditRole)
                        html += "<div class='colCommands'><span class='arrow_up' currow='" + i + "' onclick='ConfigurationPage.MoveUp(event)' id='mvt_" + side + "_" + g + "_" + currRow + "'></span><span class='arrow_down' currow='" + i + "' onclick='ConfigurationPage.MoveDown(event)' id='mvtd_" + side + "_" + g + "_" + currRow + "'></span><span class='fa_times' onclick='ConfigurationPage.DeleteRule(\"" + side + "\"," + i + ")'></span></div>";
                    else
                        html += "<div class='colCommands'></div>";
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
                    currRow++;
                }

                if (ConfigurationPage.HasEditRole)
                {
                    html += "<div class='genCommands'>";
                    html += "<span class='button' onclick='ConfigurationPage.AddRule(\"" + side + "\"," + (group ? "\"" + group.Name + "\"" : "null") + ")'>Add Rule</span>";
                    html += "</div>";
                }

                html += "</td>";
                if (s != 0)
                    html += "</tr>";
            }

            html += "</table>";
            if (group)
                html += "</div>";
        }

        if (ConfigurationPage.HasEditRole)
        {
            html += "<div class='genCommands'>";
            html += "<span class='button' onclick='ConfigurationPage.AddGroup()'>New Group</span>";
            html += "</div>";
        }

        $("#configRulesView").html(html);
    }

    static MoveUp(evt: Event)
    {
        evt = evt || window.event;
        var elemId = <string>evt.target["id"];
        var p = elemId.split("_");
        if (parseInt(p[3]) == 0)
            return;

        var side = p[1];

        var prevElemId = "mvt_" + side + "_" + p[2] + "_" + (parseInt(p[3]) - 1);
        var curId = parseInt((<any>evt.target).getAttribute("currow"));
        var prevId = parseInt($("#" + prevElemId).attr("currow"));

        var rules = <ConfigSecurityRule[]>ConfigurationPage.Config.Security["RulesSide" + side];

        var temp = rules[curId];
        rules[curId] = rules[prevId];
        rules[prevId] = temp;
        ConfigurationPage.ShowRules();
    }

    static MoveDown(evt: Event)
    {
        evt = evt || window.event;
        var elemId = <string>evt.target["id"];
        var p = elemId.split("_");

        var side = p[1];

        var nextElemId = "mvtd_" + side + "_" + p[2] + "_" + (parseInt(p[3]) + 1);
        var curId = parseInt((<any>evt.target).getAttribute("currow"));
        var nextId = parseInt($("#" + nextElemId).attr("currow"));

        if (nextId === null || nextId === undefined || isNaN(nextId))
            return;

        var rules = <ConfigSecurityRule[]>ConfigurationPage.Config.Security["RulesSide" + side];

        var temp = rules[curId];
        rules[curId] = rules[nextId];
        rules[nextId] = temp;
        ConfigurationPage.ShowRules();
    }

    static DeleteGroup(evt: Event, groupId: number)
    {
        evt = evt || window.event;
        evt.cancelBubble = true;
        var oldName = ConfigurationPage.Config.Security.Groups[groupId].Name;
        ConfigurationPage.Config.Security.Groups.splice(groupId, 1);

        var sides = ["A", "B"];
        for (var s = 0; s < sides.length; s++)
        {
            var side = sides[s];
            var rules = <ConfigSecurityRule[]>ConfigurationPage.Config.Security["RulesSide" + side];
            for (var i = 0; i < rules.length;)
            {
                if (rules[i].Filter.$type == "GWLogger.Backend.Controllers.GroupFilter, GWLogger" && rules[i].Filter.Name == oldName)
                    rules.splice(i, 1);
                else
                    i++;
            }
        }
        ConfigurationPage.Expanded = [];

        ConfigurationPage.ShowRules();
    }

    static RenameGroup(evt: Event, groupId: number)
    {
        evt = evt || window.event;
        evt.cancelBubble = true;
        var group = ConfigurationPage.Config.Security.Groups[groupId];
        kendo.prompt("Rename group name", group.Name).done((newName: string) =>
        {
            var oldName = group.Name;
            group.Name = newName;
            var sides = ["A", "B"];
            for (var s = 0; s < sides.length; s++)
            {
                var side = sides[s];
                var rules = <ConfigSecurityRule[]>ConfigurationPage.Config.Security["RulesSide" + side];
                for (var i = 0; i < rules.length; i++)
                {
                    if (rules[i].Filter.$type == "GWLogger.Backend.Controllers.GroupFilter, GWLogger" && rules[i].Filter.Name == oldName)
                        rules[i].Filter.Name = newName;
                }
            }
            ConfigurationPage.ShowRules();
        });
    }

    static AddGroup()
    {
        kendo.prompt("New group name", "Group Name").done((newName: string) =>
        {
            ConfigurationPage.Config.Security.Groups.push({ Filters: [], Name: newName });
            ConfigurationPage.SortGroups();
            ConfigurationPage.ShowRules();
        });
    }

    private static ClassName(source: string): string
    {
        return source.split(",")[0].substr(29).replace("Filter", "");
    }
}