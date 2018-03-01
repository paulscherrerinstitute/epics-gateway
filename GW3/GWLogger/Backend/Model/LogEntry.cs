using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web;

namespace GWLogger.Backend.Model
{
    [Table("LogEntries")]
    [ExcludeFromCodeCoverage]
    public class LogEntry
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long EntryId { get; set; }

        public DateTime EntryDate { get; set; }

        public string Gateway { get; set; }

        public string RemoteIpPoint { get; set; }

        public int MessageTypeId { get; set; }
    }
}