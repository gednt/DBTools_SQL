using System.Data.Common;

namespace DBTools.Abstractions
{
    /// <summary>
    /// Abstracts database provider-specific behavior for multi-provider support.
    /// Implementations handle SQL dialect differences (paging, identity, quoting, etc.).
    /// </summary>
    public interface IDbProvider
    {
        /// <summary>
        /// Creates a new database connection for the given connection string.
        /// </summary>
        DbConnection CreateConnection(string connectionString);

        /// <summary>
        /// Creates a new database command.
        /// </summary>
        DbCommand CreateCommand();

        /// <summary>
        /// Creates a new database parameter.
        /// </summary>
        DbParameter CreateParameter(string name, object value);

        /// <summary>
        /// Gets the parameter prefix for this provider (e.g., "@" for SQL Server, ":" for Oracle).
        /// </summary>
        string ParameterPrefix { get; }

        /// <summary>
        /// Gets the provider name identifier.
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Quotes an identifier (table/column name) for this provider.
        /// </summary>
        string QuoteIdentifier(string identifier);

        /// <summary>
        /// Builds a paging clause for the provider's SQL dialect.
        /// </summary>
        string BuildPagingClause(int? skip, int? take, string orderByClause);

        /// <summary>
        /// Gets the SQL fragment to retrieve the last inserted identity value.
        /// </summary>
        string GetLastInsertedIdSql();

        /// <summary>
        /// Determines whether this provider supports MERGE (upsert) natively.
        /// </summary>
        bool SupportsMerge { get; }

        /// <summary>
        /// Builds an upsert statement for this provider.
        /// </summary>
        string BuildUpsertSql(string tableName, string[] columns, string matchColumn, string parameterPrefix);
    }
}
