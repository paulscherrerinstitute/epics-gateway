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
                        Gateway = c.String(nullable: false, maxLength: 40),
                        RemoteIpPoint = c.String(nullable: false, maxLength: 128),
                        StartConnection = c.DateTime(nullable: false),
                        EndConnection = c.DateTime(),
                    })
                .PrimaryKey(t => new { t.Gateway, t.RemoteIpPoint, t.StartConnection });
            
            CreateTable(
                "dbo.ConnectedServers",
                c => new
                    {
                        Gateway = c.String(nullable: false, maxLength: 40),
                        RemoteIpPoint = c.String(nullable: false, maxLength: 128),
                        StartConnection = c.DateTime(nullable: false),
                        EndConnection = c.DateTime(),
                    })
                .PrimaryKey(t => new { t.Gateway, t.RemoteIpPoint, t.StartConnection });
            
            CreateTable(
                "dbo.GatewayErrors",
                c => new
                    {
                        Gateway = c.String(nullable: false, maxLength: 40),
                        Date = c.DateTime(nullable: false),
                        NbErrors = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Gateway, t.Date });
            
            CreateTable(
                "dbo.GatewayNbMessages",
                c => new
                    {
                        Gateway = c.String(nullable: false, maxLength: 40),
                        Date = c.DateTime(nullable: false),
                        NbMessages = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Gateway, t.Date });
            
            CreateTable(
                "dbo.GatewaySearches",
                c => new
                    {
                        Gateway = c.String(nullable: false, maxLength: 40),
                        Date = c.DateTime(nullable: false),
                        NbSearches = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Gateway, t.Date });
            
            CreateTable(
                "dbo.GatewaySessions",
                c => new
                    {
                        Gateway = c.String(nullable: false, maxLength: 40),
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
                        Name = c.String(maxLength: 64),
                    })
                .PrimaryKey(t => t.ItemId);
            
            CreateTable(
                "dbo.LogEntries",
                c => new
                    {
                        EntryId = c.Long(nullable: false),
                        EntryDate = c.DateTime(nullable: false),
                        Gateway = c.String(maxLength: 40),
                        RemoteIpPoint = c.String(maxLength: 128),
                        MessageTypeId = c.Int(nullable: false),
                        TrimmedDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.EntryId)
                .ForeignKey("dbo.LogMessageTypes", t => t.MessageTypeId, cascadeDelete: true)
                .Index(t => t.MessageTypeId);
            
            CreateTable(
                "dbo.LogEntryDetails",
                c => new
                    {
                        EntryDetailId = c.Long(nullable: false, identity: true),
                        LogEntryId = c.Long(nullable: false),
                        DetailTypeId = c.Int(nullable: false),
                        Value = c.String(maxLength: 255),
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
                        Name = c.String(maxLength: 64),
                        DisplayMask = c.String(maxLength: 1024),
                        LogLevel = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.MessageTypeId);
            
            CreateTable(
                "dbo.SearchedChannels",
                c => new
                    {
                        Id = c.Long(nullable: false),
                        Gateway = c.String(maxLength: 40),
                        Client = c.String(maxLength: 128),
                        Channel = c.String(maxLength: 128),
                        NbSearches = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => new { t.Gateway, t.Client, t.Channel }, unique: true, name: "IDX_SearchedChannels");
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.LogEntries", "MessageTypeId", "dbo.LogMessageTypes");
            DropForeignKey("dbo.LogEntryDetails", "LogEntryId", "dbo.LogEntries");
            DropForeignKey("dbo.LogEntryDetails", "DetailTypeId", "dbo.LogDetailItemTypes");
            DropIndex("dbo.SearchedChannels", "IDX_SearchedChannels");
            DropIndex("dbo.LogEntryDetails", new[] { "DetailTypeId" });
            DropIndex("dbo.LogEntryDetails", new[] { "LogEntryId" });
            DropIndex("dbo.LogEntries", new[] { "MessageTypeId" });
            DropTable("dbo.SearchedChannels");
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
