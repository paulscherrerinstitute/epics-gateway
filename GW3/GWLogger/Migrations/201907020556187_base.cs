namespace GWLogger.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _base : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.GatewayFilterTypes",
                c => new
                    {
                        FilterId = c.Int(nullable: false),
                        Name = c.String(maxLength: 64),
                        Label1 = c.String(),
                        ClassName = c.String(),
                        FieldName = c.String(),
                    })
                .PrimaryKey(t => t.FilterId)
                .Index(t => t.Name, unique: true, name: "UNK_Filter");
            
            CreateTable(
                "dbo.GatewayGroupMembers",
                c => new
                    {
                        MbrId = c.Int(nullable: false, identity: true),
                        GrpId = c.Int(nullable: false),
                        FilterType = c.Int(),
                        Value1 = c.String(),
                    })
                .PrimaryKey(t => t.MbrId)
                .ForeignKey("dbo.GatewayFilterTypes", t => t.FilterType)
                .ForeignKey("dbo.GatewayGroups", t => t.GrpId, cascadeDelete: true)
                .Index(t => t.GrpId)
                .Index(t => t.FilterType);
            
            CreateTable(
                "dbo.GatewayGroups",
                c => new
                    {
                        GrpId = c.Int(nullable: false, identity: true),
                        GatewayId = c.Int(nullable: false),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.GrpId)
                .ForeignKey("dbo.Gateways", t => t.GatewayId, cascadeDelete: true)
                .Index(t => t.GatewayId);
            
            CreateTable(
                "dbo.Gateways",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        GatewayName = c.String(maxLength: 64),
                        Directions = c.Int(nullable: false),
                        LocalAddressA = c.String(),
                        RemoteAddressA = c.String(),
                        LocalAddressB = c.String(),
                        RemoteAddressB = c.String(),
                        Comment = c.String(unicode: false, storeType: "text"),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.GatewayName, unique: true, name: "UNK_Gateway");
            
            CreateTable(
                "dbo.GatewayRules",
                c => new
                    {
                        RuleId = c.Int(nullable: false, identity: true),
                        GatewayId = c.Int(nullable: false),
                        Side = c.String(),
                        CommentLine = c.String(),
                        Channel = c.String(),
                        Position = c.Int(),
                        FilterType = c.Int(),
                        Value1 = c.String(),
                        RuleAccess = c.String(),
                    })
                .PrimaryKey(t => t.RuleId)
                .ForeignKey("dbo.Gateways", t => t.GatewayId, cascadeDelete: true)
                .ForeignKey("dbo.GatewayFilterTypes", t => t.FilterType)
                .Index(t => t.GatewayId)
                .Index(t => t.FilterType);
            
            CreateTable(
                "dbo.GatewayHistories",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        GatewayName = c.String(maxLength: 64),
                        EntryDate = c.DateTime(nullable: false),
                        UserId = c.Int(nullable: false),
                        Configuration = c.String(unicode: false, storeType: "text"),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Username = c.String(maxLength: 64),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Username, unique: true, name: "UNK_Users");
            
            CreateTable(
                "dbo.UserRoles",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        RoleTypeId = c.Int(nullable: false),
                        ParamValue1 = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.RoleTypes", t => t.RoleTypeId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.RoleTypeId);
            
            CreateTable(
                "dbo.RoleTypes",
                c => new
                    {
                        Id = c.Int(nullable: false),
                        Name = c.String(),
                        ParamLabel1 = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.GatewayHistories", "UserId", "dbo.Users");
            DropForeignKey("dbo.UserRoles", "UserId", "dbo.Users");
            DropForeignKey("dbo.UserRoles", "RoleTypeId", "dbo.RoleTypes");
            DropForeignKey("dbo.GatewayGroupMembers", "GrpId", "dbo.GatewayGroups");
            DropForeignKey("dbo.GatewayRules", "FilterType", "dbo.GatewayFilterTypes");
            DropForeignKey("dbo.GatewayRules", "GatewayId", "dbo.Gateways");
            DropForeignKey("dbo.GatewayGroups", "GatewayId", "dbo.Gateways");
            DropForeignKey("dbo.GatewayGroupMembers", "FilterType", "dbo.GatewayFilterTypes");
            DropIndex("dbo.UserRoles", new[] { "RoleTypeId" });
            DropIndex("dbo.UserRoles", new[] { "UserId" });
            DropIndex("dbo.Users", "UNK_Users");
            DropIndex("dbo.GatewayHistories", new[] { "UserId" });
            DropIndex("dbo.GatewayRules", new[] { "FilterType" });
            DropIndex("dbo.GatewayRules", new[] { "GatewayId" });
            DropIndex("dbo.Gateways", "UNK_Gateway");
            DropIndex("dbo.GatewayGroups", new[] { "GatewayId" });
            DropIndex("dbo.GatewayGroupMembers", new[] { "FilterType" });
            DropIndex("dbo.GatewayGroupMembers", new[] { "GrpId" });
            DropIndex("dbo.GatewayFilterTypes", "UNK_Filter");
            DropTable("dbo.RoleTypes");
            DropTable("dbo.UserRoles");
            DropTable("dbo.Users");
            DropTable("dbo.GatewayHistories");
            DropTable("dbo.GatewayRules");
            DropTable("dbo.Gateways");
            DropTable("dbo.GatewayGroups");
            DropTable("dbo.GatewayGroupMembers");
            DropTable("dbo.GatewayFilterTypes");
        }
    }
}
