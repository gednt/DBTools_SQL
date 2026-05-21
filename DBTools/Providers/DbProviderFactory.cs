using DBTools.Abstractions;
using DBTools.Configuration;
using System;

namespace DBTools.Providers
{
    /// <summary>
    /// Factory class for creating IDbProvider instances from configuration.
    /// Centralizes provider resolution for config-driven provider selection.
    /// </summary>
    public static class DbProviderFactory
    {
        /// <summary>
        /// Creates an IDbProvider from a DatabaseProvider enum value.
        /// </summary>
        public static IDbProvider Create(DatabaseProvider provider)
        {
            return provider switch
            {
                DatabaseProvider.SqlServer => new SqlServerProvider(),
                DatabaseProvider.PostgreSQL => new PostgresProvider(),
                DatabaseProvider.MySQL => new MySqlProvider(),
                DatabaseProvider.SQLite => new SqliteProvider(),
                _ => throw new ArgumentException($"Unsupported database provider: {provider}", nameof(provider))
            };
        }

        /// <summary>
        /// Creates an IDbProvider from a provider name string.
        /// Supports: "SqlServer", "PostgreSQL", "MySQL", "SQLite" (case-insensitive).
        /// </summary>
        public static IDbProvider Create(string providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName))
                throw new ArgumentException("Provider name cannot be null or empty.", nameof(providerName));

            return providerName.ToLowerInvariant() switch
            {
                "sqlserver" => new SqlServerProvider(),
                "postgresql" or "postgres" => new PostgresProvider(),
                "mysql" => new MySqlProvider(),
                "sqlite" => new SqliteProvider(),
                _ => throw new ArgumentException($"Unknown database provider: '{providerName}'. Supported providers: SqlServer, PostgreSQL, MySQL, SQLite.", nameof(providerName))
            };
        }
    }
}
