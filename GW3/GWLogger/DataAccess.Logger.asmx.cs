using System.Collections.Generic;
using System.IO;
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
        public void BinaryLogEntries(string gateway, byte[] data)
        {
            using (var reader = new BinaryReader(new MemoryStream(data)))
            {
                var nbEntries = reader.ReadUInt32();
                for (var i = 0; i < nbEntries; i++)
                {
                    var ipBytes = reader.ReadBytes(4);
                    string ip = null;
                    if (ipBytes[0] == 0 && ipBytes[1] == 0)
                        reader.ReadInt16();
                    else
                        ip = new System.Net.IPAddress(ipBytes).ToString() + ":" + reader.ReadUInt16();
                    var msgType = reader.ReadUInt16();
                    var details = new List<Backend.DTOs.LogEntryDetail>();
                    var nbDetails = reader.ReadByte();
                    for (var j = 0; j < nbDetails; j++)
                        details.Add(new Backend.DTOs.LogEntryDetail { TypeId = reader.ReadUInt16(), Value = reader.ReadString() });
                    Backend.Controllers.LogController.LogEntry(gateway, ip, msgType, details);
                }
            }
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