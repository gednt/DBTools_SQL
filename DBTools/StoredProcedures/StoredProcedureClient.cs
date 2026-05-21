using DBTools.Abstractions;
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace DBTools.StoredProcedures
{
    /// <summary>
    /// Executes stored procedures with support for input, output, input/output parameters, and return values.
    /// Implements both synchronous and asynchronous interfaces.
    /// </summary>
    public class StoredProcedureClient : IStoredProcedureClient, IAsyncStoredProcedureClient
    {
        private readonly IDbProvider _provider;
        private readonly string _connectionString;

        /// <summary>
        /// Creates a new StoredProcedureClient with the specified provider and connection string.
        /// </summary>
        /// <param name="provider">The database provider for creating connections and commands</param>
        /// <param name="connectionString">The connection string for database access</param>
        public StoredProcedureClient(IDbProvider provider, string connectionString)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <inheritdoc />
        public StoredProcedureResult ExecuteStoredProcedure(string procedureName, StoredProcedureParameter[] parameters)
        {
            if (string.IsNullOrEmpty(procedureName))
                throw new ArgumentException("Procedure name cannot be null or empty.", nameof(procedureName));

            using var connection = _provider.CreateConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = procedureName;

            AddParameters(command, parameters);

            int rowsAffected = command.ExecuteNonQuery();

            return BuildResult(command, parameters, rowsAffected);
        }

        /// <inheritdoc />
        public DataTable ExecuteStoredProcedureReader(string procedureName, StoredProcedureParameter[] parameters)
        {
            if (string.IsNullOrEmpty(procedureName))
                throw new ArgumentException("Procedure name cannot be null or empty.", nameof(procedureName));

            using var connection = _provider.CreateConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = procedureName;

            AddParameters(command, parameters);

            var dataTable = new DataTable();
            using var reader = command.ExecuteReader();
            dataTable.Load(reader);

            return dataTable;
        }

        /// <inheritdoc />
        public async Task<StoredProcedureResult> ExecuteStoredProcedureAsync(string procedureName, StoredProcedureParameter[] parameters, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(procedureName))
                throw new ArgumentException("Procedure name cannot be null or empty.", nameof(procedureName));

            using var connection = _provider.CreateConnection(_connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = procedureName;

            AddParameters(command, parameters);

            int rowsAffected = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            return BuildResult(command, parameters, rowsAffected);
        }

        /// <inheritdoc />
        public async Task<DataTable> ExecuteStoredProcedureReaderAsync(string procedureName, StoredProcedureParameter[] parameters, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(procedureName))
                throw new ArgumentException("Procedure name cannot be null or empty.", nameof(procedureName));

            using var connection = _provider.CreateConnection(_connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = procedureName;

            AddParameters(command, parameters);

            var dataTable = new DataTable();
            using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
            dataTable.Load(reader);

            return dataTable;
        }

        private void AddParameters(DbCommand command, StoredProcedureParameter[] parameters)
        {
            if (parameters == null) return;

            foreach (var param in parameters)
            {
                var dbParam = _provider.CreateParameter(
                    param.Name.StartsWith(_provider.ParameterPrefix) ? param.Name : _provider.ParameterPrefix + param.Name,
                    param.Value ?? DBNull.Value);

                dbParam.Direction = param.Direction;

                if (param.DbType.HasValue)
                    dbParam.DbType = param.DbType.Value;

                if (param.Size.HasValue)
                    dbParam.Size = param.Size.Value;

                command.Parameters.Add(dbParam);
            }
        }

        private StoredProcedureResult BuildResult(DbCommand command, StoredProcedureParameter[] parameters, int rowsAffected)
        {
            var result = new StoredProcedureResult
            {
                RowsAffected = rowsAffected
            };

            if (parameters == null) return result;

            foreach (var param in parameters)
            {
                var paramName = param.Name.StartsWith(_provider.ParameterPrefix)
                    ? param.Name
                    : _provider.ParameterPrefix + param.Name;

                var dbParam = command.Parameters[paramName] as DbParameter;
                if (dbParam == null) continue;

                if (param.Direction == ParameterDirection.ReturnValue)
                {
                    result.ReturnValue = dbParam.Value == DBNull.Value ? null : dbParam.Value;
                }
                else if (param.Direction == ParameterDirection.Output || param.Direction == ParameterDirection.InputOutput)
                {
                    var outputName = param.Name.StartsWith(_provider.ParameterPrefix)
                        ? param.Name.Substring(_provider.ParameterPrefix.Length)
                        : param.Name;
                    result.OutputParameters[outputName] = dbParam.Value == DBNull.Value ? null : dbParam.Value;
                }
            }

            return result;
        }
    }
}
