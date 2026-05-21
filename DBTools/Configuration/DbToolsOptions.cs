using DBTools.Abstractions;
using DBTools.Pooling;
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
        /// Connection pool configuration options.
        /// </summary>
        public ConnectionPoolOptions PoolOptions { get; set; } = new ConnectionPoolOptions();

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
        /// Configures connection pooling options using a fluent builder pattern.
        /// </summary>
        /// <param name="configure">Action to configure the pool options.</param>
        /// <returns>The current DbToolsOptions instance for method chaining.</returns>
        public DbToolsOptions ConfigurePooling(Action<ConnectionPoolOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            configure(PoolOptions);
            return this;
        }

        /// <summary>
        /// Builds a connection string from the configured properties.
        /// </summary>
        internal string BuildConnectionString()
        {
            if (!string.IsNullOrEmpty(ConnectionString))
                return ConnectionString;

            var baseConnectionString = Provider switch
            {
                DatabaseProvider.SqlServer => $"Data Source=tcp:{Host},{Port};Initial Catalog={Database};User ID={Username};Password={Password};TrustServerCertificate={TrustServerCertificate};",
                DatabaseProvider.PostgreSQL => $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};",
                DatabaseProvider.MySQL => $"Server={Host};Port={Port};Database={Database};Uid={Username};Pwd={Password};",
                DatabaseProvider.SQLite => $"Data Source={Database};",
                _ => throw new InvalidOperationException($"Unknown database provider: {Provider}")
            };

            if (PoolOptions != null)
            {
                baseConnectionString += Provider switch
                {
                    DatabaseProvider.SqlServer => $"Min Pool Size={PoolOptions.MinPoolSize};Max Pool Size={PoolOptions.MaxPoolSize};Connection Lifetime={PoolOptions.ConnectionLifetimeSeconds};Pooling={PoolOptions.Pooling};",
                    DatabaseProvider.PostgreSQL => $"Minimum Pool Size={PoolOptions.MinPoolSize};Maximum Pool Size={PoolOptions.MaxPoolSize};Connection Idle Lifetime={PoolOptions.ConnectionIdleTimeoutSeconds};Pooling={PoolOptions.Pooling};",
                    DatabaseProvider.MySQL => $"MinimumPoolSize={PoolOptions.MinPoolSize};MaximumPoolSize={PoolOptions.MaxPoolSize};ConnectionLifeTime={PoolOptions.ConnectionLifetimeSeconds};Pooling={PoolOptions.Pooling};",
                    DatabaseProvider.SQLite => $"Pooling={PoolOptions.Pooling};",
                    _ => string.Empty
                };
            }

            return baseConnectionString;
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
