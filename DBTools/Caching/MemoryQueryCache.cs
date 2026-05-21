using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DBTools.Caching
{
    /// <summary>
    /// In-memory implementation of IQueryCache using ConcurrentDictionary for thread safety.
    /// Supports configurable expiration, LRU eviction, and table-based invalidation.
    /// </summary>
    public class MemoryQueryCache : IQueryCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new ConcurrentDictionary<string, CacheEntry>();
        private readonly ConcurrentDictionary<string, HashSet<string>> _tableKeyMap = new ConcurrentDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly object _tableMapLock = new object();
        private readonly QueryCacheOptions _options;

        private long _hitCount;
        private long _missCount;
        private long _evictionCount;

        /// <summary>
        /// Creates a new MemoryQueryCache with the specified options.
        /// </summary>
        /// <param name="options">Cache configuration options.</param>
        public MemoryQueryCache(QueryCacheOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc/>
        public CacheEntry Get(string cacheKey)
        {
            if (string.IsNullOrEmpty(cacheKey))
            {
                Interlocked.Increment(ref _missCount);
                return null;
            }

            if (_cache.TryGetValue(cacheKey, out var entry))
            {
                // Check expiration
                if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value <= DateTime.UtcNow)
                {
                    // Entry expired - remove it
                    _cache.TryRemove(cacheKey, out _);
                    Interlocked.Increment(ref _missCount);
                    return null;
                }

                // Update access metadata
                entry.HitCount++;
                entry.LastAccessedAt = DateTime.UtcNow;
                Interlocked.Increment(ref _hitCount);
                return entry;
            }

            Interlocked.Increment(ref _missCount);
            return null;
        }

        /// <inheritdoc/>
        public void Set(string cacheKey, object result, TimeSpan? expiration = null)
        {
            if (string.IsNullOrEmpty(cacheKey))
                return;

            // Evict if at capacity (and we are adding a new entry)
            if (!_cache.ContainsKey(cacheKey) && _cache.Count >= _options.MaxCacheSize)
            {
                EvictOldest();
            }

            var effectiveExpiration = expiration ?? _options.DefaultExpiration;
            var now = DateTime.UtcNow;

            var entry = new CacheEntry
            {
                Value = result,
                CreatedAt = now,
                ExpiresAt = now.Add(effectiveExpiration),
                HitCount = 0,
                LastAccessedAt = now
            };

            _cache[cacheKey] = entry;
        }

        /// <summary>
        /// Stores a result in the cache and associates it with a table for invalidation tracking.
        /// </summary>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="result">The result object to cache.</param>
        /// <param name="tableName">The table name to associate with this entry.</param>
        /// <param name="expiration">Optional expiration override.</param>
        public void Set(string cacheKey, object result, string tableName, TimeSpan? expiration = null)
        {
            Set(cacheKey, result, expiration);

            if (!string.IsNullOrEmpty(tableName))
            {
                AssociateKeyWithTable(cacheKey, tableName);
            }
        }

        /// <inheritdoc/>
        public void Invalidate(string cacheKey)
        {
            if (string.IsNullOrEmpty(cacheKey))
                return;

            _cache.TryRemove(cacheKey, out _);
        }

        /// <inheritdoc/>
        public void InvalidateByTable(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return;

            HashSet<string> keys;
            lock (_tableMapLock)
            {
                if (!_tableKeyMap.TryGetValue(tableName, out keys))
                    return;

                // Remove the table mapping
                _tableKeyMap.TryRemove(tableName, out _);
            }

            // Remove all associated cache entries
            foreach (var key in keys)
            {
                _cache.TryRemove(key, out _);
            }
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _cache.Clear();
            lock (_tableMapLock)
            {
                _tableKeyMap.Clear();
            }
        }

        /// <inheritdoc/>
        public CacheStatistics GetStatistics()
        {
            return new CacheStatistics
            {
                TotalEntries = _cache.Count,
                HitCount = Interlocked.Read(ref _hitCount),
                MissCount = Interlocked.Read(ref _missCount),
                EvictionCount = Interlocked.Read(ref _evictionCount)
            };
        }

        /// <summary>
        /// Associates a cache key with a table name for invalidation tracking.
        /// </summary>
        internal void AssociateKeyWithTable(string cacheKey, string tableName)
        {
            lock (_tableMapLock)
            {
                var keys = _tableKeyMap.GetOrAdd(tableName, _ => new HashSet<string>());
                keys.Add(cacheKey);
            }
        }

        private void EvictOldest()
        {
            // Find the entry with the oldest LastAccessedAt (LRU)
            var oldest = _cache
                .OrderBy(kvp => kvp.Value.LastAccessedAt)
                .FirstOrDefault();

            if (oldest.Key != null)
            {
                _cache.TryRemove(oldest.Key, out _);
                Interlocked.Increment(ref _evictionCount);
            }
        }
    }
}
