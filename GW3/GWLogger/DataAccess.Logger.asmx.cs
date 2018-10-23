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
            if (System.Diagnostics.Debugger.IsAttached)
                Global.DataContext.Flush();
        }

        [WebMethod]
        public double GetBufferUsage()
        {
            return Global.DataContext.BufferUsage;
        }

        [WebMethod]
        public void LogEntry(string gateway, string remoteIpPoint, int messageType, List<Backend.DTOs.LogEntryDetail> details)
        {
            Backend.Controllers.LogController.LogEntry(gateway, remoteIpPoint, messageType, details);
        }

        [WebMethod]
        public FreeSpace GetFreeSpace()
        {
            ulong FreeBytesAvailable;
            ulong TotalNumberOfBytes;
            ulong TotalNumberOfFreeBytes;

            Backend.Controllers.DiskController.GetDiskFreeSpaceEx(Global.DataContext.StorageDirectory, out FreeBytesAvailable, out TotalNumberOfBytes, out TotalNumberOfFreeBytes);
            return new FreeSpace { TotMB = TotalNumberOfBytes / (1024 * 1024), FreeMB = FreeBytesAvailable / (1024 * 1024) };
        }

        public class FreeSpace
        {
            public ulong TotMB { get; set; }
            public ulong FreeMB { get; set; }
        }
    }
}