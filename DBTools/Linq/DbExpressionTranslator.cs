using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace DBTools.Linq
{
    /// <summary>
    /// Represents a JOIN clause in the translated SQL query.
    /// </summary>
    internal class JoinClause
    {
        public string JoinType { get; set; } // "INNER JOIN" or "LEFT JOIN"
        public string RightTable { get; set; }
        public string RightAlias { get; set; }
        public string LeftKey { get; set; }
        public string RightKey { get; set; }
        public Type RightModelType { get; set; }
    }

    /// <summary>
    /// Result of translating a LINQ expression tree into SQL components.
    /// </summary>
    internal class TranslationResult
    {
        public string SelectClause { get; set; } = "*";
        public string FromClause { get; set; }
        public List<JoinClause> JoinClauses { get; set; } = new List<JoinClause>();
        public string WhereClause { get; set; } = "";
        public List<object> Parameters { get; set; } = new List<object>();
        public string OrderByClause { get; set; } = "";
        public int? SkipCount { get; set; }
        public int? TakeCount { get; set; }
        public bool IsCountQuery { get; set; }
        public bool IsAnyQuery { get; set; }
        public bool IsFirstQuery { get; set; }
        public bool IsSingleQuery { get; set; }
        public bool IsFirstOrDefaultQuery { get; set; }
        public bool IsSingleOrDefaultQuery { get; set; }
    }

    /// <summary>
    /// Translates LINQ expression trees into SQL query components.
    /// Walks the expression tree and accumulates SELECT, FROM, WHERE, ORDER BY, SKIP/TAKE clauses.
    /// </summary>
    internal class DbExpressionTranslator : ExpressionVisitor
    {
        private readonly string _tableName;
        private readonly string _tableAlias;
        private readonly TranslationResult _result;
        private int _paramIndex;
        private string _rightAlias;

        public DbExpressionTranslator(string tableName, string tableAlias = "t0", string rightAlias = null)
        {
            _tableName = tableName;
            _tableAlias = tableAlias;
            _rightAlias = rightAlias;
            _result = new TranslationResult
            {
                FromClause = $"{tableName} {tableAlias}"
            };
            _paramIndex = 0;
        }

        public TranslationResult Translate(Expression expression)
        {
            Visit(expression);
            return _result;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable))
            {
                // Visit the source first (the argument before the predicate)
                if (node.Arguments.Count > 0)
                    Visit(node.Arguments[0]);

                string methodName = node.Method.Name;

                switch (methodName)
                {
                    case "Where":
                        TranslateWhere(node);
                        break;
                    case "OrderBy":
                        TranslateOrderBy(node, ascending: true);
                        break;
                    case "OrderByDescending":
                        TranslateOrderBy(node, ascending: false);
                        break;
                    case "ThenBy":
                        TranslateThenBy(node, ascending: true);
                        break;
                    case "ThenByDescending":
                        TranslateThenBy(node, ascending: false);
                        break;
                    case "Skip":
                        TranslateSkip(node);
                        break;
                    case "Take":
                        TranslateTake(node);
                        break;
                    case "Count":
                        _result.IsCountQuery = true;
                        break;
                    case "Any":
                        _result.IsAnyQuery = true;
                        break;
                    case "First":
                        _result.IsFirstQuery = true;
                        break;
                    case "FirstOrDefault":
                        _result.IsFirstOrDefaultQuery = true;
                        break;
                    case "Single":
                        _result.IsSingleQuery = true;
                        break;
                    case "SingleOrDefault":
                        _result.IsSingleOrDefaultQuery = true;
                        break;
                    case "Select":
                        // Projection not supported - ignore, keep SELECT *
                        break;
                    default:
                        throw new NotSupportedException($"LINQ method '{methodName}' is not supported for SQL translation.");
                }

                return node;
            }

            return base.VisitMethodCall(node);
        }

        private void TranslateWhere(MethodCallExpression node)
        {
            var predicate = GetLambda(node.Arguments[1]);
            var whereClause = TranslatePredicate(predicate.Body);
            if (!string.IsNullOrEmpty(_result.WhereClause))
                _result.WhereClause += $" AND ({whereClause})";
            else
                _result.WhereClause = whereClause;
        }

        private void TranslateOrderBy(MethodCallExpression node, bool ascending)
        {
            var keySelector = GetLambda(node.Arguments[1]);
            string column = TranslateKeySelector(keySelector.Body);
            _result.OrderByClause = $"{column} {(ascending ? "ASC" : "DESC")}";
        }

        private void TranslateThenBy(MethodCallExpression node, bool ascending)
        {
            var keySelector = GetLambda(node.Arguments[1]);
            string column = TranslateKeySelector(keySelector.Body);
            _result.OrderByClause += $", {column} {(ascending ? "ASC" : "DESC")}";
        }

        private void TranslateSkip(MethodCallExpression node)
        {
            var value = EvaluateConstant(node.Arguments[1]);
            _result.SkipCount = Convert.ToInt32(value);
        }

        private void TranslateTake(MethodCallExpression node)
        {
            var value = EvaluateConstant(node.Arguments[1]);
            _result.TakeCount = Convert.ToInt32(value);
        }

        private string TranslatePredicate(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.AndAlso:
                    var andExp = (BinaryExpression)expression;
                    return $"({TranslatePredicate(andExp.Left)}) AND ({TranslatePredicate(andExp.Right)})";

                case ExpressionType.OrElse:
                    var orExp = (BinaryExpression)expression;
                    return $"({TranslatePredicate(orExp.Left)}) OR ({TranslatePredicate(orExp.Right)})";

                case ExpressionType.Equal:
                    return TranslateBinary((BinaryExpression)expression, "=");

                case ExpressionType.NotEqual:
                    return TranslateBinary((BinaryExpression)expression, "<>");

                case ExpressionType.GreaterThan:
                    return TranslateBinary((BinaryExpression)expression, ">");

                case ExpressionType.GreaterThanOrEqual:
                    return TranslateBinary((BinaryExpression)expression, ">=");

                case ExpressionType.LessThan:
                    return TranslateBinary((BinaryExpression)expression, "<");

                case ExpressionType.LessThanOrEqual:
                    return TranslateBinary((BinaryExpression)expression, "<=");

                case ExpressionType.Not:
                    var notExp = (UnaryExpression)expression;
                    return $"NOT ({TranslatePredicate(notExp.Operand)})";

                case ExpressionType.MemberAccess:
                    var memberExp = (MemberExpression)expression;
                    if (memberExp.Expression != null && memberExp.Expression.NodeType == ExpressionType.Parameter)
                        return $"{_tableAlias}.{memberExp.Member.Name}";
                    // Handle JoinResult navigation: j.Left.Prop or j.Right.Prop
                    var joinColumn = TryTranslateJoinMember(memberExp);
                    if (joinColumn != null)
                        return joinColumn;
                    break;

                case ExpressionType.Constant:
                    var constExp = (ConstantExpression)expression;
                    return AddParameter(constExp.Value);

                case ExpressionType.Call:
                    return TranslateMethodCall((MethodCallExpression)expression);

                case ExpressionType.Convert:
                    var convertExp = (UnaryExpression)expression;
                    return TranslatePredicate(convertExp.Operand);
            }

            // Fallback: compile and evaluate
            try
            {
                var value = Expression.Lambda(expression).Compile().DynamicInvoke();
                return AddParameter(value);
            }
            catch (Exception ex)
            {
                throw new NotSupportedException($"Expression type '{expression.NodeType}' is not supported in WHERE clause.", ex);
            }
        }

        private string TranslateBinary(BinaryExpression expression, string op)
        {
            string left = TranslatePredicateSide(expression.Left);
            string right = TranslatePredicateSide(expression.Right);

            // Handle null comparisons: = null → IS NULL, <> null → IS NOT NULL
            if (op == "=" && right.StartsWith("@param") && IsNullParameter(right))
                return $"{left} IS NULL";
            if (op == "<>" && right.StartsWith("@param") && IsNullParameter(right))
                return $"{left} IS NOT NULL";
            if (op == "=" && left.StartsWith("@param") && IsNullParameter(left))
                return $"{right} IS NULL";
            if (op == "<>" && left.StartsWith("@param") && IsNullParameter(left))
                return $"{right} IS NOT NULL";

            return $"{left} {op} {right}";
        }

        private bool IsNullParameter(string paramName)
        {
            var idx = int.Parse(paramName.Substring(6));
            return idx < _result.Parameters.Count && _result.Parameters[idx] == DBNull.Value;
        }

        private string TranslatePredicateSide(Expression expression)
        {
            if (expression.NodeType == ExpressionType.MemberAccess)
            {
                var memberExp = (MemberExpression)expression;
                // Direct parameter access: u.Prop
                if (memberExp.Expression != null && memberExp.Expression.NodeType == ExpressionType.Parameter)
                    return $"{_tableAlias}.{memberExp.Member.Name}";
                // JoinResult navigation: j.Left.Prop or j.Right.Prop
                var joinColumn = TryTranslateJoinMember(memberExp);
                if (joinColumn != null)
                    return joinColumn;
            }

            if (expression.NodeType == ExpressionType.Constant)
            {
                var constExp = (ConstantExpression)expression;
                return AddParameter(constExp.Value);
            }

            // Handle captured variables (MemberAccess on a closure object)
            try
            {
                var value = Expression.Lambda(expression).Compile().DynamicInvoke();
                return AddParameter(value);
            }
            catch
            {
                // If we can't evaluate it, try member access
                if (expression.NodeType == ExpressionType.MemberAccess)
                {
                    var memberExp = (MemberExpression)expression;
                    if (memberExp.Expression != null && memberExp.Expression.NodeType == ExpressionType.Parameter)
                        return $"{_tableAlias}.{memberExp.Member.Name}";
                }

                throw new NotSupportedException($"Expression side '{expression}' could not be translated to SQL.");
            }
        }

        /// <summary>
        /// Tries to translate a nested MemberExpression for JoinResult navigation.
        /// Handles j.Left.Prop (→ t0.Prop) and j.Right.Prop (→ t1.Prop).
        /// Returns null if the expression is not a JoinResult navigation.
        /// </summary>
        private string TryTranslateJoinMember(MemberExpression memberExp)
        {
            if (memberExp.Expression is MemberExpression parentMember
                && parentMember.Expression != null
                && parentMember.Expression.NodeType == ExpressionType.Parameter)
            {
                string navName = parentMember.Member.Name; // "Left" or "Right"
                string propName = memberExp.Member.Name;

                if (navName == "Left")
                    return $"{_tableAlias}.{propName}";

                if (navName == "Right")
                {
                    string rightAlias = _rightAlias ?? "t1";
                    return $"{rightAlias}.{propName}";
                }
            }
            return null;
        }

        private string TranslateMethodCall(MethodCallExpression expression)
        {
            // String methods: Contains, StartsWith, EndsWith
            if (expression.Method.DeclaringType == typeof(string))
            {
                var memberExp = expression.Object as MemberExpression;
                if (memberExp == null || memberExp.Expression.NodeType != ExpressionType.Parameter)
                    goto Fallback;

                string column = $"{_tableAlias}.{memberExp.Member.Name}";
                var argValue = EvaluateConstant(expression.Arguments[0]);

                switch (expression.Method.Name)
                {
                    case "Contains":
                        return $"{column} LIKE {AddParameter($"%{argValue}%")}";
                    case "StartsWith":
                        return $"{column} LIKE {AddParameter($"{argValue}%")}";
                    case "EndsWith":
                        return $"{column} LIKE {AddParameter($"%{argValue}")}";
                }
            }

        Fallback:
            try
            {
                var value = Expression.Lambda(expression).Compile().DynamicInvoke();
                return AddParameter(value);
            }
            catch (Exception ex)
            {
                throw new NotSupportedException($"Method call '{expression.Method}' is not supported in WHERE clause.", ex);
            }
        }

        private string TranslateKeySelector(Expression expression)
        {
            if (expression.NodeType == ExpressionType.MemberAccess)
            {
                var memberExp = (MemberExpression)expression;
                if (memberExp.Expression != null && memberExp.Expression.NodeType == ExpressionType.Parameter)
                    return $"{_tableAlias}.{memberExp.Member.Name}";

                var joinColumn = TryTranslateJoinMember(memberExp);
                if (joinColumn != null)
                    return joinColumn;
            }

            if (expression.NodeType == ExpressionType.Convert)
            {
                var unaryExp = (UnaryExpression)expression;
                return TranslateKeySelector(unaryExp.Operand);
            }

            throw new NotSupportedException($"Key selector expression '{expression}' is not supported for ORDER BY.");
        }

        private string AddParameter(object value)
        {
            string paramName = $"@param{_paramIndex}";
            _result.Parameters.Add(value ?? DBNull.Value);
            _paramIndex++;
            return paramName;
        }

        private LambdaExpression GetLambda(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Quote)
                return (LambdaExpression)((UnaryExpression)expression).Operand;
            if (expression.NodeType == ExpressionType.Lambda)
                return (LambdaExpression)expression;
            throw new ArgumentException($"Expected a lambda expression, got {expression.NodeType}");
        }

        private object EvaluateConstant(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Constant)
                return ((ConstantExpression)expression).Value;

            try
            {
                return Expression.Lambda(expression).Compile().DynamicInvoke();
            }
            catch
            {
                throw new NotSupportedException($"Could not evaluate expression '{expression}' to a constant value.");
            }
        }

        /// <summary>
        /// Builds the final SQL query string from the accumulated translation result.
        /// </summary>
        public string BuildSql(TranslationResult result)
        {
            var sql = new StringBuilder();

            // SELECT clause
            if (result.IsCountQuery || result.IsAnyQuery)
            {
                sql.Append("SELECT COUNT(1) AS RecordCount");
            }
            else
            {
                sql.Append($"SELECT {result.SelectClause}");
            }

            // FROM clause
            sql.Append($" FROM {result.FromClause}");

            // JOIN clauses
            foreach (var join in result.JoinClauses)
            {
                sql.Append($" {join.JoinType} {join.RightTable} {join.RightAlias} ON {join.LeftKey} = {join.RightKey}");
            }

            // WHERE clause
            if (!string.IsNullOrEmpty(result.WhereClause))
            {
                sql.Append($" WHERE {result.WhereClause}");
            }

            // ORDER BY clause (required for OFFSET/FETCH)
            if (result.IsAnyQuery)
            {
                // Any only needs existence check, no ordering needed
            }
            else if (result.IsFirstQuery || result.IsFirstOrDefaultQuery || result.IsSingleQuery || result.IsSingleOrDefaultQuery)
            {
                if (string.IsNullOrEmpty(result.OrderByClause))
                    result.OrderByClause = $"{_tableAlias}.{GetPrimaryKeyOrFirstColumn()} ASC";
                sql.Append($" ORDER BY {result.OrderByClause}");
            }
            else if (!string.IsNullOrEmpty(result.OrderByClause) || result.SkipCount.HasValue || result.TakeCount.HasValue)
            {
                if (string.IsNullOrEmpty(result.OrderByClause))
                    result.OrderByClause = $"{_tableAlias}.{GetPrimaryKeyOrFirstColumn()} ASC";
                sql.Append($" ORDER BY {result.OrderByClause}");
            }

            // OFFSET / FETCH (SQL Server paging)
            if (result.SkipCount.HasValue || result.TakeCount.HasValue)
            {
                sql.Append($" OFFSET {result.SkipCount ?? 0} ROWS");

                if (result.TakeCount.HasValue)
                {
                    if (result.IsSingleQuery || result.IsSingleOrDefaultQuery)
                        sql.Append(" FETCH NEXT 2 ROWS ONLY"); // Fetch 2 to validate single
                    else
                        sql.Append($" FETCH NEXT {result.TakeCount.Value} ROWS ONLY");
                }
            }
            else if (result.IsFirstQuery || result.IsFirstOrDefaultQuery)
            {
                // Use TOP 1 for First/FirstOrDefault when no paging
                if (result.SkipCount.HasValue == false)
                {
                    // Rebuild with TOP 1
                    string currentSql = sql.ToString();
                    if (currentSql.StartsWith("SELECT "))
                    {
                        sql.Clear();
                        sql.Append("SELECT TOP 1 ");
                        sql.Append(currentSql.Substring(7)); // Skip "SELECT "
                    }
                }
            }
            else if (result.IsSingleQuery || result.IsSingleOrDefaultQuery)
            {
                // Use TOP 2 for Single/SingleOrDefault when no paging (fetch 2 to detect more than one element)
                if (result.SkipCount.HasValue == false)
                {
                    string currentSql = sql.ToString();
                    if (currentSql.StartsWith("SELECT "))
                    {
                        sql.Clear();
                        sql.Append("SELECT TOP 2 ");
                        sql.Append(currentSql.Substring(7)); // Skip "SELECT "
                    }
                }
            }

            return sql.ToString();
        }

        private string _primaryKeyFallback = "Id";

        internal void SetPrimaryKeyFallback(string primaryKeyName)
        {
            if (!string.IsNullOrEmpty(primaryKeyName))
                _primaryKeyFallback = primaryKeyName;
        }

        private string GetPrimaryKeyOrFirstColumn()
        {
            return _primaryKeyFallback;
        }

        internal TranslationResult GetTranslationResult()
        {
            return _result;
        }

        /// <summary>
        /// Convenience method: translate an expression and build the SQL in one call.
        /// </summary>
        public (string Sql, List<object> Parameters) TranslateAndBuild(Expression expression)
        {
            var result = Translate(expression);
            string sql = BuildSql(result);
            return (sql, result.Parameters);
        }
    }
}
