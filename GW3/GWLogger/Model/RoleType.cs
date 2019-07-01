using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web;

namespace GWLogger.Model
{
    [Table("RoleTypes")]
    [ExcludeFromCodeCoverage]
    public class RoleType
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public string Name { get; set; }

        public string ParamLabel1 { get; set; }
    }
}