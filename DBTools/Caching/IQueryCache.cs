using System;

namespace DBTools.Caching
{
    /// <summary>
    /// Interface for query result caching.
    /// Provides methods for storing, retrieving, and invalidating cached query results.
    /// </summary>
    public interface IQueryCache
    {
        /// <summary>
        /// Gets a cached entry by key. Returns null if not found or expired.
        /// </summary>
        /// <param name="cacheKey">The cache key to look up.</param>
        /// <returns>The cache entry if found and not expired; otherwise null.</returns>
        CacheEntry Get(string cacheKey);

        /// <summary>
        /// Stores a result in the cache with an optional expiration.
        /// </summary>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="result">The result object to cache.</param>
        /// <param name="expiration">Optional expiration override. Uses default if null.</param>
        void Set(string cacheKey, object result, TimeSpan? expiration = null);

        /// <summary>
        /// Invalidates (removes) a specific cache entry by key.
        /// </summary>
        /// <param name="cacheKey">The cache key to invalidate.</param>
        void Invalidate(string cacheKey);

        /// <summary>
        /// Invalidates all cache entries associated with a specific table.
        /// </summary>
        /// <param name="tableName">The table name whose cache entries should be removed.</param>
        void InvalidateByTable(string tableName);

        /// <summary>
        /// Clears all entries from the cache.
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets current cache statistics including hit rate, miss count, and eviction count.
        /// </summary>
        /// <returns>A snapshot of the current cache statistics.</returns>
        CacheStatistics GetStatistics();
    }
}
