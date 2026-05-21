using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Migrations;

namespace DBToolsUnitTest.Migrations
{
    [TestClass]
    public class MigrationBuilderTests : TestBase
    {
        #region Build Tests

        [TestMethod]
        public void Build_EmptyBuilder_ReturnsEmptyList()
        {
            var builder = new MigrationBuilder();
            var result = builder.Build();

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void Build_SingleMigration_ReturnsSingleItem()
        {
            var builder = new MigrationBuilder();
            builder.AddMigration("20240101_Init", "Initial", "CREATE TABLE T (Id INT)", "DROP TABLE T");

            var result = builder.Build();

            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void Build_MultipleMigrations_ReturnsCorrectCount()
        {
            var builder = new MigrationBuilder();
            builder.AddMigration("20240101_First", "First", "up1", "down1");
            builder.AddMigration("20240102_Second", "Second", "up2", "down2");
            builder.AddMigration("20240103_Third", "Third", "up3", "down3");

            var result = builder.Build();

            Assert.AreEqual(3, result.Count);
        }

        [TestMethod]
        public void Build_PreservesInsertionOrder()
        {
            var builder = new MigrationBuilder();
            builder.AddMigration("20240103_Third", "Third", "up3", "down3");
            builder.AddMigration("20240101_First", "First", "up1", "down1");
            builder.AddMigration("20240102_Second", "Second", "up2", "down2");

            var result = builder.Build();

            Assert.AreEqual("20240103_Third", result[0].MigrationId);
            Assert.AreEqual("20240101_First", result[1].MigrationId);
            Assert.AreEqual("20240102_Second", result[2].MigrationId);
        }

        #endregion

        #region AddMigration Tests

        [TestMethod]
        public void AddMigration_ReturnsSameBuilder()
        {
            var builder = new MigrationBuilder();
            var returned = builder.AddMigration("id", "desc", "up", "down");

            Assert.AreSame(builder, returned);
        }

        [TestMethod]
        public void AddMigration_FluentChaining_Works()
        {
            var result = new MigrationBuilder()
                .AddMigration("20240101_A", "A", "up1", "down1")
                .AddMigration("20240102_B", "B", "up2", "down2")
                .AddMigration("20240103_C", "C", "up3", "down3")
                .Build();

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("20240101_A", result[0].MigrationId);
            Assert.AreEqual("20240102_B", result[1].MigrationId);
            Assert.AreEqual("20240103_C", result[2].MigrationId);
        }

        [TestMethod]
        public void AddMigration_StoresCorrectDescription()
        {
            var result = new MigrationBuilder()
                .AddMigration("id1", "Create users table", "up", "down")
                .Build();

            Assert.AreEqual("Create users table", result[0].Description);
        }

        [TestMethod]
        public void AddMigration_StoresCorrectUpSql()
        {
            var upSql = "CREATE TABLE Users (Id INT PRIMARY KEY, Name VARCHAR(100))";
            var result = new MigrationBuilder()
                .AddMigration("id1", "desc", upSql, "down")
                .Build();

            Assert.AreEqual(upSql, result[0].UpSql);
        }

        [TestMethod]
        public void AddMigration_StoresCorrectDownSql()
        {
            var downSql = "DROP TABLE Users";
            var result = new MigrationBuilder()
                .AddMigration("id1", "desc", "up", downSql)
                .Build();

            Assert.AreEqual(downSql, result[0].DownSql);
        }

        #endregion

        #region Migration Properties Tests

        [TestMethod]
        public void Build_MigrationsAreSqlMigrationInstances()
        {
            var result = new MigrationBuilder()
                .AddMigration("id", "desc", "up", "down")
                .Build();

            Assert.IsInstanceOfType(result[0], typeof(SqlMigration));
        }

        [TestMethod]
        public void Build_ReturnsReadOnlyList()
        {
            var result = new MigrationBuilder()
                .AddMigration("id", "desc", "up", "down")
                .Build();

            // Verify it's a read-only list (IReadOnlyList interface)
            Assert.IsNotNull(result);
            Assert.IsTrue(result is System.Collections.Generic.IReadOnlyList<DBTools.Abstractions.IMigration>);
        }

        #endregion
    }
}
