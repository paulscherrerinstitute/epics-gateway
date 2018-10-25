class EpicsDebug
{
    static Show()
    {
        $("#reportView").show();
        $("#reportContent").removeAttr('style').removeClass().css({ "overflow": "auto", "padding": "5px" });
        $('#helpView').hide();

        if ($("#reportContent").data("kendoGrid"))
            $("#reportContent").data("kendoGrid").destroy();

        if ($("#dbgGateway").data("kendoDropDownList"))
            $("#dbgGateway").data("kendoDropDownList").destroy();

        if ($("#dbgNetwork").data("kendoDropDownList"))
            $("#dbgNetwork").data("kendoDropDownList").destroy();

        var html = "";

        html += "<center>";
        html += "<h2>EPICS Debug tool</h2>";
        html += "<table id='dbgEpicsTable'>";
        html += "<tr><td>EPICS Channel</td><td><input type='text' id='dbgChannel' class='k-textbox'></td>";
        html += "<tr><td>EPICS Gateway</td><td><select id='dbgGateway'></select></td>";
        html += "</table><br/><br/>";
        html += "<span class='button' onclick='EpicsDebug.Test()' id='dbgRun'>Run test</span>";
        html += "</center>";

        html += "<div id='dbgTestResult'></div>";
        $("#reportContent").html(html);


        var gateways = Live.shortInfo.map((c) => c.Name).sort();
        $("#dbgGateway").kendoDropDownList({
            dataSource: gateways,
            value: (Main.CurrentGateway ? Main.CurrentGateway.toUpperCase() : null)
        });

        $("#dbgNetwork").kendoDropDownList({});
    }

    static Test()
    {
        $("#dbgTestResult").html("Checking...");
        $("#dbgRun").hide();

        var channel = $("#dbgChannel").val();
        var gateway = $("#dbgGateway").data("kendoDropDownList").value();
        var checkDone = 0;

        $.ajax({
            type: 'POST',
            url: 'DataAccess.asmx/GetGatewayNetworks',
            data: JSON.stringify({
                "gatewayName": $("#dbgGateway").data("kendoDropDownList").value(),
            }),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                var nets: KeyValuePair[] = msg.d

                var html = "";
                for (var i = 0; i < nets.length; i++)
                {
                    html += "<div id='dbgNet_" + i + "' class='dbgCheckResult'><h2>" + nets[i].Key + "</h2>Checking...</div>";
                    var a = () =>
                    {
                        var net = i;
                        $.ajax({
                            type: 'POST',
                            url: 'DataAccess.asmx/EpicsCheck',
                            data: JSON.stringify({
                                "gatewayName": gateway,
                                "config": nets[net].Value,
                                "channel": channel
                            }),
                            contentType: 'application/json; charset=utf-8',
                            dataType: 'json',
                            success: function (msg)
                            {
                                var lines = msg.d.split("\n");
                                var html = "<h2>" + nets[net].Key + "</h2>";
                                html += "<table>";
                                html += "<tr><td>Configuration</td><td>" + nets[net].Value + "</td></tr>";
                                for (var i = 0; i < lines.length; i++)
                                {
                                    var p = lines[i].split(/:(.*)/);
                                    if (!p[1])
                                        html += "<tr><td colspan='2'>" + p[0] + "</td></tr>";
                                    else
                                    {
                                        if (p[0] == "Channel GET")
                                        {
                                            if (p[1].indexOf("Complete:") != -1)
                                            {
                                                html += "<tr><td>" + p[0] + "</td><td class='dbgSucceed'>" + p[1] + "</td></tr>";
                                                nets[net]['result'] = 'ok';
                                            }
                                            else
                                            {
                                                html += "<tr><td>" + p[0] + "</td><td class='dbgFail'>" + p[1] + "</td></tr>";
                                                nets[net]['result'] = 'fail';
                                            }
                                        }
                                        else
                                            html += "<tr><td>" + p[0] + "</td><td>" + p[1] + "</td></tr>";
                                    }
                                }
                                html += "</table>";
                                $("#dbgNet_" + net).html(html);
                                checkDone++;

                                // We have all checked
                                if (checkDone >= nets.length)
                                {
                                    if (nets.length == 4 && ((nets[0]['result'] == 'ok' && nets[3]['result'] == 'ok') || (nets[1]['result'] == 'ok' && nets[2]['result'] == 'ok')))
                                        $("#dbgTestResult").html($("#dbgTestResult").html() + "<h2>Summary</h2>Works as expected");
                                    else if (nets.length == 4 && nets[0]['result'] == 'ok' && nets[1]['result'] == 'ok' && nets[2]['result'] == 'ok' && nets[3]['result'] == 'ok')
                                        $("#dbgTestResult").html($("#dbgTestResult").html() + "<h2>Summary</h2>The channel seems to be available on both network. Either we \
have a loopback or an IOC is duplicating a gateway.");
                                    else if (nets.length == 4 && nets[0]['result'] != 'ok' && nets[1]['result'] != 'ok' && nets[2]['result'] != 'ok' && nets[3]['result'] != 'ok')
                                        $("#dbgTestResult").html($("#dbgTestResult").html() + "<h2>Summary</h2>The channel doesn't seems to exists in the selected networks. \
Either you introduced a typo or the server\ serving it is down or the channel never existed. It is also possible that the gateway tries to reach the IOC via unicast (no \
broadcast address), and multiple IOCs runs on the same machine, in this case only the first started process will intercept EPICS search messages, and thefore it will not \
be possible to discover those. The solution of this last issue is to have only one running IOC on the system or to use special ports for it.");
                                    else if (nets.length == 4 && ((nets[0]['result'] != 'ok' && nets[3]['result'] == 'ok') || (nets[1]['result'] != 'ok' && nets[2]['result'] == 'ok')))
                                        $("#dbgTestResult").html($("#dbgTestResult").html() + "<h2>Summary</h2>Either the gateway is blocking the channel due to the configuration \
or an odd state of the gateway prevents the channel to be read correctly.");
                                    else if (nets.length == 2 && ((nets[0]['result'] == 'ok' && nets[1]['result'] == 'ok')))
                                        $("#dbgTestResult").html($("#dbgTestResult").html() + "<h2>Summary</h2>Works as expected");
                                    else if (nets.length == 2 && nets[0]['result'] != 'ok' && nets[1]['result'] != 'ok')
                                        $("#dbgTestResult").html($("#dbgTestResult").html() + "<h2>Summary</h2>The channel doesn't seems to exists in the selected networks. \
Either you introduced a typo or the server\ serving it is down or the channel never existed. It is also possible that the gateway tries to reach the IOC via unicast (no \
broadcast address), and multiple IOCs runs on the same machine, in this case only the first started process will intercept EPICS search messages, and thefore it will not \
be possible to discover those. The solution of this last issue is to have only one running IOC on the system or to use special ports for it.");
                                    else if (nets.length == 1 && ((nets[0]['result'] != 'ok' && nets[1]['result'] == 'ok')))
                                        $("#dbgTestResult").html($("#dbgTestResult").html() + "<h2>Summary</h2>Either the gateway is blocking the channel due to the \
configuration or an odd state of the gateway prevents the channel to be read correctly.");
                                    else
                                        $("#dbgTestResult").html($("#dbgTestResult").html() + "<h2>Summary</h2>The gateway is reactly oddly, please contact the support.");

                                    $("#dbgRun").show();
                                }
                            }
                        });
                    };
                    a();
                }
                $("#dbgTestResult").html(html);
            }
        });
    }
}