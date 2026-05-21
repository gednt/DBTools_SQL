using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Abstractions;
using DBTools.Configuration;
using DBTools.Migrations;
using DBTools.Providers;
using System.Collections.Generic;
using System.Linq;

namespace DBToolsUnitTest.Migrations
{
    [TestClass]
    public class MigrationRunnerTests : TestBase
    {
        #region SQL Generation - SqlServer

        [TestMethod]
        public void GenerateCreateTableSql_SqlServer_ContainsIfNotExists()
        {
            var runner = CreateRunner(DatabaseProvider.SqlServer);
            var sql = runner.GenerateCreateTableSql();

            Assert.IsTrue(sql.Contains("IF NOT EXISTS"));
        }

        [TestMethod]
        public void GenerateCreateTableSql_SqlServer_ContainsTableName()
        {
            var runner = CreateRunner(DatabaseProvider.SqlServer);
            var sql = runner.GenerateCreateTableSql();

            Assert.IsTrue(sql.Contains("__DBToolsMigrations"));
        }

        [TestMethod]
        public void GenerateCreateTableSql_SqlServer_UsesBracketQuoting()
        {
            var runner = CreateRunner(DatabaseProvider.SqlServer);
            var sql = runner.GenerateCreateTableSql();

            Assert.IsTrue(sql.Contains("[__DBToolsMigrations]"));
        }

        [TestMethod]
        public void GenerateCreateTableSql_SqlServer_UsesNvarchar()
        {
            var runner = CreateRunner(DatabaseProvider.SqlServer);
            var sql = runner.GenerateCreateTableSql();

            Assert.IsTrue(sql.Contains("NVARCHAR(255)"));
            Assert.IsTrue(sql.Contains("NVARCHAR(500)"));
            Assert.IsTrue(sql.Contains("NVARCHAR(64)"));
        }

        [TestMethod]
        public void GenerateCreateTableSql_SqlServer_UsesDatetime2()
        {
            var runner = CreateRunner(DatabaseProvider.SqlServer);
            var sql = runner.GenerateCreateTableSql();

            Assert.IsTrue(sql.Contains("DATETIME2"));
        }

        [TestMethod]
        public void GenerateCreateTableSql_SqlServer_HasPrimaryKey()
        {
            var runner = CreateRunner(DatabaseProvider.SqlServer);
            var sql = runner.GenerateCreateTableSql();

            Assert.IsTrue(sql.Contains("PRIMARY KEY"));
        }

        #endregion

        #region SQL Generation - PostgreSQL

        [TestMethod]
        public void GenerateCreateTableSql_PostgreSQL_ContainsCreateTableIfNotExists()
        {
            var runner = CreateRunner(DatabaseProvider.PostgreSQL);
            var sql = runner.GenerateCreateTableSql();

            Assert.IsTrue(sql.Contains("CREATE TABLE IF NOT EXISTS"));
        }

        [TestMethod]
        public void GenerateCreateTableSql_PostgreSQL_UsesDoubleQuoteQuoting()
        {
            var runner = CreateRunner(DatabaseProvider.PostgreSQL);
            var sql = runner.GenerateCreateTableSql();

            Assert.IsTrue(sql.Contains("\"__DBToolsMigrations\""));
        }

        [TestMethod]
        public void GenerateCreateTableSql_PostgreSQL_UsesVarchar()
        {
            var runner = CreateRunner(DatabaseProvider.PostgreSQL);
            var sql = runner.GenerateCreateTableSql();

            Assert.IsTrue(sql.Contains("VARCHAR(255)"));
            Assert.IsTrue(sql.Contains("VARCHAR(500)"));
            Assert.IsTrue(sql.Contains("VARCHAR(64)"));
        }

        [TestMethod]
        public void GenerateCreateTableSql_PostgreSQL_UsesTimestamp()
        {
            var runner = CreateRunner(DatabaseProvider.PostgreSQL);
            var sql = runner.GenerateCreateTableSql();

            Assert.IsTrue(sql.Contains("TIMESTAMP"));
        }

        [TestMethod]
        public void GenerateCreateTableSql_PostgreSQL_QuotesColumnNames()
        {
            var runner = CreateRunner(DatabaseProvider.PostgreSQL);
            var sql = runner.GenerateCreateTableSql();

            Assert.IsTrue(sql.Contains("\"MigrationId\""));
            Assert.IsTrue(sql.Contains("\"Description\""));
            Assert.IsTrue(sql.Contains("\"AppliedAt\""));
            Assert.IsTrue(sql.Contains("\"Checksum\""));
        }

        #endregion

        #region SQL Generation - MySQL

        [TestMethod]
        public void GenerateCreateTableSql_MySQL_ContainsCreateTableIfNotExists()
        {
            var runner = CreateRunner(DatabaseProvider.MySQL);
            var sql = runner.GenerateCreateTableSql();

            Assert.IsTrue(sql.Contains("CREATE TABLE IF NOT EXISTS"));
        }

        [TestMethod]
        public void GenerateCreateTableSql_MySQL_UsesBacktickQuoting()
        {
            var runner = CreateRunner(DatabaseProvider.MySQL);
            var sql = runner.GenerateCreateTableSql();

            Assert.IsTrue(sql.Contains("`__DBToolsMigrations`"));
        }

        [TestMethod]
        public void GenerateCreateTableSql_MySQL_UsesDatetime()
        {
            var runner = CreateRunner(DatabaseProvider.MySQL);
            var sql = runner.GenerateCreateTableSql();

            Assert.IsTrue(sql.Contains("DATETIME"));
        }

