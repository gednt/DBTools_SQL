using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DBTools.Abstractions
{
    /// <summary>
    /// Intercepts query execution for cross-cutting concerns like logging, auditing, and soft-delete.
    /// Interceptors are called in registration order.
    /// </summary>
    public interface IQueryInterceptor
    {
        /// <summary>
        /// Called before a query is executed. Can modify the SQL or parameters.
        /// </summary>
        /// <param name="context">The interception context with query details</param>
        void BeforeExecute(QueryInterceptionContext context);

        /// <summary>
        /// Called after a query is executed successfully.
        /// </summary>
        /// <param name="context">The interception context with query details and results</param>
        void AfterExecute(QueryInterceptionContext context);

        /// <summary>
        /// Called when a query execution fails.
        /// </summary>
        /// <param name="context">The interception context with query details</param>
        /// <param name="exception">The exception that occurred</param>
        void OnError(QueryInterceptionContext context, Exception exception);
    }

    /// <summary>
    /// Async version of IQueryInterceptor for non-blocking interceptor logic.
    /// </summary>
    public interface IAsyncQueryInterceptor
    {
        Task BeforeExecuteAsync(QueryInterceptionContext context, CancellationToken ct = default);
        Task AfterExecuteAsync(QueryInterceptionContext context, CancellationToken ct = default);
        Task OnErrorAsync(QueryInterceptionContext context, Exception exception, CancellationToken ct = default);
    }

    /// <summary>
    /// Context object passed to interceptors with query execution details.
    /// </summary>
    public class QueryInterceptionContext
    {
        /// <summary>
        /// The SQL query string. Can be modified by interceptors.
        /// </summary>
        public string Sql { get; set; }

        /// <summary>
        /// The query parameters. Can be modified by interceptors.
        /// </summary>
        public List<object> Parameters { get; set; } = new List<object>();

        /// <summary>
        /// The operation type (Select, Insert, Update, Delete, Execute).
        /// </summary>
        public QueryOperationType OperationType { get; set; }

        /// <summary>
        /// The table name involved in the operation (if applicable).
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// The execution duration (populated after execution).
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// The number of rows affected (populated after execution).
        /// </summary>
        public int? RowsAffected { get; set; }

        /// <summary>
        /// Whether the interceptor has suppressed execution (useful for caching interceptors).
        /// </summary>
        public bool IsSuppressed { get; set; }

        /// <summary>
        /// Custom properties that interceptors can use to pass data between BeforeExecute and AfterExecute.
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Types of query operations for interceptor context.
    /// </summary>
    public enum QueryOperationType
    {
        Select,
        Insert,
        Update,
        Delete,
        Execute,
        Scalar
    }
}
