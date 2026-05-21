using DBTools.Abstractions;
using System;
using System.Data.Common;

namespace DBTools.Providers
{
    /// <summary>
    /// SQLite implementation of IDbProvider.
    /// Handles SQLite-specific dialect: LIMIT/OFFSET paging, double-quote quoting, ON CONFLICT upsert.
    /// Note: Requires Microsoft.Data.Sqlite package to be added when using this provider.
    /// </summary>
    public class SqliteProvider : IDbProvider
    {
        public string ParameterPrefix => "@";

        public string ProviderName => "SQLite";

        public bool SupportsMerge => true; // via INSERT OR REPLACE / ON CONFLICT

        public DbConnection CreateConnection(string connectionString)
        {
            var type = Type.GetType("Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite");
            if (type == null)
                throw new InvalidOperationException(
                    "Microsoft.Data.Sqlite package is not available. Install the 'Microsoft.Data.Sqlite' NuGet package.");

            return (DbConnection)Activator.CreateInstance(type, connectionString);
        }

        public DbCommand CreateCommand()
        {
            var type = Type.GetType("Microsoft.Data.Sqlite.SqliteCommand, Microsoft.Data.Sqlite");
            if (type == null)
                throw new InvalidOperationException("Microsoft.Data.Sqlite package is not available.");

            return (DbCommand)Activator.CreateInstance(type);
        }

        public DbParameter CreateParameter(string name, object value)
        {
            var type = Type.GetType("Microsoft.Data.Sqlite.SqliteParameter, Microsoft.Data.Sqlite");
            if (type == null)
                throw new InvalidOperationException("Microsoft.Data.Sqlite package is not available.");

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
            return "SELECT last_insert_rowid()";
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
                setClause.Append($"{columns[i]} = excluded.{columns[i]}");
            }

            return $"INSERT INTO {tableName} ({insertCols}) VALUES ({insertVals}) " +
                   $"ON CONFLICT({matchColumn}) DO UPDATE SET {setClause};";
        }
    }
}
