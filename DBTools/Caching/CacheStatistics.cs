namespace DBTools.Caching
{
    /// <summary>
    /// Provides statistics about the query cache state and performance.
    /// </summary>
    public class CacheStatistics
    {
        /// <summary>
        /// Total number of entries currently in the cache.
        /// </summary>
        public int TotalEntries { get; set; }

        /// <summary>
        /// Total number of cache hits (successful retrievals).
        /// </summary>
        public long HitCount { get; set; }

        /// <summary>
        /// Total number of cache misses.
        /// </summary>
        public long MissCount { get; set; }

        /// <summary>
        /// Cache hit rate as a ratio (0.0 to 1.0). Returns 0 if no requests have been made.
        /// </summary>
        public double HitRate
        {
            get
            {
                long total = HitCount + MissCount;
                return total == 0 ? 0.0 : (double)HitCount / total;
            }
        }

        /// <summary>
        /// Total number of entries evicted from the cache.
        /// </summary>
        public long EvictionCount { get; set; }
    }
}
