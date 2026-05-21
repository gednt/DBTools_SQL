using DBTools.Abstractions;
using System;

namespace DBTools.Interceptors
{
    /// <summary>
    /// Built-in interceptor that logs all query executions.
    /// Uses a configurable action for output (Console, ILogger, etc.).
    /// </summary>
    public class LoggingInterceptor : IQueryInterceptor
    {
        private readonly Action<string> _logAction;
        private readonly bool _logParameters;


        /// <summary>
        /// Creates a logging interceptor with the specified log action.
        /// </summary>
        /// <param name="logAction">Action to call with log messages (e.g., Console.WriteLine)</param>
        /// <param name="logParameters">Whether to include parameter values in logs (default: false for security)</param>
        public LoggingInterceptor(Action<string> logAction, bool logParameters = false)
        {
            _logAction = logAction ?? throw new ArgumentNullException(nameof(logAction));
            _logParameters = logParameters;
        }

        public void BeforeExecute(QueryInterceptionContext context)
        {
            var msg = $"[DBTools] Executing {context.OperationType}: {context.Sql}";
            if (_logParameters && context.Parameters.Count > 0)
            {
                msg += $" | Params: [{string.Join(", ", context.Parameters)}]";
            }
            _logAction(msg);
        }

        public void AfterExecute(QueryInterceptionContext context)
        {
            var msg = $"[DBTools] Completed {context.OperationType} in {context.Duration?.TotalMilliseconds:F1}ms";
            if (context.RowsAffected.HasValue)
                msg += $" | Rows: {context.RowsAffected}";
            _logAction(msg);
        }

        public void OnError(QueryInterceptionContext context, Exception exception)
        {
            _logAction($"[DBTools] ERROR {context.OperationType}: {exception.Message} | SQL: {context.Sql}");
        }
    }
}
