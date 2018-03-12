using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web;

namespace GWLogger.Backend.Model
{
    [Table("GatewayNbMessages")]
    [ExcludeFromCodeCoverage]
    public class GatewayNbMessage
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DateTime SearchDate { get; set; }
        public string Gateway { get; set; }
        public int NbMessages { get; set; }
    }
}