        [TestMethod]
        public void GenerateCreateTableSql_MySQL_QuotesColumnNames()
        {
            var runner = CreateRunner(DatabaseProvider.MySQL);
            var sql = runner.GenerateCreateTableSql();

            Assert.IsTrue(sql.Contains("`MigrationId`"));
            Assert.IsTrue(sql.Contains("`Description`"));
            Assert.IsTrue(sql.Contains("`AppliedAt`"));
            Assert.IsTrue(sql.Contains("`Checksum`"));
        }

        #endregion

        #region SQL Generation - SQLite

        [TestMethod]
        public void GenerateCreateTableSql_SQLite_ContainsCreateTableIfNotExists()
        {
            var runner = CreateRunner(DatabaseProvider.SQLite);
            var sql = runner.GenerateCreateTableSql();

            Assert.IsTrue(sql.Contains("CREATE TABLE IF NOT EXISTS"));
        }

        [TestMethod]
        public void GenerateCreateTableSql_SQLite_UsesDoubleQuoteQuoting()
        {
            var runner = CreateRunner(DatabaseProvider.SQLite);
            var sql = runner.GenerateCreateTableSql();

            Assert.IsTrue(sql.Contains("\"__DBToolsMigrations\""));
        }

        [TestMethod]
        public void GenerateCreateTableSql_SQLite_UsesTextTypes()
        {
            var runner = CreateRunner(DatabaseProvider.SQLite);
            var sql = runner.GenerateCreateTableSql();

            Assert.IsTrue(sql.Contains("TEXT PRIMARY KEY"));
            Assert.IsTrue(sql.Contains("\"Description\" TEXT"));
            Assert.IsTrue(sql.Contains("\"AppliedAt\" TEXT"));
            Assert.IsTrue(sql.Contains("\"Checksum\" TEXT"));
        }

        #endregion

        #region Custom Table Name

        [TestMethod]
        public void GenerateCreateTableSql_CustomTableName_UsesCustomName()
        {
            var options = new MigrationOptions { MigrationTableName = "CustomMigrations" };
            var provider = DbProviderFactory.Create(DatabaseProvider.SqlServer);
            var runner = new MigrationRunner(provider, "Server=test;Database=test;", options);

            var sql = runner.GenerateCreateTableSql();

            Assert.IsTrue(sql.Contains("[CustomMigrations]"));
            Assert.IsFalse(sql.Contains("__DBToolsMigrations"));
        }

        #endregion

        #region Migration Ordering Tests

        [TestMethod]
        public void MigrationOptions_Migrations_DefaultsToEmpty()
        {
            var options = new MigrationOptions();
            Assert.IsNotNull(options.Migrations);
            Assert.AreEqual(0, options.Migrations.Count);
        }

        [TestMethod]
        public void MigrationOptions_DefaultTableName_IsCorrect()
        {
            var options = new MigrationOptions();
            Assert.AreEqual("__DBToolsMigrations", options.MigrationTableName);
        }

        [TestMethod]
        public void MigrationOptions_AutoMigrateOnStartup_DefaultsFalse()
        {
            var options = new MigrationOptions();
            Assert.IsFalse(options.AutoMigrateOnStartup);
        }

        #endregion

        #region Checksum Computation Tests

        [TestMethod]
        public void ComputeChecksum_ConsistentWithCacheKeyPattern()
        {
            // Verify checksum produces a 64-char hex string (SHA256)
            var checksum = MigrationBase.ComputeChecksum("CREATE TABLE Test (Id INT)");

            Assert.AreEqual(64, checksum.Length);
            Assert.IsTrue(checksum.All(c => "0123456789abcdef".Contains(c)));
        }

        [TestMethod]
        public void ComputeChecksum_IsDeterministic()
        {
            var sql = "ALTER TABLE Users ADD COLUMN Age INT";
            var checksums = new HashSet<string>();

            for (int i = 0; i < 10; i++)
            {
                checksums.Add(MigrationBase.ComputeChecksum(sql));
            }

            Assert.AreEqual(1, checksums.Count);
        }

        [TestMethod]
        public void ComputeChecksum_WhitespaceMatters()
        {
            var checksum1 = MigrationBase.ComputeChecksum("CREATE TABLE T(Id INT)");
            var checksum2 = MigrationBase.ComputeChecksum("CREATE TABLE T (Id INT)");

            Assert.AreNotEqual(checksum1, checksum2);
        }

        [TestMethod]
        public void ComputeChecksum_CaseSensitive()
        {
            var checksum1 = MigrationBase.ComputeChecksum("CREATE TABLE Users");
            var checksum2 = MigrationBase.ComputeChecksum("create table users");

            Assert.AreNotEqual(checksum1, checksum2);
        }

        #endregion

        #region MigrationRecord Tests

        [TestMethod]
        public void MigrationRecord_PropertiesCanBeSet()
        {
            var record = new MigrationRecord
            {
                MigrationId = "20240101_Test",
                Description = "Test migration",
                AppliedAt = new System.DateTime(2024, 1, 1, 12, 0, 0),
                Checksum = "abc123"
            };

            Assert.AreEqual("20240101_Test", record.MigrationId);
            Assert.AreEqual("Test migration", record.Description);
            Assert.AreEqual(new System.DateTime(2024, 1, 1, 12, 0, 0), record.AppliedAt);
            Assert.AreEqual("abc123", record.Checksum);
        }

        #endregion

        #region Helper Methods

        private static MigrationRunner CreateRunner(DatabaseProvider providerType, MigrationOptions options = null)
        {
            var provider = DbProviderFactory.Create(providerType);
            options ??= new MigrationOptions();
            return new MigrationRunner(provider, "Server=test;Database=test;", options);
        }

        #endregion
    }
}
