using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Abstractions;
using DBTools.Providers;
using System;

namespace DBToolsUnitTest.Providers
{
    [TestClass]
    public class ProviderDialectTests : TestBase
    {
        #region QuoteIdentifier Tests

        [TestMethod]
        public void SqlServer_QuoteIdentifier_UsesBrackets()
        {
            var provider = new SqlServerProvider();
            var result = provider.QuoteIdentifier("ColumnName");
            Assert.AreEqual("[ColumnName]", result);
        }

        [TestMethod]
        public void MySQL_QuoteIdentifier_UsesBackticks()
        {
            var provider = new MySqlProvider();
            var result = provider.QuoteIdentifier("ColumnName");
            Assert.AreEqual("`ColumnName`", result);
        }

        [TestMethod]
        public void PostgreSQL_QuoteIdentifier_UsesDoubleQuotes()
        {
            var provider = new PostgresProvider();
            var result = provider.QuoteIdentifier("ColumnName");
            Assert.AreEqual("\"ColumnName\"", result);
        }

        [TestMethod]
        public void SQLite_QuoteIdentifier_UsesDoubleQuotes()
        {
            var provider = new SqliteProvider();
            var result = provider.QuoteIdentifier("ColumnName");
            Assert.AreEqual("\"ColumnName\"", result);
        }

        [TestMethod]
        public void SqlServer_QuoteIdentifier_AlreadyQuoted_ReturnsUnchanged()
        {
            var provider = new SqlServerProvider();
            var result = provider.QuoteIdentifier("[Already]");
            Assert.AreEqual("[Already]", result);
        }

        [TestMethod]
        public void MySQL_QuoteIdentifier_AlreadyQuoted_ReturnsUnchanged()
        {
            var provider = new MySqlProvider();
            var result = provider.QuoteIdentifier("`Already`");
            Assert.AreEqual("`Already`", result);
        }

        [TestMethod]
        public void PostgreSQL_QuoteIdentifier_AlreadyQuoted_ReturnsUnchanged()
        {
            var provider = new PostgresProvider();
            var result = provider.QuoteIdentifier("\"Already\"");
            Assert.AreEqual("\"Already\"", result);
        }

        [TestMethod]
        public void SQLite_QuoteIdentifier_AlreadyQuoted_ReturnsUnchanged()
        {
            var provider = new SqliteProvider();
            var result = provider.QuoteIdentifier("\"Already\"");
            Assert.AreEqual("\"Already\"", result);
        }

        [TestMethod]
        public void SqlServer_QuoteIdentifier_NullOrEmpty_ReturnsInput()
        {
            var provider = new SqlServerProvider();
            Assert.AreEqual(null, provider.QuoteIdentifier(null));
            Assert.AreEqual("", provider.QuoteIdentifier(""));
        }

        [TestMethod]
        public void MySQL_QuoteIdentifier_NullOrEmpty_ReturnsInput()
        {
            var provider = new MySqlProvider();
            Assert.AreEqual(null, provider.QuoteIdentifier(null));
            Assert.AreEqual("", provider.QuoteIdentifier(""));
        }

        #endregion

        #region BuildPagingClause Tests

        [TestMethod]
        public void SqlServer_BuildPagingClause_UsesOffsetFetch()
        {
            var provider = new SqlServerProvider();
            var result = provider.BuildPagingClause(10, 20, "Id");
            Assert.IsTrue(result.Contains("OFFSET 10 ROWS"));
            Assert.IsTrue(result.Contains("FETCH NEXT 20 ROWS ONLY"));
            Assert.IsTrue(result.Contains("ORDER BY Id"));
        }

        [TestMethod]
        public void SqlServer_BuildPagingClause_NoOrderBy_UsesSelectNull()
        {
            var provider = new SqlServerProvider();
            var result = provider.BuildPagingClause(0, 10, null);
            Assert.IsTrue(result.Contains("ORDER BY (SELECT NULL)"));
            Assert.IsTrue(result.Contains("FETCH NEXT 10 ROWS ONLY"));
        }

        [TestMethod]
        public void MySQL_BuildPagingClause_UsesLimitOffset()
        {
            var provider = new MySqlProvider();
            var result = provider.BuildPagingClause(10, 20, "Id");
            Assert.IsTrue(result.Contains("LIMIT 20"));
            Assert.IsTrue(result.Contains("OFFSET 10"));
            Assert.IsTrue(result.Contains("ORDER BY Id"));
        }

