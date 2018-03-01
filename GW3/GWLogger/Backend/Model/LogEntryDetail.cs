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
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long EntryDetailId { get; set; }

        public long LogEntryId { get; set; }

        public int DetailTypeId { get; set; }

        public string Value { get; set; }
    }
}