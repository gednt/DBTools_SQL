using DBTools.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace DBToolsUnitTest.Caching
{
    [TestClass]
    public class CacheKeyGeneratorTests : TestBase
    {
        [TestMethod]
        public void GenerateKey_SameSqlAndParams_ProducesSameKey()
        {
            var sql = "SELECT * FROM Users WHERE Id = @param0";
            var params1 = new List<object> { 1 };
            var params2 = new List<object> { 1 };

            var key1 = CacheKeyGenerator.GenerateKey(sql, params1);
            var key2 = CacheKeyGenerator.GenerateKey(sql, params2);

            Assert.AreEqual(key1, key2);
        }

        [TestMethod]
        public void GenerateKey_DifferentSql_ProducesDifferentKey()
        {
            var params1 = new List<object> { 1 };
            var key1 = CacheKeyGenerator.GenerateKey("SELECT * FROM Users WHERE Id = @param0", params1);
            var key2 = CacheKeyGenerator.GenerateKey("SELECT * FROM Orders WHERE Id = @param0", params1);

            Assert.AreNotEqual(key1, key2);
        }

        [TestMethod]
        public void GenerateKey_DifferentParams_ProducesDifferentKey()
        {
            var sql = "SELECT * FROM Users WHERE Id = @param0";
            var params1 = new List<object> { 1 };
            var params2 = new List<object> { 2 };

            var key1 = CacheKeyGenerator.GenerateKey(sql, params1);
            var key2 = CacheKeyGenerator.GenerateKey(sql, params2);

            Assert.AreNotEqual(key1, key2);
        }

        [TestMethod]
        public void GenerateKey_NullParams_DoesNotThrow()
        {
            var sql = "SELECT * FROM Users";

            var key = CacheKeyGenerator.GenerateKey(sql, null);

            Assert.IsNotNull(key);
            Assert.IsTrue(key.Length > 0);
        }

        [TestMethod]
        public void GenerateKey_EmptyParams_ProducesConsistentKey()
        {
            var sql = "SELECT * FROM Users";
            var params1 = new List<object>();
            var params2 = new List<object>();

            var key1 = CacheKeyGenerator.GenerateKey(sql, params1);
            var key2 = CacheKeyGenerator.GenerateKey(sql, params2);

            Assert.AreEqual(key1, key2);
        }

        [TestMethod]
        public void GenerateKey_WithTableName_ProducesDifferentKeyFromWithout()
        {
            var sql = "SELECT * FROM Users";
            var parameters = new List<object> { 1 };

            var keyWithTable = CacheKeyGenerator.GenerateKey(sql, parameters, "Users");
            var keyWithoutTable = CacheKeyGenerator.GenerateKey(sql, parameters);

            Assert.AreNotEqual(keyWithTable, keyWithoutTable);
        }

        [TestMethod]
        public void GenerateKey_WithDifferentTables_ProducesDifferentKeys()
        {
            var sql = "SELECT * FROM Table1";
            var parameters = new List<object> { 1 };

            var key1 = CacheKeyGenerator.GenerateKey(sql, parameters, "Table1");
            var key2 = CacheKeyGenerator.GenerateKey(sql, parameters, "Table2");

            Assert.AreNotEqual(key1, key2);
        }

        [TestMethod]
        public void GenerateKey_WithNullTableName_DoesNotThrow()
        {
            var sql = "SELECT * FROM Users";
            var parameters = new List<object> { 1 };

            var key = CacheKeyGenerator.GenerateKey(sql, parameters, null);

            Assert.IsNotNull(key);
            Assert.IsTrue(key.Length > 0);
        }

        [TestMethod]
        public void GenerateKey_NullSql_DoesNotThrow()
        {
            var key = CacheKeyGenerator.GenerateKey(null, null);

            Assert.IsNotNull(key);
            Assert.IsTrue(key.Length > 0);
        }

        [TestMethod]
        public void GenerateKey_ProducesFixedLengthKey()
        {
            var key1 = CacheKeyGenerator.GenerateKey("short", new List<object> { 1 });
            var key2 = CacheKeyGenerator.GenerateKey("a very long sql query string that goes on and on", new List<object> { 1, 2, 3, 4, 5 });

            // SHA256 produces 64 hex characters
            Assert.AreEqual(64, key1.Length);
            Assert.AreEqual(64, key2.Length);
        }

        [TestMethod]
        public void GenerateKey_ParamsWithNullValue_HandledCorrectly()
        {
            var sql = "SELECT * FROM Users WHERE Name = @param0";
            var params1 = new List<object> { null };
            var params2 = new List<object> { null };

            var key1 = CacheKeyGenerator.GenerateKey(sql, params1);
            var key2 = CacheKeyGenerator.GenerateKey(sql, params2);

            Assert.AreEqual(key1, key2);
        }
    }
}
