using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DBToolsUnitTest
{
    /// <summary>
    /// Base class for all DBTools unit tests.
    /// Provides shared constants, test data generators, and utility methods.
    /// Follows DIP: tests depend on this abstraction rather than duplicating setup.
    /// </summary>
    public abstract class TestBase
    {
        protected const string TestHost = "127.0.0.1";
        protected const string TestDatabase = "testDB";
        protected const string TestUid = "testUser";
        protected const string TestPassword = "123456";
        protected const string TestPort = "1433";

        protected static bool IsDatabaseAvailable()
        {
            try
            {
                string connectionString = $"Data Source=tcp:{TestHost},{TestPort};Initial Catalog={TestDatabase};User ID={TestUid};Password={TestPassword};Connection Timeout=2;";
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        protected static void SkipIfDatabaseUnavailable()
        {
            if (!IsDatabaseAvailable())
            {
                Assert.Inconclusive("Database is not available. Skipping test.");
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

        /// <summary>
        /// Executes test code in a temporary directory with optional config file.
        /// Useful for testing config-dependent constructors without affecting the real config.
        /// </summary>
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
