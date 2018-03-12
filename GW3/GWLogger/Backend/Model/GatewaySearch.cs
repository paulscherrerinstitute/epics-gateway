using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web;

namespace GWLogger.Backend.Model
{
    [Table("GatewaySearches")]
    [ExcludeFromCodeCoverage]
    public class GatewaySearch
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DateTime SearchDate { get; set; }
        public string Gateway { get; set; }
        public int NbSearches { get; set; }
    }
}