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

namespace DBTools.Context
{
    /// <summary>
    /// Represents a queryable set of entities for a given type within a DbContext.
    /// Provides CRUD operations with optional change tracking integration.
    /// Similar to Entity Framework's DbSet but lighter.
    /// </summary>
    /// <typeparam name="TEntity">The entity type for this set</typeparam>
    public class DbSet<TEntity> where TEntity : class, new()
    {
        private readonly DbContext _context;
        private readonly AsyncSqlClient _client;
        private readonly EntityMapping _mapping;

        /// <summary>
        /// Creates a DbSet bound to a DbContext.
        /// </summary>
        public DbSet(DbContext context, AsyncSqlClient client)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _mapping = EntityMappingResolver.Resolve<TEntity>();
        }

        /// <summary>
        /// Gets the resolved table name for this entity type.
        /// </summary>
        public string TableName => _mapping.TableName;

        #region Tracked CRUD (uses ChangeTracker)

        /// <summary>
        /// Adds an entity to the change tracker for insertion on SaveChanges.
        /// </summary>
        public void Add(TEntity entity)
        {
            _context.Add(entity);
        }

        /// <summary>
        /// Adds multiple entities to the change tracker for insertion on SaveChanges.
        /// </summary>
        public void AddRange(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
                _context.Add(entity);
        }

        /// <summary>
        /// Marks an entity for update on SaveChanges.
        /// </summary>
        public void Update(TEntity entity)
        {
            _context.Update(entity);
        }

        /// <summary>
        /// Marks an entity for deletion on SaveChanges.
        /// </summary>
        public void Remove(TEntity entity)
        {
            _context.Remove(entity);
        }

        #endregion

        #region Direct Async Query Methods (bypass change tracker)

        /// <summary>
        /// Asynchronously retrieves all records from the table.
        /// Applies global query filters.
        /// </summary>
        public async Task<List<TEntity>> ToListAsync(CancellationToken ct = default)
        {
            string filter = GetGlobalFilterClause();
            var dt = await _client.SelectAsync("*", _mapping.TableName, filter, Array.Empty<object>(), ct).ConfigureAwait(false);
            return MapDataTableToList(dt);
        }

        /// <summary>
        /// Asynchronously filters records using a LINQ predicate.
        /// </summary>
        public async Task<List<TEntity>> WhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
        {
            var (whereClause, parameters) = ParsePredicate(predicate);
            string filter = CombineWithGlobalFilter(whereClause);
            var dt = await _client.SelectAsync("*", _mapping.TableName, filter, parameters, ct).ConfigureAwait(false);
            return MapDataTableToList(dt);
        }

        /// <summary>
        /// Asynchronously finds an entity by primary key.
        /// </summary>
        public async Task<TEntity> FindAsync(object keyValue, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_mapping.PrimaryKeyColumn))
                throw new InvalidOperationException($"Entity '{typeof(TEntity).Name}' does not have a primary key defined.");

            string where = $"{_mapping.PrimaryKeyColumn} = @param0";
            string filter = CombineWithGlobalFilter(where);
            var dt = await _client.SelectAsync("*", _mapping.TableName, filter, new object[] { keyValue }, ct).ConfigureAwait(false);
            return MapDataTableToList(dt).FirstOrDefault();
        }

        /// <summary>
        /// Asynchronously gets the first entity matching the predicate, or null.
        /// </summary>
        public async Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
        {
            var results = await WhereAsync(predicate, ct).ConfigureAwait(false);
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Asynchronously checks if any entities match the predicate.
        /// </summary>
        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate = null, CancellationToken ct = default)
        {
            return await CountAsync(predicate, ct).ConfigureAwait(false) > 0;
        }

        /// <summary>
        /// Asynchronously counts entities matching the predicate.
        /// </summary>
        public async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate = null, CancellationToken ct = default)
        {
            string conditions = "";
            object[] parameters = Array.Empty<object>();

            if (predicate != null)
            {
                var (where, parms) = ParsePredicate(predicate);
                conditions = where;
                parameters = parms;
            }

            string filter = CombineWithGlobalFilter(conditions);
            var dt = await _client.SelectAsync("COUNT(1) AS RecordCount", _mapping.TableName, filter, parameters, ct).ConfigureAwait(false);
            if (dt != null && dt.Rows.Count > 0)
                return Convert.ToInt32(dt.Rows[0]["RecordCount"]);
            return 0;
        }

        #endregion

        #region Expression Parsing

        private (string whereClause, object[] parameters) ParsePredicate(Expression<Func<TEntity, bool>> predicate)
        {
            var parameters = new List<object>();
            string where = ParseExpression(predicate.Body, parameters);
            return (where, parameters.ToArray());
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
                    return ParseBinary((BinaryExpression)expression, parameters);

                case ExpressionType.Not:
                    var notExp = (UnaryExpression)expression;
                    return $"NOT ({ParseExpression(notExp.Operand, parameters)})";

                case ExpressionType.MemberAccess:
                    var memberExp = (MemberExpression)expression;
                    if (memberExp.Expression?.NodeType == ExpressionType.Parameter)
                    {
                        var prop = _mapping.Properties.FirstOrDefault(p => p.PropertyName == memberExp.Member.Name);
                        return prop?.ColumnName ?? memberExp.Member.Name;
                    }
                    break;

                case ExpressionType.Constant:
                    parameters.Add(((ConstantExpression)expression).Value ?? DBNull.Value);
                    return $"@param{parameters.Count - 1}";
            }

            try
            {
                var value = Expression.Lambda(expression).Compile().DynamicInvoke();
                parameters.Add(value ?? DBNull.Value);
                return $"@param{parameters.Count - 1}";
            }
            catch (Exception ex)
            {
                throw new NotSupportedException($"Expression '{expression.NodeType}' is not supported.", ex);
            }
        }

        private string ParseBinary(BinaryExpression expression, List<object> parameters)
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
                _ => throw new NotSupportedException($"Operator '{expression.NodeType}' is not supported.")
            };

            return $"{left} {op} {right}";
        }

        #endregion

        #region Mapping and Filters

        private string GetGlobalFilterClause()
        {
            if (_mapping.QueryFilters.Count == 0) return "";
            return string.Join(" AND ", _mapping.QueryFilters);
        }

        private string CombineWithGlobalFilter(string whereClause)
        {
            var globalFilter = GetGlobalFilterClause();
            if (string.IsNullOrEmpty(globalFilter)) return whereClause;
            if (string.IsNullOrEmpty(whereClause)) return globalFilter;
            return $"({whereClause}) AND ({globalFilter})";
        }

        private List<TEntity> MapDataTableToList(DataTable dt)
        {
            var results = new List<TEntity>();
            if (dt == null || dt.Rows.Count == 0) return results;

            var columnLookup = _mapping.Properties
                .Where(p => !p.IsNotMapped)
                .ToDictionary(p => p.ColumnName, p => p.PropertyInfo, StringComparer.OrdinalIgnoreCase);

            foreach (DataRow row in dt.Rows)
            {
                var entity = new TEntity();
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
                                property.SetValue(entity, value);
                            }
                        }
                        catch { /* Skip conversion errors */ }
                    }
                }
                results.Add(entity);
            }

            return results;
        }

        #endregion
    }
}
