using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.SqlClient;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DBToolsUnitTest
{
    /// <summary>
    /// Base class for integration tests that require a live database.
    /// Manages container lifecycle with a preferred strategy:
    ///   1. Docker Compose (if docker-compose.yml is present) — preferred for CI
    ///   2. Testcontainers (MsSql module) — fallback for local development
    ///   3. Existing database at 127.0.0.1:1433 — fallback if already running
    ///
    /// All integration test classes should inherit from this instead of TestBase
    /// and be decorated with [TestCategory("Integration")].
    /// 
    /// Usage:
    ///   [TestClass]
    ///   [TestCategory("Integration")]
    ///   public class MyIntegrationTests : IntegrationTestBase
    ///   {
    ///       [ClassInitialize]
    ///       public static void ClassSetup(TestContext context) => EnsureContainersReady();
    ///       
    ///       [TestMethod]
    ///       public void MyTest() { SkipIfDatabaseNotReady(); /* ... */ }
    ///   }
    /// </summary>
    public abstract class IntegrationTestBase : TestBase
    {
        private static readonly object _lock = new object();
        private static ContainerLifecycle _lifecycle = ContainerLifecycle.NotInitialized;
        private static bool _databaseReady;
        private static string _initError;

        /// <summary>
        /// Ensures Docker containers are running before integration tests execute.
        /// Call this from [ClassInitialize] in each integration test class.
        /// This method is idempotent — calling it multiple times is safe.
        /// </summary>
        public static void EnsureContainersReady()
        {
            lock (_lock)
            {
                if (_lifecycle != ContainerLifecycle.NotInitialized)
                    return;

                TryInitializeContainers();
            }
        }

        /// <summary>
        /// Stops containers started by Testcontainers. Docker Compose-managed
        /// containers are not stopped (they should be managed externally).
        /// </summary>
        public static async Task CleanupContainersAsync()
        {
            // Only Testcontainers-managed containers need explicit cleanup
            if (_lifecycle == ContainerLifecycle.Testcontainers)
            {
                Console.WriteLine("[IntegrationTestBase] Note: Testcontainers will be cleaned up by Ryuk.");
            }

            lock (_lock)
            {
                _lifecycle = ContainerLifecycle.NotInitialized;
                _databaseReady = false;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Skips the test if the database is not available, with a descriptive message.
        /// </summary>
        protected static void SkipIfDatabaseNotReady()
        {
            if (!_databaseReady)
            {
                Assert.Inconclusive(
                    $"Database is not available (strategy: {_lifecycle}). " +
                    $"{_initError ?? "Run 'docker compose up -d' or ensure SQL Server is reachable at 127.0.0.1:1433."}");
            }

            if (!IsDatabaseAvailable())
            {
                Assert.Inconclusive("Database was initialized but is not currently reachable. Skipping test.");
            }
        }

        #region Container Lifecycle

        private enum ContainerLifecycle
        {
            NotInitialized,
            ExistingDatabase,
            DockerCompose,
            Testcontainers
        }

        private static void TryInitializeContainers()
        {
            // Strategy 1: Check if database is already reachable
            if (IsDatabaseAvailable())
            {
                _lifecycle = ContainerLifecycle.ExistingDatabase;
                _databaseReady = true;
                Console.WriteLine("[IntegrationTestBase] Database is already reachable — no container startup needed.");
                return;
            }

            Console.WriteLine("[IntegrationTestBase] Database not reachable. Attempting to start containers...");

            // Strategy 2: Try Docker Compose
            if (TryStartDockerCompose())
            {
                if (WaitForDatabase(timeoutSeconds: 60))
                {
                    _lifecycle = ContainerLifecycle.DockerCompose;
                    _databaseReady = true;
                    Console.WriteLine("[IntegrationTestBase] Docker Compose containers started and database is ready.");
                    return;
                }

                Console.WriteLine("[IntegrationTestBase] Docker Compose started but database not ready in time.");
            }

            // Strategy 3: Try Testcontainers
            if (TryStartTestcontainers())
            {
                if (WaitForDatabase(timeoutSeconds: 60))
                {
                    _lifecycle = ContainerLifecycle.Testcontainers;
                    _databaseReady = true;
                    Console.WriteLine("[IntegrationTestBase] Testcontainers SQL Server started and database is ready.");
                    return;
                }
            }

            _lifecycle = ContainerLifecycle.NotInitialized;
            _databaseReady = false;
            _initError = "Could not start database containers via Docker Compose or Testcontainers, and no existing database was found.";
            Console.WriteLine($"[IntegrationTestBase] {_initError}");
        }

        private static bool TryStartDockerCompose()
        {
            try
            {
                Console.WriteLine("[IntegrationTestBase] Attempting Docker Compose...");

                string composeFile = FindDockerComposeFile();
                if (composeFile == null)
                {
                    Console.WriteLine("[IntegrationTestBase] No docker-compose.yml found — skipping Docker Compose.");
                    return false;
                }

                string composeDir = Path.GetDirectoryName(composeFile);
                Console.WriteLine($"[IntegrationTestBase] Using docker-compose.yml at: {composeFile}");

                // Try docker compose v2 first, then docker-compose v1
                bool started = RunProcess("docker", $"compose -f \"{composeFile}\" up -d sqlserver --wait", workingDir: composeDir, timeoutMs: 120000);
                if (started)
                {
                    started = RunProcess("docker", $"compose -f \"{composeFile}\" up sqlserver-setup --abort-on-container-exit --exit-code-from sqlserver-setup", workingDir: composeDir, timeoutMs: 120000);
                }
                if (!started)
                {
                    started = RunProcess("docker-compose", $"-f \"{composeFile}\" up -d", workingDir: composeDir, timeoutMs: 120000);
                }

                if (started)
                {
                    Console.WriteLine("[IntegrationTestBase] Docker Compose containers started. Waiting for database...");
                    return true;
                }

                Console.WriteLine("[IntegrationTestBase] Docker Compose command failed.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IntegrationTestBase] Docker Compose error: {ex.Message}");
                return false;
            }
        }

        private static bool TryStartTestcontainers()
        {
            try
            {
                Console.WriteLine("[IntegrationTestBase] Attempting Testcontainers (MsSql module)...");

                var testcontainer = new Testcontainers.MsSql.MsSqlBuilder()
                    .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                    .WithPortBinding(1433, 1433)
                    .Build();

                testcontainer.StartAsync().GetAwaiter().GetResult();
                Console.WriteLine("[IntegrationTestBase] Testcontainers container started. Setting up database and user...");

                // Create the testDB database and testUser login using SA connection
                string saConnectionString = testcontainer.GetConnectionString() + ";TrustServerCertificate=True;";
                using (var connection = new SqlConnection(saConnectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"
                            IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'testDB')
                                CREATE DATABASE testDB;
                            IF NOT EXISTS (SELECT loginname FROM master.sys.syslogins WHERE loginname = 'testUser')
                            BEGIN
                                CREATE LOGIN testUser WITH PASSWORD = 'Integration!123';
                                ALTER SERVER ROLE sysadmin ADD MEMBER testUser;
                            END";
                        cmd.ExecuteNonQuery();
                    }

                    ApplyIntegrationSchema(connection);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IntegrationTestBase] Testcontainers error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Helpers

        private static string FindDockerComposeFile()
        {
            string dir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 8; i++)
            {
                string candidate = Path.GetFullPath(Path.Combine(dir, "docker-compose.yml"));
                if (File.Exists(candidate))
                    return candidate;
                dir = Path.Combine(dir, "..");
            }
            return null;
        }

        private static string FindIntegrationSchemaScript()
        {
            string dir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 8; i++)
            {
                string candidate = Path.GetFullPath(Path.Combine(dir, "scripts", "sqlserver-integration-setup.sql"));
                if (File.Exists(candidate))
                    return candidate;
                dir = Path.Combine(dir, "..");
            }
            return null;
        }

        private static void ApplyIntegrationSchema(SqlConnection saConnection)
        {
            string scriptPath = FindIntegrationSchemaScript();
            if (scriptPath == null)
            {
                Console.WriteLine("[IntegrationTestBase] Integration schema script not found; skipping table seed.");
                return;
            }

            string script = File.ReadAllText(scriptPath);
            using (var cmd = saConnection.CreateCommand())
            {
                cmd.CommandText = "USE testDB;";
                cmd.ExecuteNonQuery();
                cmd.CommandText = script;
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine("[IntegrationTestBase] Applied integration test schema from " + scriptPath);
        }

        private static bool WaitForDatabase(int timeoutSeconds)
        {
            Console.WriteLine($"[IntegrationTestBase] Waiting up to {timeoutSeconds}s for database...");
            for (int i = 0; i < timeoutSeconds; i++)
            {
                if (IsDatabaseAvailable())
                    return true;
                Thread.Sleep(1000);
            }
            return false;
        }

        private static bool RunProcess(string fileName, string arguments, string workingDir = null, int timeoutMs = 30000)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                if (workingDir != null)
                    psi.WorkingDirectory = workingDir;

                using (var process = Process.Start(psi))
                {
                    if (process == null) return false;
                    bool exited = process.WaitForExit(timeoutMs);
                    if (!exited)
                    {
                        try { process.Kill(); } catch { }
                        return false;
                    }
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}