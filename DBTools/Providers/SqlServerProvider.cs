using DBTools.Abstractions;
using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace DBTools.Providers
{
    /// <summary>
    /// SQL Server implementation of IDbProvider.
    /// Handles SQL Server-specific dialect: OFFSET/FETCH paging, SCOPE_IDENTITY(), bracket quoting, MERGE.
    /// </summary>
    public class SqlServerProvider : IDbProvider
    {
        public string ParameterPrefix => "@";

        public string ProviderName => "SqlServer";

        public bool SupportsMerge => true;

        public DbConnection CreateConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

        public DbCommand CreateCommand()
        {
            return new SqlCommand();
        }

        public DbParameter CreateParameter(string name, object value)
        {
            return new SqlParameter(name, value ?? System.DBNull.Value);
        }

        public string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return identifier;

            // Already quoted
            if (identifier.StartsWith("[") && identifier.EndsWith("]"))
                return identifier;

            return $"[{identifier.Replace("]", "]]")}]";
        }

        public string BuildPagingClause(int? skip, int? take, string orderByClause)
        {
            if (!skip.HasValue && !take.HasValue)
                return string.Empty;

            var sb = new System.Text.StringBuilder();

            if (string.IsNullOrEmpty(orderByClause))
                orderByClause = "(SELECT NULL)";

            sb.Append($" ORDER BY {orderByClause}");
            sb.Append($" OFFSET {skip ?? 0} ROWS");

            if (take.HasValue)
                sb.Append($" FETCH NEXT {take.Value} ROWS ONLY");

            return sb.ToString();
        }

        public string GetLastInsertedIdSql()
        {
            return "SELECT SCOPE_IDENTITY()";
        }

        public string BuildUpsertSql(string tableName, string[] columns, string matchColumn, string parameterPrefix)
        {
            var setClause = new System.Text.StringBuilder();
            var insertCols = new System.Text.StringBuilder();
            var insertVals = new System.Text.StringBuilder();

            for (int i = 0; i < columns.Length; i++)
            {
                if (i > 0)
                {
                    setClause.Append(", ");
                    insertCols.Append(", ");
                    insertVals.Append(", ");
                }

                setClause.Append($"target.{columns[i]} = {parameterPrefix}p{i}");
                insertCols.Append(columns[i]);
                insertVals.Append($"{parameterPrefix}p{i}");
            }

            return $"MERGE INTO {tableName} AS target " +
                   $"USING (SELECT {parameterPrefix}match AS {matchColumn}) AS source ON target.{matchColumn} = source.{matchColumn} " +
                   $"WHEN MATCHED THEN UPDATE SET {setClause} " +
                   $"WHEN NOT MATCHED THEN INSERT ({insertCols}) VALUES ({insertVals});";
        }
    }
}
