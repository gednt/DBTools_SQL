using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DBTools.Abstractions;
using DBTools.Providers;

namespace DBToolsUnitTest
{
    public enum TestDatabaseProvider
    {
        SqlServer,
        PostgreSQL,
        MySQL,
        SQLite
    }

    public abstract class TestBase
    {
        public const string TestHost = "127.0.0.1";
        public const string TestPort = "1433";
        public const string TestDatabase = "testDB";
        public const string TestUid = "testUser";
        public const string TestPassword = "Integration!123";
        public const string TestSqlServerPassword = "TestStrong!Passw0rd";
        public const string TestMySqlRootPassword = "Root!Passw0rd";

        public const string SqlServerHost = "127.0.0.1";
        public const string SqlServerPort = "1433";
        public const string PostgresHost = "127.0.0.1";
        public const string PostgresPort = "5432";
        public const string MySqlHost = "127.0.0.1";
        public const string MySqlPort = "3306";

        private static string _solutionDir;

        public static string GetSolutionDirectory()
        {
            if (_solutionDir != null) return _solutionDir;
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "DBTools.sln")))
            {
                dir = dir.Parent;
            }
            _solutionDir = dir?.FullName ?? Directory.GetCurrentDirectory();
            return _solutionDir;
        }

        public static string GetSqlServerConnectionString()
        {
            return $"Data Source=tcp:{TestHost},{TestPort};Initial Catalog={TestDatabase};User ID={TestUid};Password={TestPassword};Connection Timeout=5;TrustServerCertificate=True;";
        }

        public static string GetPostgreSqlConnectionString()
        {
            return $"Host={PostgresHost};Port={PostgresPort};Database={TestDatabase};Username={TestUid};Password={TestPassword};Timeout=5;";
        }

        public static string GetMySqlConnectionString()
        {
            return $"Server={MySqlHost};Port={MySqlPort};Database={TestDatabase};User={TestUid};Password={TestPassword};Connection Timeout=5;";
        }

        public static string GetSqliteConnectionString()
        {
            return $"Data Source={Path.Combine(GetSolutionDirectory(), "testDB.db")}";
        }

        public static string GetConnectionString(TestDatabaseProvider provider)
        {
            return provider switch
            {
                TestDatabaseProvider.SqlServer => GetSqlServerConnectionString(),
                TestDatabaseProvider.PostgreSQL => GetPostgreSqlConnectionString(),
                TestDatabaseProvider.MySQL => GetMySqlConnectionString(),
                TestDatabaseProvider.SQLite => GetSqliteConnectionString(),
                _ => throw new ArgumentOutOfRangeException(nameof(provider))
            };
        }

        public static IDbProvider GetDbProvider(TestDatabaseProvider provider)
        {
            return provider switch
            {
                TestDatabaseProvider.SqlServer => new SqlServerProvider(),
                TestDatabaseProvider.PostgreSQL => new PostgresProvider(),
                TestDatabaseProvider.MySQL => new MySqlProvider(),
                TestDatabaseProvider.SQLite => new SqliteProvider(),
                _ => throw new ArgumentOutOfRangeException(nameof(provider))
            };
        }

        public static bool IsSqlServerAvailable()
        {
            try
            {
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(GetSqlServerConnectionString());
                connection.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsPostgreSqlAvailable()
        {
            try
            {
                var connStr = GetPostgreSqlConnectionString();
                using var connection = new Npgsql.NpgsqlConnection(connStr);
                connection.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsMySqlAvailable()
        {
            try
            {
                var connStr = GetMySqlConnectionString();
                using var connection = new MySql.Data.MySqlClient.MySqlConnection(connStr);
                connection.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsSqliteAvailable()
        {
            try
            {
                var dbPath = Path.Combine(GetSolutionDirectory(), "testDB.db");
                if (!File.Exists(dbPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dbPath) ?? ".");
                    using var connection = new Microsoft.Data.Sqlite.SqliteConnection(GetSqliteConnectionString());
                    connection.Open();
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS users (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            name TEXT,
                            email TEXT,
                            age INTEGER NOT NULL,
                            status TEXT
                        );
                        CREATE TABLE IF NOT EXISTS orders (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            userid INTEGER NOT NULL,
                            product TEXT,
                            amount REAL NOT NULL
                        );
                        INSERT OR IGNORE INTO users (id, name, email, age, status) VALUES
                            (1, 'Alice', 'alice@test.com', 25, 'active'),
                            (2, 'Bob', 'bob@test.com', 30, 'active'),
                            (3, 'Charlie', 'charlie@test.com', 16, 'inactive');
                        INSERT OR IGNORE INTO orders (userid, product, amount) VALUES
                            (1, 'Widget', 10.00),
                            (1, 'Gadget', 20.00),
                            (2, 'Service', 15.50);";
                    cmd.ExecuteNonQuery();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsAnyDatabaseAvailable()
        {
            return IsSqlServerAvailable() || IsPostgreSqlAvailable() || IsMySqlAvailable() || IsSqliteAvailable();
        }

        [Obsolete("Use IsAnyDatabaseAvailable instead")]
        public static bool IsDatabaseAvailable()
        {
            return IsAnyDatabaseAvailable();
        }

        public static TestDatabaseProvider[] GetAvailableProviders()
        {
            var providers = new List<TestDatabaseProvider>();
            if (IsSqlServerAvailable()) providers.Add(TestDatabaseProvider.SqlServer);
            if (IsPostgreSqlAvailable()) providers.Add(TestDatabaseProvider.PostgreSQL);
            if (IsMySqlAvailable()) providers.Add(TestDatabaseProvider.MySQL);
            if (IsSqliteAvailable()) providers.Add(TestDatabaseProvider.SQLite);
            return providers.ToArray();
        }

        protected static void SkipIfDatabaseUnavailable()
        {
            if (!IsAnyDatabaseAvailable())
            {
                Assert.Inconclusive("No database is available. Please start docker-compose to run integration tests.");
            }
        }

        protected static string Surnames()
        {
            var names = "Smith,Johnson,Williams,Jones,Brown,Davis,Miller,Wilson,Moore,Taylor,Anderson,Thomas,Jackson,White,Harris,Martin,Thompson,Garcia,Martinez,Robinson,Clark,Rodriguez,Lewis,Lee,Walker,Hall,Allen,Young,Hernandez,King,Wright,Lopez,Hill,Scott,Green,Adams,Baker,Gonzalez,Nelson,Carter,Mitchell,Perez,Roberts".Split(',');
            Random rand = new Random();
            return names.ToList().ElementAt(rand.Next(names.Count()));
        }

        protected static string Names()
        {
            IEnumerable<string> surnames = "James,John,Robert,Michael,William,David,Richard,Joseph,Thomas,Charles,Christopher,Daniel,Matthew,Anthony,Donald,Mark,Paul,Steven,Andrew,Joshua,Kenneth,Kevin,Brian,George,Edward,Ronald,Timothy,Jason,Jeffrey,Ryan,Gary,Nicholas,Eric,Stephen,Jonathan,Larry,Justin,Scott,Brandon,Benjamin,Samantha".Split(',');
            Random rand = new Random();
            return surnames.ToList().ElementAt(rand.Next(surnames.Count()));
        }

        protected static string Emails(string name, string surname)
        {
            IEnumerable<string> domains = "test.com,example.com,mail.com,domain.com,email.com,web.com,inbox.com,site.com,online.com,service.com".Split(',');
            Random rand = new Random();
            return $"{name.ToLower()}.{surname.ToLower()}@{domains.ToList().ElementAt(rand.Next(domains.Count()))}";
        }

        protected static void ExecuteInTempDirectory(Action testAction, string configContent = null)
        {
            string originalDir = Directory.GetCurrentDirectory();
            string tempDir = Path.Combine(Path.GetTempPath(), "DBToolsTest_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                if (configContent != null)
                {
                    string configPath = Path.Combine(tempDir, "config.json");
                    File.WriteAllText(configPath, configContent);
                }

                Directory.SetCurrentDirectory(tempDir);
                testAction();
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
                if (Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
