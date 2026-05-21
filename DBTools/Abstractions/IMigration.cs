namespace DBTools.Abstractions
{
    /// <summary>
    /// Represents a database schema migration with up and down SQL scripts.
    /// </summary>
    public interface IMigration
    {
        /// <summary>
        /// Unique identifier for the migration, typically timestamp-based (e.g., "20240101120000_CreateUsersTable").
        /// </summary>
        string MigrationId { get; }

        /// <summary>
        /// Human-readable description of what this migration does.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// SQL script to apply the migration (upgrade).
        /// </summary>
        string UpSql { get; }

        /// <summary>
        /// SQL script to revert the migration (downgrade).
        /// </summary>
        string DownSql { get; }
    }
}
