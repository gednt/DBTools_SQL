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
    public class AsyncDbQueryProvider<TModel> : IQueryProvider where TModel : class, new()
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
                        catch (Exception)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"MapDataViewToModels: Failed to convert column '{column.ColumnName}' for property '{property.Name}' on type {typeof(TModel).Name}");
                        }
                    }
                }
                models.Add(model);
            }

            return models;
        }
    }
}
