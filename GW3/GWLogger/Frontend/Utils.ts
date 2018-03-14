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

/**
* Transorms the string into a CSS valid ID
*/
String.prototype.id = function ()
{
    return this.replace(/ /g, "_").replace(/\//g, "_").replace(/#/g, "_").replace(/\./g, "_").replace(/</g, "_")
        .replace(/:/g, "_").replace(/\+/g, "_").replace(/\*/g, "_").replace(/\-/g, "_").replace(/\\/g, "_")
        .replace(/\(/g, "_").replace(/\)/g, "_").replace(/\&/g, "_").replace(/,/g, "_").replace(/\=/g, "_");
}


function isString(variable)
{
    // tests if this is a string
    return (typeof variable == 'string' || variable instanceof String);
}

class Utils
{
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

    static NetDate(source: Date)
    {
        return "/Date(" + source.getTime() + ")/";
    }

    static DateFormat(source: Date)
    {
        if (!source)
            return "";
        return source.getFullYear() + "/" + ("" + (source.getMonth() + 1)).padLeft("0", 2) + "/" + ("" + source.getDate()).padLeft("0", 2);
        //return ("" + source.getDate()).padLeft("0", 2) + "/" + ("" + (source.getMonth() + 1)).padLeft("0", 2) + "/" + source.getFullYear();
    }

    static FullDateFormat(source: Date)
    {
        if (!source)
            return "";
        return source.getFullYear() + "/" + ("" + (source.getMonth() + 1)).padLeft("0", 2) + "/" + ("" + source.getDate()).padLeft("0", 2) + " " +
            ("" + source.getHours()).padLeft("0", 2) + ":" + ("" + source.getMinutes()).padLeft("0", 2) + ":" + ("" + source.getSeconds()).padLeft("0", 2);
        /*return ("" + source.getDate()).padLeft("0", 2) + "/" + ("" + (source.getMonth() + 1)).padLeft("0", 2) + "/" + source.getFullYear() + " " +
            ("" + source.getHours()).padLeft("0", 2) + ":" + ("" + source.getMinutes()).padLeft("0", 2) + ":" + ("" + source.getSeconds()).padLeft("0", 2);*/
    }

    static ParseDate(source: string)
    {
        if (!source || source == "")
            return null;
        if (source.charAt(source.length - 1) == "Z")
            return new Date(source);
        source = source.replace(/\./g, "/").replace(/\-/g, "/");
        if (source.charAt(2) == "/")
        {
            return new Date(parseInt(source.substr(6, 4)), parseInt(source.substr(3, 2)) - 1, parseInt(source.substr(0, 2)));
        }
        else if (source.charAt(4) == "/")
        {
            return new Date(parseInt(source.substr(0, 4)), parseInt(source.substr(5, 2)) - 1, parseInt(source.substr(8, 2)));
        }
        return new Date(parseInt(source));
    }
}