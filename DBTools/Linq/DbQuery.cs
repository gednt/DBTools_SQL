using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace DBTools.Linq
{
    /// <summary>
    /// IQueryable implementation that represents a deferred SQL query.
    /// No SQL is executed until the query is enumerated.
    /// Supports LINQ method chaining: Where, OrderBy, ThenBy, Skip, Take, First, Count, Any, etc.
    /// </summary>
    /// <typeparam name="TModel">The model type</typeparam>
    public class DbQuery<TModel> : IQueryable<TModel>, IOrderedQueryable<TModel>
        where TModel : class, new()
    {
        private readonly DbQueryProvider _provider;
        private readonly Expression _expression;
        private readonly string _tableName;

        /// <summary>
        /// Creates a new DbQuery as the root of a query, using a ConstantExpression pointing to this object.
        /// </summary>
        public DbQuery(DbQueryProvider provider, string tableName)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _tableName = tableName ?? "";
            _expression = Expression.Constant(this);
        }

        /// <summary>
        /// Creates a new DbQuery with the given provider, expression and table name.
        /// </summary>
        public DbQuery(DbQueryProvider provider, Expression expression, string tableName)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _expression = expression ?? throw new ArgumentNullException(nameof(expression));
            _tableName = tableName ?? "";
        }

        public Type ElementType => typeof(TModel);

        public Expression Expression => _expression;

        public IQueryProvider Provider => _provider;

        public IEnumerator<TModel> GetEnumerator()
        {
            var result = _provider.ExecuteSequence<TModel>(_expression);
            return result.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            var translator = new DbExpressionTranslator(_tableName);
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
