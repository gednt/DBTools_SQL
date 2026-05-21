using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Migrations;
using System;

namespace DBToolsUnitTest.Migrations
{
    [TestClass]
    public class SqlMigrationTests : TestBase
    {
        #region Constructor Tests

        [TestMethod]
        public void Constructor_SetsAllProperties()
        {
            var migration = new SqlMigration(
                "20240101120000_CreateUsersTable",
                "Creates the Users table",
                "CREATE TABLE Users (Id INT PRIMARY KEY)",
                "DROP TABLE Users");

            Assert.AreEqual("20240101120000_CreateUsersTable", migration.MigrationId);
            Assert.AreEqual("Creates the Users table", migration.Description);
            Assert.AreEqual("CREATE TABLE Users (Id INT PRIMARY KEY)", migration.UpSql);
            Assert.AreEqual("DROP TABLE Users", migration.DownSql);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullMigrationId_Throws()
        {
            new SqlMigration(null, "desc", "up", "down");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullDescription_Throws()
        {
            new SqlMigration("id", null, "up", "down");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullUpSql_Throws()
        {
            new SqlMigration("id", "desc", null, "down");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullDownSql_Throws()
        {
            new SqlMigration("id", "desc", "up", null);
        }

        #endregion

        #region Property Access Tests

        [TestMethod]
        public void MigrationId_ReturnsCorrectValue()
        {
            var migration = new SqlMigration("20240615_AddIndex", "Add index", "CREATE INDEX", "DROP INDEX");
            Assert.AreEqual("20240615_AddIndex", migration.MigrationId);
        }

        [TestMethod]
        public void Description_ReturnsCorrectValue()
        {
            var migration = new SqlMigration("id", "My migration description", "up", "down");
            Assert.AreEqual("My migration description", migration.Description);
        }

        [TestMethod]
        public void UpSql_ReturnsCorrectValue()
        {
            var upSql = "ALTER TABLE Users ADD COLUMN Email VARCHAR(255)";
            var migration = new SqlMigration("id", "desc", upSql, "down");
            Assert.AreEqual(upSql, migration.UpSql);
        }

        [TestMethod]
        public void DownSql_ReturnsCorrectValue()
        {
            var downSql = "ALTER TABLE Users DROP COLUMN Email";
            var migration = new SqlMigration("id", "desc", "up", downSql);
            Assert.AreEqual(downSql, migration.DownSql);
        }

        [TestMethod]
        public void EmptyStrings_AreAllowed()
        {
            var migration = new SqlMigration("id", "desc", "", "");
            Assert.AreEqual("", migration.UpSql);
            Assert.AreEqual("", migration.DownSql);
        }

        #endregion

        #region Checksum Tests

        [TestMethod]
        public void ComputeChecksum_SameSql_ProducesSameChecksum()
        {
            var sql = "CREATE TABLE Users (Id INT PRIMARY KEY)";
            var checksum1 = MigrationBase.ComputeChecksum(sql);
            var checksum2 = MigrationBase.ComputeChecksum(sql);

            Assert.AreEqual(checksum1, checksum2);
        }

        [TestMethod]
        public void ComputeChecksum_DifferentSql_ProducesDifferentChecksum()
        {
            var checksum1 = MigrationBase.ComputeChecksum("CREATE TABLE Users (Id INT)");
            var checksum2 = MigrationBase.ComputeChecksum("CREATE TABLE Orders (Id INT)");

            Assert.AreNotEqual(checksum1, checksum2);
        }

        [TestMethod]
        public void ComputeChecksum_ProducesFixedLengthHash()
        {
            var checksum = MigrationBase.ComputeChecksum("SELECT 1");
            // SHA256 produces 64 hex characters
            Assert.AreEqual(64, checksum.Length);
        }

        [TestMethod]
        public void ComputeChecksum_NullSql_DoesNotThrow()
        {
            var checksum = MigrationBase.ComputeChecksum(null);
            Assert.IsNotNull(checksum);
            Assert.AreEqual(64, checksum.Length);
        }

        [TestMethod]
        public void ComputeChecksum_EmptySql_DoesNotThrow()
        {
            var checksum = MigrationBase.ComputeChecksum("");
            Assert.IsNotNull(checksum);
            Assert.AreEqual(64, checksum.Length);
        }

        [TestMethod]
        public void ComputeChecksum_NullAndEmpty_ProduceSameResult()
        {
            var checksumNull = MigrationBase.ComputeChecksum(null);
            var checksumEmpty = MigrationBase.ComputeChecksum("");

            Assert.AreEqual(checksumNull, checksumEmpty);
        }

        #endregion
    }
}
