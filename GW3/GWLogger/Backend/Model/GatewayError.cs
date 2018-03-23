﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web;

namespace GWLogger.Backend.Model
{
    [Table("GatewayErrors")]
    [ExcludeFromCodeCoverage]
    public class GatewayError
    {
        [Key, Column(Order = 0), MaxLength(40)]
        public string Gateway { get; set; }
        [Key, Column(Order = 1)]
        public DateTime Date { get; set; }
        public int NbErrors { get; set; }
    }
}