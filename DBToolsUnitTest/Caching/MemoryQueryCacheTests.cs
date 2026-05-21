using DBTools.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace DBToolsUnitTest.Caching
{
    [TestClass]
    public class MemoryQueryCacheTests : TestBase
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

        [TestMethod]
        public void Get_ReturnsNull_WhenKeyNotFound()
        {
            var cache = new MemoryQueryCache(CreateDefaultOptions());

            var result = cache.Get("nonexistent");

            Assert.IsNull(result);
        }

        [TestMethod]
        public void Set_And_Get_ReturnsStoredValue()
        {
            var cache = new MemoryQueryCache(CreateDefaultOptions());
            var testValue = "test result";

            cache.Set("key1", testValue);
            var entry = cache.Get("key1");

            Assert.IsNotNull(entry);
            Assert.AreEqual(testValue, entry.Value);
        }

        [TestMethod]
        public void Get_IncrementsHitCount()
        {
            var cache = new MemoryQueryCache(CreateDefaultOptions());
            cache.Set("key1", "value1");

            cache.Get("key1");
            cache.Get("key1");
            var entry = cache.Get("key1");

            Assert.IsNotNull(entry);
            Assert.AreEqual(3, entry.HitCount);
        }

        [TestMethod]
        public void Get_ReturnsNull_WhenEntryExpired()
        {
            var options = CreateDefaultOptions();
            options.DefaultExpiration = TimeSpan.FromMilliseconds(50);
            var cache = new MemoryQueryCache(options);

            cache.Set("key1", "value1");
            Thread.Sleep(100);

            var result = cache.Get("key1");

            Assert.IsNull(result);
        }

        [TestMethod]
        public void Set_WithCustomExpiration_OverridesDefault()
        {
            var options = CreateDefaultOptions();
            options.DefaultExpiration = TimeSpan.FromMilliseconds(50);
            var cache = new MemoryQueryCache(options);

            cache.Set("key1", "value1", TimeSpan.FromMinutes(10));
            Thread.Sleep(100);

            var result = cache.Get("key1");

            Assert.IsNotNull(result);
            Assert.AreEqual("value1", result.Value);
        }

        [TestMethod]
        public void Invalidate_RemovesEntry()
        {
            var cache = new MemoryQueryCache(CreateDefaultOptions());
            cache.Set("key1", "value1");

            cache.Invalidate("key1");

            var result = cache.Get("key1");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void InvalidateByTable_RemovesAllAssociatedEntries()
        {
            var cache = new MemoryQueryCache(CreateDefaultOptions());
            cache.Set("key1", "value1", "Users");
            cache.Set("key2", "value2", "Users");
            cache.Set("key3", "value3", "Orders");

            cache.InvalidateByTable("Users");

            Assert.IsNull(cache.Get("key1"));
            Assert.IsNull(cache.Get("key2"));
            Assert.IsNotNull(cache.Get("key3"));
        }

        [TestMethod]
        public void InvalidateByTable_IsCaseInsensitive()
        {
            var cache = new MemoryQueryCache(CreateDefaultOptions());
            cache.Set("key1", "value1", "Users");

            cache.InvalidateByTable("users");

            Assert.IsNull(cache.Get("key1"));
        }

        [TestMethod]
        public void Clear_RemovesAllEntries()
        {
            var cache = new MemoryQueryCache(CreateDefaultOptions());
            cache.Set("key1", "value1");
            cache.Set("key2", "value2");

            cache.Clear();

            Assert.IsNull(cache.Get("key1"));
            Assert.IsNull(cache.Get("key2"));
        }

        [TestMethod]
        public void MaxCacheSize_EvictsOldestEntry()
        {
            var options = CreateDefaultOptions();
            options.MaxCacheSize = 3;
            var cache = new MemoryQueryCache(options);

            cache.Set("key1", "value1");
            Thread.Sleep(10);
            cache.Set("key2", "value2");
            Thread.Sleep(10);
            cache.Set("key3", "value3");
            Thread.Sleep(10);

            // This should evict key1 (oldest accessed)
            cache.Set("key4", "value4");

            Assert.IsNull(cache.Get("key1"));
            Assert.IsNotNull(cache.Get("key2"));
            Assert.IsNotNull(cache.Get("key3"));
            Assert.IsNotNull(cache.Get("key4"));
        }

        [TestMethod]
        public void GetStatistics_TracksTotalEntries()
        {
            var cache = new MemoryQueryCache(CreateDefaultOptions());
            cache.Set("key1", "value1");
            cache.Set("key2", "value2");

            var stats = cache.GetStatistics();

            Assert.AreEqual(2, stats.TotalEntries);
        }

        [TestMethod]
        public void GetStatistics_TracksHitsAndMisses()
        {
            var cache = new MemoryQueryCache(CreateDefaultOptions());
            cache.Set("key1", "value1");

            cache.Get("key1"); // hit
            cache.Get("key1"); // hit
            cache.Get("nonexistent"); // miss

            var stats = cache.GetStatistics();

            Assert.AreEqual(2, stats.HitCount);
            Assert.AreEqual(1, stats.MissCount);
        }

        [TestMethod]
        public void GetStatistics_TracksEvictions()
        {
            var options = CreateDefaultOptions();
            options.MaxCacheSize = 2;
            var cache = new MemoryQueryCache(options);

            cache.Set("key1", "value1");
            cache.Set("key2", "value2");
            cache.Set("key3", "value3"); // evicts one

            var stats = cache.GetStatistics();

            Assert.AreEqual(1, stats.EvictionCount);
        }

        [TestMethod]
        public void GetStatistics_HitRate_ReturnsCorrectRatio()
        {
            var cache = new MemoryQueryCache(CreateDefaultOptions());
            cache.Set("key1", "value1");

            cache.Get("key1"); // hit
            cache.Get("key1"); // hit
            cache.Get("nonexistent"); // miss
            cache.Get("nonexistent2"); // miss

            var stats = cache.GetStatistics();

            Assert.AreEqual(0.5, stats.HitRate, 0.001);
        }

        [TestMethod]
        public void GetStatistics_HitRate_ReturnsZero_WhenNoRequests()
        {
            var cache = new MemoryQueryCache(CreateDefaultOptions());

            var stats = cache.GetStatistics();

            Assert.AreEqual(0.0, stats.HitRate, 0.001);
        }

        [TestMethod]
        public void Set_WithNullKey_DoesNotThrow()
        {
            var cache = new MemoryQueryCache(CreateDefaultOptions());

            cache.Set(null, "value1");

            var stats = cache.GetStatistics();
            Assert.AreEqual(0, stats.TotalEntries);
        }

        [TestMethod]
        public void Get_WithNullKey_ReturnsNull()
        {
            var cache = new MemoryQueryCache(CreateDefaultOptions());

            var result = cache.Get(null);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void Set_OverwritesExistingEntry()
        {
            var cache = new MemoryQueryCache(CreateDefaultOptions());
            cache.Set("key1", "value1");
            cache.Set("key1", "value2");

            var entry = cache.Get("key1");

            Assert.IsNotNull(entry);
            Assert.AreEqual("value2", entry.Value);
        }
    }
}
