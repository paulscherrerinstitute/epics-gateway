using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace GWLogger.Model
{
    [Table("GatewayGroups")]
    [ExcludeFromCodeCoverage]
    public class GatewayGroup
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GrpId { get; set; }
        public int GatewayId { get; set; }
        public string Name { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<GatewayGroupMember> GatewayGroupMembers { get; set; } = new HashSet<GatewayGroupMember>();

        [ForeignKey("GatewayId")]
        public virtual GatewayEntry Gateway { get; set; }
    }
}