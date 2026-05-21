using DBTools.Abstractions;
using DBTools.Configuration;
using DBTools.Core;
using DBTools.Mapping;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DBTools.Context
{
    /// <summary>
    /// A DbContext represents a session with the database and provides opt-in change tracking.
    /// Similar to Entity Framework's DbContext but lighter and more flexible for existing systems.
    /// 
    /// Usage:
    /// 1. Subclass DbContext and add DbSet&lt;T&gt; properties
    /// 2. Override OnModelCreating for fluent configuration
    /// 3. Use SaveChanges/SaveChangesAsync to flush tracked changes
    /// </summary>
    public abstract class DbContext : IDisposable, IAsyncDisposable
    {
        private readonly AsyncSqlClient _client;
        private readonly ChangeTracker _changeTracker;
        private readonly List<IQueryInterceptor> _interceptors = new List<IQueryInterceptor>();
        private bool _disposed = false;

        /// <summary>
        /// Gets the underlying async client for direct database access.
        /// </summary>
        public AsyncSqlClient Database => _client;

        /// <summary>
        /// Gets the change tracker for this context.
        /// </summary>
        public ChangeTracker ChangeTracker => _changeTracker;

        /// <summary>
        /// Creates a DbContext with default configuration (reads from config.json).
        /// </summary>
        protected DbContext()
        {
            _client = new AsyncSqlClient();
            _changeTracker = new ChangeTracker();
            InitializeDbSets();
            OnModelCreating(new ModelBuilder());
        }

        /// <summary>
        /// Creates a DbContext with explicit options.
        /// </summary>
        protected DbContext(DbToolsOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            IDbProvider provider = options.Provider switch
            {
                DatabaseProvider.SqlServer => new Providers.SqlServerProvider(),
                DatabaseProvider.PostgreSQL => new Providers.PostgresProvider(),
                DatabaseProvider.MySQL => new Providers.MySqlProvider(),
                DatabaseProvider.SQLite => new Providers.SqliteProvider(),
                _ => new Providers.SqlServerProvider()
            };

            var validator = new SqlValidator();
            var queryBuilder = new SqlQueryBuilder(validator);
            var config = new OptionsDbConfiguration(options);

            _client = new AsyncSqlClient(config, validator, queryBuilder, provider);
            _changeTracker = new ChangeTracker();

            foreach (var interceptor in options.Interceptors)
                _client.AddInterceptor(interceptor);
            foreach (var asyncInterceptor in options.AsyncInterceptors)
                _client.AddInterceptor(asyncInterceptor);

            InitializeDbSets();
            OnModelCreating(new ModelBuilder());
        }

        /// <summary>
        /// Creates a DbContext with an existing AsyncSqlClient.
        /// </summary>
        protected DbContext(AsyncSqlClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _changeTracker = new ChangeTracker();
            InitializeDbSets();
            OnModelCreating(new ModelBuilder());
        }

        /// <summary>
        /// Override to configure entity mappings using the fluent API.
        /// Called once during context initialization.
        /// </summary>
        protected virtual void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        /// <summary>
        /// Saves all tracked changes to the database in a single transaction.
        /// Returns the number of state entries written to the database.
        /// </summary>
        public int SaveChanges()
        {
            return SaveChangesAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously saves all tracked changes to the database in a single transaction.
        /// Returns the number of state entries written to the database.
        /// </summary>
        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            var entries = _changeTracker.GetPendingEntries();
            if (entries.Count == 0) return 0;

            int count = 0;
            await using var tx = await _client.BeginTransactionAsync(ct).ConfigureAwait(false);

            try
            {
                foreach (var entry in entries)
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (await ExecuteInsertAsync(entry, tx, ct).ConfigureAwait(false))
                                count++;
                            break;
                        case EntityState.Modified:
                            if (await ExecuteUpdateAsync(entry, tx, ct).ConfigureAwait(false))
                                count++;
                            break;
                        case EntityState.Deleted:
                            if (await ExecuteDeleteAsync(entry, tx, ct).ConfigureAwait(false))
                                count++;
                            break;
                    }
                }

                await tx.CommitAsync(ct).ConfigureAwait(false);
                _changeTracker.AcceptChanges();
                return count;
            }
            catch
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Attaches an entity to the context as Unchanged.
        /// </summary>
        public void Attach<TEntity>(TEntity entity) where TEntity : class
        {
            _changeTracker.Track(entity, EntityState.Unchanged);
        }

        /// <summary>
        /// Marks an entity for insertion.
        /// </summary>
        public void Add<TEntity>(TEntity entity) where TEntity : class
        {
            _changeTracker.Track(entity, EntityState.Added);
        }

        /// <summary>
        /// Marks an entity for update.
        /// </summary>
        public void Update<TEntity>(TEntity entity) where TEntity : class
        {
            _changeTracker.Track(entity, EntityState.Modified);
        }

        /// <summary>
        /// Marks an entity for deletion.
        /// </summary>
        public void Remove<TEntity>(TEntity entity) where TEntity : class
        {
            _changeTracker.Track(entity, EntityState.Deleted);
        }

        /// <summary>
        /// Begins a database transaction.
        /// </summary>
        public Task<DbToolsTransaction> BeginTransactionAsync(CancellationToken ct = default)
        {
            return _client.BeginTransactionAsync(ct);
        }

        #region Internal Execution

        private async Task<bool> ExecuteInsertAsync(ChangeTrackerEntry entry, DbToolsTransaction tx, CancellationToken ct)
        {
            var mapping = EntityMappingResolver.Resolve(entry.EntityType);
            var fields = new List<string>();
            var values = new List<object>();

            foreach (var prop in mapping.Properties)
            {
                if (prop.IsNotMapped) continue;
                if (prop.IsPrimaryKey && mapping.PrimaryKeyAutoIncrement) continue;
                if (prop.GeneratedOption == DatabaseGeneratedOption.Identity) continue;
                if (prop.GeneratedOption == DatabaseGeneratedOption.Computed) continue;

                fields.Add(prop.ColumnName);
                values.Add(prop.PropertyInfo.GetValue(entry.Entity) ?? DBNull.Value);
            }

            var paramPlaceholders = string.Join(",", fields.Select((_, i) => $"@param{i}"));
            var fieldList = string.Join(",", fields);
            string sql = $"INSERT INTO {mapping.TableName}({fieldList}) VALUES({paramPlaceholders})";

            return await _client.ExecuteInTransactionAsync(tx, sql, values.ToArray(), ct).ConfigureAwait(false);
        }

        private async Task<bool> ExecuteUpdateAsync(ChangeTrackerEntry entry, DbToolsTransaction tx, CancellationToken ct)
        {
            var mapping = EntityMappingResolver.Resolve(entry.EntityType);
            if (string.IsNullOrEmpty(mapping.PrimaryKeyColumn))
                throw new InvalidOperationException($"Cannot update entity '{entry.EntityType.Name}' without a primary key.");

            var setClauses = new List<string>();
            var parameters = new List<object>();
            int paramIdx = 0;

            foreach (var prop in mapping.Properties)
            {
                if (prop.IsNotMapped) continue;
                if (prop.IsPrimaryKey) continue;
                if (prop.GeneratedOption == DatabaseGeneratedOption.Identity) continue;
                if (prop.GeneratedOption == DatabaseGeneratedOption.Computed) continue;

                setClauses.Add($"{prop.ColumnName} = @param{paramIdx}");
                parameters.Add(prop.PropertyInfo.GetValue(entry.Entity) ?? DBNull.Value);
                paramIdx++;
            }

            // Get PK value for WHERE clause
            var pkProp = mapping.Properties.First(p => p.IsPrimaryKey);
            var pkValue = pkProp.PropertyInfo.GetValue(entry.Entity);
            parameters.Add(pkValue ?? DBNull.Value);

            string sql = $"UPDATE {mapping.TableName} SET {string.Join(", ", setClauses)} WHERE {mapping.PrimaryKeyColumn} = @param{paramIdx}";
            return await _client.ExecuteInTransactionAsync(tx, sql, parameters.ToArray(), ct).ConfigureAwait(false);
        }

        private async Task<bool> ExecuteDeleteAsync(ChangeTrackerEntry entry, DbToolsTransaction tx, CancellationToken ct)
        {
            var mapping = EntityMappingResolver.Resolve(entry.EntityType);
            if (string.IsNullOrEmpty(mapping.PrimaryKeyColumn))
                throw new InvalidOperationException($"Cannot delete entity '{entry.EntityType.Name}' without a primary key.");

            var pkProp = mapping.Properties.First(p => p.IsPrimaryKey);
            var pkValue = pkProp.PropertyInfo.GetValue(entry.Entity);

            string sql = $"DELETE FROM {mapping.TableName} WHERE {mapping.PrimaryKeyColumn} = @param0";
            return await _client.ExecuteInTransactionAsync(tx, sql, new object[] { pkValue ?? DBNull.Value }, ct).ConfigureAwait(false);
        }

        #endregion

        #region DbSet Initialization

        private void InitializeDbSets()
        {
            var dbSetProperties = GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType &&
                            p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

            foreach (var prop in dbSetProperties)
            {
                var entityType = prop.PropertyType.GetGenericArguments()[0];
                var dbSetType = typeof(DbSet<>).MakeGenericType(entityType);
                var dbSet = Activator.CreateInstance(dbSetType, this, _client);
                prop.SetValue(this, dbSet);
            }
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _client?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            if (_client != null)
                await _client.DisposeAsync().ConfigureAwait(false);
        }

        #endregion
    }

    /// <summary>
    /// ModelBuilder used in OnModelCreating to register fluent configurations.
    /// </summary>
    public class ModelBuilder
    {
        /// <summary>
        /// Gets a builder for configuring the specified entity type.
        /// </summary>
        public EntityBuilder<TEntity> Entity<TEntity>() where TEntity : class
        {
            var builder = new EntityBuilder<TEntity>();
            // Register a configuration that applies the builder
            var config = new InlineEntityConfiguration<TEntity>(builder);
            EntityMappingResolver.Register(config);
            return builder;
        }

        /// <summary>
        /// Applies an IEntityConfiguration implementation.
        /// </summary>
        public ModelBuilder ApplyConfiguration<TEntity>(IEntityConfiguration<TEntity> configuration) where TEntity : class
        {
            EntityMappingResolver.Register(configuration);
            return this;
        }
    }

    internal class InlineEntityConfiguration<TEntity> : IEntityConfiguration<TEntity> where TEntity : class
    {
        private readonly EntityBuilder<TEntity> _builder;

        public InlineEntityConfiguration(EntityBuilder<TEntity> builder)
        {
            _builder = builder;
        }

        public void Configure(EntityBuilder<TEntity> builder)
        {
            // Builder already configured, this is a wrapper
        }

        public void Apply(EntityMapping mapping)
        {
            // Apply the stored builder's configuration
            var dummy = new EntityTypeConfigurationFromBuilder<TEntity>(_builder);
            dummy.Apply(mapping);
        }
    }

    internal class EntityTypeConfigurationFromBuilder<TEntity> : EntityTypeConfiguration<TEntity> where TEntity : class
    {
        private readonly EntityBuilder<TEntity> _builder;

        public EntityTypeConfigurationFromBuilder(EntityBuilder<TEntity> builder)
        {
            _builder = builder;
        }

        public override void Configure(EntityBuilder<TEntity> builder)
        {
            // Copy settings from stored builder
        }

        public new void Apply(EntityMapping mapping)
        {
            if (!string.IsNullOrEmpty(_builder.TableNameOverride))
                mapping.TableName = _builder.TableNameOverride;
            if (!string.IsNullOrEmpty(_builder.SchemaOverride))
                mapping.Schema = _builder.SchemaOverride;

            if (!string.IsNullOrEmpty(_builder.PrimaryKeyOverride))
            {
                mapping.PrimaryKeyColumn = _builder.PrimaryKeyOverride;
                foreach (var prop in mapping.Properties)
                    prop.IsPrimaryKey = prop.PropertyName == _builder.PrimaryKeyOverride;
            }

            if (_builder.PrimaryKeyAutoIncrementOverride.HasValue)
                mapping.PrimaryKeyAutoIncrement = _builder.PrimaryKeyAutoIncrementOverride.Value;

            foreach (var ignored in _builder.IgnoredProperties)
            {
                var prop = mapping.Properties.Find(p => p.PropertyName == ignored);
                if (prop != null) prop.IsNotMapped = true;
            }

            foreach (var kvp in _builder.PropertyOverrides)
            {
                var prop = mapping.Properties.Find(p => p.PropertyName == kvp.Key);
                if (prop == null) continue;
                var pb = kvp.Value;

                if (!string.IsNullOrEmpty(pb.ColumnNameOverride))
                    prop.ColumnName = pb.ColumnNameOverride;
                if (pb.MaxLengthOverride.HasValue)
                    prop.MaxLength = pb.MaxLengthOverride.Value;
                if (pb.IsRequiredOverride.HasValue)
                    prop.IsRequired = pb.IsRequiredOverride.Value;
                if (pb.GeneratedOptionOverride.HasValue)
                    prop.GeneratedOption = pb.GeneratedOptionOverride.Value;
                if (pb.IsPrimaryKeyOverride.HasValue && pb.IsPrimaryKeyOverride.Value)
                {
                    prop.IsPrimaryKey = true;
                    mapping.PrimaryKeyColumn = prop.ColumnName;
                }
            }

            foreach (var filter in _builder.QueryFilterOverrides)
            {
                if (!mapping.QueryFilters.Contains(filter))
                    mapping.QueryFilters.Add(filter);
            }
        }
    }
}
