using DBTools.Abstractions;
using System;
using System.Data.Common;

namespace DBTools.Providers
{
    /// <summary>
    /// MySQL implementation of IDbProvider.
    /// Handles MySQL-specific dialect: LIMIT/OFFSET paging, backtick quoting, ON DUPLICATE KEY upsert.
    /// Note: Requires MySqlConnector or MySql.Data package to be added when using this provider.
    /// </summary>
    public class MySqlProvider : IDbProvider
    {
        public string ParameterPrefix => "@";

        public string ProviderName => "MySQL";

        public bool SupportsMerge => true; // via ON DUPLICATE KEY UPDATE

        public DbConnection CreateConnection(string connectionString)
        {
            // Try MySqlConnector first, then MySql.Data
            var type = Type.GetType("MySqlConnector.MySqlConnection, MySqlConnector")
                       ?? Type.GetType("MySql.Data.MySqlClient.MySqlConnection, MySql.Data");

            if (type == null)
                throw new InvalidOperationException(
                    "MySQL connector package is not available. Install 'MySqlConnector' or 'MySql.Data' NuGet package.");

            return (DbConnection)Activator.CreateInstance(type, connectionString);
        }

        public DbCommand CreateCommand()
        {
            var type = Type.GetType("MySqlConnector.MySqlCommand, MySqlConnector")
                       ?? Type.GetType("MySql.Data.MySqlClient.MySqlCommand, MySql.Data");

            if (type == null)
                throw new InvalidOperationException("MySQL connector package is not available.");

            return (DbCommand)Activator.CreateInstance(type);
        }

        public DbParameter CreateParameter(string name, object value)
        {
            var type = Type.GetType("MySqlConnector.MySqlParameter, MySqlConnector")
                       ?? Type.GetType("MySql.Data.MySqlClient.MySqlParameter, MySql.Data");

            if (type == null)
                throw new InvalidOperationException("MySQL connector package is not available.");

            return (DbParameter)Activator.CreateInstance(type, name, value ?? DBNull.Value);
        }

        public string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return identifier;

            if (identifier.StartsWith("`") && identifier.EndsWith("`"))
                return identifier;

            return $"`{identifier.Replace("`", "``")}`";
        }

        public string BuildPagingClause(int? skip, int? take, string orderByClause)
        {
            var sb = new System.Text.StringBuilder();

            if (!string.IsNullOrEmpty(orderByClause))
                sb.Append($" ORDER BY {orderByClause}");

            if (take.HasValue)
            {
                sb.Append($" LIMIT {take.Value}");
                if (skip.HasValue && skip.Value > 0)
                    sb.Append($" OFFSET {skip.Value}");
            }
            else if (skip.HasValue && skip.Value > 0)
            {
                // MySQL requires LIMIT with OFFSET
                sb.Append($" LIMIT 18446744073709551615 OFFSET {skip.Value}");
            }

            return sb.ToString();
        }

        public string GetLastInsertedIdSql()
        {
            return "SELECT LAST_INSERT_ID()";
        }

        public bool UsesTopNSyntax => false;

        public string BuildUpsertSql(string tableName, string[] columns, string matchColumn, string parameterPrefix)
        {
            var insertCols = new System.Text.StringBuilder();
            var insertVals = new System.Text.StringBuilder();
            var updateClause = new System.Text.StringBuilder();

            for (int i = 0; i < columns.Length; i++)
            {
                if (i > 0)
                {
                    insertCols.Append(", ");
                    insertVals.Append(", ");
                    updateClause.Append(", ");
                }

                insertCols.Append(columns[i]);
                insertVals.Append($"{parameterPrefix}p{i}");
                updateClause.Append($"{columns[i]} = VALUES({columns[i]})");
            }

            return $"INSERT INTO {tableName} ({insertCols}) VALUES ({insertVals}) " +
                   $"ON DUPLICATE KEY UPDATE {updateClause};";
        }
    }
}
