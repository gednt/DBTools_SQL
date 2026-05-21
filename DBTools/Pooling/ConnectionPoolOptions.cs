namespace DBTools.Pooling
{
    /// <summary>
    /// Configuration options for database connection pooling.
    /// Controls how connections are pooled and reused across database operations.
    /// </summary>
    public class ConnectionPoolOptions
    {
        /// <summary>
        /// The minimum number of connections maintained in the pool.
        /// Default is 0 (no minimum).
        /// </summary>
        public int MinPoolSize { get; set; } = 0;

        /// <summary>
        /// The maximum number of connections allowed in the pool.
        /// Default is 100.
        /// </summary>
        public int MaxPoolSize { get; set; } = 100;

        /// <summary>
        /// The maximum lifetime of a connection in seconds before it is destroyed.
        /// Default is 0 (unlimited lifetime).
        /// </summary>
        public int ConnectionLifetimeSeconds { get; set; } = 0;

        /// <summary>
        /// The time in seconds a connection can remain idle in the pool before being removed.
        /// Default is 300 seconds (5 minutes).
        /// </summary>
        public int ConnectionIdleTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// Whether connection pooling is enabled.
        /// Default is true.
        /// </summary>
        public bool Pooling { get; set; } = true;
    }
}
