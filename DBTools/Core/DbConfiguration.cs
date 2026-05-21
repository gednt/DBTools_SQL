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

            ConnectionString = $"Data Source=tcp:{Host},{Port};Initial Catalog={Database};User ID={Uid};Password={Password};";
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

            ConnectionString = $"Data Source=tcp:{Host},{Port};Initial Catalog={Database};User ID={Uid};Password={Password};";
        }
    }
}
