using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace GWLogger
{
    public partial class DataAccess : System.Web.Services.WebService
    {
        [WebMethod]
        public void LogEntry(string gateway, string remoteIpPoint, int messageType, List<Backend.DTOs.LogEntryDetail> details)
        {
            Backend.Controllers.LogController.LogEntry(gateway, remoteIpPoint, messageType, details);
        }
    }
}