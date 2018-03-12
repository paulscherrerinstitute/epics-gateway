using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web;

namespace GWLogger.Backend.Model
{
    [Table("ConnectedServers")]
    [ExcludeFromCodeCoverage]
    public class ConnectedServer
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DateTime StartConnection { get; set; }
        public DateTime EndConnection { get; set; }
        public string Gateway { get; set; }
        public string RemoteIpPoint { get; set; }
    }
}