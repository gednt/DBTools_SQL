using DBTools.Abstractions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace DBTools.Core
{
    public class DbConfiguration : IDbConfiguration
    {
        public string Host { get; }
        public string Database { get; }
        public string Uid { get; }
        public string Password { get; }
        public string Port { get; }
        public string ConnectionString { get; }
        public string Provider { get; }

        public DbConfiguration()
        {
            var basePath = Directory.GetCurrentDirectory();
            var configFilePath = Path.Combine(basePath, "config.json");

            if (!File.Exists(configFilePath))
            {
                throw new FileNotFoundException(
                    $"The configuration file 'config.json' was not found in directory '{basePath}'.",
                    configFilePath);
            }

            try
            {
                IConfiguration configuration = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                    .Build();

                Host = configuration["Host"];
                Database = configuration["Database"];
                Uid = configuration["Uid"];
                Password = configuration["Password"];
                Port = configuration["Port"];
                Provider = configuration["Provider"] ?? "SqlServer";

                var missingKeys = new List<string>();
                if (string.IsNullOrWhiteSpace(Host)) missingKeys.Add("Host");
                if (string.IsNullOrWhiteSpace(Database)) missingKeys.Add("Database");
                if (string.IsNullOrWhiteSpace(Uid)) missingKeys.Add("Uid");
                if (string.IsNullOrWhiteSpace(Password)) missingKeys.Add("Password");
                if (string.IsNullOrWhiteSpace(Port)) missingKeys.Add("Port");

                if (missingKeys.Count > 0)
                {
                    throw new InvalidOperationException(
                        "The following required configuration keys are missing or empty in 'config.json': " +
                        string.Join(", ", missingKeys));
                }
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException(
                    "Failed to load database configuration from 'config.json'. See inner exception for details.",
                    ex);
            }

            ConnectionString = BuildConnectionString(Provider, Host, Port, Database, Uid, Password);
        }

        public DbConfiguration(IConfiguration configuration)
        {
            Host = configuration["Host"];
            Database = configuration["Database"];
            Uid = configuration["Uid"];
            Password = configuration["Password"];
            Port = configuration["Port"];
            Provider = configuration["Provider"] ?? "SqlServer";

            var missingKeys = new List<string>();
            if (string.IsNullOrWhiteSpace(Host)) missingKeys.Add("Host");
            if (string.IsNullOrWhiteSpace(Database)) missingKeys.Add("Database");
            if (string.IsNullOrWhiteSpace(Uid)) missingKeys.Add("Uid");
            if (string.IsNullOrWhiteSpace(Password)) missingKeys.Add("Password");
            if (string.IsNullOrWhiteSpace(Port)) missingKeys.Add("Port");

            if (missingKeys.Count > 0)
            {
                throw new InvalidOperationException(
                    "The following required configuration keys are missing or empty: " +
                    string.Join(", ", missingKeys));
            }

            ConnectionString = BuildConnectionString(Provider, Host, Port, Database, Uid, Password);
        }

        private static string BuildConnectionString(string provider, string host, string port, string database, string uid, string password)
        {
            return provider?.ToLowerInvariant() switch
            {
                "postgresql" or "postgres" => $"Host={host};Port={port};Database={database};Username={uid};Password={password};",
                "mysql" => $"Server={host};Port={port};Database={database};Uid={uid};Pwd={password};",
                "sqlite" => $"Data Source={database};",
                _ => $"Data Source=tcp:{host},{port};Initial Catalog={database};User ID={uid};Password={password};TrustServerCertificate=True;"
            };
        }
    }
}
