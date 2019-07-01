using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GWLogger
{
    /// <summary>
    /// Summary description for config
    /// </summary>
    public class Config : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/xml";
            context.Response.Write(Backend.Controllers.ConfigController.GetXmlConfiguration(context.Request.Path.Split('/')[2]));
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