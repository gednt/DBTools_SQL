using DBTools.Abstractions;
using DBTools.Core;
using DBTools.Mapping;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DBTools.Controllers
{
    /// <summary>
    /// Async-first generic helper for LINQ-style database manipulation.
    /// Provides async Insert, Update, Delete, and Select operations.
    /// Uses EntityMappingResolver for attribute-based column/table mapping.
    /// </summary>
    /// <typeparam name="TModel">The model type that represents a database table record.</typeparam>
    public class AsyncLinqHelper<TModel> where TModel : class, new()
    {
        private readonly AsyncSqlClient _client;
        private readonly string _tableName;
        private readonly string _primaryKeyName;
        private readonly bool _autoIncrement;
        private readonly EntityMapping _mapping;

        /// <summary>
        /// Creates an AsyncLinqHelper with an AsyncSqlClient and table configuration.
        /// Table name and column mappings are resolved from attributes if present.
        /// </summary>
        public AsyncLinqHelper(AsyncSqlClient client, string tableName = null, string primaryKeyName = null, bool autoIncrement = true)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _mapping = EntityMappingResolver.Resolve<TModel>();
            _tableName = tableName ?? _mapping.TableName;
            _primaryKeyName = primaryKeyName ?? _mapping.PrimaryKeyColumn ?? "";
            _autoIncrement = autoIncrement;
        }

        /// <summary>
        /// Gets the underlying async client.
        /// </summary>
        public AsyncSqlClient Client => _client;

        /// <summary>
        /// Gets the resolved table name.
        /// </summary>
        public string TableName => _tableName;

        /// <summary>
        /// Gets the resolved primary key column name.
        /// </summary>
        public string PrimaryKeyName => _primaryKeyName;

        #region Async CRUD Operations

        /// <summary>
        /// Asynchronously selects records matching the WHERE clause.
        /// </summary>
        public async Task<IEnumerable<TModel>> SelectAsync(string conditions = "", object[] parameters = null, CancellationToken ct = default)
        {
            var dt = await _client.SelectAsync("*", _tableName, conditions, parameters ?? Array.Empty<object>(), ct).ConfigureAwait(false);
            return MapDataTableToModels(dt);
        }

        /// <summary>
        /// Asynchronously retrieves all records from the table.
        /// </summary>
        public Task<IEnumerable<TModel>> AllAsync(CancellationToken ct = default)
        {
            return SelectAsync("", null, ct);
        }

        /// <summary>
        /// Asynchronously retrieves the first record matching the predicate.
        /// </summary>
        public async Task<TModel> FirstOrDefaultAsync(Expression<Func<TModel, bool>> predicate, CancellationToken ct = default)
        {
            var result = ParseWhereExpression(predicate);
            var items = await SelectAsync(result.WhereClause, result.Parameters.ToArray(), ct).ConfigureAwait(false);
            return items.FirstOrDefault();
        }

        /// <summary>
        /// Asynchronously retrieves the first record matching conditions.
        /// </summary>
        public async Task<TModel> FirstOrDefaultAsync(string conditions = "", object[] parameters = null, CancellationToken ct = default)
        {
            var items = await SelectAsync(conditions, parameters, ct).ConfigureAwait(false);
            return items.FirstOrDefault();
        }

        /// <summary>
        /// Asynchronously filters records using a LINQ lambda expression.
        /// </summary>
        public async Task<List<TModel>> WhereAsync(Expression<Func<TModel, bool>> predicate, CancellationToken ct = default)
        {
            var result = ParseWhereExpression(predicate);
            var items = await SelectAsync(result.WhereClause, result.Parameters.ToArray(), ct).ConfigureAwait(false);
            return items.ToList();
        }

        /// <summary>
        /// Asynchronously counts records matching the predicate.
        /// </summary>
        public async Task<int> CountAsync(Expression<Func<TModel, bool>> predicate = null, CancellationToken ct = default)
        {
            string conditions = "";
            object[] parameters = Array.Empty<object>();

            if (predicate != null)
            {
                var result = ParseWhereExpression(predicate);
                conditions = result.WhereClause;
                parameters = result.Parameters.ToArray();
            }

            var dt = await _client.SelectAsync("COUNT(1) AS RecordCount", _tableName, conditions, parameters, ct).ConfigureAwait(false);
            if (dt != null && dt.Rows.Count > 0)
                return Convert.ToInt32(dt.Rows[0]["RecordCount"]);
            return 0;
        }

        /// <summary>
        /// Asynchronously checks if any records match the predicate.
        /// </summary>
        public async Task<bool> AnyAsync(Expression<Func<TModel, bool>> predicate = null, CancellationToken ct = default)
        {
            return await CountAsync(predicate, ct).ConfigureAwait(false) > 0;
        }

        /// <summary>
        /// Asynchronously finds a record by primary key.
        /// </summary>
        public async Task<TModel> FindAsync(object primaryKeyValue, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_primaryKeyName))
                throw new InvalidOperationException("Primary key must be specified to use FindAsync.");

            var items = await SelectAsync($"{_primaryKeyName} = @param0", new object[] { primaryKeyValue }, ct).ConfigureAwait(false);
            return items.FirstOrDefault();
        }

        /// <summary>
        /// Asynchronously inserts a model into the database.
        /// </summary>
        public async Task<bool> InsertAsync(TModel model, CancellationToken ct = default)
        {
            var (fields, values) = ExtractFieldsAndValues(model, excludePrimaryKey: _autoIncrement);
            return await _client.InsertAsync(fields, _tableName, values, _primaryKeyName, _autoIncrement, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously inserts a model and returns it after retrieval (with auto-generated values).
        /// </summary>
        public async Task<TModel> InsertAndFindAsync<TProperty>(TModel model, Expression<Func<TModel, TProperty>> findByProperty, CancellationToken ct = default)
        {
            if (!await InsertAsync(model, ct).ConfigureAwait(false))
                return null;

            var propInfo = GetPropertyInfo(findByProperty);
            var value = propInfo.GetValue(model);
            var items = await SelectAsync($"{GetColumnName(propInfo)} = @param0", new object[] { value }, ct).ConfigureAwait(false);
            return items.FirstOrDefault();
        }

        /// <summary>
        /// Asynchronously updates records matching the predicate.
        /// </summary>
        public async Task<bool> UpdateAsync(TModel model, Expression<Func<TModel, bool>> predicate, CancellationToken ct = default)
        {
            var result = ParseWhereExpression(predicate);
            if (string.IsNullOrEmpty(result.WhereClause))
                throw new ArgumentException("WHERE clause is required for UPDATE operations.");

            var (fields, values) = ExtractFieldsAndValues(model, excludePrimaryKey: false);
            var stringValues = values.Select(v => v?.ToString() ?? "").ToArray();
            return await _client.UpdateAsync(fields, _tableName, stringValues, result.WhereClause, result.Parameters.ToArray(), ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously saves changes to an entity by primary key.
        /// </summary>
        public async Task<bool> SaveChangesAsync(TModel entity, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_primaryKeyName))
                throw new InvalidOperationException("Primary key must be specified to use SaveChangesAsync.");

            var pkProperty = typeof(TModel).GetProperty(_primaryKeyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (pkProperty == null)
                throw new InvalidOperationException($"Primary key property '{_primaryKeyName}' not found on type {typeof(TModel).Name}.");

            var pkValue = pkProperty.GetValue(entity);
            if (pkValue == null)
                throw new InvalidOperationException("Primary key value cannot be null for SaveChangesAsync.");

            var (fields, values) = ExtractFieldsAndValues(entity, excludePrimaryKey: false);
            var stringValues = values.Select(v => v?.ToString() ?? "").ToArray();
            return await _client.UpdateAsync(fields, _tableName, stringValues, $"{_primaryKeyName} = @whereParam0", new object[] { pkValue }, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously deletes records matching the predicate.
        /// </summary>
        public async Task<bool> RemoveAsync(Expression<Func<TModel, bool>> predicate, CancellationToken ct = default)
        {
            var result = ParseWhereExpression(predicate);
            if (string.IsNullOrEmpty(result.WhereClause))
                throw new ArgumentException("WHERE clause is required for DELETE operations.");

            return await _client.DeleteAsync(_tableName, result.WhereClause, result.Parameters.ToArray(), ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously inserts multiple records in a transaction.
        /// </summary>
        public async Task<bool> InsertRangeAsync(IEnumerable<TModel> models, CancellationToken ct = default)
        {
            await using var tx = await _client.BeginTransactionAsync(ct).ConfigureAwait(false);
            try
            {
                foreach (var model in models)
                {
                    var (fields, values) = ExtractFieldsAndValues(model, excludePrimaryKey: _autoIncrement);

                    // Remove PK from fields/values if needed
                    var paramPlaceholders = string.Join(",", fields.Select((_, i) => $"@param{i}"));
                    var fieldList = string.Join(",", fields);
                    string sql = $"INSERT INTO {_tableName}({fieldList}) VALUES({paramPlaceholders})";

                    if (!await _client.ExecuteInTransactionAsync(tx, sql, values, ct).ConfigureAwait(false))
                    {
                        await tx.RollbackAsync(ct).ConfigureAwait(false);
                        return false;
                    }
                }

                await tx.CommitAsync(ct).ConfigureAwait(false);
                return true;
            }
            catch
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                return false;
            }
        }

        #endregion

        #region Expression Parsing (reuses same logic as LinqHelper)

        protected WhereClauseResult ParseWhereExpression(Expression<Func<TModel, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var parameters = new List<object>();
            string whereClause = ParseExpression(predicate.Body, parameters);
            return new WhereClauseResult { WhereClause = whereClause, Parameters = parameters };
        }

        private string ParseExpression(Expression expression, List<object> parameters)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.AndAlso:
                    var andExp = (BinaryExpression)expression;
                    return $"({ParseExpression(andExp.Left, parameters)}) AND ({ParseExpression(andExp.Right, parameters)})";

                case ExpressionType.OrElse:
                    var orExp = (BinaryExpression)expression;
                    return $"({ParseExpression(orExp.Left, parameters)}) OR ({ParseExpression(orExp.Right, parameters)})";

                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    return ParseBinaryExpression((BinaryExpression)expression, parameters);

                case ExpressionType.Not:
                    var notExp = (UnaryExpression)expression;
                    return $"NOT ({ParseExpression(notExp.Operand, parameters)})";

                case ExpressionType.MemberAccess:
                    var memberExp = (MemberExpression)expression;
                    if (memberExp.Expression != null && memberExp.Expression.NodeType == ExpressionType.Parameter)
                        return GetColumnName(memberExp.Member as PropertyInfo ?? typeof(TModel).GetProperty(memberExp.Member.Name));
                    break;

                case ExpressionType.Constant:
                    var constExp = (ConstantExpression)expression;
                    string paramName = $"@param{parameters.Count}";
                    parameters.Add(constExp.Value ?? DBNull.Value);
                    return paramName;
            }

            try
            {
                var value = Expression.Lambda(expression).Compile().DynamicInvoke();
                string pName = $"@param{parameters.Count}";
                parameters.Add(value ?? DBNull.Value);
                return pName;
            }
            catch (Exception ex)
            {
                throw new NotSupportedException($"Expression type '{expression.NodeType}' is not supported.", ex);
            }
        }

        private string ParseBinaryExpression(BinaryExpression expression, List<object> parameters)
        {
            string left = ParseExpression(expression.Left, parameters);
            string right = ParseExpression(expression.Right, parameters);

            string op = expression.NodeType switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "<>",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                _ => throw new NotSupportedException($"Binary operator '{expression.NodeType}' is not supported.")
            };

            return $"{left} {op} {right}";
        }

        #endregion

        #region Mapping Helpers

        private (string[] fields, object[] values) ExtractFieldsAndValues(TModel model, bool excludePrimaryKey)
        {
            var fields = new List<string>();
            var values = new List<object>();

            foreach (var prop in _mapping.Properties)
            {
                if (prop.IsNotMapped) continue;
                if (excludePrimaryKey && prop.IsPrimaryKey) continue;

                fields.Add(prop.ColumnName);
                var value = prop.PropertyInfo.GetValue(model);
                values.Add(value ?? DBNull.Value);
            }

            return (fields.ToArray(), values.ToArray());
        }

        private string GetColumnName(PropertyInfo propInfo)
        {
            if (propInfo == null) return null;
            var mapped = _mapping.Properties.FirstOrDefault(p => p.PropertyInfo.Name == propInfo.Name);
            return mapped?.ColumnName ?? propInfo.Name;
        }

        private PropertyInfo GetPropertyInfo<TProperty>(Expression<Func<TModel, TProperty>> selector)
        {
            if (selector.Body is MemberExpression memberExpr)
                return memberExpr.Member as PropertyInfo;
            if (selector.Body is UnaryExpression unary && unary.Operand is MemberExpression member)
                return member.Member as PropertyInfo;
            throw new ArgumentException("Expression must be a property access expression.");
        }

        protected IEnumerable<TModel> MapDataTableToModels(DataTable dt)
        {
            var models = new List<TModel>();
            if (dt == null || dt.Rows.Count == 0) return models;

            var columnLookup = _mapping.Properties
                .Where(p => !p.IsNotMapped)
                .ToDictionary(p => p.ColumnName, p => p.PropertyInfo, StringComparer.OrdinalIgnoreCase);

            foreach (DataRow row in dt.Rows)
            {
                var model = new TModel();
                foreach (DataColumn column in dt.Columns)
                {
                    if (columnLookup.TryGetValue(column.ColumnName, out var property))
                    {
                        try
                        {
                            var value = row[column.ColumnName];
                            if (value != null && value != DBNull.Value)
                            {
                                if (property.PropertyType != value.GetType())
                                {
                                    if (property.PropertyType.IsGenericType &&
                                        property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                    {
                                        var underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
                                        value = Convert.ChangeType(value, underlyingType);
                                    }
                                    else
                                    {
                                        value = Convert.ChangeType(value, property.PropertyType);
                                    }
                                }
                                property.SetValue(model, value);
                            }
                        }
                        catch { /* Skip conversion errors */ }
                    }
                }
                models.Add(model);
            }

            return models;
        }

        #endregion
    }
}
