using System.Collections.Generic;

namespace DBTools.StoredProcedures
{
    /// <summary>
    /// Represents the result of a stored procedure execution.
    /// Contains the return value, output parameters, and affected row count.
    /// </summary>
    public class StoredProcedureResult
    {
        /// <summary>
        /// The stored procedure return value (typically an integer status code).
        /// </summary>
        public object ReturnValue { get; set; }

        /// <summary>
        /// Dictionary of output parameter names and their values after execution.
        /// </summary>
        public Dictionary<string, object> OutputParameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// The number of rows affected by the stored procedure execution.
        /// </summary>
        public int RowsAffected { get; set; }
    }
}
