using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web;

namespace GWLogger.Backend.Model
{
    [Table("SearchedChannels")]
    [ExcludeFromCodeCoverage]
    public class SearchedChannel
    {
        [Key, Column(Order = 1)]
        public DateTime SearchDate { get; set; }

        [Key, Column(Order = 2)]
        public string Gateway { get; set; }

        [Key, Column(Order = 3)]
        public string Client { get; set; }

        [Key, Column(Order = 4)]
        public string Channel { get; set; }

        public int NbSearches { get; set; }
    }
}