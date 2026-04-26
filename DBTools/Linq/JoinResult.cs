namespace DBTools.Linq
{
    /// <summary>
    /// Represents a single row result from a JOIN query, containing both the left and right model instances.
    /// For LEFT JOINs, the Right property will be null when there is no matching right record.
    /// </summary>
    /// <typeparam name="TLeft">The left (primary) table model type</typeparam>
    /// <typeparam name="TRight">The right (joined) table model type</typeparam>
    public class JoinResult<TLeft, TRight>
        where TLeft : class, new()
        where TRight : class, new()
    {
        /// <summary>
        /// The model instance from the left (primary) table.
        /// </summary>
        public TLeft Left { get; set; }

        /// <summary>
        /// The model instance from the right (joined) table.
        /// Will be null for LEFT JOIN non-matches.
        /// </summary>
        public TRight Right { get; set; }
    }
}
