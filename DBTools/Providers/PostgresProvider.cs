using DBTools.Abstractions;
using System;
using System.Data.Common;

namespace DBTools.Providers
{
    /// <summary>
    /// PostgreSQL implementation of IDbProvider.
    /// Handles PostgreSQL-specific dialect: LIMIT/OFFSET paging, double-quote quoting, ON CONFLICT upsert.
    /// Note: Requires Npgsql package to be added when using this provider.
    /// </summary>
    public class PostgresProvider : IDbProvider
    {
        public string ParameterPrefix => "@";

        public string ProviderName => "PostgreSQL";

        public bool SupportsMerge => true; // via ON CONFLICT

        public DbConnection CreateConnection(string connectionString)
        {
            // Uses reflection to avoid hard dependency on Npgsql
            var type = Type.GetType("Npgsql.NpgsqlConnection, Npgsql");
            if (type == null)
                throw new InvalidOperationException(
                    "Npgsql package is not available. Install the 'Npgsql' NuGet package to use PostgreSQL provider.");

            return (DbConnection)Activator.CreateInstance(type, connectionString);
        }

        public DbCommand CreateCommand()
        {
            var type = Type.GetType("Npgsql.NpgsqlCommand, Npgsql");
            if (type == null)
                throw new InvalidOperationException("Npgsql package is not available.");

            return (DbCommand)Activator.CreateInstance(type);
        }

        public DbParameter CreateParameter(string name, object value)
        {
            var type = Type.GetType("Npgsql.NpgsqlParameter, Npgsql");
            if (type == null)
                throw new InvalidOperationException("Npgsql package is not available.");

            return (DbParameter)Activator.CreateInstance(type, name, value ?? DBNull.Value);
        }

        public string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return identifier;

            if (identifier.StartsWith("\"") && identifier.EndsWith("\""))
                return identifier;

            return $"\"{identifier.Replace("\"", "\"\"")}\"";
        }

        public string BuildPagingClause(int? skip, int? take, string orderByClause)
        {
            var sb = new System.Text.StringBuilder();

            if (!string.IsNullOrEmpty(orderByClause))
                sb.Append($" ORDER BY {orderByClause}");

            if (take.HasValue)
                sb.Append($" LIMIT {take.Value}");

            if (skip.HasValue && skip.Value > 0)
                sb.Append($" OFFSET {skip.Value}");

            return sb.ToString();
        }

        public string GetLastInsertedIdSql()
        {
            return "RETURNING id"; // Appended to INSERT statement
        }

        public string BuildUpsertSql(string tableName, string[] columns, string matchColumn, string parameterPrefix)
        {
            var insertCols = new System.Text.StringBuilder();
            var insertVals = new System.Text.StringBuilder();
            var setClause = new System.Text.StringBuilder();

            for (int i = 0; i < columns.Length; i++)
            {
                if (i > 0)
                {
                    insertCols.Append(", ");
                    insertVals.Append(", ");
                    setClause.Append(", ");
                }

                insertCols.Append(columns[i]);
                insertVals.Append($"{parameterPrefix}p{i}");
                setClause.Append($"{columns[i]} = EXCLUDED.{columns[i]}");
            }

            return $"INSERT INTO {tableName} ({insertCols}) VALUES ({insertVals}) " +
                   $"ON CONFLICT ({matchColumn}) DO UPDATE SET {setClause};";
        }
    }
}
