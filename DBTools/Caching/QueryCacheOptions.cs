using System;

namespace DBTools.Caching
{
    /// <summary>
    /// Configuration options for the query result cache.
    /// </summary>
    public class QueryCacheOptions
    {
        /// <summary>
        /// Whether caching is enabled (default: false).
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Default expiration time for cache entries (default: 5 minutes).
        /// </summary>
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Maximum number of entries in the cache (default: 1000).
        /// Oldest entries are evicted when this limit is reached.
        /// </summary>
        public int MaxCacheSize { get; set; } = 1000;

        /// <summary>
        /// Whether to automatically invalidate cache entries for a table
        /// when an Insert/Update/Delete operation targets that table (default: true).
        /// </summary>
        public bool EnableAutoInvalidation { get; set; } = true;
    }
}
