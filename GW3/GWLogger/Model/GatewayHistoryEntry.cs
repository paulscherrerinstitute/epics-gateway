using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace GWLogger.Model
{
    [Table("GatewayHistories")]
    [ExcludeFromCodeCoverage]
    public class GatewayHistoryEntry
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [MaxLength(64)]
        public string GatewayName { get; set; }
        public DateTime EntryDate { get; set; }
        public int UserId { get; set; }
        [Column(TypeName = "text")]
        public string Configuration { get; set; }
        [ForeignKey("UserId")]
        public virtual CaesarUser User { get; set; }
    }
}