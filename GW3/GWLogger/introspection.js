/**
 *  Code written in JS as it should not be bundled with the interface. Will be used only by the introspection tool
 **/

String.prototype.htmlEntities = function ()
{
    return this.replace(/&/g, "&amp;").replace(/>/g, "&gt;").replace(/</g, "&lt;").replace(/'/g, "&#39;").replace(/"/g, "&quot;");
}

function showParameters(keepForm)
{
    var html = "";
    var data = "";
    html += "<table>";
    for (var i = 0; i < method.Parameters.length; i++)
    {
        if (i != 0)
            data += ", ";
        var type = (method.Parameters[i].Type ? method.Parameters[i].Type.Type : "class");
        var val = (method.DemoParameters[i] == "" ? "xxx" : method.DemoParameters[i]);
        if (keepForm === true && document.getElementById("param_" + i) && document.getElementById("param_" + i).value != "")
            val = document.getElementById("param_" + i).value;
        if (val === undefined || val === null)
            val = null;
        if (type == "string" || type == "number" || type == "boolean" || type == "date")
            data += "\"" + method.Parameters[i].Name + "\" : " + JSON.stringify(val);
        else if (type == "class")
            data += "\"" + method.Parameters[i].Name + "\" : " + val;
        else
            data += "\"" + method.Parameters[i].Name + "\" : " + JSON.stringify(val, null, 4);
        html += "<tr><td>" + method.Parameters[i].Name + "</td><td><input type='text' id='param_" + i + "' onkeyup='keyUp(event)' value='" + (method.DemoParameters[i] ? method.DemoParameters[i].htmlEntities() : "") + "'></td></tr>";
    }
    html += "</table>";
    html += "<input type='button' value='Invoke' onclick='invokeMethod()'>";
    html += "<input type='button' value='Parameters' onclick='methodParams()' style='margin-left: 10px;'>";
    html += "<input type='button' value='result TypeScript DTO' onclick='generateDTO()' style='margin-left: 10px;'>";
    if (keepForm !== true)
        $("#methodParameters").html(html);

    var code = "<b>JQuery example:</b>\n" +
        "$.ajax({\n" +
        "   type: 'POST',\n" +
        "   url: '" + document.location.origin + "/DataAccess.asmx/" + method.Name + "',\n" +
        "   data: JSON.stringify({" + data + "}),\n" +
        "   contentType: 'application/json; charset=utf-8',\n" +
        "   dataType: 'jsonp',\n" +
        "   success: function (msg)\n" +
        "   {\n" +
        "       // msg.d contains the return object in case of complex return value.\n" +
        "       console.log(msg);\n" +
        "   },\n" +
        "   error: function (msg, textStatus)\n" +
        "   {\n" +
        "       console.log(msg.responseText);\n" +
        "   }\n" +
        "});";
    code += "\n\n<b>Command line example:</b>\n";

    code += "curl -H 'Content-Type:application/json; charset=utf-8' -X POST -d '" + JSON.stringify(JSON.parse("{" + data + "}")) + "' " + document.location.origin + "/DataAccess.asmx/" + method.Name
    $("#prototype").html(code).css({ "white-space": "pre-wrap" });
    if (keepForm !== true)
    {
        $("#param_0").focus();
        methodParams();
    }
}

function methodParams()
{
    var code = "<b>Function parameters:</b><br>";
    code += JSON.stringify(method.Parameters, null, 4);
    $("#result").html(code);

}

function generateDTO()
{
    var code = "";
    code += dtoClass(method.Result.OfType ? method.Result.OfType : method.Result, method.Result.OfType ? method.Result.OfType.ClassName : method.Result.ClassName);
    $("#result").text(code);
}

Array.prototype.inArray = function (obj)
{
    for (var i = 0; i < this.length; i++)
        if (this[i] == obj)
            return true;
    return false;
}

function dtoClass(info, className, knownClasses)
{
    if (!knownClasses)
        knownClasses = ["string", "number", "class", "boolean", "date", "enum"];
    var classToDo = [];
    var enums = [];

    var code = "";
    code += "/**\n";
    code += " * DTO of " + info.NameSpace + "." + className + "\n";
    code += " */\n";
    code += "class " + className + "\n";
    code += "{\n";
    // Generate the fields of the class
    for (var i = 0; info.Properties && i < info.Properties.length; i++)
    {
        code += "\t" + info.Properties[i].Name;
        if (info.Properties[i].ClassType)
        {
            code += ": " + info.Properties[i].ClassType;
        }
        else if (info.Properties[i].Type.Type == "array")
        {
            if (info.Properties[i].Type.OfType && info.Properties[i].Type.OfType.ClassName)
            {
                if (!knownClasses.inArray(info.Properties[i].Type.OfType.ClassName))
                {
                    classToDo.push({ Name: info.Properties[i].Type.OfType.ClassName, Info: info.Properties[i].Type.OfType });
                    knownClasses.push(info.Properties[i].Type.OfType.ClassName);
                }
                code += ": " + info.Properties[i].Type.OfType.ClassName + "[]";
            }
            else if (info.Properties[i].Type.OfClassType)
                code += ": " + info.Properties[i].Type.OfClassType + "[]";
            else
                code += ": " + info.Properties[i].Type.OfType.Type + "[]";
        }
        else
        {
            if (!knownClasses.inArray(info.Properties[i].Type.Type))
            {
                classToDo.push({ Name: info.Properties[i].Type.Type, Info: info.Properties[i].Type });
                knownClasses.push(info.Properties[i].Type.Type);
            }

            if (info.Properties[i].Type.Type == "date")
                code += ": Date";
            else if (info.Properties[i].Type.Type == "enum")
            {
                code += ": " + info.Properties[i].Type.EnumName;
                enums.push(info.Properties[i].Type);
            }
            else
                code += ": " + info.Properties[i].Type.Type;
        }
        code += ";\n";
    }

    code += "\n";
    code += "\tconstructor(";
    for (var i = 0; info.Properties && i < info.Properties.length; i++)
    {
        if (i != 0)
            code += ",\n\t\t";
        code += "_" + info.Properties[i].Name.toLowerCase();

        if (info.Properties[i].ClassType)
            code += ": " + info.Properties[i].ClassType;
        else if (info.Properties[i].Type.Type == "array")
        {
            if (info.Properties[i].Type.OfType && info.Properties[i].Type.OfType.ClassName)
                code += ": " + info.Properties[i].Type.OfType.ClassName + "[]";
            else if (info.Properties[i].Type.OfClassType)
                code += ": " + info.Properties[i].Type.OfClassType + "[]";
            else
                code += ": " + info.Properties[i].Type.OfType.Type + "[]";
        }
        else if (info.Properties[i].Type.Type == "date")
            code += ": string";
        else if (info.Properties[i].Type.Type == "enum")
            code += ": " + info.Properties[i].Type.EnumName;
        else
            code += ": " + info.Properties[i].Type.Type;
    }
    code += ")\n";
    code += "\t{\n";
    for (var i = 0; info.Properties && i < info.Properties.length; i++)
    {
        if (info.Properties[i].Type && info.Properties[i].Type.Type == "date")
            code += "\t\tthis." + info.Properties[i].Name + " = (_" + info.Properties[i].Name.toLowerCase() + " ? new Date(parseInt(_" + info.Properties[i].Name.toLowerCase() + ".substr(6, _" + info.Properties[i].Name.toLowerCase() + ".length-8))) : null);\n"
        else
            code += "\t\tthis." + info.Properties[i].Name + " = _" + info.Properties[i].Name.toLowerCase() + ";\n";
    }
    code += "\t}\n";

    code += "\n";
    code += "\tpublic static CreateFromObject(obj: any): " + className + "\n";
    code += "\t{\n";
    code += "\t\tif (!obj)\n";
    code += "\t\t\treturn null;\n";

    code += "\t\treturn new " + className + "(";

    for (var i = 0; info.Properties && i < info.Properties.length; i++)
    {
        if (i != 0)
            code += ", \n\t\t\t";
        if (info.Properties[i].Type && info.Properties[i].Type.Type == "array" && info.Properties[i].Type.OfType && info.Properties[i].Type.OfType.ClassName)
            code += "(obj." + info.Properties[i].Name + " ? obj." + info.Properties[i].Name + ".map(function(c) { return " + info.Properties[i].Type.OfType.ClassName + ".CreateFromObject(c);}) : null)";
        else if (info.Properties[i].Type && info.Properties[i].Type.Type == "array" && info.Properties[i].Type.OfClassType)
            code += "(obj." + info.Properties[i].Name + " ? obj." + info.Properties[i].Name + ".map(function(c) { return " + info.Properties[i].Type.OfClassType + ".CreateFromObject(c);}) : null)";
        else
            code += "obj." + info.Properties[i].Name;
    }
    code += ");\n";
    code += "\t}\n";
    code += "}\n";

    for (var i = 0; i < enums.length; i++)
    {
        code += "\nenum " + enums[i].EnumName + "\n{\n";
        for (var j = 0; j < enums[i].Properties.length; j++)
            code += enums[i].Properties[j].Name + " = " + enums[i].Properties[j].Value + ",\n";
        code += "}\n";
    }

    // Do all the missing classes
    for (var i = 0; i < classToDo.length; i++)
    {
        code += "\n" + dtoClass(classToDo[i].Info, classToDo[i].Name, knownClasses);
    }

    return code;
}

function keyUp(e)
{
    e = e ? e : event;

    if (e.keyCode == 13)
        invokeMethod();
    else
        showParameters(true);
}

function invokeMethod()
{
    $("#result").text("Loading...");
    var startTime = new Date();
    var data = {};
    for (var i = 0; i < method.Parameters.length; i++)
    {
        // Simple types will be passed as is
        var type = (method.Parameters[i].Type ? method.Parameters[i].Type.Type : "class");
        if (type == "string" || type == "number" || type == "boolean" || type == "date")
            data[method.Parameters[i].Name] = $("#param_" + i).val();
        else
            data[method.Parameters[i].Name] = JSON.parse($("#param_" + i).val());
    }

    $.ajax({
        type: "POST",
        url: "/DataAccess.asmx/" + method.Name,
        data: JSON.stringify(data),
        contentType: "application/json; charset=utf-8",
        dataType: "jsonp",
        success: function (msg)
        {
            var endTime = new Date();
            var timeDiff = (endTime - startTime) / 1000;
            $("#result").html("<b>Response in " + timeDiff + " sec.:</b><br>" + JSON.stringify(msg.d, null, 4));
        },
        error: function (msg, textStatus)
        {
            try
            {
                $("#result").text(JSON.stringify(JSON.parse(msg.responseText), null, 4));
            }
            catch (ex)
            {
                $("#result").text(msg.responseText);
            }
        }
    });
}

function showInfos()
{
    var searchtext = document.getElementById("searchFunction").value.toLowerCase();

    var html = "";
    var lastCategory = null;
    html += "<table class='apiDetails'>";
    for (var i = 0; i < infos.length; i++)
    {
        if (searchtext && infos[i].Category.toLowerCase().indexOf(searchtext) == -1
            && infos[i].Name.toLowerCase().indexOf(searchtext) == -1
            && infos[i].Description.toLowerCase().indexOf(searchtext) == -1)
            continue;
        if (lastCategory != infos[i].Category)
        {
            lastCategory = infos[i].Category;
            html += "<tr class='apiCategory'><td colspan='2'><b>" + infos[i].Category + "</b></td></tr>";
        }
        html += "<tr><td><a href='/Introspection.ashx/" + infos[i].Name + "'>" + infos[i].Name + "</a></td><td>" + infos[i].Description + "</td></tr>";
    }
    html += "</table>";

    if (searchtext)
        html = html.replace(new RegExp("(>[^<]*)(" + searchtext + ")", "gi"), "$1<span class='highlight'>$2</span>");

    document.getElementById("functionList").innerHTML = html;
}

//showParameters();