        [TestMethod]
        public void PostgreSQL_BuildPagingClause_UsesLimitOffset()
        {
            var provider = new PostgresProvider();
            var result = provider.BuildPagingClause(10, 20, "Id");
            Assert.IsTrue(result.Contains("LIMIT 20"));
            Assert.IsTrue(result.Contains("OFFSET 10"));
            Assert.IsTrue(result.Contains("ORDER BY Id"));
        }

        [TestMethod]
        public void SQLite_BuildPagingClause_UsesLimitOffset()
        {
            var provider = new SqliteProvider();
            var result = provider.BuildPagingClause(10, 20, "Id");
            Assert.IsTrue(result.Contains("LIMIT 20"));
            Assert.IsTrue(result.Contains("OFFSET 10"));
            Assert.IsTrue(result.Contains("ORDER BY Id"));
        }

        [TestMethod]
        public void SqlServer_BuildPagingClause_NoSkipNoTake_ReturnsEmpty()
        {
            var provider = new SqlServerProvider();
            var result = provider.BuildPagingClause(null, null, "Id");
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void MySQL_BuildPagingClause_OnlySkip_UsesLargeLimit()
        {
            var provider = new MySqlProvider();
            var result = provider.BuildPagingClause(5, null, "Id");
            // MySQL requires LIMIT when using OFFSET
            Assert.IsTrue(result.Contains("LIMIT"));
            Assert.IsTrue(result.Contains("OFFSET 5"));
        }

        #endregion

        #region GetLastInsertedIdSql Tests

        [TestMethod]
        public void SqlServer_GetLastInsertedIdSql_ReturnsScopeIdentity()
        {
            var provider = new SqlServerProvider();
            Assert.AreEqual("SELECT SCOPE_IDENTITY()", provider.GetLastInsertedIdSql());
        }

        [TestMethod]
        public void MySQL_GetLastInsertedIdSql_ReturnsLastInsertId()
        {
            var provider = new MySqlProvider();
            Assert.AreEqual("SELECT LAST_INSERT_ID()", provider.GetLastInsertedIdSql());
        }

        [TestMethod]
        public void SQLite_GetLastInsertedIdSql_ReturnsLastInsertRowid()
        {
            var provider = new SqliteProvider();
            Assert.AreEqual("SELECT last_insert_rowid()", provider.GetLastInsertedIdSql());
        }

        [TestMethod]
        public void PostgreSQL_GetLastInsertedIdSql_ReturnsReturningId()
        {
            var provider = new PostgresProvider();
            Assert.AreEqual("RETURNING id", provider.GetLastInsertedIdSql());
        }

        #endregion

        #region BuildUpsertSql Tests

        [TestMethod]
        public void SqlServer_BuildUpsertSql_UsesMerge()
        {
            var provider = new SqlServerProvider();
            var columns = new[] { "Id", "Name", "Email" };
            var result = provider.BuildUpsertSql("Users", columns, "Id", "@");
            Assert.IsTrue(result.Contains("MERGE INTO Users"));
            Assert.IsTrue(result.Contains("WHEN MATCHED THEN UPDATE SET"));
            Assert.IsTrue(result.Contains("WHEN NOT MATCHED THEN INSERT"));
        }

        [TestMethod]
        public void PostgreSQL_BuildUpsertSql_UsesOnConflict()
        {
            var provider = new PostgresProvider();
            var columns = new[] { "Id", "Name", "Email" };
            var result = provider.BuildUpsertSql("Users", columns, "Id", "@");
            Assert.IsTrue(result.Contains("INSERT INTO Users"));
            Assert.IsTrue(result.Contains("ON CONFLICT (Id) DO UPDATE SET"));
            Assert.IsTrue(result.Contains("EXCLUDED"));
        }

        [TestMethod]
        public void MySQL_BuildUpsertSql_UsesOnDuplicateKey()
        {
            var provider = new MySqlProvider();
            var columns = new[] { "Id", "Name", "Email" };
            var result = provider.BuildUpsertSql("Users", columns, "Id", "@");
            Assert.IsTrue(result.Contains("INSERT INTO Users"));
            Assert.IsTrue(result.Contains("ON DUPLICATE KEY UPDATE"));
            Assert.IsTrue(result.Contains("VALUES("));
        }

        [TestMethod]
        public void SQLite_BuildUpsertSql_UsesOnConflict()
        {
            var provider = new SqliteProvider();
            var columns = new[] { "Id", "Name", "Email" };
            var result = provider.BuildUpsertSql("Users", columns, "Id", "@");
            Assert.IsTrue(result.Contains("INSERT INTO Users"));
            Assert.IsTrue(result.Contains("ON CONFLICT(Id) DO UPDATE SET"));
            Assert.IsTrue(result.Contains("excluded"));
        }

        [TestMethod]
        public void SqlServer_BuildUpsertSql_CorrectParameterFormat()
        {
            var provider = new SqlServerProvider();
            var columns = new[] { "Name", "Email" };
            var result = provider.BuildUpsertSql("Users", columns, "Id", "@");
            Assert.IsTrue(result.Contains("@p0"));
            Assert.IsTrue(result.Contains("@p1"));
            Assert.IsTrue(result.Contains("@match"));
        }

        [TestMethod]
        public void PostgreSQL_BuildUpsertSql_CorrectParameterFormat()
        {
            var provider = new PostgresProvider();
            var columns = new[] { "Name", "Email" };
            var result = provider.BuildUpsertSql("Users", columns, "Id", "@");
            Assert.IsTrue(result.Contains("@p0"));
            Assert.IsTrue(result.Contains("@p1"));
        }

        #endregion

        #region ParameterPrefix Tests

        [TestMethod]
        public void SqlServer_ParameterPrefix_ReturnsAt()
        {
            var provider = new SqlServerProvider();
            Assert.AreEqual("@", provider.ParameterPrefix);
        }

        [TestMethod]
        public void MySQL_ParameterPrefix_ReturnsAt()
        {
            var provider = new MySqlProvider();
            Assert.AreEqual("@", provider.ParameterPrefix);
        }

        [TestMethod]
        public void PostgreSQL_ParameterPrefix_ReturnsAt()
        {
            var provider = new PostgresProvider();
            Assert.AreEqual("@", provider.ParameterPrefix);
        }

        [TestMethod]
        public void SQLite_ParameterPrefix_ReturnsAt()
        {
            var provider = new SqliteProvider();
            Assert.AreEqual("@", provider.ParameterPrefix);
        }

        #endregion

        #region ProviderName Tests

        [TestMethod]
        public void SqlServer_ProviderName_ReturnsSqlServer()
        {
            var provider = new SqlServerProvider();
            Assert.AreEqual("SqlServer", provider.ProviderName);
        }

        [TestMethod]
        public void MySQL_ProviderName_ReturnsMySQL()
        {
            var provider = new MySqlProvider();
            Assert.AreEqual("MySQL", provider.ProviderName);
        }

        [TestMethod]
        public void PostgreSQL_ProviderName_ReturnsPostgreSQL()
        {
            var provider = new PostgresProvider();
            Assert.AreEqual("PostgreSQL", provider.ProviderName);
        }

        [TestMethod]
        public void SQLite_ProviderName_ReturnsSQLite()
        {
            var provider = new SqliteProvider();
            Assert.AreEqual("SQLite", provider.ProviderName);
        }

        #endregion

        #region SupportsMerge Tests

        [TestMethod]
        public void SqlServer_SupportsMerge_ReturnsTrue()
        {
            var provider = new SqlServerProvider();
            Assert.IsTrue(provider.SupportsMerge);
        }

        [TestMethod]
        public void MySQL_SupportsMerge_ReturnsTrue()
        {
            var provider = new MySqlProvider();
            Assert.IsTrue(provider.SupportsMerge);
        }

        [TestMethod]
        public void PostgreSQL_SupportsMerge_ReturnsTrue()
        {
            var provider = new PostgresProvider();
            Assert.IsTrue(provider.SupportsMerge);
        }

        [TestMethod]
        public void SQLite_SupportsMerge_ReturnsTrue()
        {
            var provider = new SqliteProvider();
            Assert.IsTrue(provider.SupportsMerge);
        }

        #endregion

        #region CreateConnection Tests

        [TestMethod]
        public void SqlServer_CreateConnection_ReturnsSqlConnection()
        {
            var provider = new SqlServerProvider();
            var connection = provider.CreateConnection("Data Source=localhost;Initial Catalog=test;User ID=sa;Password=pass;");
            Assert.IsNotNull(connection);
            Assert.AreEqual("Microsoft.Data.SqlClient.SqlConnection", connection.GetType().FullName);
            connection.Dispose();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void PostgreSQL_CreateConnection_ThrowsWhenPackageNotLoaded()
        {
            var provider = new PostgresProvider();
            provider.CreateConnection("Host=localhost;Database=test;");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void MySQL_CreateConnection_ThrowsWhenPackageNotLoaded()
        {
            var provider = new MySqlProvider();
            provider.CreateConnection("Server=localhost;Database=test;");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SQLite_CreateConnection_ThrowsWhenPackageNotLoaded()
        {
            var provider = new SqliteProvider();
            provider.CreateConnection("Data Source=:memory:");
        }

        #endregion
    }
}
