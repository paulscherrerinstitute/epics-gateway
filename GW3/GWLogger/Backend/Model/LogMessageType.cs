using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web;

namespace GWLogger.Backend.Model
{
    [Table("LogMessageTypes")]
    [ExcludeFromCodeCoverage]
    public class LogMessageType
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int MessageTypeId { get; set; }

        [MaxLength(64)]
        public string Name { get; set; }

        [MaxLength(1024)]
        public string DisplayMask { get; set; }

        public int LogLevel { get; set; }
    }
}