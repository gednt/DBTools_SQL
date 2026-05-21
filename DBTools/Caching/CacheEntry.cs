using System;

namespace DBTools.Caching
{
    /// <summary>
    /// Represents a cached query result entry with metadata.
    /// </summary>
    public class CacheEntry
    {
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
        /// </summary>
        public int HitCount { get; set; }

        /// <summary>
        /// Last time this entry was accessed (used for LRU eviction).
        /// </summary>
        public DateTime LastAccessedAt { get; set; }
    }
}
