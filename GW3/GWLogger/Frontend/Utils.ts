interface String
{
    /**
    * Escape the string for a "value" attribute.
    */
    htmlEntities(): string;
    /**
    * Fills the string left with the character c.
    */
    padLeft(c, nb): string;
    /**
    * Capitalize the first character of the string.
    */
    capitalize(): string;
    /**
    * Transorms the string into a CSS valid ID
    */
    id(): string;
    /**
    * Adds spaces before a capitle case.
    */
    title(): string;
    /**
    * Returns true if the string starts with toSearch
    */
    startsWith(toSearch: string): boolean;
    /**
    * Returns true if the string ends with toSearch
    */
    endsWith(toSearch: string): boolean;

    linkify(): string;
}

String.prototype.linkify = function ()
{
    return this.replace(/(^|\s|\>)(http[s]{0,1}:\/\/[a-zA-Z0-9\/\-\+:\.\?=_\&\#\;\%\,]*)/g, "$1<a href='$2' target='_blank'>$2</a>");
}

String.prototype.startsWith = function (toSearch: string)
{
    return (toSearch.length <= this.length && this.substr(0, toSearch.length) == toSearch);
}

String.prototype.endsWith = function (toSearch: string)
{
    return (toSearch.length <= this.length && this.substr(this.length - toSearch.length) == toSearch);
}

String.prototype.title = function ()
{
    return this.replace(/(\w)([A-Z][a-z])/g, "$1 $2");
}

String.prototype.padLeft = function (c, nb)
{
    if (this.length >= nb)
        return this;
    return Array(nb - this.length + 1).join(c) + this;
}

String.prototype.htmlEntities = function ()
{
    return this.replace(/&/g, "&amp;").replace(/>/g, "&gt;").replace(/</g, "&lt;").replace(/'/g, "&#39;").replace(/"/g, "&quot;");
}

String.prototype.capitalize = function ()
{
    return this.charAt(0).toUpperCase() + this.substr(1);
}

interface Date
{
    toUtc(): Date
}

Date.prototype.toUtc = function ()
{
    return new Date(this.getTime() + ((new Date()).getTimezoneOffset() * 60000));
}

/**
* Transforms the string into a CSS valid ID
*/
String.prototype.id = function ()
{
    return this.replace(/ /g, "_").replace(/\//g, "_").replace(/#/g, "_").replace(/\./g, "_").replace(/</g, "_")
        .replace(/:/g, "_").replace(/\+/g, "_").replace(/\*/g, "_").replace(/\-/g, "_").replace(/\\/g, "_")
        .replace(/\(/g, "_").replace(/\)/g, "_").replace(/\&/g, "_").replace(/,/g, "_").replace(/\=/g, "_");
}

interface Array<T>
{
    /**
    * Escape the string for a "value" attribute.
    */
    sum(prop?: string): number;
    mean(prop?: string): number;
}

Array.prototype.sum = function (prop?: string): number
{
    var total = 0
    for (var i = 0, _len = this.length; i < _len; i++)
    {
        if (prop)
            total += this[i][prop]
        else
            total += this[i]
    }
    return total;
}

Array.prototype.mean = function (prop?: string): number
{
    var total = 0
    for (var i = 0, _len = this.length; i < _len; i++)
    {
        if (prop)
            total += this[i][prop]
        else
            total += this[i]
    }
    return total / this.length;
}


function isString(variable)
{
    // tests if this is a string
    return (typeof variable == 'string' || variable instanceof String);
}

var humanSizes = ["", "Kb", "Mb", "Gb", "Tb"];

class Utils
{
    static HumanReadable(size: number): string
    {
        var val = size;
        var arrayPos = 0;
        while (val > 1024 && arrayPos < humanSizes.length - 1)
        {
            val = Math.round((val / 1024) * 10) / 10;
            arrayPos++;
        }
        return "" + val + humanSizes[arrayPos];
    }

    static FixObjectDates(source: any)
    {
        var dest = JSON.parse(JSON.stringify(source));
        for (var i in dest)
        {
            if (source[i] instanceof Date)
                dest[i] = Utils.FullDateFormat(source[i]);
        }
        return dest;
    }

    static DateFromNet(source)
    {
        return new Date(parseInt(source.substr(6, source.length - 8)));
    }

    static NetDate(source: Date)
    {
        return "/Date(" + source.getTime() + ")/";
    }

    static DateFormat(source: Date)
    {
        if (!source)
            return "";
        return source.getFullYear() + "/" + ("" + (source.getMonth() + 1)).padLeft("0", 2) + "/" + ("" + source.getDate()).padLeft("0", 2);
    }

    static AsUtc(source: Date): Date
    {
        return new Date(Date.UTC(source.getFullYear(), source.getMonth(), source.getDate(), source.getHours(), source.getMinutes(), source.getSeconds(), source.getMilliseconds()));
    }

    static GWDateFormat(source: Date)
    {
        if (!source)
            return "";
        return ("" + (source.getUTCMonth() + 1)).padLeft("0", 2) + "/" + ("" + source.getUTCDate()).padLeft("0", 2) + " " +
            ("" + source.getUTCHours()).padLeft("0", 2) + ":" + ("" + source.getUTCMinutes()).padLeft("0", 2) + ":" + ("" + source.getUTCSeconds()).padLeft("0", 2);
    }

    static GWDateFormatMilis(source: Date)
    {
        if (!source)
            return "";
        return ("" + (source.getUTCMonth() + 1)).padLeft("0", 2) + "/" + ("" + source.getUTCDate()).padLeft("0", 2) + " " +
            ("" + source.getUTCHours()).padLeft("0", 2) + ":" + ("" + source.getUTCMinutes()).padLeft("0", 2) + ":" + ("" + source.getUTCSeconds()).padLeft("0", 2) + "." + ("" + source.getUTCMilliseconds()).padLeft("0", 3);
    }

    static ShortGWDateFormat(source: Date)
    {
        if (!source)
            return "";
        return ("" + (source.getUTCMonth() + 1)).padLeft("0", 2) + "/" + ("" + source.getUTCDate()).padLeft("0", 2) + " " +
            ("" + source.getUTCHours()).padLeft("0", 2) + ":" + ("" + source.getUTCMinutes()).padLeft("0", 2);
    }

    static FullDateFormat(source: Date)
    {
        if (!source)
            return "";
        return source.getFullYear() + "/" + ("" + (source.getMonth() + 1)).padLeft("0", 2) + "/" + ("" + source.getDate()).padLeft("0", 2) + " " +
            ("" + source.getHours()).padLeft("0", 2) + ":" + ("" + source.getMinutes()).padLeft("0", 2) + ":" + ("" + source.getSeconds()).padLeft("0", 2);
    }

    static FullUtcDateFormat(source: Date)
    {
        if (!source)
            return "";
        return source.getUTCFullYear() + "/" + ("" + (source.getUTCMonth() + 1)).padLeft("0", 2) + "/" + ("" + source.getUTCDate()).padLeft("0", 2) + " " +
            ("" + source.getUTCHours()).padLeft("0", 2) + ":" + ("" + source.getUTCMinutes()).padLeft("0", 2) + ":" + ("" + source.getUTCSeconds()).padLeft("0", 2);
    }

    static ParseDate(source: string)
    {
        if (!source || source == "")
            return null;
        if (source.charAt(source.length - 1) == "Z")
            return new Date(source);
        source = source.replace(/\./g, "/").replace(/\-/g, "/");

        var parts = source.trim().split(' ');

        if (parts.length > 1)
        {
            var time = parts[1].split(":");
            if (source.charAt(2) == "/")
            {
                return new Date(parseInt(source.substr(6, 4)), parseInt(source.substr(3, 2)) - 1, parseInt(source.substr(0, 2)), parseInt(time[0]), parseInt(time[1]), parseInt(time[2]));
            }
            else if (source.charAt(4) == "/")
            {
                return new Date(parseInt(source.substr(0, 4)), parseInt(source.substr(5, 2)) - 1, parseInt(source.substr(8, 2)), parseInt(time[0]), parseInt(time[1]), parseInt(time[2]));
            }
        }
        else
        {
            if (source.charAt(2) == "/")
            {
                return new Date(parseInt(source.substr(6, 4)), parseInt(source.substr(3, 2)) - 1, parseInt(source.substr(0, 2)));
            }
            else if (source.charAt(4) == "/")
            {
                return new Date(parseInt(source.substr(0, 4)), parseInt(source.substr(5, 2)) - 1, parseInt(source.substr(8, 2)));
            }
        }

        return new Date(parseInt(source));
    }

    public static DurationString(millis: number): string {
        var duration = "";

        var seconds = millis / 1000;
        if (seconds >= 1)
            duration = (seconds % 60).toFixed(0) + " s";

        var minutes = seconds / 60;
        if (minutes >= 1)
            duration = (minutes % 60).toFixed(0) + " min " + duration;

        var hours = minutes / 60;
        if (hours >= 1)
            duration = (hours % 24).toFixed(0) + " h " + duration;

        return duration;
    }

/**
 * Save the preference object to local storage
 */
    static set Preferences(preferences: object)
    {
        try
        {
            localStorage.setItem("preferences", JSON.stringify(preferences));
        }
        catch (ex)
        {
        }
    }

    static get Preferences(): object
    {
        if (localStorage.getItem("preferences") != null && localStorage.getItem("preferences") != undefined)
            return JSON.parse(localStorage.getItem("preferences"));
        return {};
    }

    static async Loader(functionName: string, data: any = {})
    {
        return $.ajax({
            type: 'POST',
            url: 'DataAccess.asmx/' + functionName,
            data: data ? JSON.stringify(data) : {},
            contentType: 'application/json; charset=utf-8',
            dataType: 'json'
        });
    }

    static LoaderXHR(functionName: string, data: any = {}): JQueryXHR
    {
        if (functionName.startsWith('/'))
            return $.ajax({
                type: 'GET',
                url: functionName
            });

        return $.ajax({
            type: 'POST',
            url: 'DataAccess.asmx/' + functionName,
            data: JSON.stringify(data),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json'
        });
    }
}