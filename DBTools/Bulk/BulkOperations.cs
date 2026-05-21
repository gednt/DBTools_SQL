using DBTools.Abstractions;
using DBTools.Core;
using DBTools.Mapping;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DBTools.Bulk
{
    /// <summary>
    /// High-performance bulk operations using SqlBulkCopy (SQL Server)
    /// or batched INSERT statements (other providers).
    /// </summary>
    public class BulkOperations<TEntity> where TEntity : class, new()
    {
        private readonly string _connectionString;
        private readonly IDbProvider _provider;
        private readonly EntityMapping _mapping;


        /// <summary>
        /// Creates bulk operations for the specified entity type.
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <param name="provider">Database provider (null defaults to SQL Server)</param>
        public BulkOperations(string connectionString, IDbProvider provider = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _provider = provider ?? new Providers.SqlServerProvider();
            _mapping = EntityMappingResolver.Resolve<TEntity>();
        }

        /// <summary>
        /// Creates bulk operations from an AsyncSqlClient.
        /// </summary>
        public BulkOperations(AsyncSqlClient client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            _connectionString = client.ConnectionString;
            _provider = new Providers.SqlServerProvider();
            _mapping = EntityMappingResolver.Resolve<TEntity>();
        }

        /// <summary>
        /// Bulk inserts entities using SqlBulkCopy for SQL Server,
        /// or batched INSERT for other providers.
        /// </summary>
        /// <param name="entities">The entities to insert</param>
        /// <param name="batchSize">Batch size for SqlBulkCopy (default: 1000)</param>
        /// <param name="timeout">Timeout in seconds (default: 60)</param>
        /// <param name="ct">Cancellation token</param>
        public async Task BulkInsertAsync(
            IEnumerable<TEntity> entities,
            int batchSize = 1000,
            int timeout = 60,
            CancellationToken ct = default)
        {
            var entityList = entities as IList<TEntity> ?? entities.ToList();
            if (entityList.Count == 0) return;

            if (_provider is Providers.SqlServerProvider)
            {
                await BulkInsertSqlServerAsync(entityList, batchSize, timeout, ct).ConfigureAwait(false);
            }
            else
            {
                await BulkInsertGenericAsync(entityList, batchSize, ct).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Bulk updates entities by primary key.
        /// Uses batched UPDATE statements within a transaction.
        /// </summary>
        public async Task BulkUpdateAsync(
            IEnumerable<TEntity> entities,
            int batchSize = 500,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_mapping.PrimaryKeyColumn))
                throw new InvalidOperationException("Primary key required for BulkUpdate.");

            var entityList = entities as IList<TEntity> ?? entities.ToList();
            if (entityList.Count == 0) return;

            using var connection = _provider.CreateConnection(_connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);
            using var transaction = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

            try
            {
                var props = GetMappedProperties(excludePK: true);
                var pkProp = _mapping.Properties.First(p => p.IsPrimaryKey);

                for (int i = 0; i < entityList.Count; i++)
                {
                    var entity = entityList[i];
                    var setClauses = new List<string>();
                    using var cmd = connection.CreateCommand();
                    cmd.Transaction = transaction;
                    int paramIdx = 0;

                    foreach (var prop in props)
                    {
                        setClauses.Add($"{prop.ColumnName} = @p{paramIdx}");
                        var param = _provider.CreateParameter($"@p{paramIdx}", prop.PropertyInfo.GetValue(entity) ?? DBNull.Value);
                        cmd.Parameters.Add(param);
                        paramIdx++;
                    }

                    var pkParam = _provider.CreateParameter($"@pk", pkProp.PropertyInfo.GetValue(entity) ?? DBNull.Value);
                    cmd.Parameters.Add(pkParam);

                    cmd.CommandText = $"UPDATE {_mapping.TableName} SET {string.Join(", ", setClauses)} WHERE {_mapping.PrimaryKeyColumn} = @pk";
                    await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                await transaction.CommitAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }


        /// <summary>
        /// Bulk deletes entities by primary key.
        /// Uses batched DELETE statements within a transaction.
        /// </summary>
        public async Task BulkDeleteAsync(
            IEnumerable<TEntity> entities,
            int batchSize = 1000,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_mapping.PrimaryKeyColumn))
                throw new InvalidOperationException("Primary key required for BulkDelete.");

            var entityList = entities as IList<TEntity> ?? entities.ToList();
            if (entityList.Count == 0) return;

            var pkProp = _mapping.Properties.First(p => p.IsPrimaryKey);
            var pkValues = entityList.Select(e => pkProp.PropertyInfo.GetValue(e)).ToList();

            using var connection = _provider.CreateConnection(_connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);
            using var transaction = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

            try
            {
                // Batch deletions
                for (int batch = 0; batch < pkValues.Count; batch += batchSize)
                {
                    var batchValues = pkValues.Skip(batch).Take(batchSize).ToList();
                    using var cmd = connection.CreateCommand();
                    cmd.Transaction = transaction;

                    var paramNames = new List<string>();
                    for (int i = 0; i < batchValues.Count; i++)
                    {
                        var paramName = $"@p{i}";
                        paramNames.Add(paramName);
                        cmd.Parameters.Add(_provider.CreateParameter(paramName, batchValues[i] ?? DBNull.Value));
                    }

                    cmd.CommandText = $"DELETE FROM {_mapping.TableName} WHERE {_mapping.PrimaryKeyColumn} IN ({string.Join(",", paramNames)})";
                    await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                await transaction.CommitAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }


        #region Private Helpers

        private async Task BulkInsertSqlServerAsync(IList<TEntity> entities, int batchSize, int timeout, CancellationToken ct)
        {
            var dataTable = CreateDataTable(entities);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);

            using var bulkCopy = new SqlBulkCopy(connection)
            {
                DestinationTableName = _mapping.TableName,
                BatchSize = batchSize,
                BulkCopyTimeout = timeout
            };

            // Map columns
            foreach (DataColumn col in dataTable.Columns)
            {
                bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
            }

            await bulkCopy.WriteToServerAsync(dataTable, ct).ConfigureAwait(false);
        }

        private async Task BulkInsertGenericAsync(IList<TEntity> entities, int batchSize, CancellationToken ct)
        {
            var props = GetMappedProperties(excludePK: _mapping.PrimaryKeyAutoIncrement);

            using var connection = _provider.CreateConnection(_connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);
            using var transaction = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

            try
            {
                for (int batch = 0; batch < entities.Count; batch += batchSize)
                {
                    var batchEntities = entities.Skip(batch).Take(batchSize).ToList();
                    foreach (var entity in batchEntities)
                    {
                        using var cmd = connection.CreateCommand();
                        cmd.Transaction = transaction;

                        var fieldNames = new List<string>();
                        var paramNames = new List<string>();
                        int idx = 0;

                        foreach (var prop in props)
                        {
                            fieldNames.Add(prop.ColumnName);
                            paramNames.Add($"@p{idx}");
                            cmd.Parameters.Add(_provider.CreateParameter($"@p{idx}", prop.PropertyInfo.GetValue(entity) ?? DBNull.Value));
                            idx++;
                        }

                        cmd.CommandText = $"INSERT INTO {_mapping.TableName} ({string.Join(",", fieldNames)}) VALUES ({string.Join(",", paramNames)})";
                        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                    }
                }

                await transaction.CommitAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }


        private DataTable CreateDataTable(IList<TEntity> entities)
        {
            var dt = new DataTable(_mapping.TableName);
            var props = GetMappedProperties(excludePK: _mapping.PrimaryKeyAutoIncrement);

            foreach (var prop in props)
            {
                var colType = Nullable.GetUnderlyingType(prop.PropertyInfo.PropertyType) ?? prop.PropertyInfo.PropertyType;
                dt.Columns.Add(prop.ColumnName, colType);
            }

            foreach (var entity in entities)
            {
                var row = dt.NewRow();
                foreach (var prop in props)
                {
                    var value = prop.PropertyInfo.GetValue(entity);
                    row[prop.ColumnName] = value ?? DBNull.Value;
                }
                dt.Rows.Add(row);
            }

            return dt;
        }

        private List<PropertyMapping> GetMappedProperties(bool excludePK)
        {
            return _mapping.Properties
                .Where(p => !p.IsNotMapped &&
                            (!excludePK || !p.IsPrimaryKey) &&
                            p.GeneratedOption != DatabaseGeneratedOption.Identity &&
                            p.GeneratedOption != DatabaseGeneratedOption.Computed)
                .ToList();
        }

        #endregion
    }
}
