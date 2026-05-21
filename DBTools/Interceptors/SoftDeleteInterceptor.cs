using DBTools.Abstractions;
using System;

namespace DBTools.Interceptors
{
    /// <summary>
    /// Built-in interceptor that automatically applies soft-delete filters.
    /// Appends "AND IsDeleted = 0" (or custom column) to all SELECT queries.
    /// Converts DELETE operations to UPDATE SET IsDeleted = 1.
    /// </summary>
    public class SoftDeleteInterceptor : IQueryInterceptor
    {
        private readonly string _columnName;
        private readonly string _activeValue;

        /// <summary>
        /// Creates a soft-delete interceptor.
        /// </summary>
        /// <param name="columnName">The soft-delete column name (default: "IsDeleted")</param>
        /// <param name="activeValue">The value representing active records (default: "0")</param>
        public SoftDeleteInterceptor(string columnName = "IsDeleted", string activeValue = "0")
        {
            _columnName = columnName;
            _activeValue = activeValue;
        }

        public void BeforeExecute(QueryInterceptionContext context)
        {
            if (context.OperationType == QueryOperationType.Select)
            {
                // Append soft-delete filter to SELECT queries
                if (context.Sql.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
                {
                    // Insert before ORDER BY or at end
                    int orderByIdx = context.Sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
                    if (orderByIdx > 0)
                    {
                        context.Sql = context.Sql.Insert(orderByIdx, $"AND {_columnName} = {_activeValue} ");
                    }
                    else
                    {
                        context.Sql += $" AND {_columnName} = {_activeValue}";
                    }
                }
                else
                {
                    // No WHERE clause - add one
                    int orderByIdx = context.Sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
                    if (orderByIdx > 0)
                    {
                        context.Sql = context.Sql.Insert(orderByIdx, $"WHERE {_columnName} = {_activeValue} ");
                    }
                    else
                    {
                        context.Sql += $" WHERE {_columnName} = {_activeValue}";
                    }
                }
            }
            else if (context.OperationType == QueryOperationType.Delete)
            {
                // Convert DELETE to UPDATE SET IsDeleted = 1
                if (context.Sql.StartsWith("DELETE FROM", StringComparison.OrdinalIgnoreCase))
                {
                    var tablePart = context.Sql.Substring("DELETE FROM".Length);
                    int whereIdx = tablePart.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
                    if (whereIdx > 0)
                    {
                        var table = tablePart.Substring(0, whereIdx).Trim();
                        var where = tablePart.Substring(whereIdx);
                        context.Sql = $"UPDATE {table} SET {_columnName} = 1 {where}";
                        context.OperationType = QueryOperationType.Update;
                    }
                }
            }
        }

        public void AfterExecute(QueryInterceptionContext context) { }
        public void OnError(QueryInterceptionContext context, Exception exception) { }
    }
}
