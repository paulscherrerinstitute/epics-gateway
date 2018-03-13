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
        [Key, Column(Order = 0)]
        public string Gateway { get; set; }
        [Key, Column(Order = 1)]
        public string RemoteIpPoint { get; set; }
        [Key, Column(Order = 2)]
        public DateTime StartConnection { get; set; }
        public DateTime? EndConnection { get; set; }
    }
}