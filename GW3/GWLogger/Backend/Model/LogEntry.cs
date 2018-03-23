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
        public LogEntry()
        {
            this.LogEntryDetails = new HashSet<LogEntryDetail>();
        }

        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long EntryId { get; set; }

        public DateTime EntryDate { get; set; }

        [MaxLength(40)]
        public string Gateway { get; set; }

        [MaxLength(128)]
        public string RemoteIpPoint { get; set; }

        public int MessageTypeId { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime TrimmedDate { get; set; }

        [ForeignKey("MessageTypeId")]
        public LogMessageType LogMessageType { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<LogEntryDetail> LogEntryDetails { get; set; }
    }
}