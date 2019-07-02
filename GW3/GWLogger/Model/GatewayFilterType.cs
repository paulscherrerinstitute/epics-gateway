using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace GWLogger.Model
{
    [Table("GatewayFilterTypes")]
    [ExcludeFromCodeCoverage]
    public class GatewayFilterType
    {
        [Key, Column(Order = 0), DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int FilterId { get; set; }
        [Index("UNK_Filter", 1, IsUnique = true), MaxLength(64)]
        public string Name { get; set; }
        public string Label1 { get; set; }
        public string ClassName { get; set; }
        public string FieldName { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<GatewayGroupMember> GatewayGroupMembers { get; set; } = new HashSet<GatewayGroupMember>();
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<GatewayRule> GatewayRules { get; set; } = new HashSet<GatewayRule>();
    }
}