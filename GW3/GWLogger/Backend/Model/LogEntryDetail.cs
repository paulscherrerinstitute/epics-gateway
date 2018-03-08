using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web;

namespace GWLogger.Backend.Model
{
    [Table("LogEntryDetails")]
    [ExcludeFromCodeCoverage]
    public class LogEntryDetail
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid EntryDetailId { get; set; }

        public Guid LogEntryId { get; set; }

        public int DetailTypeId { get; set; }

        public string Value { get; set; }

        [ForeignKey("LogEntryId")]
        public LogEntry LogEntry { get; set; }

        [ForeignKey("DetailTypeId")]
        public LogDetailItemType LogDetailItemType { get; set; }
    }
}