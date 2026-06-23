using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DBTools.Abstractions;
using DBTools.Core;
using DBTools.Mapping;

namespace DBTools.Linq
{
    public class AsyncDbQueryProvider<TModel> : IQueryProvider where TModel : class
    {
        private readonly AsyncSqlClient _client;
        private readonly IDbProvider _dbProvider;
        private readonly string _tableName;
        private readonly string _primaryKeyName;
        private readonly EntityMapping _mapping;

        public AsyncDbQueryProvider(
            AsyncSqlClient client,
            IDbProvider provider,
            string tableName,
            string primaryKeyName)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _dbProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _primaryKeyName = primaryKeyName ?? "";
            _mapping = EntityMappingResolver.Resolve<TModel>();
        }

        public IDbProvider DbProvider => _dbProvider;

        public IQueryable CreateQuery(Expression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var elementType = expression.Type.GetGenericArguments().FirstOrDefault()
                ?? throw new InvalidOperationException("Could not determine element type from expression.");

            try
            {
                var queryType = typeof(AsyncDbQuery<>).MakeGenericType(elementType);
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
                var queryType = typeof(AsyncDbQuery<>).MakeGenericType(typeof(TElement));
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
            // CAUTION: This blocks on async execution (.Result) and may deadlock
            // in UI/ASP.NET contexts with a synchronization context.
            // Use ToListAsync() in async contexts to avoid this.
            var translator = new DbExpressionTranslator(_tableName, "t0", null, _dbProvider);

            if (!string.IsNullOrEmpty(_primaryKeyName))
                translator.SetPrimaryKeyFallback(_primaryKeyName);

            var (sql, parameters) = translator.TranslateAndBuild(expression);
            var result = translator.GetTranslationResult();

            if (result.IsCountQuery)
            {
                var dt = _client.SelectAsync("COUNT(1) AS RecordCount", _tableName, "", parameters.ToArray()).Result;
                int count = 0;
                if (dt != null && dt.Rows.Count > 0)
                    count = Convert.ToInt32(dt.Rows[0]["RecordCount"]);
                return (TResult)(object)count;
            }

            if (result.IsAnyQuery)
            {
                var dt = _client.SelectAsync("COUNT(1) AS RecordCount", _tableName, "", parameters.ToArray()).Result;
                int count = 0;
                if (dt != null && dt.Rows.Count > 0)
                    count = Convert.ToInt32(dt.Rows[0]["RecordCount"]);
                return (TResult)(object)(count > 0);
            }

            if (result.IsFirstQuery || result.IsFirstOrDefaultQuery)
            {
                var dataView = _client.SelectAsync("*", _tableName, "", parameters.ToArray()).Result;
                var models = MapDataViewToModels(dataView);
                var first = models.FirstOrDefault();
                if (first == null)
                {
                    if (result.IsFirstQuery)
                        throw new InvalidOperationException("Sequence contains no elements.");
                    return default;
                }
                return (TResult)(object)first;
            }

            throw new InvalidOperationException(
                "Async queries should be executed via ToListAsync() or enumeration, not Execute<TResult>.");
        }

        internal async Task<List<TModel>> ExecuteSequenceAsync(Expression expression, CancellationToken ct = default)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var translator = new DbExpressionTranslator(_tableName, "t0", null, _dbProvider);

            if (!string.IsNullOrEmpty(_primaryKeyName))
                translator.SetPrimaryKeyFallback(_primaryKeyName);

            var (sql, parameters) = translator.TranslateAndBuild(expression);
            var result = translator.GetTranslationResult();

            if (result.IsCountQuery || result.IsAnyQuery)
            {
                var dt = await _client.SelectRawAsync(sql, parameters.ToArray(), ct).ConfigureAwait(false);
                int count = 0;
                if (dt != null && dt.Rows.Count > 0)
                    count = Convert.ToInt32(dt.Rows[0]["RecordCount"]);

                if (result.IsAnyQuery)
                    return new List<TModel> { (TModel)(object)(count > 0) };

                return new List<TModel> { (TModel)(object)count };
            }

            var dataView = await _client.SelectRawAsync(sql, parameters.ToArray(), ct).ConfigureAwait(false);
            return MapDataViewToModels(dataView);
        }

        private List<TModel> MapDataViewToModels(DataTable dt)
        {
            var models = new List<TModel>();
            if (dt == null || dt.Rows.Count == 0) return models;

            // When a ModelFactory is configured on the entity mapping, hand each row
            // to it (via IDataRecord). This is the supported path for entities with
            // private constructors or factory-only construction.
            if (_mapping.ModelFactory != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    var record = new DataRowRecordAdapter(row);
                    var created = _mapping.ModelFactory(record);
                    if (created is TModel typed)
                        models.Add(typed);
                    else
                        models.Add((TModel)created);
                }
                return models;
            }

            var columnLookup = _mapping.Properties
                .Where(p => !p.IsNotMapped)
                .ToDictionary(p => p.ColumnName, p => p, StringComparer.OrdinalIgnoreCase);

            foreach (DataRow row in dt.Rows)
            {
                TModel model;
                try
                {
                    // Activator.CreateInstance is used (instead of new TModel()) so the
                    // class does not need the `new()` constraint. This is the fallback
                    // path — preferred path is _mapping.ModelFactory, configured via
                    // EntityBuilder<TModel>.HasModelFactory(...).
                    model = (TModel)Activator.CreateInstance(typeof(TModel));
                }
                catch (MissingMethodException ex)
                {
                    throw new InvalidOperationException(
                        $"Cannot hydrate '{typeof(TModel).FullName}': no public parameterless constructor and no EntityMapping.ModelFactory is configured. " +
                        "Register an IEntityConfiguration<TModel> with HasModelFactory(...) or expose a public parameterless constructor.",
                        ex);
                }
                foreach (DataColumn column in dt.Columns)
                {
                    if (columnLookup.TryGetValue(column.ColumnName, out var propMapping))
                    {
                        try
                        {
                            var value = row[column.ColumnName];
                            if (value != null && value != DBNull.Value)
                            {
                                var property = propMapping.PropertyInfo;
                                if (propMapping.ValueConverter != null)
                                {
                                    value = propMapping.ValueConverter(value);
                                }
                                else if (property.PropertyType != value.GetType())
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
                        catch (Exception)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"MapDataViewToModels: Failed to convert column '{column.ColumnName}' for property '{propMapping.PropertyName}' on type {typeof(TModel).Name}");
                        }
                    }
                }
                models.Add(model);
            }

            return models;
        }
    }
}
