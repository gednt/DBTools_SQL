using DBTools_Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DbTools.Controller
{
    /// <summary>
    /// A derived controller from UtilsController that enables property-based LINQ-style queries.
    /// Uses object properties to automatically build SQL queries, similar to Entity Framework.
    /// </summary>
    /// <typeparam name="TModel">The model type that represents a database table record.</typeparam>
    public class PropertyBasedUtilsController<TModel> : UtilsController<TModel> where TModel : class, new()
    {
        /// <summary>
        /// Initializes a new instance of the PropertyBasedUtilsController with database connection settings.
        /// </summary>
        /// <param name="tableName">The name of the database table</param>
        /// <param name="primaryKeyName">The name of the primary key column (optional)</param>
        /// <param name="autoIncrement">Whether the primary key is auto-incremented (default: true)</param>
        public PropertyBasedUtilsController(string tableName, string primaryKeyName = "", bool autoIncrement = true)
            : base(tableName, primaryKeyName, autoIncrement)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PropertyBasedUtilsController with an existing Utils instance.
        /// </summary>
        /// <param name="utils">An existing Utils instance with database connection configured</param>
        /// <param name="tableName">The name of the database table</param>
        /// <param name="primaryKeyName">The name of the primary key column (optional)</param>
        /// <param name="autoIncrement">Whether the primary key is auto-incremented (default: true)</param>
        public PropertyBasedUtilsController(Utils utils, string tableName, string primaryKeyName = "", bool autoIncrement = true)
            : base(utils, tableName, primaryKeyName, autoIncrement)
        {
        }

        /// <summary>
        /// Filters records where the specified property equals the given value.
        /// Uses property expressions for type-safe queries.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="propertySelector">Expression to select the property (e.g., u => u.Name)</param>
        /// <param name="value">The value to match</param>
        /// <returns>An enumerable collection of matching model instances</returns>
        public IEnumerable<TModel> WhereEquals<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)
        {
            string propertyName = GetPropertyName(propertySelector);
            string condition = $"{propertyName} = @param0";
            return Select(condition, new object[] { value });
        }

        /// <summary>
        /// Filters records where the specified property does not equal the given value.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="propertySelector">Expression to select the property</param>
        /// <param name="value">The value to exclude</param>
        /// <returns>An enumerable collection of matching model instances</returns>
        public IEnumerable<TModel> WhereNotEquals<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)
        {
            string propertyName = GetPropertyName(propertySelector);
            string condition = $"{propertyName} <> @param0";
            return Select(condition, new object[] { value });
        }

        /// <summary>
        /// Filters records where the specified numeric property is greater than the given value.
        /// </summary>
        /// <typeparam name="TProperty">The numeric type of the property</typeparam>
        /// <param name="propertySelector">Expression to select the property</param>
        /// <param name="value">The threshold value</param>
        /// <returns>An enumerable collection of matching model instances</returns>
        public IEnumerable<TModel> WhereGreaterThan<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)
            where TProperty : IComparable
        {
            string propertyName = GetPropertyName(propertySelector);
            string condition = $"{propertyName} > @param0";
            return Select(condition, new object[] { value });
        }

        /// <summary>
        /// Filters records where the specified numeric property is greater than or equal to the given value.
        /// </summary>
        /// <typeparam name="TProperty">The numeric type of the property</typeparam>
        /// <param name="propertySelector">Expression to select the property</param>
        /// <param name="value">The threshold value</param>
        /// <returns>An enumerable collection of matching model instances</returns>
        public IEnumerable<TModel> WhereGreaterThanOrEquals<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)
            where TProperty : IComparable
        {
            string propertyName = GetPropertyName(propertySelector);
            string condition = $"{propertyName} >= @param0";
            return Select(condition, new object[] { value });
        }

        /// <summary>
        /// Filters records where the specified numeric property is less than the given value.
        /// </summary>
        /// <typeparam name="TProperty">The numeric type of the property</typeparam>
        /// <param name="propertySelector">Expression to select the property</param>
        /// <param name="value">The threshold value</param>
        /// <returns>An enumerable collection of matching model instances</returns>
        public IEnumerable<TModel> WhereLessThan<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)
            where TProperty : IComparable
        {
            string propertyName = GetPropertyName(propertySelector);
            string condition = $"{propertyName} < @param0";
            return Select(condition, new object[] { value });
        }

        /// <summary>
        /// Filters records where the specified numeric property is less than or equal to the given value.
        /// </summary>
        /// <typeparam name="TProperty">The numeric type of the property</typeparam>
        /// <param name="propertySelector">Expression to select the property</param>
        /// <param name="value">The threshold value</param>
        /// <returns>An enumerable collection of matching model instances</returns>
        public IEnumerable<TModel> WhereLessThanOrEquals<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)
            where TProperty : IComparable
        {
            string propertyName = GetPropertyName(propertySelector);
            string condition = $"{propertyName} <= @param0";
            return Select(condition, new object[] { value });
        }

        /// <summary>
        /// Filters records where the specified numeric property is between two values (inclusive).
        /// </summary>
        /// <typeparam name="TProperty">The numeric type of the property</typeparam>
        /// <param name="propertySelector">Expression to select the property</param>
        /// <param name="minValue">The minimum value (inclusive)</param>
        /// <param name="maxValue">The maximum value (inclusive)</param>
        /// <returns>An enumerable collection of matching model instances</returns>
        public IEnumerable<TModel> WhereBetween<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty minValue, TProperty maxValue)
            where TProperty : IComparable
        {
            string propertyName = GetPropertyName(propertySelector);
            string condition = $"{propertyName} >= @param0 AND {propertyName} <= @param1";
            return Select(condition, new object[] { minValue, maxValue });
        }

        /// <summary>
        /// Filters records where the specified string property contains the given substring.
        /// Uses SQL LIKE with wildcards.
        /// </summary>
        /// <param name="propertySelector">Expression to select the string property</param>
        /// <param name="substring">The substring to search for</param>
        /// <returns>An enumerable collection of matching model instances</returns>
        public IEnumerable<TModel> WhereContains(Expression<Func<TModel, string>> propertySelector, string substring)
        {
            string propertyName = GetPropertyName(propertySelector);
            string condition = $"{propertyName} LIKE @param0";
            return Select(condition, new object[] { $"%{substring}%" });
        }

        /// <summary>
        /// Filters records where the specified string property starts with the given prefix.
        /// Uses SQL LIKE with wildcard.
        /// </summary>
        /// <param name="propertySelector">Expression to select the string property</param>
        /// <param name="prefix">The prefix to match</param>
        /// <returns>An enumerable collection of matching model instances</returns>
        public IEnumerable<TModel> WhereStartsWith(Expression<Func<TModel, string>> propertySelector, string prefix)
        {
            string propertyName = GetPropertyName(propertySelector);
            string condition = $"{propertyName} LIKE @param0";
            return Select(condition, new object[] { $"{prefix}%" });
        }

        /// <summary>
        /// Filters records where the specified string property ends with the given suffix.
        /// Uses SQL LIKE with wildcard.
        /// </summary>
        /// <param name="propertySelector">Expression to select the string property</param>
        /// <param name="suffix">The suffix to match</param>
        /// <returns>An enumerable collection of matching model instances</returns>
        public IEnumerable<TModel> WhereEndsWith(Expression<Func<TModel, string>> propertySelector, string suffix)
        {
            string propertyName = GetPropertyName(propertySelector);
            string condition = $"{propertyName} LIKE @param0";
            return Select(condition, new object[] { $"%{suffix}" });
        }

        /// <summary>
        /// Filters records where the specified property value is in the given list.
        /// Uses SQL IN clause.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="propertySelector">Expression to select the property</param>
        /// <param name="values">The list of values to match</param>
        /// <returns>An enumerable collection of matching model instances</returns>
        public IEnumerable<TModel> WhereIn<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, IEnumerable<TProperty> values)
        {
            var valuesList = values.ToList();
            if (valuesList.Count == 0)
                return Enumerable.Empty<TModel>();

            string propertyName = GetPropertyName(propertySelector);
            
            // Build parameterized IN clause
            var paramNames = new List<string>();
            var parameters = new List<object>();
            for (int i = 0; i < valuesList.Count; i++)
            {
                paramNames.Add($"@param{i}");
                parameters.Add(valuesList[i]);
            }
            
            string condition = $"{propertyName} IN ({string.Join(", ", paramNames)})";
            return Select(condition, parameters.ToArray());
        }

        /// <summary>
        /// Filters records where the specified property value is not in the given list.
        /// Uses SQL NOT IN clause.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="propertySelector">Expression to select the property</param>
        /// <param name="values">The list of values to exclude</param>
        /// <returns>An enumerable collection of matching model instances</returns>
        public IEnumerable<TModel> WhereNotIn<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, IEnumerable<TProperty> values)
        {
            var valuesList = values.ToList();
            if (valuesList.Count == 0)
                return All();

            string propertyName = GetPropertyName(propertySelector);
            
            // Build parameterized NOT IN clause
            var paramNames = new List<string>();
            var parameters = new List<object>();
            for (int i = 0; i < valuesList.Count; i++)
            {
                paramNames.Add($"@param{i}");
                parameters.Add(valuesList[i]);
            }
            
            string condition = $"{propertyName} NOT IN ({string.Join(", ", paramNames)})";
            return Select(condition, parameters.ToArray());
        }

        /// <summary>
        /// Filters records where the specified property is null.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="propertySelector">Expression to select the property</param>
        /// <returns>An enumerable collection of matching model instances</returns>
        public IEnumerable<TModel> WhereIsNull<TProperty>(Expression<Func<TModel, TProperty>> propertySelector)
        {
            string propertyName = GetPropertyName(propertySelector);
            string condition = $"{propertyName} IS NULL";
            return Select(condition, new object[] { });
        }

        /// <summary>
        /// Filters records where the specified property is not null.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="propertySelector">Expression to select the property</param>
        /// <returns>An enumerable collection of matching model instances</returns>
        public IEnumerable<TModel> WhereIsNotNull<TProperty>(Expression<Func<TModel, TProperty>> propertySelector)
        {
            string propertyName = GetPropertyName(propertySelector);
            string condition = $"{propertyName} IS NOT NULL";
            return Select(condition, new object[] { });
        }

        /// <summary>
        /// Gets the first record where the specified property equals the given value, or null if not found.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="propertySelector">Expression to select the property</param>
        /// <param name="value">The value to match</param>
        /// <returns>The first matching model instance or null</returns>
        public TModel FirstOrDefaultByProperty<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)
        {
            return WhereEquals(propertySelector, value).FirstOrDefault();
        }

        /// <summary>
        /// Gets the single record where the specified property equals the given value.
        /// Throws an exception if zero or more than one record matches.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="propertySelector">Expression to select the property</param>
        /// <param name="value">The value to match</param>
        /// <returns>The single matching model instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when zero or more than one record matches</exception>
        public TModel SingleByProperty<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)
        {
            return WhereEquals(propertySelector, value).Single();
        }

        /// <summary>
        /// Gets the single record where the specified property equals the given value, or null if not found.
        /// Throws an exception if more than one record matches.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="propertySelector">Expression to select the property</param>
        /// <param name="value">The value to match</param>
        /// <returns>The single matching model instance or null</returns>
        /// <exception cref="InvalidOperationException">Thrown when more than one record matches</exception>
        public TModel SingleOrDefaultByProperty<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)
        {
            return WhereEquals(propertySelector, value).SingleOrDefault();
        }

        /// <summary>
        /// Counts records where the specified property equals the given value.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="propertySelector">Expression to select the property</param>
        /// <param name="value">The value to match</param>
        /// <returns>The number of matching records</returns>
        public int CountByProperty<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)
        {
            string propertyName = GetPropertyName(propertySelector);
            string condition = $"{propertyName} = @param0";
            return Count(condition, new object[] { value });
        }

        /// <summary>
        /// Checks if any records exist where the specified property equals the given value.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="propertySelector">Expression to select the property</param>
        /// <param name="value">The value to match</param>
        /// <returns>True if any matching records exist, false otherwise</returns>
        public bool AnyByProperty<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)
        {
            return CountByProperty(propertySelector, value) > 0;
        }

        /// <summary>
        /// Deletes records where the specified property equals the given value.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="propertySelector">Expression to select the property</param>
        /// <param name="value">The value to match for deletion</param>
        /// <returns>True if the deletion was successful, false otherwise</returns>
        public bool DeleteByProperty<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)
        {
            string propertyName = GetPropertyName(propertySelector);
            string condition = $"{propertyName} = @param0";
            return Delete(condition, new object[] { value });
        }

        /// <summary>
        /// Updates records where the specified property equals the given value.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="model">The model with updated values</param>
        /// <param name="propertySelector">Expression to select the property for the WHERE clause</param>
        /// <param name="value">The value to match for the update</param>
        /// <returns>True if the update was successful, false otherwise</returns>
        public bool UpdateByProperty<TProperty>(TModel model, Expression<Func<TModel, TProperty>> propertySelector, TProperty value)
        {
            string propertyName = GetPropertyName(propertySelector);
            string whereClause = $"{propertyName} = @whereParam0";
            return Update(model, whereClause, new object[] { value });
        }

        /// <summary>
        /// Filters records using a model instance as a filter template.
        /// Only non-null and non-default properties are used in the WHERE clause.
        /// </summary>
        /// <param name="filterModel">A model instance with properties set to filter values</param>
        /// <returns>An enumerable collection of matching model instances</returns>
        public IEnumerable<TModel> WhereByExample(TModel filterModel)
        {
            var properties = typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead);

            var conditions = new List<string>();
            var parameters = new List<object>();
            int paramIndex = 0;

            foreach (var property in properties)
            {
                var value = property.GetValue(filterModel);
                
                // Skip null values
                if (value == null)
                    continue;
                
                // Skip default values for value types
                if (property.PropertyType.IsValueType)
                {
                    var defaultValue = Activator.CreateInstance(property.PropertyType);
                    if (defaultValue != null && value.Equals(defaultValue))
                        continue;
                }

                conditions.Add($"{property.Name} = @param{paramIndex}");
                parameters.Add(value);
                paramIndex++;
            }

            if (conditions.Count == 0)
                return All();

            string whereClause = string.Join(" AND ", conditions);
            return Select(whereClause, parameters.ToArray());
        }

        /// <summary>
        /// Extracts the property name from a property selector expression.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="propertySelector">The property selector expression</param>
        /// <returns>The name of the property</returns>
        /// <exception cref="ArgumentException">Thrown when the expression is not a valid property selector</exception>
        private string GetPropertyName<TProperty>(Expression<Func<TModel, TProperty>> propertySelector)
        {
            if (propertySelector == null)
                throw new ArgumentNullException(nameof(propertySelector));

            MemberExpression memberExpression = null;

            if (propertySelector.Body is MemberExpression)
            {
                memberExpression = (MemberExpression)propertySelector.Body;
            }
            else if (propertySelector.Body is UnaryExpression unaryExpression)
            {
                // Handle boxing/unboxing scenarios
                memberExpression = unaryExpression.Operand as MemberExpression;
            }

            if (memberExpression == null)
                throw new ArgumentException("Expression must be a property access expression (e.g., x => x.PropertyName)", nameof(propertySelector));

            if (!(memberExpression.Member is PropertyInfo))
                throw new ArgumentException("Expression must access a property, not a field", nameof(propertySelector));

            return memberExpression.Member.Name;
        }
    }
}
