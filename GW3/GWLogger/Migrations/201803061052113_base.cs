namespace GWLogger.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _base : DbMigration
    {
        public override void Up()
        {
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
                        EntryId = c.Long(nullable: false, identity: true),
                        EntryDate = c.DateTime(nullable: false),
                        Gateway = c.String(),
                        RemoteIpPoint = c.String(),
                        MessageTypeId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.EntryId);
            
            CreateTable(
                "dbo.LogEntryDetails",
                c => new
                    {
                        EntryDetailId = c.Long(nullable: false, identity: true),
                        LogEntryId = c.Long(nullable: false),
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
                    })
                .PrimaryKey(t => t.MessageTypeId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.LogEntryDetails", "LogEntryId", "dbo.LogEntries");
            DropForeignKey("dbo.LogEntryDetails", "DetailTypeId", "dbo.LogDetailItemTypes");
            DropIndex("dbo.LogEntryDetails", new[] { "DetailTypeId" });
            DropIndex("dbo.LogEntryDetails", new[] { "LogEntryId" });
            DropTable("dbo.LogMessageTypes");
            DropTable("dbo.LogEntryDetails");
            DropTable("dbo.LogEntries");
            DropTable("dbo.LogDetailItemTypes");
        }
    }
}
