using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace DBTools.Mapping
{
    /// <summary>
    /// Holds the resolved mapping information for an entity type.
    /// Contains table name, column mappings, primary key, and other metadata.
    /// </summary>
    public class EntityMapping
    {
        /// <summary>
        /// The CLR type of the entity.
        /// </summary>
        public Type EntityType { get; set; }

        /// <summary>
        /// The resolved database table name.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Optional schema name.
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// The resolved primary key column name (null if no primary key is defined).
        /// </summary>
        public string PrimaryKeyColumn { get; set; }

        /// <summary>
        /// Whether the primary key is auto-incremented.
        /// </summary>
        public bool PrimaryKeyAutoIncrement { get; set; } = true;

        /// <summary>
        /// Property-to-column mappings.
        /// </summary>
        public List<PropertyMapping> Properties { get; set; } = new List<PropertyMapping>();

        /// <summary>
        /// Global query filters applied to all queries on this entity.
        /// </summary>
        public List<string> QueryFilters { get; set; } = new List<string>();

        /// <summary>
        /// Optional factory used to materialize entities from a raw <see cref="IDataRecord"/>.
        /// When set, <see cref="DBTools.Linq.AsyncDbQueryProvider{TModel}"/> and
        /// <see cref="DBTools.Controllers.AsyncLinqHelper{TModel}"/> call this delegate
        /// per row instead of <c>new TModel()</c> + property setters. This enables hydration
        /// of entities that have a private parameterless constructor or use factory-only
        /// construction. The returned object is added to the result list.
        /// </summary>
        public Func<IDataRecord, object> ModelFactory { get; set; }
    }

    /// <summary>
    /// Mapping information for a single property/column pair.
    /// </summary>
    public class PropertyMapping
    {
        /// <summary>
        /// The CLR property info.
        /// </summary>
        public PropertyInfo PropertyInfo { get; set; }

        /// <summary>
        /// The CLR property name.
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// The resolved database column name.
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Whether this property is the primary key.
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// Whether this property is excluded from mapping.
        /// </summary>
        public bool IsNotMapped { get; set; }

        /// <summary>
        /// Whether the column is database-generated (identity, computed).
        /// </summary>
        public DatabaseGeneratedOption GeneratedOption { get; set; } = DatabaseGeneratedOption.None;

        /// <summary>
        /// Whether this property is required (NOT NULL).
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Maximum length for string/binary properties.
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// Whether this property is a concurrency check token.
        /// </summary>
        public bool IsConcurrencyToken { get; set; }

        /// <summary>
        /// Whether this property is a timestamp/row version.
        /// </summary>
        public bool IsTimestamp { get; set; }

        /// <summary>
        /// Foreign key target (navigation property or column name).
        /// </summary>
        public string ForeignKey { get; set; }

        /// <summary>
        /// Default value for the column.
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Default value SQL expression.
        /// </summary>
        public string DefaultValueSql { get; set; }

        /// <summary>
        /// Optional converter that maps a raw database column value to this property's CLR
        /// type during hydration. When set, this delegate is invoked by
        /// <see cref="DBTools.Linq.AsyncDbQueryProvider{TModel}"/> and
        /// <see cref="DBTools.Controllers.AsyncLinqHelper{TModel}"/> instead of
        /// <see cref="Convert.ChangeType(object, Type)"/>. Use this for sealed value
        /// objects (e.g. <c>string</c> → <c>Currency</c>), enums, or any CLR type that has
        /// no default conversion path from the underlying column type.
        /// The delegate receives the raw column value (never null / DBNull — those bypass
        /// the converter) and returns the property value to set.
        /// </summary>
        public Func<object, object> ValueConverter { get; set; }

        /// <summary>
        /// Optional converter that maps the property's CLR value to the underlying
        /// database column value during INSERT / UPDATE. When set, this delegate is
        /// invoked by <see cref="DBTools.Controllers.AsyncLinqHelper{TModel}.InsertAsync"/>,
        /// <see cref="DBTools.Controllers.AsyncLinqHelper{TModel}.UpdateAsync"/>, and
        /// <see cref="DBTools.Controllers.AsyncLinqHelper{TModel}.SaveChangesAsync"/>
        /// instead of using the raw property value. Use this for sealed value objects
        /// (e.g. <c>Currency</c> → <c>string</c> code), enums, or any transformation
        /// needed before sending the value to the database.
        /// The delegate receives the property value (never null / DBNull — those bypass
        /// the converter) and returns the value to bind to the parameter.
        /// </summary>
        public Func<object, object> WriteConverter { get; set; }
    }
}
