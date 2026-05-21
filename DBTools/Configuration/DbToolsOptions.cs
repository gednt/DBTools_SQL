using DBTools.Abstractions;
using System;
using System.Collections.Generic;

namespace DBTools.Configuration
{
    /// <summary>
    /// Options for configuring DBTools services via dependency injection.
    /// Supports both connection string and individual property configuration.
    /// </summary>
    public class DbToolsOptions
    {
        /// <summary>
        /// Full connection string. If provided, takes precedence over individual properties.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Database server host/address.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Database name.
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Database username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Database password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Database port (default: 1433 for SQL Server).
        /// </summary>
        public string Port { get; set; } = "1433";

        /// <summary>
        /// Command timeout in seconds (default: 30).
        /// </summary>
        public int CommandTimeout { get; set; } = 30;

        /// <summary>
        /// Whether to trust the server certificate (default: true for development).
        /// </summary>
        public bool TrustServerCertificate { get; set; } = true;

        /// <summary>
        /// The database provider to use (default: SqlServer).
        /// </summary>
        public DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;

        /// <summary>
        /// Registered query interceptors.
        /// </summary>
        internal List<IQueryInterceptor> Interceptors { get; } = new List<IQueryInterceptor>();

        /// <summary>
        /// Registered async query interceptors.
        /// </summary>
        internal List<IAsyncQueryInterceptor> AsyncInterceptors { get; } = new List<IAsyncQueryInterceptor>();

        /// <summary>
        /// Registers a query interceptor.
        /// </summary>
        public DbToolsOptions AddInterceptor(IQueryInterceptor interceptor)
        {
            Interceptors.Add(interceptor ?? throw new ArgumentNullException(nameof(interceptor)));
            return this;
        }

        /// <summary>
        /// Registers an async query interceptor.
        /// </summary>
        public DbToolsOptions AddInterceptor(IAsyncQueryInterceptor interceptor)
        {
            AsyncInterceptors.Add(interceptor ?? throw new ArgumentNullException(nameof(interceptor)));
            return this;
        }

        /// <summary>
        /// Builds a connection string from the configured properties.
        /// </summary>
        internal string BuildConnectionString()
        {
            if (!string.IsNullOrEmpty(ConnectionString))
                return ConnectionString;

            return Provider switch
            {
                DatabaseProvider.SqlServer => $"Data Source=tcp:{Host},{Port};Initial Catalog={Database};User ID={Username};Password={Password};TrustServerCertificate={TrustServerCertificate};",
                DatabaseProvider.PostgreSQL => $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};",
                DatabaseProvider.MySQL => $"Server={Host};Port={Port};Database={Database};Uid={Username};Pwd={Password};",
                DatabaseProvider.SQLite => $"Data Source={Database};",
                _ => throw new InvalidOperationException($"Unknown database provider: {Provider}")
            };
        }
    }

    /// <summary>
    /// Supported database providers.
    /// </summary>
    public enum DatabaseProvider
    {
        SqlServer,
        PostgreSQL,
        MySQL,
        SQLite
    }
}
