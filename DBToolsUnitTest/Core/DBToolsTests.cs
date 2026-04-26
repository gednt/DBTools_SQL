using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Core;
using System;
using System.Collections.Generic;
using System.Data;

namespace DBToolsUnitTest.Core
{
    [TestClass]
    public class DBToolsTests : TestBase
    {
        [TestMethod]
        public void Constructor_ShouldSetDefaultPort()
        {
            var dbTools = new DBTools.Core.DBTools();
            Assert.AreEqual("1433", dbTools.Port);
        }

        [TestMethod]
        public void Properties_GetterSetter_ShouldWork()
        {
            var dbTools = new DBTools.Core.DBTools();
            dbTools.Host = TestHost;
            dbTools.Database = TestDatabase;
            dbTools.Uid = TestUid;
            dbTools.Password = TestPassword;
            dbTools.Query = "SELECT * FROM Users";
            dbTools.Table = "Users";
            dbTools.Port = TestPort;

            Assert.AreEqual(TestHost, dbTools.Host);
            Assert.AreEqual(TestDatabase, dbTools.Database);
            Assert.AreEqual(TestUid, dbTools.Uid);
            Assert.AreEqual(TestPassword, dbTools.Password);
            Assert.AreEqual("SELECT * FROM Users", dbTools.Query);
            Assert.AreEqual("Users", dbTools.Table);
            Assert.AreEqual(TestPort, dbTools.Port);
        }

        [TestMethod]
        public void ConnectionString_WithPort_ShouldIncludePort()
        {
            var dbTools = new DBTools.Core.DBTools
            {
                Host = TestHost,
                Database = TestDatabase,
                Uid = TestUid,
                Password = TestPassword,
                Port = TestPort
            };
            string connString = dbTools.ConnectionString;
            Assert.IsNotNull(connString);
            Assert.IsTrue(connString.Contains(TestHost));
            Assert.IsTrue(connString.Contains(TestDatabase));
            Assert.IsTrue(connString.Contains(TestUid));
            Assert.IsTrue(connString.Contains(TestPort));
        }

        [TestMethod]
        public void ConnectionString_SetCustom_ShouldReturnCustom()
        {
            var dbTools = new DBTools.Core.DBTools();
            string customConnString = "Server=myserver;Database=mydb;";
            dbTools.ConnectionString = customConnString;
            Assert.AreEqual(customConnString, dbTools.ConnectionString);
        }

        [TestMethod]
        public void LegacyGetterSetters_ShouldWork()
        {
            var dbTools = new DBTools.Core.DBTools();
            dbTools.setHost(TestHost);
            dbTools.setUid(TestUid);
            dbTools.setPassword(TestPassword);
            dbTools.setDataBase(TestDatabase);
            dbTools.setQuery("SELECT * FROM Users");

            Assert.AreEqual(TestHost, dbTools.getHost());
            Assert.AreEqual(TestUid, dbTools.getUid());
            Assert.AreEqual(TestPassword, dbTools.getPassword());
            Assert.AreEqual(TestDatabase, dbTools.getDatabase());
            Assert.AreEqual("SELECT * FROM Users", dbTools.getQuery());
        }

        [TestMethod]
        public void Error_Property_ShouldStoreError()
        {
            var dbTools = new DBTools.Core.DBTools();
            string errorMessage = "Test error message";
            dbTools.Error = errorMessage;
            Assert.AreEqual(errorMessage, dbTools.Error);
        }

        [TestMethod]
        public void SqlParameters_ShouldAcceptParameters()
        {
            var dbTools = new DBTools.Core.DBTools();
            var parameters = new List<Microsoft.Data.SqlClient.SqlParameter>
            {
                new Microsoft.Data.SqlClient.SqlParameter("@param0", 1)
            };
            dbTools.SqlParameters = parameters;
            Assert.IsNotNull(dbTools.SqlParameters);
            Assert.AreEqual(1, dbTools.SqlParameters.Count);
        }
    }
}
