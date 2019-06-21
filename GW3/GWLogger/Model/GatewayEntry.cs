using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace GWLogger.Model
{
    public enum GatewayDirection : int
    {
        Unidirectional = 0,
        Bidirectional = 1
    }

    [Table("Gateways")]
    [ExcludeFromCodeCoverage]
    public class GatewayEntry
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string GatewayName { get; set; }

        public GatewayDirection Directions { get; set; }

        public string LocalAddressA { get; set; }
        public string RemoteAddressA { get; set; }
        public string LocalAddressB { get; set; }
        public string RemoteAddressB { get; set; }

        [Column(TypeName = "text")]
        public string Comment { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<GatewayRule> GatewayRules { get; set; } = new HashSet<GatewayRule>();
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<GatewayGroup> GatewayGroups { get; set; } = new HashSet<GatewayGroup>();
    }
}