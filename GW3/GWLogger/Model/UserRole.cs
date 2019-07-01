using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web;

namespace GWLogger.Model
{
    [Table("UserRoles")]
    [ExcludeFromCodeCoverage]
    public class UserRole
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RoleTypeId { get; set; }
        public string ParamValue1 { get; set; }

        [ForeignKey("UserId")]
        public virtual CaesarUser User { get; set; }
        [ForeignKey("RoleTypeId")]
        public virtual RoleType RoleType { get; set; }
    }
}