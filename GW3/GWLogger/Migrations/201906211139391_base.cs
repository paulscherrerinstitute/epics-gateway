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
                    Name = c.String(),
                    Label1 = c.String(),
                    ClassName = c.String(),
                    FieldName = c.String(),
                })
                .PrimaryKey(t => t.FilterId);

            CreateTable(
                "dbo.GatewayGroupMembers",
                c => new
                {
                    MbrId = c.Int(nullable: false, identity: true),
                    GrpId = c.Int(),
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
                    GatewayName = c.String(),
                    Directions = c.Int(nullable: false),
                    LocalAddressA = c.String(),
                    RemoteAddressA = c.String(),
                    LocalAddressB = c.String(),
                    RemoteAddressB = c.String(),
                    Comment = c.String(unicode: false, storeType: "text"),
                })
                .PrimaryKey(t => t.Id);

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
                    GatewayName = c.String(),
                    EntryDate = c.DateTime(nullable: false),
                    Configuration = c.String(unicode: false, storeType: "text"),
                })
                .PrimaryKey(t => t.Id);
        }

        public override void Down()
        {
            DropForeignKey("dbo.GatewayGroupMembers", "GrpId", "dbo.GatewayGroups");
            DropForeignKey("dbo.GatewayRules", "FilterType", "dbo.GatewayFilterTypes");
            DropForeignKey("dbo.GatewayRules", "GatewayId", "dbo.Gateways");
            DropForeignKey("dbo.GatewayGroups", "GatewayId", "dbo.Gateways");
            DropForeignKey("dbo.GatewayGroupMembers", "FilterType", "dbo.GatewayFilterTypes");
            DropIndex("dbo.GatewayRules", new[] { "FilterType" });
            DropIndex("dbo.GatewayRules", new[] { "GatewayId" });
            DropIndex("dbo.GatewayGroups", new[] { "GatewayId" });
            DropIndex("dbo.GatewayGroupMembers", new[] { "FilterType" });
            DropIndex("dbo.GatewayGroupMembers", new[] { "GrpId" });
            DropTable("dbo.GatewayHistories");
            DropTable("dbo.GatewayRules");
            DropTable("dbo.Gateways");
            DropTable("dbo.GatewayGroups");
            DropTable("dbo.GatewayGroupMembers");
            DropTable("dbo.GatewayFilterTypes");
        }
    }
}
