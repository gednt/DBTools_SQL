using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace DBTools.Mapping
{
    /// <summary>
    /// Non-generic base interface for entity configuration.
    /// </summary>
    public interface IEntityConfiguration
    {
        void Apply(EntityMapping mapping);
    }

    /// <summary>
    /// Interface for fluent entity configuration.
    /// Implement this interface to configure entity mappings without attributes.
    /// </summary>
    /// <typeparam name="TModel">The entity type being configured</typeparam>
    public interface IEntityConfiguration<TModel> : IEntityConfiguration where TModel : class
    {
        /// <summary>
        /// Configures the entity using the fluent EntityBuilder API.
        /// </summary>
        void Configure(EntityBuilder<TModel> builder);
    }

    /// <summary>
    /// Fluent API builder for configuring entity-to-table mappings.
    /// Provides a code-first alternative to attribute-based configuration.
    /// </summary>
    /// <typeparam name="TModel">The entity type being configured</typeparam>
    public class EntityBuilder<TModel> where TModel : class
    {
        internal string TableNameOverride { get; private set; }
        internal string SchemaOverride { get; private set; }
        internal Dictionary<string, PropertyBuilder> PropertyOverrides { get; } = new Dictionary<string, PropertyBuilder>();
        internal List<string> IgnoredProperties { get; } = new List<string>();
        internal string PrimaryKeyOverride { get; private set; }
        internal bool? PrimaryKeyAutoIncrementOverride { get; private set; }
        internal List<string> QueryFilterOverrides { get; } = new List<string>();

        /// <summary>
        /// Sets the table name for this entity.
        /// </summary>
        public EntityBuilder<TModel> ToTable(string tableName)
        {
            TableNameOverride = tableName;
            return this;
        }

        /// <summary>
        /// Sets the schema for this entity's table.
        /// </summary>
        public EntityBuilder<TModel> HasSchema(string schema)
        {
            SchemaOverride = schema;
            return this;
        }

        /// <summary>
        /// Configures a property with the fluent API.
        /// </summary>
        public PropertyBuilder Property<TProperty>(Expression<Func<TModel, TProperty>> propertySelector)
        {
            var memberExpr = GetMemberExpression(propertySelector);
            var propName = memberExpr.Member.Name;

            if (!PropertyOverrides.ContainsKey(propName))
                PropertyOverrides[propName] = new PropertyBuilder(propName);

            return PropertyOverrides[propName];
        }

        /// <summary>
        /// Marks a property as the primary key.
        /// </summary>
        public EntityBuilder<TModel> HasKey<TProperty>(Expression<Func<TModel, TProperty>> propertySelector)
        {
            var memberExpr = GetMemberExpression(propertySelector);
            PrimaryKeyOverride = memberExpr.Member.Name;
            return this;
        }

        /// <summary>
        /// Sets whether the primary key is auto-incremented.
        /// </summary>
        public EntityBuilder<TModel> HasAutoIncrement(bool autoIncrement = true)
        {
            PrimaryKeyAutoIncrementOverride = autoIncrement;
            return this;
        }

        /// <summary>
        /// Marks a property to be ignored (not mapped to any column).
        /// </summary>
        public EntityBuilder<TModel> Ignore<TProperty>(Expression<Func<TModel, TProperty>> propertySelector)
        {
            var memberExpr = GetMemberExpression(propertySelector);
            IgnoredProperties.Add(memberExpr.Member.Name);
            return this;
        }

        /// <summary>
        /// Adds a global query filter that will be applied to all queries on this entity.
        /// </summary>
        public EntityBuilder<TModel> HasQueryFilter(string condition)
        {
            QueryFilterOverrides.Add(condition);
            return this;
        }

        private static MemberExpression GetMemberExpression<TProperty>(Expression<Func<TModel, TProperty>> selector)
        {
            if (selector.Body is MemberExpression member)
                return member;
            if (selector.Body is UnaryExpression unary && unary.Operand is MemberExpression m)
                return m;
            throw new ArgumentException("Expression must be a property access expression.");
        }
    }

    /// <summary>
    /// Fluent API builder for configuring individual property mappings.
    /// </summary>
    public class PropertyBuilder
    {
        internal string PropertyName { get; }
        internal string ColumnNameOverride { get; private set; }
        internal string TypeNameOverride { get; private set; }
        internal int? MaxLengthOverride { get; private set; }
        internal bool? IsRequiredOverride { get; private set; }
        internal bool? IsPrimaryKeyOverride { get; private set; }
        internal DatabaseGeneratedOption? GeneratedOptionOverride { get; private set; }
        internal Func<object, object> WriteConverter { get; private set; }
        internal Func<object, object> ReadConverter { get; private set; }

        internal PropertyBuilder(string propertyName)
        {
            PropertyName = propertyName;
        }

        /// <summary>
        /// Sets the database column name for this property.
        /// </summary>
        public PropertyBuilder HasColumnName(string columnName)
        {
            ColumnNameOverride = columnName;
            return this;
        }

        /// <summary>
        /// Sets the database column type for this property.
        /// </summary>
        public PropertyBuilder HasColumnType(string typeName)
        {
            TypeNameOverride = typeName;
            return this;
        }

        /// <summary>
        /// Sets the maximum length for string/binary properties.
        /// </summary>
        public PropertyBuilder HasMaxLength(int maxLength)
        {
            MaxLengthOverride = maxLength;
            return this;
        }

        /// <summary>
        /// Marks this property as required (NOT NULL).
        /// </summary>
        public PropertyBuilder IsRequired(bool required = true)
        {
            IsRequiredOverride = required;
            return this;
        }

        /// <summary>
        /// Marks this property as a primary key.
        /// </summary>
        public PropertyBuilder IsKey()
        {
            IsPrimaryKeyOverride = true;
            return this;
        }

        /// <summary>
        /// Marks this property as database-generated.
        /// </summary>
        public PropertyBuilder ValueGeneratedOnAdd()
        {
            GeneratedOptionOverride = DatabaseGeneratedOption.Identity;
            return this;
        }

        /// <summary>
        /// Marks this property as computed by the database.
        /// </summary>
        public PropertyBuilder ValueGeneratedOnAddOrUpdate()
        {
            GeneratedOptionOverride = DatabaseGeneratedOption.Computed;
            return this;
        }

        /// <summary>
        /// Adds a value conversion for writing to and reading from the database.
        /// Useful for enum-to-string, encryption, or custom serialization.
        /// </summary>
        /// <param name="writeConversion">Converts the CLR value to the database value</param>
        /// <param name="readConversion">Converts the database value back to the CLR value</param>
        public PropertyBuilder HasConversion(Func<object, object> writeConversion, Func<object, object> readConversion)
        {
            WriteConverter = writeConversion;
            ReadConverter = readConversion;
            return this;
        }

        /// <summary>
        /// Adds a typed value conversion.
        /// </summary>
        public PropertyBuilder HasConversion<TProperty, TDatabase>(
            Func<TProperty, TDatabase> writeConversion,
            Func<TDatabase, TProperty> readConversion)
        {
            WriteConverter = obj => writeConversion((TProperty)obj);
            ReadConverter = obj => readConversion((TDatabase)obj);
            return this;
        }
    }

    /// <summary>
    /// Base class for fluent entity configuration that implements IEntityConfiguration&lt;T&gt;.
    /// Override Configure to provide mappings.
    /// </summary>
    public abstract class EntityTypeConfiguration<TModel> : IEntityConfiguration<TModel> where TModel : class
    {
        public abstract void Configure(EntityBuilder<TModel> builder);

        public void Apply(EntityMapping mapping)
        {
            var builder = new EntityBuilder<TModel>();
            Configure(builder);

            // Apply table name override
            if (!string.IsNullOrEmpty(builder.TableNameOverride))
                mapping.TableName = builder.TableNameOverride;
            if (!string.IsNullOrEmpty(builder.SchemaOverride))
                mapping.Schema = builder.SchemaOverride;

            // Apply primary key override
            if (!string.IsNullOrEmpty(builder.PrimaryKeyOverride))
            {
                mapping.PrimaryKeyColumn = builder.PrimaryKeyOverride;
                foreach (var prop in mapping.Properties)
                {
                    prop.IsPrimaryKey = prop.PropertyName == builder.PrimaryKeyOverride;
                }
            }

            if (builder.PrimaryKeyAutoIncrementOverride.HasValue)
                mapping.PrimaryKeyAutoIncrement = builder.PrimaryKeyAutoIncrementOverride.Value;

            // Apply ignored properties
            foreach (var ignoredProp in builder.IgnoredProperties)
            {
                var prop = mapping.Properties.Find(p => p.PropertyName == ignoredProp);
                if (prop != null)
                    prop.IsNotMapped = true;
            }

            // Apply property overrides
            foreach (var kvp in builder.PropertyOverrides)
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

            // Apply query filter overrides
            foreach (var filter in builder.QueryFilterOverrides)
            {
                if (!mapping.QueryFilters.Contains(filter))
                    mapping.QueryFilters.Add(filter);
            }
        }
    }
}
