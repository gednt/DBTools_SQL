using System;

namespace DBTools.Migrations
{
    /// <summary>
    /// A concrete migration implementation that accepts migration details via constructor.
    /// Useful for programmatic or inline migration creation.
    /// </summary>
    public class SqlMigration : MigrationBase
    {
        /// <inheritdoc />
        public override string MigrationId { get; }

        /// <inheritdoc />
        public override string Description { get; }

        /// <inheritdoc />
        public override string UpSql { get; }

        /// <inheritdoc />
        public override string DownSql { get; }

        /// <summary>
        /// Creates a new SQL migration with the specified details.
        /// </summary>
        /// <param name="migrationId">Unique migration identifier (e.g., "20240101120000_CreateUsersTable").</param>
        /// <param name="description">Human-readable description of the migration.</param>
        /// <param name="upSql">SQL to apply the migration.</param>
        /// <param name="downSql">SQL to revert the migration.</param>
        public SqlMigration(string migrationId, string description, string upSql, string downSql)
        {
            MigrationId = migrationId ?? throw new ArgumentNullException(nameof(migrationId));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            UpSql = upSql ?? throw new ArgumentNullException(nameof(upSql));
            DownSql = downSql ?? throw new ArgumentNullException(nameof(downSql));
        }
    }
}
