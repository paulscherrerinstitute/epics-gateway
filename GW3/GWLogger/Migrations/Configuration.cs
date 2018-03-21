namespace GWLogger.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<GWLogger.Backend.Model.LoggerContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(GWLogger.Backend.Model.LoggerContext context)
        {
            try
            {
                // Drop useless column
                context.Database.ExecuteSqlCommand("ALTER TABLE LogEntries DROP COLUMN TrimmedDate");

                // Create computed column
                context.Database.ExecuteSqlCommand("ALTER TABLE LogEntries ADD TrimmedDate AS DATETIMEFROMPARTS(\n" +
                    "DATEPART(YEAR, EntryDate),\n" +
                    "DATEPART(MONTH, EntryDate),\n" +
                    "DATEPART(DAY, EntryDate),\n" +
                    "DATEPART(HOUR, EntryDate),\n" +
                    "DATEPART(MINUTE, EntryDate) - DATEPART(MINUTE, EntryDate) % 10\n" +
                    ", 0, 0) PERSISTED");

                // Create Index on computed column
                context.Database.ExecuteSqlCommand("CREATE NONCLUSTERED INDEX IDXLogEntriesTrimmedDate ON LogEntries (TrimmedDate)");
            }
            catch
            {
            }

            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data.
        }
    }
}
