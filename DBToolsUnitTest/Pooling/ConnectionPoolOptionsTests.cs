using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Configuration;
using DBTools.Pooling;
using System;

namespace DBToolsUnitTest.Pooling
{
    [TestClass]
    public class ConnectionPoolOptionsTests : TestBase
    {
        #region Default Values Tests

        [TestMethod]
        public void ConnectionPoolOptions_DefaultMinPoolSize_IsZero()
        {
            var options = new ConnectionPoolOptions();
            Assert.AreEqual(0, options.MinPoolSize);
        }

        [TestMethod]
        public void ConnectionPoolOptions_DefaultMaxPoolSize_Is100()
        {
            var options = new ConnectionPoolOptions();
            Assert.AreEqual(100, options.MaxPoolSize);
        }

        [TestMethod]
        public void ConnectionPoolOptions_DefaultConnectionLifetimeSeconds_IsZero()
        {
            var options = new ConnectionPoolOptions();
            Assert.AreEqual(0, options.ConnectionLifetimeSeconds);
        }

        [TestMethod]
        public void ConnectionPoolOptions_DefaultConnectionIdleTimeoutSeconds_Is300()
        {
            var options = new ConnectionPoolOptions();
            Assert.AreEqual(300, options.ConnectionIdleTimeoutSeconds);
        }

        [TestMethod]
        public void ConnectionPoolOptions_DefaultPooling_IsTrue()
        {
            var options = new ConnectionPoolOptions();
            Assert.IsTrue(options.Pooling);
        }

        #endregion

        #region BuildConnectionString Pool Parameters Tests

        [TestMethod]
        public void BuildConnectionString_SqlServer_IncludesPoolParameters()
        {
            var options = new DbToolsOptions
            {
                Host = "localhost",
                Database = "testdb",
                Username = "user",
                Password = "pass",
                Provider = DatabaseProvider.SqlServer
            };
            options.ConfigurePooling(p =>
            {
                p.MinPoolSize = 5;
                p.MaxPoolSize = 50;
                p.ConnectionLifetimeSeconds = 600;
                p.Pooling = true;
            });

            var connectionString = options.BuildConnectionString();

            Assert.IsTrue(connectionString.Contains("Min Pool Size=5;"));
            Assert.IsTrue(connectionString.Contains("Max Pool Size=50;"));
            Assert.IsTrue(connectionString.Contains("Connection Lifetime=600;"));
            Assert.IsTrue(connectionString.Contains("Pooling=True;"));
        }

        [TestMethod]
        public void BuildConnectionString_PostgreSQL_IncludesPoolParameters()
        {
            var options = new DbToolsOptions
            {
                Host = "localhost",
                Database = "testdb",
                Username = "user",
                Password = "pass",
                Provider = DatabaseProvider.PostgreSQL
            };
            options.ConfigurePooling(p =>
            {
                p.MinPoolSize = 2;
                p.MaxPoolSize = 20;
                p.ConnectionIdleTimeoutSeconds = 120;
                p.Pooling = true;
            });

            var connectionString = options.BuildConnectionString();

            Assert.IsTrue(connectionString.Contains("Minimum Pool Size=2;"));
            Assert.IsTrue(connectionString.Contains("Maximum Pool Size=20;"));
            Assert.IsTrue(connectionString.Contains("Connection Idle Lifetime=120;"));
            Assert.IsTrue(connectionString.Contains("Pooling=True;"));
        }

        [TestMethod]
        public void BuildConnectionString_MySQL_IncludesPoolParameters()
        {
            var options = new DbToolsOptions
            {
                Host = "localhost",
                Database = "testdb",
                Username = "user",
                Password = "pass",
                Provider = DatabaseProvider.MySQL
            };
            options.ConfigurePooling(p =>
            {
                p.MinPoolSize = 3;
                p.MaxPoolSize = 30;
                p.ConnectionLifetimeSeconds = 900;
                p.Pooling = true;
            });

            var connectionString = options.BuildConnectionString();

            Assert.IsTrue(connectionString.Contains("MinimumPoolSize=3;"));
            Assert.IsTrue(connectionString.Contains("MaximumPoolSize=30;"));
            Assert.IsTrue(connectionString.Contains("ConnectionLifeTime=900;"));
            Assert.IsTrue(connectionString.Contains("Pooling=True;"));
        }

