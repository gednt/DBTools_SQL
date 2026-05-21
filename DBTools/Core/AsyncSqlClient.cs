using DBTools.Abstractions;
using DBTools.Models;
using DBTools.Providers;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBTools.Core
{
    /// <summary>
    /// Async-first SQL client that provides full async/await database operations.
    /// Works alongside the existing synchronous SqlClient for backward compatibility.
    /// Supports interceptors, transactions, and multiple database providers.
    /// </summary>
    public class AsyncSqlClient : IAsyncSqlClient
    {
        private readonly IDbProvider _provider;
        private readonly ISqlValidator _validator;
        private readonly ISqlQueryBuilder _queryBuilder;
        private readonly List<IQueryInterceptor> _interceptors = new List<IQueryInterceptor>();
        private readonly List<IAsyncQueryInterceptor> _asyncInterceptors = new List<IAsyncQueryInterceptor>();
        private string _error;
        private bool _disposed = false;

        public IDbConfiguration Configuration { get; }
        public ISqlQueryBuilder QueryBuilderInstance => _queryBuilder;
        public string ConnectionString { get; }
        public string Error => _error;

        /// <summary>
        /// Creates an AsyncSqlClient with default SQL Server provider reading from config.json.
        /// </summary>
        public AsyncSqlClient()
        {
            Configuration = new DbConfiguration();
            _validator = new SqlValidator();
            _queryBuilder = new SqlQueryBuilder(_validator);
            _provider = new SqlServerProvider();
            ConnectionString = Configuration.ConnectionString;
        }

        /// <summary>
        /// Creates an AsyncSqlClient with explicit configuration and provider.
        /// </summary>
        public AsyncSqlClient(IDbConfiguration configuration, ISqlValidator validator, ISqlQueryBuilder queryBuilder, IDbProvider provider = null)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _queryBuilder = queryBuilder ?? throw new ArgumentNullException(nameof(queryBuilder));
            _provider = provider ?? new SqlServerProvider();
            ConnectionString = configuration.ConnectionString;
        }

        /// <summary>
        /// Creates an AsyncSqlClient with a connection string and optional provider.
        /// </summary>
        public AsyncSqlClient(string connectionString, IDbProvider provider = null)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _validator = new SqlValidator();
            _queryBuilder = new SqlQueryBuilder(_validator);
            _provider = provider ?? new SqlServerProvider();
        }

        #region Interceptor Registration

        /// <summary>
        /// Adds a synchronous query interceptor.
        /// </summary>
        public AsyncSqlClient AddInterceptor(IQueryInterceptor interceptor)
        {
            _interceptors.Add(interceptor ?? throw new ArgumentNullException(nameof(interceptor)));
            return this;
        }

        /// <summary>
        /// Adds an async query interceptor.
        /// </summary>
        public AsyncSqlClient AddInterceptor(IAsyncQueryInterceptor interceptor)
        {
            _asyncInterceptors.Add(interceptor ?? throw new ArgumentNullException(nameof(interceptor)));
            return this;
        }

        #endregion

        #region Transaction Support

        /// <summary>
        /// Begins a new database transaction.
        /// </summary>
        public DbToolsTransaction BeginTransaction()
        {
            var connection = _provider.CreateConnection(ConnectionString);
            connection.Open();
            var tx = connection.BeginTransaction();
            return new DbToolsTransaction(connection, tx);
        }

        /// <summary>
        /// Begins a new database transaction asynchronously.
        /// </summary>
        public async Task<DbToolsTransaction> BeginTransactionAsync(CancellationToken ct = default)
        {
            var connection = _provider.CreateConnection(ConnectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);
            var tx = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);
            return new DbToolsTransaction(connection, tx);
        }

        #endregion

        #region Async CRUD Operations

        /// <summary>
        /// Asynchronously executes a SELECT query with parameterized WHERE clause.
        /// </summary>
        public async Task<DataTable> SelectAsync(string fields, string table, string whereClause, object[] parameters, CancellationToken ct = default)
        {
            if (!_validator.IsValidIdentifier(fields))
                throw new ArgumentException("Invalid field names.", nameof(fields));
            if (!_validator.IsValidIdentifier(table))
                throw new ArgumentException("Invalid table name.", nameof(table));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            string sql;
            if (!string.IsNullOrEmpty(whereClause))
                sql = $"SELECT {fields} FROM {table} WHERE {whereClause}";
            else
                sql = $"SELECT {fields} FROM {table}";

            return await ExecuteReaderAsync(sql, parameters, QueryOperationType.Select, table, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously executes a SELECT query from partial query string.
        /// </summary>
        public async Task<DataTable> SelectAsync(string queryWithoutSelect, object[] parameters, CancellationToken ct = default)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            string sql = "SELECT " + queryWithoutSelect;
            return await ExecuteReaderAsync(sql, parameters, QueryOperationType.Select, null, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously executes a full SQL query and returns a DataTable.
        /// </summary>
        public async Task<DataTable> SelectRawAsync(string fullSql, object[] parameters, CancellationToken ct = default)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            return await ExecuteReaderAsync(fullSql, parameters, QueryOperationType.Select, null, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously inserts a record into the database.
        /// </summary>
        public async Task<bool> InsertAsync(string[] fields, string table, object[] values, string primaryKeyName = null, bool autoIncrement = true, CancellationToken ct = default)
        {
            if (!_validator.IsValidIdentifier(table))
                throw new ArgumentException("Invalid table name.", nameof(table));
            if (fields == null || fields.Length == 0)
                throw new ArgumentException("Fields array cannot be null or empty.", nameof(fields));
            if (values == null || values.Length == 0)
                throw new ArgumentException("Values array cannot be null or empty.", nameof(values));

            // Remove PK field if auto-increment
            if (autoIncrement && !string.IsNullOrEmpty(primaryKeyName))
            {
                var fieldsLower = Array.ConvertAll(fields, f => f.ToLower());
                int pkIndex = Array.IndexOf(fieldsLower, primaryKeyName.ToLower());
                if (pkIndex >= 0)
                {
                    values = values.Where((_, i) => i != pkIndex).ToArray();
                    fields = fields.Where((_, i) => i != pkIndex).ToArray();
                }
            }

            // Build parameterized INSERT
            var paramPlaceholders = string.Join(",", fields.Select((_, i) => $"@param{i}"));
            var fieldList = string.Join(",", fields);
            string sql = $"INSERT INTO {table}({fieldList}) VALUES({paramPlaceholders})";

            return await ExecuteNonQueryAsync(sql, values, QueryOperationType.Insert, table, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously updates records with parameterized WHERE clause.
        /// </summary>
        public async Task<bool> UpdateAsync(string[] fields, string table, string[] values, string whereClause, object[] whereParameters, CancellationToken ct = default)
        {
            if (!_validator.IsValidIdentifier(table))
                throw new ArgumentException("Invalid table name.", nameof(table));
            if (fields == null || fields.Length == 0)
                throw new ArgumentException("Fields array cannot be null or empty.", nameof(fields));
            if (values == null || values.Length == 0)
                throw new ArgumentException("Values array cannot be null or empty.", nameof(values));
            if (string.IsNullOrEmpty(whereClause))
                throw new ArgumentException("WHERE clause is required for UPDATE operations.", nameof(whereClause));

            // Build SET clause with parameters
            var setClause = string.Join(",", fields.Select((f, i) => $"{f}=@param{i}"));

            // Combine SET parameters + WHERE parameters
            var allParams = new List<object>();
            for (int i = 0; i < values.Length; i++)
                allParams.Add(values[i] ?? (object)DBNull.Value);
            if (whereParameters != null)
                allParams.AddRange(whereParameters);

            string sql = $"UPDATE {table} SET {setClause} WHERE {whereClause}";
            return await ExecuteNonQueryAsync(sql, allParams.ToArray(), QueryOperationType.Update, table, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously deletes records with parameterized WHERE clause.
        /// </summary>
        public async Task<bool> DeleteAsync(string table, string whereClause, object[] parameters, CancellationToken ct = default)
        {
            if (!_validator.IsValidIdentifier(table))
                throw new ArgumentException("Invalid table name.", nameof(table));
            if (string.IsNullOrEmpty(whereClause))
                throw new ArgumentException("Condition is required for DELETE operations.", nameof(whereClause));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            string sql = $"DELETE FROM {table} WHERE {whereClause}";
            return await ExecuteNonQueryAsync(sql, parameters, QueryOperationType.Delete, table, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously executes a non-query SQL command.
        /// </summary>
        public async Task ExecuteQueryAsync(string query, CancellationToken ct = default)
        {
            await ExecuteNonQueryAsync(query, Array.Empty<object>(), QueryOperationType.Execute, null, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously executes a scalar query and returns the first column of the first row.
        /// </summary>
        public async Task<object> ExecuteScalarAsync(string query, object[] parameters, CancellationToken ct = default)
        {
            var context = new QueryInterceptionContext
            {
                Sql = query,
                Parameters = parameters?.ToList() ?? new List<object>(),
                OperationType = QueryOperationType.Scalar
            };

            await RunBeforeInterceptorsAsync(context, ct).ConfigureAwait(false);
            if (context.IsSuppressed) return null;

            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = _provider.CreateConnection(ConnectionString);
                await connection.OpenAsync(ct).ConfigureAwait(false);

                using var cmd = connection.CreateCommand();
                cmd.CommandText = context.Sql;
                AddParameters(cmd, context.Parameters);

                var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);

                sw.Stop();
                context.Duration = sw.Elapsed;
                await RunAfterInterceptorsAsync(context, ct).ConfigureAwait(false);

                _error = null;
                return result == DBNull.Value ? null : result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                context.Duration = sw.Elapsed;
                _error = ex.ToString();
                await RunErrorInterceptorsAsync(context, ex, ct).ConfigureAwait(false);
                return null;
            }
        }

        #endregion

        #region Transaction-Aware Operations

        /// <summary>
        /// Executes a SELECT within an existing transaction.
        /// </summary>
        public async Task<DataTable> SelectAsync(DbToolsTransaction transaction, string fields, string table, string whereClause, object[] parameters, CancellationToken ct = default)
        {
            if (!_validator.IsValidIdentifier(fields))
                throw new ArgumentException("Invalid field names.", nameof(fields));
            if (!_validator.IsValidIdentifier(table))
                throw new ArgumentException("Invalid table name.", nameof(table));

            string sql = !string.IsNullOrEmpty(whereClause)
                ? $"SELECT {fields} FROM {table} WHERE {whereClause}"
                : $"SELECT {fields} FROM {table}";

            return await ExecuteReaderWithTransactionAsync(transaction, sql, parameters, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a non-query within an existing transaction.
        /// </summary>
        public async Task<bool> ExecuteInTransactionAsync(DbToolsTransaction transaction, string sql, object[] parameters, CancellationToken ct = default)
        {
            try
            {
                using var cmd = transaction.Connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.Transaction = transaction.UnderlyingTransaction;
                AddParameters(cmd, parameters?.ToList() ?? new List<object>());
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                _error = null;
                return true;
            }
            catch (Exception ex)
            {
                _error = ex.ToString();
                return false;
            }
        }

        private async Task<DataTable> ExecuteReaderWithTransactionAsync(DbToolsTransaction transaction, string sql, object[] parameters, CancellationToken ct)
        {
            using var cmd = transaction.Connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Transaction = transaction.UnderlyingTransaction;
            AddParameters(cmd, parameters?.ToList() ?? new List<object>());

            var dt = new DataTable();
            using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            dt.Load(reader);
            _error = null;
            return dt;
        }

        #endregion

        #region Internal Helpers

        private async Task<DataTable> ExecuteReaderAsync(string sql, object[] parameters, QueryOperationType opType, string tableName, CancellationToken ct)
        {
            var context = new QueryInterceptionContext
            {
                Sql = sql,
                Parameters = parameters?.ToList() ?? new List<object>(),
                OperationType = opType,
                TableName = tableName
            };

            await RunBeforeInterceptorsAsync(context, ct).ConfigureAwait(false);
            if (context.IsSuppressed) return new DataTable();

            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = _provider.CreateConnection(ConnectionString);
                await connection.OpenAsync(ct).ConfigureAwait(false);

                using var cmd = connection.CreateCommand();
                cmd.CommandText = context.Sql;
                AddParameters(cmd, context.Parameters);

                var dt = new DataTable();
                using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                dt.Load(reader);

                sw.Stop();
                context.Duration = sw.Elapsed;
                context.RowsAffected = dt.Rows.Count;
                await RunAfterInterceptorsAsync(context, ct).ConfigureAwait(false);

                _error = null;
                return dt;
            }
            catch (Exception ex)
            {
                sw.Stop();
                context.Duration = sw.Elapsed;
                _error = ex.ToString();
                await RunErrorInterceptorsAsync(context, ex, ct).ConfigureAwait(false);
                return new DataTable();
            }
        }

        private async Task<bool> ExecuteNonQueryAsync(string sql, object[] parameters, QueryOperationType opType, string tableName, CancellationToken ct)
        {
            var context = new QueryInterceptionContext
            {
                Sql = sql,
                Parameters = parameters?.ToList() ?? new List<object>(),
                OperationType = opType,
                TableName = tableName
            };

            await RunBeforeInterceptorsAsync(context, ct).ConfigureAwait(false);
            if (context.IsSuppressed) return true;

            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = _provider.CreateConnection(ConnectionString);
                await connection.OpenAsync(ct).ConfigureAwait(false);

                using var cmd = connection.CreateCommand();
                cmd.CommandText = context.Sql;
                AddParameters(cmd, context.Parameters);

                int rows = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

                sw.Stop();
                context.Duration = sw.Elapsed;
                context.RowsAffected = rows;
                await RunAfterInterceptorsAsync(context, ct).ConfigureAwait(false);

                _error = null;
                return true;
            }
            catch (Exception ex)
            {
                sw.Stop();
                context.Duration = sw.Elapsed;
                _error = ex.ToString();
                await RunErrorInterceptorsAsync(context, ex, ct).ConfigureAwait(false);
                return false;
            }
        }

        private void AddParameters(DbCommand cmd, List<object> parameters)
        {
            if (parameters == null) return;
            for (int i = 0; i < parameters.Count; i++)
            {
                var param = _provider.CreateParameter($"@param{i}", parameters[i] ?? DBNull.Value);
                cmd.Parameters.Add(param);
            }
        }

        private async Task RunBeforeInterceptorsAsync(QueryInterceptionContext context, CancellationToken ct)
        {
            foreach (var interceptor in _interceptors)
                interceptor.BeforeExecute(context);
            foreach (var interceptor in _asyncInterceptors)
                await interceptor.BeforeExecuteAsync(context, ct).ConfigureAwait(false);
        }

        private async Task RunAfterInterceptorsAsync(QueryInterceptionContext context, CancellationToken ct)
        {
            foreach (var interceptor in _interceptors)
                interceptor.AfterExecute(context);
            foreach (var interceptor in _asyncInterceptors)
                await interceptor.AfterExecuteAsync(context, ct).ConfigureAwait(false);
        }

        private async Task RunErrorInterceptorsAsync(QueryInterceptionContext context, Exception ex, CancellationToken ct)
        {
            foreach (var interceptor in _interceptors)
                interceptor.OnError(context, ex);
            foreach (var interceptor in _asyncInterceptors)
                await interceptor.OnErrorAsync(context, ex, ct).ConfigureAwait(false);
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }

        public ValueTask DisposeAsync()
        {
            if (_disposed) return ValueTask.CompletedTask;
            _disposed = true;
            return ValueTask.CompletedTask;
        }

        #endregion
    }
}
