namespace GWLogger.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class mainpage : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Gateways", "IsMain", c => c.Boolean(nullable: false, defaultValue: false));
        }

        public override void Down()
        {
            DropColumn("dbo.Gateways", "IsMain");
        }
    }
}
