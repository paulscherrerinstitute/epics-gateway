using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;

namespace GWLogger
{
    /// <summary>
    /// Summary description for introspection
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Introspection : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            if (context.Request.RawUrl.EndsWith("?description"))
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                context.Response.Write("[");
                var webMethods = typeof(DataAccess).GetMethods()
                    .Where(row => row.CustomAttributes.Any(r2 => r2.AttributeType == typeof(System.Web.Services.WebMethodAttribute)))
                    .OrderBy(row => ((CategoryAttribute)(row.GetCustomAttributes(typeof(CategoryAttribute), false).FirstOrDefault()))?.Category)
                    .ThenBy(row => row.Name)
                    .ToList();
                var isFirst = true;
                foreach (var i in webMethods)
                {
                    if (!isFirst)
                        context.Response.Write(",");
                    isFirst = false;
                    DescribeMethod(context, i);
                }
                context.Response.Write("]");
                return;
            }
            else if (context.Request.PathInfo != "")
            {
                MethodInterface(context, context.Request.PathInfo.Substring(1));
            }
            else
                HomeInterface(context);
        }

        private void MethodInterface(HttpContext context, string methodName)
        {
            var method = typeof(DataAccess).GetMethod(methodName);
            context.Response.ContentType = "text/html";
            context.Response.Write("<html><head><title>Logger - JSON introspection</title><link href='/Less/introspect.css' type='text/css' rel='stylesheet'></head>");
            context.Response.Write("<body>");
            context.Response.Write("<h1>" + methodName + "</h1>");
            context.Response.Write("<a href='/Introspection.ashx'>Back to the service list</a><br>");
            context.Response.Write("<div id='methodParameters'></div>");
            context.Response.Write("<div id='prototype'></div>");
            context.Response.Write("<div id='result'></div>");
            context.Response.Write("<script src='/Scripts/jquery-3.3.1.min.js'></script>");
            context.Response.Write("<script>var method=");
            DescribeMethod(context, method);
            context.Response.Write(";</script>");
            context.Response.Write("<script src='/Frontend/introspection.js'></script>");
            context.Response.Write("<script>showParameters();</script>");
            context.Response.Write("</body></html>");
        }

        class FunctionInformation
        {
            public string Category { get; internal set; }
            public string Name { get; internal set; }
            public string Description { get; internal set; }

            public string ToJson()
            {
                return "{\"Category\":\"" + this.Category + "\"," +
                    "\"Name\":\"" + this.Name + "\"," +
                    "\"Description\":\"" + this.Description?.Replace("\\", "\\\\")?.Replace("\"", "\\\"") + "\"}";
            }
        }

        void HomeInterface(HttpContext context)
        {
            context.Response.ContentType = "text/html";
            context.Response.Write("<html><head><title>Inventory - JSON introspection</title><link href='/Less/introspect.css' type='text/css' rel='stylesheet'></head>");
            context.Response.Write("<body>");
            context.Response.Write("<a href='/Introspection.ashx?description'>JSON description of all the services</a><br>");
            context.Response.Write("<a href='/DataAccess.asmx?WSDL'>WSDL (SOAP) description of all the services</a><br>");
            context.Response.Write("<br>");
            context.Response.Write("<input type='text' id='searchFunction' onkeyup='showInfos();' placeholder='Search...'><br>");

            var functions = typeof(DataAccess).GetMethods()
                .Where(row => row.CustomAttributes.Any(r2 => r2.AttributeType == typeof(System.Web.Services.WebMethodAttribute)))
                .Select(row => new FunctionInformation
                {
                    Category = ((CategoryAttribute)(row.GetCustomAttributes(typeof(CategoryAttribute), false).FirstOrDefault()))?.Category,
                    Name = row.Name,
                    Description = row.GetCustomAttributes(false).ToList().OfType<DescriptionAttribute>().FirstOrDefault()?.Description
                })
                .OrderBy(row => row.Category)
                .ThenBy(row => row.Name)
                .ToList();

            context.Response.Write("<div id='functionList'></div>");
            context.Response.Write("<script src='/Frontend/introspection.js'></script>");
            context.Response.Write("<script>var infos=[" + string.Join(",", functions.Select(row => row.ToJson())) + "];\nshowInfos();</script>");
            context.Response.Write("</body></html>");
        }

        private void DescribeMethod(HttpContext context, System.Reflection.MethodInfo method)
        {
            context.Response.Write("{");
            context.Response.Write("\"Name\":\"" + method.Name + "\"");
            context.Response.Write(",\"Parameters\":[");
            var isFirst = true;
            foreach (var i in method.GetParameters())
            {
                if (!isFirst)
                    context.Response.Write(",");
                isFirst = false;
                context.Response.Write("{");
                context.Response.Write("\"Name\":\"" + i.Name + "\"");
                context.Response.Write(",\"Type\":");
                DescribeType(context, i.ParameterType);
                context.Response.Write("}");
            }
            context.Response.Write("]");
            context.Response.Write(",\"DemoParameters\":[");
            context.Response.Write("]");
            context.Response.Write(",\"Result\":");
            DescribeType(context, method.ReturnType);
            context.Response.Write("}");
        }

        private bool DescribeBaseType(HttpContext context, Type type)
        {
            switch (type.Name)
            {
                case "String":
                    context.Response.Write("{\"Type\":\"string\"}");
                    return true;
                case "Boolean":
                    context.Response.Write("{\"Type\":\"boolean\"}");
                    return true;
                case "Int64":
                case "Int32":
                case "Int16":
                case "Single":
                case "Double":
                case "Decimal":
                    context.Response.Write("{\"Type\":\"number\"}");
                    return true;
                case "DateTime":
                    context.Response.Write("{\"Type\":\"date\"}");
                    return true;
                default:
                    return false;
            }
        }

        private void DescribeType(HttpContext context, Type type, List<string> knownTypes = null)
        {
            if (knownTypes == null)
                knownTypes = new List<string>();

            if (DescribeBaseType(context, type))
                return;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                DescribeBaseType(context, type.GetGenericArguments().First());
                return;
            }
            // An array
            else if (type.IsArray)
            {
                context.Response.Write("{\"Type\":\"array\"");
                if (knownTypes.Contains(type.GetElementType().Name))
                {
                    context.Response.Write(",\"OfClassType\":\"" + type.GetElementType().Name + "\"");
                }
                else
                {
                    context.Response.Write(",\"OfType\":");
                    DescribeType(context, type.GetElementType(), knownTypes);
                    if (type.GetElementType().Namespace.StartsWith("IV4"))
                        knownTypes.Add(type.GetElementType().Name);
                }
                context.Response.Write("}");
            }
            // A list
            else if (type.IsGenericType)
            {
                context.Response.Write("{\"Type\":\"array\"");
                if (knownTypes.Contains(type.GetGenericArguments().First().Name))
                {
                    context.Response.Write(",\"OfClassType\":\"" + type.GetGenericArguments().First().Name + "\"");
                }
                else
                {
                    context.Response.Write(",\"OfType\":");
                    DescribeType(context, type.GetGenericArguments().First(), knownTypes);
                    if (type.GetGenericArguments().First().Namespace.StartsWith("IV4"))
                        knownTypes.Add(type.GetGenericArguments().First().Name);
                }
                context.Response.Write("}");
            }
            else
            {
                knownTypes.Add(type.Name);
                context.Response.Write("{\"Type\":\"class\"");
                context.Response.Write(",\"ClassName\":\"" + type.Name + "\"");
                context.Response.Write(",\"NameSpace\":\"" + type.Namespace + "\"");
                context.Response.Write(",\"Properties\":[");
                var isFirst = true;
                foreach (var i in type.GetProperties())
                {
                    if (i.GetCustomAttributes(typeof(System.Web.Script.Serialization.ScriptIgnoreAttribute), true).Any())
                        continue;
                    if (!isFirst)
                        context.Response.Write(",");
                    isFirst = false;
                    context.Response.Write("{\"Name\":\"" + i.Name + "\"");
                    if (knownTypes.Contains(i.PropertyType.Name) && i.PropertyType.Namespace.StartsWith("IV4"))
                    {
                        context.Response.Write(",\"ClassType\":\"" + i.PropertyType.Name + "\"");
                    }
                    else
                    {
                        context.Response.Write(",\"Type\":");
                        var fName = i.Name;
                        DescribeType(context, i.PropertyType, knownTypes);
                    }
                    context.Response.Write("}");
                }
                context.Response.Write("]}");
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}