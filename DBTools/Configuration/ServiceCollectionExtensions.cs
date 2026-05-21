using DBTools.Abstractions;
using DBTools.Core;
using DBTools.Providers;
using DBTools.StoredProcedures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace DBTools.Configuration
{
    /// <summary>
    /// Extension methods for registering DBTools services with Microsoft.Extensions.DependencyInjection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds DBTools services to the service collection with the specified options.
        /// Registers AsyncSqlClient as a scoped service, and ISqlValidator/ISqlQueryBuilder as singletons.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configure">Action to configure DBTools options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddDbTools(this IServiceCollection services, Action<DbToolsOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var options = new DbToolsOptions();
            configure(options);

            // Register options as singleton
            services.AddSingleton(options);

            // Register core infrastructure as singleton
            services.TryAddSingleton<ISqlValidator, SqlValidator>();
            services.TryAddSingleton<ISqlQueryBuilder>(sp =>
                new SqlQueryBuilder(sp.GetRequiredService<ISqlValidator>()));

            // Register provider based on configuration
            services.TryAddSingleton<IDbProvider>(sp => CreateProvider(options.Provider));

            // Register configuration from options
            services.TryAddSingleton<IDbConfiguration>(sp =>
                new OptionsDbConfiguration(options));

            // Register AsyncSqlClient as scoped (one per request/scope)
            services.AddScoped<IAsyncSqlClient>(sp =>
            {
                var validator = sp.GetRequiredService<ISqlValidator>();
                var queryBuilder = sp.GetRequiredService<ISqlQueryBuilder>();
                var provider = sp.GetRequiredService<IDbProvider>();
                var config = sp.GetRequiredService<IDbConfiguration>();

                var client = new AsyncSqlClient(config, validator, queryBuilder, provider);

                foreach (var interceptor in options.Interceptors)
                    client.AddInterceptor(interceptor);
                foreach (var asyncInterceptor in options.AsyncInterceptors)
                    client.AddInterceptor(asyncInterceptor);

                return client;
            });

            // Register concrete AsyncSqlClient as scoped
            services.AddScoped<AsyncSqlClient>(sp => (AsyncSqlClient)sp.GetRequiredService<IAsyncSqlClient>());

            // Keep backward-compatible SqlClient available
            services.TryAddTransient<SqlClient>(sp =>
            {
                var config = sp.GetRequiredService<IDbConfiguration>();
                var validator = sp.GetRequiredService<ISqlValidator>();
                var queryBuilder = sp.GetRequiredService<ISqlQueryBuilder>();
                var provider = sp.GetRequiredService<IDbProvider>();
                return new SqlClient(config, validator, queryBuilder, provider);
            });

            // Register StoredProcedureClient as scoped (implements both sync and async interfaces)
            services.AddScoped<StoredProcedureClient>(sp =>
            {
                var provider = sp.GetRequiredService<IDbProvider>();
                var config = sp.GetRequiredService<IDbConfiguration>();
                return new StoredProcedureClient(provider, config.ConnectionString);
            });

            services.AddScoped<IStoredProcedureClient>(sp =>
                sp.GetRequiredService<StoredProcedureClient>());

            services.AddScoped<IAsyncStoredProcedureClient>(sp =>
                sp.GetRequiredService<StoredProcedureClient>());

            return services;
        }

        /// <summary>
        /// Adds DBTools services with just a connection string (SQL Server default).
        /// </summary>
        public static IServiceCollection AddDbTools(this IServiceCollection services, string connectionString)
        {
            return services.AddDbTools(options =>
            {
                options.ConnectionString = connectionString;
                options.Provider = DatabaseProvider.SqlServer;
            });
        }

        private static IDbProvider CreateProvider(DatabaseProvider provider)
        {
            return DbProviderFactory.Create(provider);
        }
    }

    /// <summary>
    /// Internal IDbConfiguration implementation that reads from DbToolsOptions.
    /// </summary>
    internal class OptionsDbConfiguration : IDbConfiguration
    {
        public string Host { get; }
        public string Database { get; }
        public string Uid { get; }
        public string Password { get; }
        public string Port { get; }
        public string ConnectionString { get; }
        public string Provider { get; }

        public OptionsDbConfiguration(DbToolsOptions options)
        {
            Host = options.Host ?? "";
            Database = options.Database ?? "";
            Uid = options.Username ?? "";
            Password = options.Password ?? "";
            Port = options.Port ?? "1433";
            Provider = options.Provider.ToString();
            ConnectionString = options.BuildConnectionString();
        }
    }
}
