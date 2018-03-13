namespace GWLogger.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _base : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ConnectedClients",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        StartConnection = c.DateTime(nullable: false),
                        EndConnection = c.DateTime(nullable: false),
                        Gateway = c.String(),
                        RemoteIpPoint = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ConnectedServers",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        StartConnection = c.DateTime(nullable: false),
                        EndConnection = c.DateTime(nullable: false),
                        Gateway = c.String(),
                        RemoteIpPoint = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.GatewayErrors",
                c => new
                    {
                        Gateway = c.String(nullable: false, maxLength: 128),
                        Date = c.DateTime(nullable: false),
                        NbErrors = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Gateway, t.Date });
            
            CreateTable(
                "dbo.GatewayNbMessages",
                c => new
                    {
                        Gateway = c.String(nullable: false, maxLength: 128),
                        Date = c.DateTime(nullable: false),
                        NbMessages = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Gateway, t.Date });
            
            CreateTable(
                "dbo.GatewaySearches",
                c => new
                    {
                        Gateway = c.String(nullable: false, maxLength: 128),
                        Date = c.DateTime(nullable: false),
                        NbSearches = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Gateway, t.Date });
            
            CreateTable(
                "dbo.GatewaySessions",
                c => new
                    {
                        Gateway = c.String(nullable: false, maxLength: 128),
                        StartDate = c.DateTime(nullable: false),
                        NbEntries = c.Long(nullable: false),
                        LastEntry = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => new { t.Gateway, t.StartDate });
            
            CreateTable(
                "dbo.LogDetailItemTypes",
                c => new
                    {
                        ItemId = c.Int(nullable: false),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.ItemId);
            
            CreateTable(
                "dbo.LogEntries",
                c => new
                    {
                        EntryId = c.Guid(nullable: false),
                        EntryDate = c.DateTime(nullable: false),
                        Gateway = c.String(),
                        RemoteIpPoint = c.String(),
                        MessageTypeId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.EntryId)
                .ForeignKey("dbo.LogMessageTypes", t => t.MessageTypeId, cascadeDelete: true)
                .Index(t => t.MessageTypeId);
            
            CreateTable(
                "dbo.LogEntryDetails",
                c => new
                    {
                        EntryDetailId = c.Guid(nullable: false),
                        LogEntryId = c.Guid(nullable: false),
                        DetailTypeId = c.Int(nullable: false),
                        Value = c.String(),
                    })
                .PrimaryKey(t => t.EntryDetailId)
                .ForeignKey("dbo.LogDetailItemTypes", t => t.DetailTypeId, cascadeDelete: true)
                .ForeignKey("dbo.LogEntries", t => t.LogEntryId, cascadeDelete: true)
                .Index(t => t.LogEntryId)
                .Index(t => t.DetailTypeId);
            
            CreateTable(
                "dbo.LogMessageTypes",
                c => new
                    {
                        MessageTypeId = c.Int(nullable: false),
                        Name = c.String(),
                        DisplayMask = c.String(),
                        LogLevel = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.MessageTypeId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.LogEntries", "MessageTypeId", "dbo.LogMessageTypes");
            DropForeignKey("dbo.LogEntryDetails", "LogEntryId", "dbo.LogEntries");
            DropForeignKey("dbo.LogEntryDetails", "DetailTypeId", "dbo.LogDetailItemTypes");
            DropIndex("dbo.LogEntryDetails", new[] { "DetailTypeId" });
            DropIndex("dbo.LogEntryDetails", new[] { "LogEntryId" });
            DropIndex("dbo.LogEntries", new[] { "MessageTypeId" });
            DropTable("dbo.LogMessageTypes");
            DropTable("dbo.LogEntryDetails");
            DropTable("dbo.LogEntries");
            DropTable("dbo.LogDetailItemTypes");
            DropTable("dbo.GatewaySessions");
            DropTable("dbo.GatewaySearches");
            DropTable("dbo.GatewayNbMessages");
            DropTable("dbo.GatewayErrors");
            DropTable("dbo.ConnectedServers");
            DropTable("dbo.ConnectedClients");
        }
    }
}
