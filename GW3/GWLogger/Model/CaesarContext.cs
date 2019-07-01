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
                try
                {
                    if (this.GatewayFilterTypes.Count() != 5)
                    {
                        this.GatewayFilterTypes.AddRange(new GatewayFilterType[]
                        {
                            new GatewayFilterType { FilterId=1, Name = "Group", Label1 = "Group", ClassName = "GroupFilter", FieldName = "Name" },
                            new GatewayFilterType { FilterId=2, Name = "All", Label1 = null, ClassName = "AllFilter", FieldName = null },
                            new GatewayFilterType { FilterId=3, Name = "User", Label1 = "Username", ClassName = "UserFilter", FieldName = "Name" },
                            new GatewayFilterType { FilterId=4, Name = "Host", Label1 = "Hostname", ClassName = "HostFilter", FieldName = "Name" },
                            new GatewayFilterType { FilterId=5, Name = "IP", Label1 = "IP", ClassName = "IPFilter", FieldName = "IP" },
                        });

                        this.RoleTypes.AddRange(new RoleType[]
                        {
                            new RoleType{Id=50, Name="Restarter", ParamLabel1="Gateway"},
                            new RoleType{Id=100, Name="Piquet"},
                            new RoleType{Id=200, Name="Configurator", ParamLabel1="Gateway"},
                            new RoleType{Id=500, Name="Super Configurator"},
                            new RoleType{Id=1000, Name="Administrator" }
                        });
                        this.SaveChanges();
                    }
                }
                catch
                {
                }
            }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GatewayGroupMember>()
                .HasRequired(p => p.GatewayGroup)
                .WithMany(p => p.GatewayGroupMembers)
                .WillCascadeOnDelete();
        }

        public virtual DbSet<GatewayEntry> Gateways { get; set; }
        public virtual DbSet<GatewayFilterType> GatewayFilterTypes { get; set; }
        public virtual DbSet<GatewayGroupMember> GatewayGroupMembers { get; set; }
        public virtual DbSet<GatewayGroup> GatewayGroups { get; set; }
        public virtual DbSet<GatewayRule> GatewayRules { get; set; }
        public virtual DbSet<GatewayHistoryEntry> GatewayHistories { get; set; }
        public virtual DbSet<CaesarUser> Users { get; set; }
        public virtual DbSet<UserRole> Roles { get; set; }
        public virtual DbSet<RoleType> RoleTypes { get; set; }
    }
}