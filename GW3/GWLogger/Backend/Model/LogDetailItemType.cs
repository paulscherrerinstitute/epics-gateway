using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web;

namespace GWLogger.Backend.Model
{
    [Table("LogDetailItemTypes")]
    [ExcludeFromCodeCoverage]
    public class LogDetailItemType
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ItemId { get; set; }

        [MaxLength(64)]
        public string Name { get; set; }
    }
}