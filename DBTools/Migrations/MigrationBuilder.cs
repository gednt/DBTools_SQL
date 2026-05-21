using DBTools.Abstractions;
using System.Collections.Generic;

namespace DBTools.Migrations
{
    /// <summary>
    /// Fluent builder for defining migrations in configuration.
    /// </summary>
    public class MigrationBuilder
    {
        private readonly List<IMigration> _migrations = new List<IMigration>();

        /// <summary>
        /// Adds a migration to the builder.
        /// </summary>
        /// <param name="id">Unique migration identifier (e.g., "20240101120000_CreateUsersTable").</param>
        /// <param name="description">Human-readable description of the migration.</param>
        /// <param name="upSql">SQL to apply the migration.</param>
        /// <param name="downSql">SQL to revert the migration.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public MigrationBuilder AddMigration(string id, string description, string upSql, string downSql)
        {
            _migrations.Add(new SqlMigration(id, description, upSql, downSql));
            return this;
        }

        /// <summary>
        /// Builds and returns the list of registered migrations.
        /// </summary>
        /// <returns>A read-only list of migrations in the order they were added.</returns>
        public IReadOnlyList<IMigration> Build()
        {
            return _migrations.AsReadOnly();
        }
    }
}
