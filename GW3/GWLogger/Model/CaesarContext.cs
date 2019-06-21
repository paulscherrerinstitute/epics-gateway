using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using GWLogger.Live;

namespace GWLogger.Model
{
    public class CaesarContext : DbContext
    {
        public CaesarContext() : base("name=CaesarConnection")
        {
        }

        public virtual DbSet<GatewayEntry> Gateways { get; set; }
        public virtual DbSet<GatewayFilterType> GatewayFilterTypes { get; set; }
        public virtual DbSet<GatewayGroupMember> GatewayGroupMembers { get; set; }
        public virtual DbSet<GatewayGroup> GatewayGroups { get; set; }
        public virtual DbSet<GatewayRule> GatewayRules { get; set; }
        public virtual DbSet<GatewayHistoryEntry> GatewayHistories { get; set; }
    }
}