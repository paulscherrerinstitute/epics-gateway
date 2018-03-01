using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace GWLogger.Backend.Model
{
    // Open Packet Manager Console
    // Set Default project to GWLogger
    // Enable-Migrations
    // Add-migration base

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class LoggerContext : DbContext
    {
        public LoggerContext()
    : base("name=LoggerConnection")
        {
        }

        public virtual DbSet<LogEntry> LogEntries { get; set; }
        public virtual DbSet<LogEntryDetail> LogEntryDetails { get; set; }
        public virtual DbSet<LogDetailItemType> LogDetailItemTypes { get; set; }
        public virtual DbSet<LogMessageType> LogMessageTypes { get; set; }
    }
}