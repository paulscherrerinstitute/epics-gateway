using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web;

namespace GWLogger.Backend.Model
{
    [Table("GatewaySessions")]
    [ExcludeFromCodeCoverage]
    public class GatewaySession
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime StartDate { get; set; }

        public string Gateway { get; set; }

        public long NbEntries { get; set; }

        public DateTime LastEntry { get; set; }
    }
}