using DBTools.Abstractions;
using DBTools.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;

namespace DBToolsUnitTest.Caching
{
    [TestClass]
    public class CachingInterceptorTests : TestBase
    {
        private QueryCacheOptions CreateDefaultOptions()
        {
            return new QueryCacheOptions
            {
                Enabled = true,
                DefaultExpiration = TimeSpan.FromMinutes(5),
                MaxCacheSize = 1000,
                EnableAutoInvalidation = true
            };
        }

        private MemoryQueryCache CreateCache()
        {
            return new MemoryQueryCache(CreateDefaultOptions());
        }

        [TestMethod]
        public void BeforeExecute_CacheHit_SetsIsSuppressedAndCachedResult()
        {
            var options = CreateDefaultOptions();
            var cache = CreateCache();
            var interceptor = new CachingInterceptor(cache, options);

            // Pre-populate cache with a DataTable
            var cachedTable = new DataTable();
            cachedTable.Columns.Add("Id", typeof(int));
            cachedTable.Rows.Add(1);

            var sql = "SELECT * FROM Users WHERE Id = @param0";
            var parameters = new List<object> { 1 };
            var cacheKey = CacheKeyGenerator.GenerateKey(sql, parameters, "Users");
            cache.Set(cacheKey, cachedTable, "Users");

            var context = new QueryInterceptionContext
            {
                Sql = sql,
                Parameters = parameters,
                OperationType = QueryOperationType.Select,
                TableName = "Users"
            };

            interceptor.BeforeExecute(context);

            Assert.IsTrue(context.IsSuppressed);
            Assert.IsTrue(context.Properties.ContainsKey("CachedResult"));
            Assert.AreSame(cachedTable, context.Properties["CachedResult"]);
        }

        [TestMethod]
        public void BeforeExecute_CacheMiss_DoesNotSetIsSuppressed()
        {
            var options = CreateDefaultOptions();
            var cache = CreateCache();
            var interceptor = new CachingInterceptor(cache, options);

            var context = new QueryInterceptionContext
            {
                Sql = "SELECT * FROM Users WHERE Id = @param0",
                Parameters = new List<object> { 1 },
                OperationType = QueryOperationType.Select,
                TableName = "Users"
            };

            interceptor.BeforeExecute(context);

            Assert.IsFalse(context.IsSuppressed);
            Assert.IsFalse(context.Properties.ContainsKey("CachedResult"));
        }

        [TestMethod]
        public void BeforeExecute_NonSelectQuery_DoesNotCheckCache()
        {
            var options = CreateDefaultOptions();
            var cache = CreateCache();
            var interceptor = new CachingInterceptor(cache, options);

            var context = new QueryInterceptionContext
            {
                Sql = "INSERT INTO Users (Name) VALUES (@param0)",
                Parameters = new List<object> { "John" },
                OperationType = QueryOperationType.Insert,
                TableName = "Users"
            };

            interceptor.BeforeExecute(context);

            Assert.IsFalse(context.IsSuppressed);
        }

        [TestMethod]
        public void AfterExecute_SelectWithResult_StoresInCache()
        {
            var options = CreateDefaultOptions();
            var cache = CreateCache();
            var interceptor = new CachingInterceptor(cache, options);

            var resultTable = new DataTable();
            resultTable.Columns.Add("Id", typeof(int));
            resultTable.Rows.Add(1);

            var sql = "SELECT * FROM Users WHERE Id = @param0";
            var parameters = new List<object> { 1 };

            var context = new QueryInterceptionContext
            {
                Sql = sql,
                Parameters = parameters,
                OperationType = QueryOperationType.Select,
                TableName = "Users"
            };

            // Simulate BeforeExecute (cache miss - stores the key)
            interceptor.BeforeExecute(context);

            // Simulate the client storing the result
            context.Properties["QueryResult"] = resultTable;

            // AfterExecute should cache the result
            interceptor.AfterExecute(context);

            // Now verify the result is cached
            var cacheKey = CacheKeyGenerator.GenerateKey(sql, parameters, "Users");
            var entry = cache.Get(cacheKey);

            Assert.IsNotNull(entry);
            Assert.AreSame(resultTable, entry.Value);
        }

        [TestMethod]
        public void AfterExecute_InsertOperation_InvalidatesTableCache()
        {
            var options = CreateDefaultOptions();
            var cache = CreateCache();
            var interceptor = new CachingInterceptor(cache, options);

            // Pre-populate cache
            var cacheKey = CacheKeyGenerator.GenerateKey("SELECT * FROM Users", new List<object>(), "Users");
            cache.Set(cacheKey, new DataTable(), "Users");

            var context = new QueryInterceptionContext
            {
                Sql = "INSERT INTO Users (Name) VALUES (@param0)",
                Parameters = new List<object> { "John" },
                OperationType = QueryOperationType.Insert,
                TableName = "Users"
            };

            interceptor.AfterExecute(context);

            // Cache should be invalidated for Users table
            var entry = cache.Get(cacheKey);
            Assert.IsNull(entry);
        }

