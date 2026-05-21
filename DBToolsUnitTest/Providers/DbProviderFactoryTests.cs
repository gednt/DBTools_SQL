using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Configuration;
using DBTools.Providers;
using System;

namespace DBToolsUnitTest.Providers
{
    [TestClass]
    public class DbProviderFactoryTests : TestBase
    {
        #region Create from DatabaseProvider enum

        [TestMethod]
        public void Create_SqlServer_ReturnsSqlServerProvider()
        {
            var provider = DbProviderFactory.Create(DatabaseProvider.SqlServer);
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(SqlServerProvider));
        }

        [TestMethod]
        public void Create_PostgreSQL_ReturnsPostgresProvider()
        {
            var provider = DbProviderFactory.Create(DatabaseProvider.PostgreSQL);
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(PostgresProvider));
        }

        [TestMethod]
        public void Create_MySQL_ReturnsMySqlProvider()
        {
            var provider = DbProviderFactory.Create(DatabaseProvider.MySQL);
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(MySqlProvider));
        }

        [TestMethod]
        public void Create_SQLite_ReturnsSqliteProvider()
        {
            var provider = DbProviderFactory.Create(DatabaseProvider.SQLite);
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(SqliteProvider));
        }

        #endregion

        #region Create from string

        [TestMethod]
        public void Create_FromString_SqlServer_ReturnsSqlServerProvider()
        {
            var provider = DbProviderFactory.Create("SqlServer");
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(SqlServerProvider));
        }

        [TestMethod]
        public void Create_FromString_CaseInsensitive_Works()
        {
            var provider1 = DbProviderFactory.Create("SQLSERVER");
            Assert.IsInstanceOfType(provider1, typeof(SqlServerProvider));

            var provider2 = DbProviderFactory.Create("postgresql");
            Assert.IsInstanceOfType(provider2, typeof(PostgresProvider));

            var provider3 = DbProviderFactory.Create("MYSQL");
            Assert.IsInstanceOfType(provider3, typeof(MySqlProvider));

            var provider4 = DbProviderFactory.Create("SQLite");
            Assert.IsInstanceOfType(provider4, typeof(SqliteProvider));
        }

        [TestMethod]
        public void Create_FromString_Postgres_Alias_Works()
        {
            var provider = DbProviderFactory.Create("postgres");
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(PostgresProvider));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Create_FromString_Invalid_ThrowsArgumentException()
        {
            DbProviderFactory.Create("Oracle");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Create_FromString_NullOrEmpty_ThrowsArgumentException()
        {
            DbProviderFactory.Create((string)null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Create_FromString_EmptyString_ThrowsArgumentException()
        {
            DbProviderFactory.Create("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Create_FromString_Whitespace_ThrowsArgumentException()
        {
            DbProviderFactory.Create("   ");
        }

        #endregion

        #region Create from string - all providers

        [TestMethod]
        public void Create_FromString_PostgreSQL_ReturnsPostgresProvider()
        {
            var provider = DbProviderFactory.Create("PostgreSQL");
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(PostgresProvider));
        }

        [TestMethod]
        public void Create_FromString_MySQL_ReturnsMySqlProvider()
        {
            var provider = DbProviderFactory.Create("MySQL");
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(MySqlProvider));
        }

        [TestMethod]
        public void Create_FromString_SQLite_ReturnsSqliteProvider()
        {
            var provider = DbProviderFactory.Create("SQLite");
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(SqliteProvider));
        }

        #endregion

        #region Verify factory returns correct provider names

        [TestMethod]
        public void Create_AllProviders_HaveCorrectProviderName()
        {
            Assert.AreEqual("SqlServer", DbProviderFactory.Create(DatabaseProvider.SqlServer).ProviderName);
            Assert.AreEqual("PostgreSQL", DbProviderFactory.Create(DatabaseProvider.PostgreSQL).ProviderName);
            Assert.AreEqual("MySQL", DbProviderFactory.Create(DatabaseProvider.MySQL).ProviderName);
            Assert.AreEqual("SQLite", DbProviderFactory.Create(DatabaseProvider.SQLite).ProviderName);
        }

        #endregion
    }
}
