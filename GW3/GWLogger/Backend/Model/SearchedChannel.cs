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
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public long Id { get; set; }

        [Index("IDX_SearchedChannels", 1, IsUnique = true), MaxLength(40)]
        public string Gateway { get; set; }

        [Index("IDX_SearchedChannels", 2, IsUnique = true)]
        DateTime SearchDate { get; set; }

        [Index("IDX_SearchedChannels", 3, IsUnique = true), MaxLength(128)]
        public string Client { get; set; }

        [Index("IDX_SearchedChannels", 4, IsUnique = true), MaxLength(128)]
        public string Channel { get; set; }

        public int NbSearches { get; set; }
    }
}