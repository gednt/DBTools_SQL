using DBTools.Migrations;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DBTools.Abstractions
{
    /// <summary>
    /// Defines the contract for applying, rolling back, and querying database migrations.
    /// </summary>
    public interface IMigrationRunner
    {
        /// <summary>
        /// Applies all pending migrations in order.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        Task ApplyAsync(CancellationToken ct = default);

        /// <summary>
        /// Rolls back migrations. If targetMigrationId is null, rolls back the last applied migration.
        /// Otherwise, rolls back all migrations after (and including) the target.
        /// </summary>
        /// <param name="targetMigrationId">The migration ID to roll back to (inclusive), or null for the last one.</param>
        /// <param name="ct">Cancellation token.</param>
        Task RollbackAsync(string targetMigrationId = null, CancellationToken ct = default);

        /// <summary>
        /// Gets all migrations that have been applied to the database.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A read-only list of applied migration records, sorted by MigrationId.</returns>
        Task<IReadOnlyList<MigrationRecord>> GetAppliedMigrationsAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets all registered migrations that have not yet been applied.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A read-only list of pending migrations, sorted by MigrationId.</returns>
        Task<IReadOnlyList<IMigration>> GetPendingMigrationsAsync(CancellationToken ct = default);
    }
}
