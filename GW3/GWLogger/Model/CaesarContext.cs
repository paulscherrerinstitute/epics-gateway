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
        static object initLock = new object();
        static bool IsInit = false;

        public CaesarContext() : base("name=CaesarConnection")
        {
            Initialize();
        }

        private void Initialize()
        {
            lock (initLock)
            {
                if (IsInit)
                    return;
                IsInit = true;
                if (this.GatewayFilterTypes.Count() != 5)
                {
                    this.GatewayFilterTypes.AddRange(new Model.GatewayFilterType[]
                    {
                new Model.GatewayFilterType { FilterId=1, Name = "Group", Label1 = "Group", ClassName = "GroupFilter", FieldName = "Name" },
                new Model.GatewayFilterType { FilterId=2, Name = "All", Label1 = null, ClassName = "AllFilter", FieldName = null },
                new Model.GatewayFilterType { FilterId=3, Name = "User", Label1 = "Username", ClassName = "UserFilter", FieldName = "Name" },
                new Model.GatewayFilterType { FilterId=4, Name = "Host", Label1 = "Hostname", ClassName = "HostFilter", FieldName = "Name" },
                new Model.GatewayFilterType { FilterId=5, Name = "IP", Label1 = "IP", ClassName = "IPFilter", FieldName = "IP" },
                    });
                    this.SaveChanges();
                }
            }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<GatewayGroup>()
            .HasOptional(p => p.GatewayGroupMembers)
            .WithMany()
            .WillCascadeOnDelete(true);
        }

        public virtual DbSet<GatewayEntry> Gateways { get; set; }
        public virtual DbSet<GatewayFilterType> GatewayFilterTypes { get; set; }
        public virtual DbSet<GatewayGroupMember> GatewayGroupMembers { get; set; }
        public virtual DbSet<GatewayGroup> GatewayGroups { get; set; }
        public virtual DbSet<GatewayRule> GatewayRules { get; set; }
        public virtual DbSet<GatewayHistoryEntry> GatewayHistories { get; set; }
    }
}