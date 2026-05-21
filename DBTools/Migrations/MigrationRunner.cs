using DBTools.Abstractions;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBTools.Migrations
{
    /// <summary>
    /// Applies and rolls back database schema migrations, tracking them in a database table.
    /// Each migration runs in its own transaction for atomicity.
    /// </summary>
    public class MigrationRunner : IMigrationRunner
    {
        private readonly IDbProvider _provider;
        private readonly string _connectionString;
        private readonly MigrationOptions _options;

        /// <summary>
        /// Creates a new MigrationRunner.
        /// </summary>
        /// <param name="provider">The database provider for creating connections and commands.</param>
        /// <param name="connectionString">The connection string to the target database.</param>
        /// <param name="options">Migration configuration options.</param>
        public MigrationRunner(IDbProvider provider, string connectionString, MigrationOptions options)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        public async Task ApplyAsync(CancellationToken ct = default)
        {
            await EnsureTableExistsAsync(ct).ConfigureAwait(false);

            var pending = await GetPendingMigrationsAsync(ct).ConfigureAwait(false);

            foreach (var migration in pending)
            {
                using var connection = _provider.CreateConnection(_connectionString);
                await connection.OpenAsync(ct).ConfigureAwait(false);
                using var transaction = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

                try
                {
                    // Execute the Up SQL
                    using var upCmd = connection.CreateCommand();
                    upCmd.Transaction = transaction;
                    upCmd.CommandText = migration.UpSql;
                    await upCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

                    // Insert the migration record
                    var checksum = MigrationBase.ComputeChecksum(migration.UpSql);
                    var insertSql = BuildInsertRecordSql();

                    using var insertCmd = connection.CreateCommand();
                    insertCmd.Transaction = transaction;
                    insertCmd.CommandText = insertSql;
                    insertCmd.Parameters.Add(_provider.CreateParameter("@migrationId", migration.MigrationId));
                    insertCmd.Parameters.Add(_provider.CreateParameter("@description", migration.Description));
                    insertCmd.Parameters.Add(_provider.CreateParameter("@appliedAt", DateTime.UtcNow));
                    insertCmd.Parameters.Add(_provider.CreateParameter("@checksum", checksum));
                    await insertCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

                    await transaction.CommitAsync(ct).ConfigureAwait(false);
                }
                catch
                {
                    await transaction.RollbackAsync(ct).ConfigureAwait(false);
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public async Task RollbackAsync(string targetMigrationId = null, CancellationToken ct = default)
        {
            await EnsureTableExistsAsync(ct).ConfigureAwait(false);

            var applied = await GetAppliedMigrationsAsync(ct).ConfigureAwait(false);
            if (applied.Count == 0)
                return;

            // Determine which migrations to roll back
            IEnumerable<MigrationRecord> toRollback;
            if (targetMigrationId == null)
            {
                // Roll back only the last applied migration
                toRollback = new[] { applied[applied.Count - 1] };
            }
            else
            {
                // Roll back all migrations at or after the target (in reverse order)
                toRollback = applied
                    .Where(m => string.Compare(m.MigrationId, targetMigrationId, StringComparison.Ordinal) >= 0)
                    .OrderByDescending(m => m.MigrationId);
            }

            foreach (var record in toRollback)
            {
                // Find the matching registered migration to get the DownSql
                var migration = _options.Migrations
                    .FirstOrDefault(m => m.MigrationId == record.MigrationId);

                if (migration == null)
                    throw new InvalidOperationException(
                        $"Cannot rollback migration '{record.MigrationId}': migration is not registered in the current options.");

                using var connection = _provider.CreateConnection(_connectionString);
                await connection.OpenAsync(ct).ConfigureAwait(false);
                using var transaction = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

                try
                {
                    // Execute the Down SQL
                    using var downCmd = connection.CreateCommand();
                    downCmd.Transaction = transaction;
                    downCmd.CommandText = migration.DownSql;
                    await downCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

                    // Delete the migration record
                    var deleteSql = BuildDeleteRecordSql();

                    using var deleteCmd = connection.CreateCommand();
                    deleteCmd.Transaction = transaction;
                    deleteCmd.CommandText = deleteSql;
                    deleteCmd.Parameters.Add(_provider.CreateParameter("@migrationId", record.MigrationId));
                    await deleteCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

                    await transaction.CommitAsync(ct).ConfigureAwait(false);
                }
                catch
                {
                    await transaction.RollbackAsync(ct).ConfigureAwait(false);
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<MigrationRecord>> GetAppliedMigrationsAsync(CancellationToken ct = default)
        {
            await EnsureTableExistsAsync(ct).ConfigureAwait(false);

            var records = new List<MigrationRecord>();

            using var connection = _provider.CreateConnection(_connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = BuildSelectAllSql();

            using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                records.Add(new MigrationRecord
                {
                    MigrationId = reader.GetString(0),
                    Description = reader.GetString(1),
                    AppliedAt = reader.GetDateTime(2),
                    Checksum = reader.GetString(3)
                });
            }

            return records.OrderBy(r => r.MigrationId, StringComparer.Ordinal).ToList().AsReadOnly();
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<IMigration>> GetPendingMigrationsAsync(CancellationToken ct = default)
        {
            var applied = await GetAppliedMigrationsAsync(ct).ConfigureAwait(false);
            var appliedIds = new HashSet<string>(applied.Select(r => r.MigrationId), StringComparer.Ordinal);

            var pending = _options.Migrations
                .Where(m => !appliedIds.Contains(m.MigrationId))
                .OrderBy(m => m.MigrationId, StringComparer.Ordinal)
                .ToList();

            return pending.AsReadOnly();
        }

        /// <summary>
        /// Ensures the migration tracking table exists in the database.
        /// Creates it if it does not exist, using provider-appropriate SQL.
        /// </summary>
        internal async Task EnsureTableExistsAsync(CancellationToken ct = default)
        {
            using var connection = _provider.CreateConnection(_connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = GenerateCreateTableSql();
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Generates the CREATE TABLE IF NOT EXISTS SQL for the migration tracking table,
        /// using provider-appropriate syntax and data types.
        /// </summary>
        internal string GenerateCreateTableSql()
        {
            var tableName = _options.MigrationTableName;

            return _provider.ProviderName switch
            {
                "SqlServer" => $"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{tableName}') " +
                               $"CREATE TABLE [{tableName}] " +
                               $"(MigrationId NVARCHAR(255) PRIMARY KEY, Description NVARCHAR(500), AppliedAt DATETIME2, Checksum NVARCHAR(64))",

                "PostgreSQL" => $"CREATE TABLE IF NOT EXISTS \"{tableName}\" " +
                                $"(\"MigrationId\" VARCHAR(255) PRIMARY KEY, \"Description\" VARCHAR(500), \"AppliedAt\" TIMESTAMP, \"Checksum\" VARCHAR(64))",

                "MySQL" => $"CREATE TABLE IF NOT EXISTS `{tableName}` " +
                           $"(`MigrationId` VARCHAR(255) PRIMARY KEY, `Description` VARCHAR(500), `AppliedAt` DATETIME, `Checksum` VARCHAR(64))",

                "SQLite" => $"CREATE TABLE IF NOT EXISTS \"{tableName}\" " +
                            $"(\"MigrationId\" TEXT PRIMARY KEY, \"Description\" TEXT, \"AppliedAt\" TEXT, \"Checksum\" TEXT)",

                _ => throw new InvalidOperationException($"Unsupported provider: {_provider.ProviderName}")
            };
        }

        private string BuildInsertRecordSql()
        {
            var tableName = _options.MigrationTableName;

            return _provider.ProviderName switch
            {
                "SqlServer" => $"INSERT INTO [{tableName}] (MigrationId, Description, AppliedAt, Checksum) VALUES (@migrationId, @description, @appliedAt, @checksum)",
                "PostgreSQL" => $"INSERT INTO \"{tableName}\" (\"MigrationId\", \"Description\", \"AppliedAt\", \"Checksum\") VALUES (@migrationId, @description, @appliedAt, @checksum)",
                "MySQL" => $"INSERT INTO `{tableName}` (`MigrationId`, `Description`, `AppliedAt`, `Checksum`) VALUES (@migrationId, @description, @appliedAt, @checksum)",
                "SQLite" => $"INSERT INTO \"{tableName}\" (\"MigrationId\", \"Description\", \"AppliedAt\", \"Checksum\") VALUES (@migrationId, @description, @appliedAt, @checksum)",
                _ => throw new InvalidOperationException($"Unsupported provider: {_provider.ProviderName}")
            };
        }

        private string BuildDeleteRecordSql()
        {
            var tableName = _options.MigrationTableName;

            return _provider.ProviderName switch
            {
                "SqlServer" => $"DELETE FROM [{tableName}] WHERE MigrationId = @migrationId",
                "PostgreSQL" => $"DELETE FROM \"{tableName}\" WHERE \"MigrationId\" = @migrationId",
                "MySQL" => $"DELETE FROM `{tableName}` WHERE `MigrationId` = @migrationId",
                "SQLite" => $"DELETE FROM \"{tableName}\" WHERE \"MigrationId\" = @migrationId",
                _ => throw new InvalidOperationException($"Unsupported provider: {_provider.ProviderName}")
            };
        }

        private string BuildSelectAllSql()
        {
            var tableName = _options.MigrationTableName;

            return _provider.ProviderName switch
            {
                "SqlServer" => $"SELECT MigrationId, Description, AppliedAt, Checksum FROM [{tableName}] ORDER BY MigrationId",
                "PostgreSQL" => $"SELECT \"MigrationId\", \"Description\", \"AppliedAt\", \"Checksum\" FROM \"{tableName}\" ORDER BY \"MigrationId\"",
                "MySQL" => $"SELECT `MigrationId`, `Description`, `AppliedAt`, `Checksum` FROM `{tableName}` ORDER BY `MigrationId`",
                "SQLite" => $"SELECT \"MigrationId\", \"Description\", \"AppliedAt\", \"Checksum\" FROM \"{tableName}\" ORDER BY \"MigrationId\"",
                _ => throw new InvalidOperationException($"Unsupported provider: {_provider.ProviderName}")
            };
        }
    }
}
