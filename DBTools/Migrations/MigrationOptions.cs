using DBTools.Abstractions;
using System.Collections.Generic;

namespace DBTools.Migrations
{
    /// <summary>
    /// Configuration options for the migration system.
    /// </summary>
    public class MigrationOptions
    {
        /// <summary>
        /// The name of the table used to track applied migrations.
        /// Default: "__DBToolsMigrations".
        /// </summary>
        public string MigrationTableName { get; set; } = "__DBToolsMigrations";

        /// <summary>
        /// The list of registered migrations to manage.
        /// </summary>
        public List<IMigration> Migrations { get; set; } = new List<IMigration>();

        /// <summary>
        /// Whether to automatically apply pending migrations on startup.
        /// Default: false.
        /// </summary>
        public bool AutoMigrateOnStartup { get; set; } = false;
    }
}
