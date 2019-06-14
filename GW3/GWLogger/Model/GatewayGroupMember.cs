using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace GWLogger.Model
{
    [Table("GatewayGroupMembers")]
    [ExcludeFromCodeCoverage]
    public class GatewayGroupMember
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MbrId { get; set; }
        public int? GrpId { get; set; }
        public int? FilterType { get; set; }
        public string Value1 { get; set; }

        [ForeignKey("FilterType")]
        public virtual GatewayFilterType GatewayFilterType { get; set; }
        [ForeignKey("GrpId")]
        public virtual GatewayGroup GatewayGroup { get; set; }
    }
}