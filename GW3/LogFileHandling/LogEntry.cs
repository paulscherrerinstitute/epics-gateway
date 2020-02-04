using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace GWLogger.Backend.DataContext
{
    public class LogEntry
    {
        public string CurrentFile { get; set; }
        private DateTime? entryDate = null;
        public DateTime EntryDate
        {
            get
            {
                if (!entryDate.HasValue)
                    entryDate = DateTime.FromBinary(BitConverter.ToInt64(Buff, 0));
                return entryDate.Value;
            }
            set
            {
                entryDate = value;
            }
        }

        // SourceMemberName
        private static int sourceMemberNameId = -1;

        // SourceFilePath
        private static int sourceFilePathId = -1;

        public string Gateway { get; set; }
        private List<LogEntryDetail> details = null;
        public List<LogEntryDetail> LogEntryDetails
        {
            get
            {
                if (details == null)
                {
                    if (Buff == null)
                        details = new List<LogEntryDetail>();
                    else
                    {
                        if (sourceMemberNameId == -1)
                            sourceMemberNameId = Context.MessageDetailTypes.First(row => row.Value == "SourceMemberName").Id;
                        if (sourceFilePathId == -1)
                            sourceFilePathId = Context.MessageDetailTypes.First(row => row.Value == "SourceFilePath").Id;
                        details = new List<LogEntryDetail>();

                        int nb = Buff[15];
                        int pos = 16;
                        for (var i = 0; i < nb && pos < Buff.Length - 2; i++)
                        {
                            int tp = Buff[pos];
                            var detail = new LogEntryDetail
                            {
                                DetailTypeId = tp,
                            };
                            var n = BitConverter.ToUInt16(Buff, pos + 1);
                            pos += 3;
                            if (tp == sourceFilePathId)
                            {
                                lock (Context.filePaths)
                                {
                                    if (Context.reverseFilePaths.ContainsKey(n))
                                        detail.Value = Context.reverseFilePaths[n];
                                }
                            }
                            else if (tp == sourceMemberNameId)
                            {
                                lock (Context.filePaths)
                                {
                                    if (Context.reverseMemberNames.ContainsKey(n))
                                        detail.Value = Context.reverseMemberNames[n];
                                }
                            }
                            else
                            {
                                if(Buff.Length >= pos+n)
                                    detail.Value = System.Text.Encoding.ASCII.GetString(Buff, pos, n);
                                pos += n;
                            }
                            details.Add(detail);
                        }
                    }
                }
                return details;
            }
            set
            {
                details = value;
            }
        }
        private int? messageTypeId = null;
        public int MessageTypeId
        {
            get
            {
                if (!messageTypeId.HasValue)
                    messageTypeId = (int)Buff[8];
                return messageTypeId.Value;
            }
            set
            {
                messageTypeId = value;
            }
        }
        public long Position { get; set; }
        private string remoteIpPoint = null;
        public string RemoteIpPoint
        {
            get
            {
                if (remoteIpPoint == null && Ip != null)
                    remoteIpPoint = Ip.ToString() + ":" + Port.ToString();
                return remoteIpPoint;
            }
            set
            {
                remoteIpPoint = value;
            }
        }
        private IPAddress ip = null;
        public IPAddress Ip
        {
            get
            {
                if (ip == null)
                    ip = new System.Net.IPAddress(Buff.Skip(9).Take(4).ToArray());
                return ip;
            }
            set
            {
                ip = value;
            }
        }

        private int? port;
        public int Port
        {
            get
            {
                if (!port.HasValue)
                    port = BitConverter.ToUInt16(Buff, 13);
                return port.Value;
            }
            set
            {
                port = value;
            }
        }
        // Read from the file, not yet decoded
        public byte[] Buff { get; set; }
        public Context Context { get; set; }
    }
}