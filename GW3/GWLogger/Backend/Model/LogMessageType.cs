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
        public LogMessageType()
        {
            this.LogDetailItemTypes = new HashSet<LogDetailItemType>();
        }

        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MessageTypeId { get; set; }

        public string Name { get; set; }

        public string DisplayMask { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<LogDetailItemType> LogDetailItemTypes { get; set; }
    }
}