        [TestMethod]
        public void AfterExecute_UpdateOperation_InvalidatesTableCache()
        {
            var options = CreateDefaultOptions();
            var cache = CreateCache();
            var interceptor = new CachingInterceptor(cache, options);

            // Pre-populate cache
            var cacheKey = CacheKeyGenerator.GenerateKey("SELECT * FROM Users", new List<object>(), "Users");
            cache.Set(cacheKey, new DataTable(), "Users");

            var context = new QueryInterceptionContext
            {
                Sql = "UPDATE Users SET Name = @param0 WHERE Id = @param1",
                Parameters = new List<object> { "John", 1 },
                OperationType = QueryOperationType.Update,
                TableName = "Users"
            };

            interceptor.AfterExecute(context);

            var entry = cache.Get(cacheKey);
            Assert.IsNull(entry);
        }

        [TestMethod]
        public void AfterExecute_DeleteOperation_InvalidatesTableCache()
        {
            var options = CreateDefaultOptions();
            var cache = CreateCache();
            var interceptor = new CachingInterceptor(cache, options);

            // Pre-populate cache
            var cacheKey = CacheKeyGenerator.GenerateKey("SELECT * FROM Users", new List<object>(), "Users");
            cache.Set(cacheKey, new DataTable(), "Users");

            var context = new QueryInterceptionContext
            {
                Sql = "DELETE FROM Users WHERE Id = @param0",
                Parameters = new List<object> { 1 },
                OperationType = QueryOperationType.Delete,
                TableName = "Users"
            };

            interceptor.AfterExecute(context);

            var entry = cache.Get(cacheKey);
            Assert.IsNull(entry);
        }

        [TestMethod]
        public void AfterExecute_WriteOperation_DoesNotInvalidate_WhenAutoInvalidationDisabled()
        {
            var options = CreateDefaultOptions();
            options.EnableAutoInvalidation = false;
            var cache = new MemoryQueryCache(options);
            var interceptor = new CachingInterceptor(cache, options);

            // Pre-populate cache
            var cacheKey = CacheKeyGenerator.GenerateKey("SELECT * FROM Users", new List<object>(), "Users");
            cache.Set(cacheKey, new DataTable(), "Users");

            var context = new QueryInterceptionContext
            {
                Sql = "INSERT INTO Users (Name) VALUES (@param0)",
                Parameters = new List<object> { "John" },
                OperationType = QueryOperationType.Insert,
                TableName = "Users"
            };

            interceptor.AfterExecute(context);

            // Cache should NOT be invalidated
            var entry = cache.Get(cacheKey);
            Assert.IsNotNull(entry);
        }

        [TestMethod]
        public void AfterExecute_WriteOperation_NoTableName_DoesNotThrow()
        {
            var options = CreateDefaultOptions();
            var cache = CreateCache();
            var interceptor = new CachingInterceptor(cache, options);

            var context = new QueryInterceptionContext
            {
                Sql = "INSERT INTO Users (Name) VALUES (@param0)",
                Parameters = new List<object> { "John" },
                OperationType = QueryOperationType.Insert,
                TableName = null
            };

            // Should not throw
            interceptor.AfterExecute(context);
        }

        [TestMethod]
        public void OnError_DoesNotThrow()
        {
            var options = CreateDefaultOptions();
            var cache = CreateCache();
            var interceptor = new CachingInterceptor(cache, options);

            var context = new QueryInterceptionContext
            {
                Sql = "SELECT * FROM Users",
                Parameters = new List<object>(),
                OperationType = QueryOperationType.Select,
                TableName = "Users"
            };

            // Should not throw
            interceptor.OnError(context, new Exception("Test error"));
        }

        [TestMethod]
        public void BeforeExecute_CacheHit_StoresCacheKeyInProperties()
        {
            var options = CreateDefaultOptions();
            var cache = CreateCache();
            var interceptor = new CachingInterceptor(cache, options);

            var sql = "SELECT * FROM Users";
            var parameters = new List<object>();
            var cacheKey = CacheKeyGenerator.GenerateKey(sql, parameters, "Users");
            cache.Set(cacheKey, new DataTable(), "Users");

            var context = new QueryInterceptionContext
            {
                Sql = sql,
                Parameters = parameters,
                OperationType = QueryOperationType.Select,
                TableName = "Users"
            };

            interceptor.BeforeExecute(context);

            Assert.IsTrue(context.Properties.ContainsKey("CacheKey"));
            Assert.AreEqual(cacheKey, context.Properties["CacheKey"]);
        }
    }
}
