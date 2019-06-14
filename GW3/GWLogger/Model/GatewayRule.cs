using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace GWLogger.Model
{
    [Table("GatewayRules")]
    [ExcludeFromCodeCoverage]
    public class GatewayRule
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RuleId { get; set; }
        public int GatewayId { get; set; }
        public string Side { get; set; }
        public string CommentLine { get; set; }
        public string Channel { get; set; }
        public int? Position { get; set; }
        public int? FilterType { get; set; }
        public string Value1 { get; set; }
        public string RuleAccess { get; set; }

        [ForeignKey("FilterType")]
        public virtual GatewayFilterType GwFilterType { get; set; }

        [ForeignKey("GatewayId")]
        public virtual GatewayEntry Gateway { get; set; }
    }
}