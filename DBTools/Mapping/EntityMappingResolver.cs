using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DBTools.Mapping
{
    /// <summary>
    /// Resolves entity mapping information from attributes and fluent configuration.
    /// Caches results for performance. Thread-safe.
    /// </summary>
    public static class EntityMappingResolver
    {
        private static readonly ConcurrentDictionary<Type, EntityMapping> _cache = new ConcurrentDictionary<Type, EntityMapping>();
        private static readonly ConcurrentDictionary<Type, IEntityConfiguration> _fluentConfigs = new ConcurrentDictionary<Type, IEntityConfiguration>();

        /// <summary>
        /// Resolves the mapping for the specified entity type using attributes and fluent configuration.
        /// Results are cached.
        /// </summary>
        public static EntityMapping Resolve<TModel>() where TModel : class
        {
            return Resolve(typeof(TModel));
        }

        /// <summary>
        /// Resolves the mapping for the specified entity type.
        /// </summary>
        public static EntityMapping Resolve(Type entityType)
        {
            return _cache.GetOrAdd(entityType, BuildMapping);
        }

        /// <summary>
        /// Registers a fluent configuration for an entity type.
        /// Must be called before Resolve for the configuration to take effect.
        /// </summary>
        public static void Register<TModel>(IEntityConfiguration<TModel> configuration) where TModel : class
        {
            _fluentConfigs[typeof(TModel)] = configuration;
            _cache.TryRemove(typeof(TModel), out _); // Invalidate cache
        }

        /// <summary>
        /// Clears all cached mappings. Useful for testing.
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
        }

        private static EntityMapping BuildMapping(Type entityType)
        {
            var mapping = new EntityMapping
            {
                EntityType = entityType,
                TableName = entityType.Name // Default: class name
            };

            // Apply [Table] attribute
            var tableAttr = entityType.GetCustomAttribute<TableAttribute>();
            if (tableAttr != null)
            {
                mapping.TableName = tableAttr.Name;
                mapping.Schema = tableAttr.Schema;
            }

            // Apply [QueryFilter] attributes
            var filterAttrs = entityType.GetCustomAttributes<QueryFilterAttribute>();
            foreach (var filter in filterAttrs)
            {
                mapping.QueryFilters.Add(filter.Condition);
            }

            // Map properties
            var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite);

            foreach (var prop in properties)
            {
                var propMapping = new PropertyMapping
                {
                    PropertyInfo = prop,
                    PropertyName = prop.Name,
                    ColumnName = prop.Name // Default: property name
                };

                // [NotMapped]
                if (prop.GetCustomAttribute<NotMappedAttribute>() != null)
                {
                    propMapping.IsNotMapped = true;
                    mapping.Properties.Add(propMapping);
                    continue;
                }

                // [Column]
                var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                if (columnAttr != null)
                    propMapping.ColumnName = columnAttr.Name;

                // [Key]
                var keyAttr = prop.GetCustomAttribute<KeyAttribute>();
                if (keyAttr != null)
                {
                    propMapping.IsPrimaryKey = true;
                    mapping.PrimaryKeyColumn = propMapping.ColumnName;
                    mapping.PrimaryKeyAutoIncrement = keyAttr.AutoIncrement;
                }

                // [DatabaseGenerated]
                var generatedAttr = prop.GetCustomAttribute<DatabaseGeneratedAttribute>();
                if (generatedAttr != null)
                    propMapping.GeneratedOption = generatedAttr.Option;

                // [Required]
                if (prop.GetCustomAttribute<RequiredAttribute>() != null)
                    propMapping.IsRequired = true;

                // [MaxLength]
                var maxLenAttr = prop.GetCustomAttribute<MaxLengthAttribute>();
                if (maxLenAttr != null)
                    propMapping.MaxLength = maxLenAttr.Length;

                // [ConcurrencyCheck]
                if (prop.GetCustomAttribute<ConcurrencyCheckAttribute>() != null)
                    propMapping.IsConcurrencyToken = true;

                // [Timestamp]
                if (prop.GetCustomAttribute<TimestampAttribute>() != null)
                    propMapping.IsTimestamp = true;

                // [ForeignKey]
                var fkAttr = prop.GetCustomAttribute<ForeignKeyAttribute>();
                if (fkAttr != null)
                    propMapping.ForeignKey = fkAttr.Name;

                // [DefaultValue]
                var defaultAttr = prop.GetCustomAttribute<DefaultValueAttribute>();
                if (defaultAttr != null)
                {
                    propMapping.DefaultValue = defaultAttr.Value;
                    propMapping.DefaultValueSql = defaultAttr.Sql;
                }

                mapping.Properties.Add(propMapping);
            }

            // Convention: If no [Key] found, look for "Id" or "{TypeName}Id" property
            if (string.IsNullOrEmpty(mapping.PrimaryKeyColumn))
            {
                var idProp = mapping.Properties.FirstOrDefault(p =>
                    !p.IsNotMapped && (
                        p.PropertyName.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                        p.PropertyName.Equals(entityType.Name + "Id", StringComparison.OrdinalIgnoreCase)
                    ));

                if (idProp != null)
                {
                    idProp.IsPrimaryKey = true;
                    mapping.PrimaryKeyColumn = idProp.ColumnName;
                }
            }

            // Apply fluent configuration (overrides attributes)
            if (_fluentConfigs.TryGetValue(entityType, out var fluentConfig))
            {
                fluentConfig.Apply(mapping);
            }

            return mapping;
        }
    }
}
