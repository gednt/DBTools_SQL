using DBTools.Abstractions;
using DBTools.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DBTools.Linq
{
    /// <summary>
    /// IQueryProvider implementation that translates LINQ expressions into SQL queries
    /// and executes them through the DBTools Utils layer.
    /// </summary>
    public class DbQueryProvider : IQueryProvider
    {
        private readonly SqlClient _utils;
        private readonly IDbProvider _provider;
        private readonly string _tableName;
        private readonly string _primaryKeyName;
        private readonly Func<DataView, IEnumerable<object>> _mapDataView;

        public DbQueryProvider(
            SqlClient utils,
            IDbProvider provider,
            string tableName,
            string primaryKeyName,
            Func<DataView, IEnumerable<object>> mapDataView)
        {
            _utils = utils ?? throw new ArgumentNullException(nameof(utils));
            _provider = provider;
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _primaryKeyName = primaryKeyName;
            _mapDataView = mapDataView ?? throw new ArgumentNullException(nameof(mapDataView));
        }

        public IQueryable CreateQuery(Expression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var elementType = expression.Type.GetGenericArguments().FirstOrDefault()
                ?? throw new InvalidOperationException("Could not determine element type from expression.");

            try
            {
                var queryType = typeof(DbQuery<>).MakeGenericType(elementType);
                return (IQueryable)Activator.CreateInstance(queryType, this, expression, _tableName);
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException ?? tie;
            }
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            try
            {
                var queryType = typeof(DbQuery<>).MakeGenericType(typeof(TElement));
                return (IQueryable<TElement>)Activator.CreateInstance(queryType, this, expression, _tableName);
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException ?? tie;
            }
        }

        public object Execute(Expression expression)
        {
            return Execute<object>(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var translator = new DbExpressionTranslator(_tableName, "t0", null, _provider);

            // Set primary key for ORDER BY fallback
            if (!string.IsNullOrEmpty(_primaryKeyName))
                translator.SetPrimaryKeyFallback(_primaryKeyName);

            var (sql, parameters) = translator.TranslateAndBuild(expression);
            var result = translator.GetTranslationResult();

            // Execute the query
            DataView dataView = _utils.SelectRaw(sql, parameters.ToArray());

            // Handle different result types
            if (result.IsCountQuery || result.IsAnyQuery)
            {
                int count = 0;
                if (dataView != null && dataView.Count > 0)
                    count = Convert.ToInt32(dataView[0]["RecordCount"]);

                if (result.IsAnyQuery)
                {
                    var anyResult = (object)(count > 0);
                    return (TResult)anyResult;
                }

                var countResult = (object)count;
                return (TResult)countResult;
            }

            // Map results to models
            var models = _mapDataView(dataView);

            // Handle element operators
            if (result.IsFirstQuery || result.IsFirstOrDefaultQuery)
            {
                var firstResult = models.FirstOrDefault();
                if (firstResult == null)
                {
                    if (result.IsFirstQuery)
                        throw new InvalidOperationException("Sequence contains no elements.");
                    return default;
                }
                return (TResult)firstResult;
            }

            if (result.IsSingleQuery || result.IsSingleOrDefaultQuery)
            {
                var modelList = models.ToList();
                if (modelList.Count == 0)
                {
                    if (result.IsSingleQuery)
                        throw new InvalidOperationException("Sequence contains no elements.");
                    return default;
                }
                if (modelList.Count > 1)
                    throw new InvalidOperationException("Sequence contains more than one element.");
                return (TResult)modelList[0];
            }

            // Sequence results must go through GetEnumerator / ExecuteSequence, not Execute<TResult>
            throw new InvalidOperationException(
                "Sequence results should be accessed by enumerating the IQueryable (e.g. ToList()), not via Execute<TResult>.");
        }

        /// <summary>
        /// Executes the expression and returns the result as an IEnumerable for enumeration.
        /// </summary>
        internal IEnumerable<TElement> ExecuteSequence<TElement>(Expression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var translator = new DbExpressionTranslator(_tableName, "t0", null, _provider);

            if (!string.IsNullOrEmpty(_primaryKeyName))
                translator.SetPrimaryKeyFallback(_primaryKeyName);

            var (sql, parameters) = translator.TranslateAndBuild(expression);
            var result = translator.GetTranslationResult();

            // For count/any queries executed as sequence, return the scalar result
            if (result.IsCountQuery || result.IsAnyQuery)
            {
                DataView dataView = _utils.SelectRaw(sql, parameters.ToArray());
                int count = 0;
                if (dataView != null && dataView.Count > 0)
                    count = Convert.ToInt32(dataView[0]["RecordCount"]);

                if (result.IsAnyQuery)
                    return new TElement[] { (TElement)(object)(count > 0) };

                return new TElement[] { (TElement)(object)count };
            }

            DataView dataViewSeq = _utils.SelectRaw(sql, parameters.ToArray());
            var models = _mapDataView(dataViewSeq);
            return models.Cast<TElement>();
        }
    }
}
