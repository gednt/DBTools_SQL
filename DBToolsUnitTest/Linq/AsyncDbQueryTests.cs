using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Abstractions;
using DBTools.Providers;
using System;

namespace DBToolsUnitTest.Linq
{
    [TestClass]
    public class AsyncDbQueryTests : TestBase
    {
        [TestMethod]
        public void SqliteProvider_BuildPagingClause_WithSkipAndTake_ReturnsLimitOffset()
        {
            var provider = new SqliteProvider();
            var result = provider.BuildPagingClause(10, 20, "name ASC");
            Assert.IsTrue(result.Contains("ORDER BY name ASC"), $"Expected ORDER BY but got: {result}");
            Assert.IsTrue(result.Contains("LIMIT 20"), $"Expected LIMIT but got: {result}");
            Assert.IsTrue(result.Contains("OFFSET 10"), $"Expected OFFSET but got: {result}");
            Assert.IsFalse(result.Contains("ROWS FETCH"), $"Should not contain SQL Server syntax: {result}");
        }

        [TestMethod]
        public void SqlServerProvider_BuildPagingClause_WithSkipAndTake_ReturnsOffsetFetch()
        {
            var provider = new SqlServerProvider();
            var result = provider.BuildPagingClause(10, 20, "name ASC");
            Assert.IsTrue(result.Contains("ORDER BY name ASC"), $"Expected ORDER BY but got: {result}");
            Assert.IsTrue(result.Contains("OFFSET 10 ROWS"), $"Expected OFFSET but got: {result}");
            Assert.IsTrue(result.Contains("FETCH NEXT 20 ROWS ONLY"), $"Expected FETCH but got: {result}");
            Assert.IsFalse(result.Contains("LIMIT"), $"Should not contain LIMIT: {result}");
        }

        [TestMethod]
        public void PostgresProvider_BuildPagingClause_WithSkipAndTake_ReturnsLimitOffset()
        {
            var provider = new PostgresProvider();
            var result = provider.BuildPagingClause(10, 20, "name ASC");
            Assert.IsTrue(result.Contains("ORDER BY name ASC"), $"Expected ORDER BY but got: {result}");
            Assert.IsTrue(result.Contains("LIMIT 20"), $"Expected LIMIT but got: {result}");
            Assert.IsTrue(result.Contains("OFFSET 10"), $"Expected OFFSET but got: {result}");
            Assert.IsFalse(result.Contains("ROWS FETCH"), $"Should not contain SQL Server syntax: {result}");
        }

        [TestMethod]
        public void MySqlProvider_BuildPagingClause_WithSkipAndTake_ReturnsLimitOffset()
        {
            var provider = new MySqlProvider();
            var result = provider.BuildPagingClause(10, 20, "name ASC");
            Assert.IsTrue(result.Contains("ORDER BY name ASC"), $"Expected ORDER BY but got: {result}");
            Assert.IsTrue(result.Contains("LIMIT 20"), $"Expected LIMIT but got: {result}");
            Assert.IsTrue(result.Contains("OFFSET 10"), $"Expected OFFSET but got: {result}");
            Assert.IsFalse(result.Contains("ROWS FETCH"), $"Should not contain SQL Server syntax: {result}");
        }

        [TestMethod]
        public void SqliteProvider_BuildPagingClause_WithOnlyTake_ReturnsLimitNoOffset()
        {
            var provider = new SqliteProvider();
            var result = provider.BuildPagingClause(null, 10, "id ASC");
            Assert.IsTrue(result.Contains("ORDER BY id ASC"), $"Expected ORDER BY but got: {result}");
            Assert.IsTrue(result.Contains("LIMIT 10"), $"Expected LIMIT but got: {result}");
            Assert.IsFalse(result.Contains("OFFSET"), $"Should not contain OFFSET when skip is null: {result}");
        }

        [TestMethod]
        public void SqliteProvider_BuildPagingClause_WithOnlySkip_ReturnsLargeLimitWithOffset()
        {
            var provider = new SqliteProvider();
            var result = provider.BuildPagingClause(5, null, "id ASC");
            Assert.IsTrue(result.Contains("ORDER BY id ASC"), $"Expected ORDER BY but got: {result}");
            Assert.IsTrue(result.Contains("OFFSET 5"), $"Expected OFFSET but got: {result}");
        }

        [TestMethod]
        public void SqlServerProvider_BuildPagingClause_WithOnlyTake_IncludesOffsetZeroAndFetch()
        {
            var provider = new SqlServerProvider();
            var result = provider.BuildPagingClause(null, 10, "id ASC");
            Assert.IsTrue(result.Contains("ORDER BY id ASC"), $"Expected ORDER BY but got: {result}");
            Assert.IsTrue(result.Contains("OFFSET 0 ROWS"), $"Expected OFFSET 0 but got: {result}");
            Assert.IsTrue(result.Contains("FETCH NEXT 10 ROWS ONLY"), $"Expected FETCH but got: {result}");
        }
    }
}
