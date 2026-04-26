using DBTools.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DBTools.Linq
{
    /// <summary>
    /// IQueryable implementation for JOIN queries.
    /// Represents a query that joins two tables and returns JoinResult&lt;TLeft, TRight&gt; rows.
    /// Supports Where, OrderBy, Skip, Take, and other LINQ operators on the joined result.
    /// </summary>
    public class JoinQuery<TLeft, TRight> : IQueryable<JoinResult<TLeft, TRight>>, IOrderedQueryable<JoinResult<TLeft, TRight>>
        where TLeft : class, new()
        where TRight : class, new()
    {
        private readonly JoinQueryProvider<TLeft, TRight> _provider;
        private readonly Expression _expression;

        public JoinQuery(JoinQueryProvider<TLeft, TRight> provider, Expression expression)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        public JoinQuery(JoinQueryProvider<TLeft, TRight> provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _expression = Expression.Constant(this);
        }

        public Type ElementType => typeof(JoinResult<TLeft, TRight>);

        public Expression Expression => _expression;

        public IQueryProvider Provider => _provider;

        public IEnumerator<JoinResult<TLeft, TRight>> GetEnumerator()
        {
            var result = _provider.ExecuteJoinSequence(_expression);
            return result.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// IQueryProvider for JOIN queries. Translates LINQ expressions into SQL with JOIN clauses.
    /// </summary>
    public class JoinQueryProvider<TLeft, TRight> : IQueryProvider
        where TLeft : class, new()
        where TRight : class, new()
    {
        private readonly SqlClient _utils;
        private readonly string _leftTable;
        private readonly string _leftAlias;
        private readonly string _leftKey;
        private readonly string _rightTable;
        private readonly string _rightAlias;
        private readonly string _rightKey;
        private readonly string _joinType; // "INNER JOIN" or "LEFT JOIN"
        private readonly string _primaryKeyName;

        public JoinQueryProvider(
            SqlClient utils,
            string leftTable,
            string leftAlias,
            string leftKey,
            string rightTable,
            string rightAlias,
            string rightKey,
            string joinType,
            string primaryKeyName)
        {
            _utils = utils;
            _leftTable = leftTable;
            _leftAlias = leftAlias;
            _leftKey = leftKey;
            _rightTable = rightTable;
            _rightAlias = rightAlias;
            _rightKey = rightKey;
            _joinType = joinType;
            _primaryKeyName = primaryKeyName;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(JoinQuery<TLeft, TRight>), this, expression);
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException ?? tie;
            }
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (typeof(TElement) == typeof(JoinResult<TLeft, TRight>))
                return (IQueryable<TElement>)(object)new JoinQuery<TLeft, TRight>(this, expression);
            throw new InvalidOperationException($"JoinQuery does not support element type {typeof(TElement)}.");
        }

        public object Execute(Expression expression)
        {
            return Execute<JoinResult<TLeft, TRight>>(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var (sql, parameters, translation) = TranslateAndBuild(expression);
            DataView dataView = _utils.SelectRaw(sql, parameters.ToArray());

            if (translation.IsCountQuery || translation.IsAnyQuery)
            {
                int count = 0;
                if (dataView != null && dataView.Count > 0)
                    count = Convert.ToInt32(dataView[0]["RecordCount"]);

                if (translation.IsAnyQuery)
                    return (TResult)(object)(count > 0);
                return (TResult)(object)count;
            }

            var results = MapJoinResult(dataView);

            if (translation.IsFirstQuery || translation.IsFirstOrDefaultQuery)
            {
                var first = results.FirstOrDefault();
                if (first == null)
                {
                    if (translation.IsFirstQuery)
                        throw new InvalidOperationException("Sequence contains no elements.");
                    return default;
                }
                return (TResult)(object)first;
            }

            if (translation.IsSingleQuery || translation.IsSingleOrDefaultQuery)
            {
                var list = results.ToList();
                if (list.Count == 0)
                {
                    if (translation.IsSingleQuery)
                        throw new InvalidOperationException("Sequence contains no elements.");
                    return default;
                }
                if (list.Count > 1)
                    throw new InvalidOperationException("Sequence contains more than one element.");
                return (TResult)(object)list[0];
            }

            throw new InvalidOperationException("Sequence results should be accessed via enumeration, not Execute.");
        }

        internal IEnumerable<JoinResult<TLeft, TRight>> ExecuteJoinSequence(Expression expression)
        {
            var (sql, parameters, _) = TranslateAndBuild(expression);
            DataView dataView = _utils.SelectRaw(sql, parameters.ToArray());
            return MapJoinResult(dataView);
        }

        private (string sql, List<object> parameters, TranslationResult translation) TranslateAndBuild(Expression expression)
        {
            var translator = new DbExpressionTranslator(_leftTable, _leftAlias, _rightAlias);

            if (!string.IsNullOrEmpty(_primaryKeyName))
                translator.SetPrimaryKeyFallback(_primaryKeyName);

            // Add the JOIN clause before translation
            var result = translator.Translate(expression);
            result.JoinClauses.Add(new JoinClause
            {
                JoinType = _joinType,
                RightTable = _rightTable,
                RightAlias = _rightAlias,
                LeftKey = $"{_leftAlias}.{_leftKey}",
                RightKey = $"{_rightAlias}.{_rightKey}",
                RightModelType = typeof(TRight)
            });

            // Build explicit SELECT with aliased column names to avoid collisions when both
            // tables share a column name (e.g. Id). Columns are selected as t0_Col / t1_Col.
            var leftCols = typeof(TLeft).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .Select(p => $"{_leftAlias}.{p.Name} AS {_leftAlias}_{p.Name}");
            var rightCols = typeof(TRight).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .Select(p => $"{_rightAlias}.{p.Name} AS {_rightAlias}_{p.Name}");
            result.SelectClause = string.Join(", ", leftCols.Concat(rightCols));

            string sql = translator.BuildSql(result);
            return (sql, result.Parameters, result);
        }

        private IEnumerable<JoinResult<TLeft, TRight>> MapJoinResult(DataView dataView)
        {
            var results = new List<JoinResult<TLeft, TRight>>();
            if (dataView == null || dataView.Count == 0)
                return results;

            var leftProps = typeof(TLeft).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            var rightProps = typeof(TRight).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            // Columns are selected as t0_PropName / t1_PropName to avoid collisions.
            // Map each DataColumn back to its model side by stripping the alias prefix.
            string leftPrefix = _leftAlias + "_";
            string rightPrefix = _rightAlias + "_";

            var leftColumns = new List<(DataColumn Col, string PropName)>();
            var rightColumns = new List<(DataColumn Col, string PropName)>();

            foreach (DataColumn col in dataView.Table.Columns)
            {
                if (col.ColumnName.StartsWith(leftPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string propName = col.ColumnName.Substring(leftPrefix.Length);
                    if (leftProps.ContainsKey(propName))
                        leftColumns.Add((col, propName));
                }
                else if (col.ColumnName.StartsWith(rightPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string propName = col.ColumnName.Substring(rightPrefix.Length);
                    if (rightProps.ContainsKey(propName))
                        rightColumns.Add((col, propName));
                }
            }

            foreach (DataRowView row in dataView)
            {
                var leftModel = new TLeft();
                bool hasLeftData = false;

                foreach (var (col, propName) in leftColumns)
                {
                    var value = row[col.ColumnName];
                    if (value != null && value != DBNull.Value)
                    {
                        hasLeftData = true;
                        try
                        {
                            var prop = leftProps[propName];
                            SetValue(prop, leftModel, value);
                        }
                        catch { /* skip conversion errors */ }
                    }
                }

                var rightModel = new TRight();
                bool hasRightData = false;

                foreach (var (col, propName) in rightColumns)
                {
                    var value = row[col.ColumnName];
                    if (value != null && value != DBNull.Value)
                    {
                        hasRightData = true;
                        try
                        {
                            var prop = rightProps[propName];
                            SetValue(prop, rightModel, value);
                        }
                        catch { /* skip conversion errors */ }
                    }
                }

                results.Add(new JoinResult<TLeft, TRight>
                {
                    Left = hasLeftData ? leftModel : null,
                    Right = hasRightData ? rightModel : null
                });
            }

            return results;
        }

        private static void SetValue(PropertyInfo prop, object target, object value)
        {
            if (prop.PropertyType != value.GetType())
            {
                if (prop.PropertyType.IsGenericType &&
                    prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
                    value = Convert.ChangeType(value, underlyingType);
                }
                else
                {
                    value = Convert.ChangeType(value, prop.PropertyType);
                }
            }
            prop.SetValue(target, value);
        }
    }
}
