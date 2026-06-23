using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DBTools.Abstractions;
using DBTools.Core;

namespace DBTools.Linq
{
    /// <summary>
    /// IQueryable implementation that supports async deferred SQL execution.
    /// SQL translation uses DbExpressionTranslator; execution uses AsyncSqlClient.
    /// Supports LINQ method chaining: Where, OrderBy, ThenBy, Skip, Take, First, Count, Any, etc.
    /// Use ToListAsync() for async enumeration.
    /// </summary>
    /// <typeparam name="TModel">The model type</typeparam>
    public class AsyncDbQuery<TModel> : IQueryable<TModel>, IOrderedQueryable<TModel>
        where TModel : class, new()
    {
        private readonly AsyncDbQueryProvider<TModel> _provider;
        private readonly Expression _expression;
        private readonly string _tableName;

        public AsyncDbQuery(AsyncDbQueryProvider<TModel> provider, string tableName)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _tableName = tableName ?? "";
            _expression = Expression.Constant(this);
        }

        public AsyncDbQuery(AsyncDbQueryProvider<TModel> provider, Expression expression, string tableName)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _expression = expression ?? Expression.Constant(this);
            _tableName = tableName ?? "";
        }

        public Type ElementType => typeof(TModel);

        public Expression Expression => _expression;

        public IQueryProvider Provider => _provider;

        public IEnumerator<TModel> GetEnumerator()
        {
            // CAUTION: This blocks on async execution (.Result) and may deadlock
            // in UI/ASP.NET contexts with a synchronization context.
            // Use ToListAsync() in async contexts to avoid this.
            return _provider.ExecuteSequenceAsync(_expression).Result.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Task<List<TModel>> ToListAsync(CancellationToken ct = default)
        {
            return _provider.ExecuteSequenceAsync(_expression, ct);
        }

        public override string ToString()
        {
            var translator = new DbExpressionTranslator(_tableName, "t0", null, _provider.DbProvider);
            try
            {
                var (sql, _) = translator.TranslateAndBuild(_expression);
                return sql;
            }
            catch
            {
                return base.ToString();
            }
        }
    }
}
