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
        public void RegisterLogMessageType(List<Backend.DTOs.IdValue> types)
        {
            Backend.Controllers.LogController.RegisterLogMessageType(types);
        }

        [WebMethod]
        public void RegisterLogMessageDetailType(List<Backend.DTOs.IdValue> types)
        {
            Backend.Controllers.LogController.RegisterLogMessageDetailType(types);
        }

        [WebMethod]
        public void LogEntry(string gateway, string remoteIpPoint, int messageType, List<Backend.DTOs.LogEntryDetail> details)
        {
            Backend.Controllers.LogController.LogEntry(gateway, remoteIpPoint, messageType, details);
        }
    }
}