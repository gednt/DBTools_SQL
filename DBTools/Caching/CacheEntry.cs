using System;
using System.Threading;

namespace DBTools.Caching
{
    /// <summary>
    /// Represents a cached query result entry with metadata.
    /// </summary>
    public class CacheEntry
    {
        private int _hitCount;

        /// <summary>
        /// The cached result value.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// When this entry was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When this entry expires (null means no expiration).
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Number of times this entry has been retrieved from cache.
        /// Thread-safe via Interlocked operations.
        /// </summary>
        public int HitCount
        {
            get => Volatile.Read(ref _hitCount);
            set => Interlocked.Exchange(ref _hitCount, value);
        }

        /// <summary>
        /// Last time this entry was accessed (used for LRU eviction).
        /// Eventual consistency is acceptable for this field under concurrent access.
        /// </summary>
        public DateTime LastAccessedAt { get; set; }

        /// <summary>
        /// Atomically increments the hit count.
        /// </summary>
        internal void IncrementHitCount()
        {
            Interlocked.Increment(ref _hitCount);
        }
    }
}
