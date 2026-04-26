using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Core;
using System;
using System.IO;

namespace DBToolsUnitTest.Core
{
    [TestClass]
    public class SqlClientConnectionTests : TestBase
    {
        [TestMethod]
        public void Constructor_WithParameters_ShouldSetProperties()
        {
            var utils = new SqlClient();
            Assert.AreEqual(TestHost, utils.Host);
            Assert.AreEqual(TestDatabase, utils.Database);
            Assert.AreEqual(TestUid, utils.Uid);
            Assert.AreEqual(TestPassword, utils.Password);
            Assert.AreEqual(TestPort, utils.Port);
        }

        [TestMethod]
        public void Constructor_Parameterless_ShouldCreateInstance()
        {
            var utils = new SqlClient();
            Assert.IsNotNull(utils);
        }

        [TestMethod]
        public void ConnectDB_ShouldSetConnectionProperties()
        {
            var utils = new SqlClient();
            utils.connectDB();
            Assert.AreEqual(TestDatabase, utils.Database);
            Assert.AreEqual(TestHost, utils.Host);
        }

        [TestMethod]
        public void Constructor_WithMissingConfigFile_ShouldThrowFileNotFoundException()
        {
            ExecuteInTempDirectory(() =>
            {
                var exception = Assert.ThrowsException<FileNotFoundException>(() => new SqlClient());
                Assert.IsTrue(exception.Message.Contains("config.json"));
                Assert.IsTrue(exception.Message.Contains("was not found"));
            });
        }

        [TestMethod]
        public void Constructor_WithMalformedConfigFile_ShouldThrowInvalidOperationException()
        {
            ExecuteInTempDirectory(() =>
            {
                var exception = Assert.ThrowsException<InvalidOperationException>(() => new SqlClient());
                Assert.IsTrue(exception.Message.Contains("Failed to load database configuration"));
            }, "{ invalid json }");
        }

        [TestMethod]
        public void Constructor_WithMissingRequiredKeys_ShouldThrowInvalidOperationException()
        {
            ExecuteInTempDirectory(() =>
            {
                var exception = Assert.ThrowsException<InvalidOperationException>(() => new SqlClient());
                Assert.IsTrue(exception.Message.Contains("required configuration keys are missing"));
                Assert.IsTrue(exception.Message.Contains("Password"));
                Assert.IsTrue(exception.Message.Contains("Port"));
            }, "{ \"Host\": \"localhost\", \"Database\": \"testDB\", \"Uid\": \"testUser\" }");
        }

        [TestMethod]
        public void Constructor_WithEmptyConfigValues_ShouldThrowInvalidOperationException()
        {
            ExecuteInTempDirectory(() =>
            {
                var exception = Assert.ThrowsException<InvalidOperationException>(() => new SqlClient());
                Assert.IsTrue(exception.Message.Contains("required configuration keys are missing or empty"));
                Assert.IsTrue(exception.Message.Contains("Host"));
                Assert.IsTrue(exception.Message.Contains("Database"));
            }, "{ \"Host\": \"\", \"Database\": \"\", \"Uid\": \"testUser\", \"Password\": \"123\", \"Port\": \"1433\" }");
        }

        [TestMethod]
        public void Constructor_WithValidConfig_ShouldLoadAllProperties()
        {
            ExecuteInTempDirectory(() =>
            {
                var utils = new SqlClient();
                Assert.AreEqual("testhost", utils.Host);
                Assert.AreEqual("testdb", utils.Database);
                Assert.AreEqual("testuid", utils.Uid);
                Assert.AreEqual("testpass", utils.Password);
                Assert.AreEqual("1234", utils.Port);
            }, "{ \"Host\": \"testhost\", \"Database\": \"testdb\", \"Uid\": \"testuid\", \"Password\": \"testpass\", \"Port\": \"1234\" }");
        }
    }
}
