using DBTools.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DBTools.Caching
{
    /// <summary>
    /// Query interceptor that provides transparent result caching.
    /// Caches SELECT query results and automatically invalidates cache entries
    /// when write operations (Insert/Update/Delete) target the same table.
    /// </summary>
    public class CachingInterceptor : IQueryInterceptor, IAsyncQueryInterceptor
    {
        private readonly IQueryCache _cache;
        private readonly QueryCacheOptions _options;

        /// <summary>
        /// Creates a new CachingInterceptor.
        /// </summary>
        /// <param name="cache">The query cache instance.</param>
        /// <param name="options">Cache configuration options.</param>
        public CachingInterceptor(IQueryCache cache, QueryCacheOptions options)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc/>
        public void BeforeExecute(QueryInterceptionContext context)
        {
            if (context.OperationType == QueryOperationType.Select)
            {
                var cacheKey = CacheKeyGenerator.GenerateKey(context.Sql, context.Parameters, context.TableName);
                var entry = _cache.Get(cacheKey);

                if (entry != null)
                {
                    // Cache hit - suppress actual query execution
                    context.IsSuppressed = true;
                    context.Properties["CachedResult"] = entry.Value;
                    context.Properties["CacheKey"] = cacheKey;
                }
                else
                {
                    // Cache miss - store key for AfterExecute to cache the result
                    context.Properties["CacheKey"] = cacheKey;
                }
            }
        }

        /// <inheritdoc/>
        public void AfterExecute(QueryInterceptionContext context)
        {
            if (context.OperationType == QueryOperationType.Select)
            {
                // Cache the result if it was not already cached
                if (!context.IsSuppressed && context.Properties.ContainsKey("CacheKey") && context.Properties.ContainsKey("QueryResult"))
                {
                    var cacheKey = (string)context.Properties["CacheKey"];
                    var result = context.Properties["QueryResult"];

                    if (_cache is MemoryQueryCache memoryCache)
                    {
                        memoryCache.Set(cacheKey, result, context.TableName, _options.DefaultExpiration);
                    }
                    else
                    {
                        _cache.Set(cacheKey, result, _options.DefaultExpiration);
                    }
                }
            }
            else if (_options.EnableAutoInvalidation &&
                     (context.OperationType == QueryOperationType.Insert ||
                      context.OperationType == QueryOperationType.Update ||
                      context.OperationType == QueryOperationType.Delete))
            {
                // Invalidate cache for the affected table
                if (!string.IsNullOrEmpty(context.TableName))
                {
                    _cache.InvalidateByTable(context.TableName);
                }
            }
        }

        /// <inheritdoc/>
        public void OnError(QueryInterceptionContext context, Exception exception)
        {
            // No-op: do not cache errors
        }

        /// <inheritdoc/>
        public Task BeforeExecuteAsync(QueryInterceptionContext context, CancellationToken ct = default)
        {
            BeforeExecute(context);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task AfterExecuteAsync(QueryInterceptionContext context, CancellationToken ct = default)
        {
            AfterExecute(context);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnErrorAsync(QueryInterceptionContext context, Exception exception, CancellationToken ct = default)
        {
            OnError(context, exception);
            return Task.CompletedTask;
        }
    }
}
