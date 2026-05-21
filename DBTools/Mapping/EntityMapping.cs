using System;
using System.Collections.Generic;
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
    }
}
