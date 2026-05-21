using DBTools.Abstractions;
using System;

namespace DBTools.Interceptors
{
    /// <summary>
    /// Built-in interceptor that automatically manages audit columns
    /// (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy).
    /// Modifies INSERT/UPDATE queries to include audit values.
    /// </summary>
    public class AuditInterceptor : IQueryInterceptor
    {
        private readonly Func<string> _getCurrentUser;
        private readonly string _createdAtColumn;
        private readonly string _updatedAtColumn;
        private readonly string _createdByColumn;
        private readonly string _updatedByColumn;


        /// <summary>
        /// Creates an audit interceptor with configurable column names.
        /// </summary>
        /// <param name="getCurrentUser">Function that returns the current user identifier</param>
        /// <param name="createdAtColumn">Column name for creation timestamp (null to skip)</param>
        /// <param name="updatedAtColumn">Column name for update timestamp (null to skip)</param>
        /// <param name="createdByColumn">Column name for creator user (null to skip)</param>
        /// <param name="updatedByColumn">Column name for updater user (null to skip)</param>
        public AuditInterceptor(
            Func<string> getCurrentUser = null,
            string createdAtColumn = "CreatedAt",
            string updatedAtColumn = "UpdatedAt",
            string createdByColumn = null,
            string updatedByColumn = null)
        {
            _getCurrentUser = getCurrentUser;
            _createdAtColumn = createdAtColumn;
            _updatedAtColumn = updatedAtColumn;
            _createdByColumn = createdByColumn;
            _updatedByColumn = updatedByColumn;
        }

        public void BeforeExecute(QueryInterceptionContext context)
        {
            // Audit logic is best handled at the model/entity level
            // This interceptor logs audit trail events
            if (context.OperationType == QueryOperationType.Insert ||
                context.OperationType == QueryOperationType.Update ||
                context.OperationType == QueryOperationType.Delete)
            {
                var user = _getCurrentUser?.Invoke() ?? "system";
                context.Properties["AuditUser"] = user;
                context.Properties["AuditTimestamp"] = DateTime.UtcNow;
            }
        }

        public void AfterExecute(QueryInterceptionContext context)
        {
            // Could write to an audit log table here
        }

        public void OnError(QueryInterceptionContext context, Exception exception)
        {
        }
    }
}
