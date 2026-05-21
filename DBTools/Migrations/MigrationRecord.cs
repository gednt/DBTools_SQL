using System;

namespace DBTools.Migrations
{
    /// <summary>
    /// Represents a record of an applied migration stored in the tracking table.
    /// </summary>
    public class MigrationRecord
    {
        /// <summary>
        /// The unique identifier of the applied migration.
        /// </summary>
        public string MigrationId { get; set; }

        /// <summary>
        /// Human-readable description of the migration.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The UTC date and time when the migration was applied.
        /// </summary>
        public DateTime AppliedAt { get; set; }

        /// <summary>
        /// SHA256 checksum of the UpSql to detect if the migration was modified after application.
        /// </summary>
        public string Checksum { get; set; }
    }
}