        [TestMethod]
        public void BuildConnectionString_MySQL_PoolingDisabled_OnlyIncludesPoolingFalse()
        {
            var options = new DbToolsOptions
            {
                Host = "localhost",
                Database = "testdb",
                Username = "user",
                Password = "pass",
                Provider = DatabaseProvider.MySQL
            };
            options.ConfigurePooling(p =>
            {
                p.MinPoolSize = 3;
                p.MaxPoolSize = 30;
                p.ConnectionLifetimeSeconds = 900;
                p.Pooling = false;
            });

            var connectionString = options.BuildConnectionString();

            Assert.IsTrue(connectionString.Contains("Pooling=False;"));
            Assert.IsFalse(connectionString.Contains("MinimumPoolSize="));
            Assert.IsFalse(connectionString.Contains("MaximumPoolSize="));
            Assert.IsFalse(connectionString.Contains("ConnectionLifeTime="));
        }

        [TestMethod]
        public void BuildConnectionString_SQLite_IncludesPoolingParameter()
        {
            var options = new DbToolsOptions
            {
                Database = "test.db",
                Provider = DatabaseProvider.SQLite
            };
            options.ConfigurePooling(p =>
            {
                p.Pooling = true;
            });

            var connectionString = options.BuildConnectionString();

            Assert.IsTrue(connectionString.Contains("Pooling=True;"));
        }

        [TestMethod]
        public void BuildConnectionString_SQLite_PoolingDisabled_IncludesFalse()
        {
            var options = new DbToolsOptions
            {
                Database = "test.db",
                Provider = DatabaseProvider.SQLite
            };
            options.ConfigurePooling(p =>
            {
                p.Pooling = false;
            });

            var connectionString = options.BuildConnectionString();

            Assert.IsTrue(connectionString.Contains("Pooling=False;"));
        }

        [TestMethod]
        public void BuildConnectionString_WithExplicitConnectionString_IgnoresPoolOptions()
        {
            var options = new DbToolsOptions
            {
                ConnectionString = "Data Source=explicit;",
                Provider = DatabaseProvider.SqlServer
            };
            options.ConfigurePooling(p =>
            {
                p.MinPoolSize = 10;
                p.MaxPoolSize = 200;
            });

            var connectionString = options.BuildConnectionString();

            Assert.AreEqual("Data Source=explicit;", connectionString);
        }

        #endregion

        #region ConfigurePooling Fluent Method Tests

        [TestMethod]
        public void ConfigurePooling_ReturnsDbToolsOptionsInstance()
        {
            var options = new DbToolsOptions();

            var result = options.ConfigurePooling(p => p.MaxPoolSize = 50);

            Assert.AreSame(options, result);
        }

        [TestMethod]
        public void ConfigurePooling_ModifiesPoolOptions()
        {
            var options = new DbToolsOptions();

            options.ConfigurePooling(p =>
            {
                p.MinPoolSize = 10;
                p.MaxPoolSize = 200;
                p.ConnectionLifetimeSeconds = 3600;
                p.ConnectionIdleTimeoutSeconds = 600;
                p.Pooling = false;
            });

            Assert.AreEqual(10, options.PoolOptions.MinPoolSize);
            Assert.AreEqual(200, options.PoolOptions.MaxPoolSize);
            Assert.AreEqual(3600, options.PoolOptions.ConnectionLifetimeSeconds);
            Assert.AreEqual(600, options.PoolOptions.ConnectionIdleTimeoutSeconds);
            Assert.IsFalse(options.PoolOptions.Pooling);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConfigurePooling_NullAction_ThrowsArgumentNullException()
        {
            var options = new DbToolsOptions();
            options.ConfigurePooling(null);
        }

        [TestMethod]
        public void ConfigurePooling_CanBeChainedWithOtherMethods()
        {
            var options = new DbToolsOptions();

            var result = options
                .ConfigurePooling(p => p.MaxPoolSize = 50)
                .ConfigurePooling(p => p.MinPoolSize = 5);

            Assert.AreEqual(50, result.PoolOptions.MaxPoolSize);
            Assert.AreEqual(5, result.PoolOptions.MinPoolSize);
        }

        #endregion

        #region DbToolsOptions PoolOptions Property Tests

        [TestMethod]
        public void DbToolsOptions_PoolOptions_IsInitializedByDefault()
        {
            var options = new DbToolsOptions();
            Assert.IsNotNull(options.PoolOptions);
        }

        [TestMethod]
        public void DbToolsOptions_PoolOptions_CanBeSet()
        {
            var options = new DbToolsOptions();
            var poolOptions = new ConnectionPoolOptions
            {
                MinPoolSize = 5,
                MaxPoolSize = 25
            };

            options.PoolOptions = poolOptions;

            Assert.AreEqual(5, options.PoolOptions.MinPoolSize);
            Assert.AreEqual(25, options.PoolOptions.MaxPoolSize);
        }

        #endregion
    }
}
