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
        public void RegisterLogMessageType(List<Backend.DTOs.MessageType> types)
        {
            Backend.Controllers.LogController.RegisterLogMessageType(types);
        }

        [WebMethod]
        public void RegisterLogMessageDetailType(List<Backend.DTOs.IdValue> types)
        {
            Backend.Controllers.LogController.RegisterLogMessageDetailType(types);
        }

        [WebMethod]
        public void LogEntries(List<Backend.DTOs.LogEntry> logEntries)
        {
            foreach (var i in logEntries)
                Backend.Controllers.LogController.LogEntry(i.Gateway, i.RemoteIpPoint, i.MessageType, i.Details);
        }

        [WebMethod]
        public void LogEntry(string gateway, string remoteIpPoint, int messageType, List<Backend.DTOs.LogEntryDetail> details)
        {
            Backend.Controllers.LogController.LogEntry(gateway, remoteIpPoint, messageType, details);
        }

        [WebMethod]
        public long GetFreeSpace()
        {
            return 0L;
        }
    }
}