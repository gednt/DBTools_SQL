using DBTools.Core;
using DBTools.Linq;
using DBTools.Abstractions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DBTools.Controllers
{
    /// <summary>
    /// A LINQ-compliant controller that extends LinqHelper with IQueryable support, property-based queries, and JOINs.
    /// Uses object properties to automatically build SQL queries, similar to Entity Framework.
    /// </summary>
    /// <typeparam name="TModel">The model type that represents a database table record.</typeparam>
    public class Linq<TModel> : LinqHelper<TModel> where TModel : class, new()
    {
        /// <summary>
        /// Initializes a new instance of Linq with database connection settings.
        /// </summary>
        /// <param name="tableName">The name of the database table</param>
        /// <param name="primaryKeyName">The name of the primary key column (optional)</param>
        /// <param name="autoIncrement">Whether the primary key is auto-incremented (default: true)</param>
        public Linq(string tableName, string primaryKeyName = "", bool autoIncrement = true)
            : base(tableName, primaryKeyName, autoIncrement)
        {
        }

        /// <summary>
        /// Initializes a new instance of Linq with an existing SqlClient instance.
        /// </summary>
        /// <param name="utils">An existing SqlClient instance with database connection configured</param>
        /// <param name="tableName">The name of the database table</param>
        /// <param name="primaryKeyName">The name of the primary key column (optional)</param>
        /// <param name="autoIncrement">Whether the primary key is auto-incremented (default: true)</param>
        public Linq(SqlClient utils, string tableName, string primaryKeyName = "", bool autoIncrement = true)
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
                
                // Skip null values (value types are never null, so this only applies to reference types)
                if (value == null)
                    continue;

                conditions.Add($"{property.Name} = @param{paramIndex}");
                parameters.Add(value);
                paramIndex++;
            }

            if (conditions.Count == 0)
                return All();

            string whereClause = string.Join(" AND ", conditions);
            return Select(whereClause, parameters.ToArray());
        }

        #region Additional CRUD Operations

        /// <summary>
        /// Inserts a new record and returns it after retrieval (useful for getting auto-generated values like Id).
        /// This method inserts the model then retrieves it using the specified property to find it.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property used to find the inserted record</typeparam>
        /// <param name="model">The model instance to insert</param>
        /// <param name="propertySelector">Expression to select the property used to find the inserted record</param>
        /// <returns>The inserted model instance with database-generated values, or null if insert failed</returns>
        public TModel InsertAndFind<TProperty>(TModel model, Expression<Func<TModel, TProperty>> propertySelector)
        {
            if (!Insert(model))
                return null;

            var propertyInfo = GetPropertyInfo(propertySelector);
            var value = propertyInfo.GetValue(model);
            return FirstOrDefaultByProperty(propertySelector, (TProperty)value);
        }

        /// <summary>
        /// Updates records matching multiple property conditions.
        /// </summary>
        /// <typeparam name="TProperty1">The type of the first property</typeparam>
        /// <typeparam name="TProperty2">The type of the second property</typeparam>
        /// <param name="model">The model with updated values</param>
        /// <param name="property1Selector">First property selector</param>
        /// <param name="value1">First property value to match</param>
        /// <param name="property2Selector">Second property selector</param>
        /// <param name="value2">Second property value to match</param>
        /// <returns>True if the update was successful, false otherwise</returns>
        public bool UpdateWhere<TProperty1, TProperty2>(
            TModel model,
            Expression<Func<TModel, TProperty1>> property1Selector, TProperty1 value1,
            Expression<Func<TModel, TProperty2>> property2Selector, TProperty2 value2)
        {
            string prop1Name = GetPropertyName(property1Selector);
            string prop2Name = GetPropertyName(property2Selector);
            string whereClause = $"{prop1Name} = @whereParam0 AND {prop2Name} = @whereParam1";
            return Update(model, whereClause, new object[] { value1, value2 });
        }

        /// <summary>
        /// Updates records matching three property conditions.
        /// </summary>
        public bool UpdateWhere<TProperty1, TProperty2, TProperty3>(
            TModel model,
            Expression<Func<TModel, TProperty1>> property1Selector, TProperty1 value1,
            Expression<Func<TModel, TProperty2>> property2Selector, TProperty2 value2,
            Expression<Func<TModel, TProperty3>> property3Selector, TProperty3 value3)
        {
            string prop1Name = GetPropertyName(property1Selector);
            string prop2Name = GetPropertyName(property2Selector);
            string prop3Name = GetPropertyName(property3Selector);
            string whereClause = $"{prop1Name} = @whereParam0 AND {prop2Name} = @whereParam1 AND {prop3Name} = @whereParam2";
            return Update(model, whereClause, new object[] { value1, value2, value3 });
        }

        /// <summary>
        /// Deletes records matching multiple property conditions.
        /// </summary>
        /// <typeparam name="TProperty1">The type of the first property</typeparam>
        /// <typeparam name="TProperty2">The type of the second property</typeparam>
        /// <param name="property1Selector">First property selector</param>
        /// <param name="value1">First property value to match</param>
        /// <param name="property2Selector">Second property selector</param>
        /// <param name="value2">Second property value to match</param>
        /// <returns>True if the deletion was successful, false otherwise</returns>
        public bool DeleteWhere<TProperty1, TProperty2>(
            Expression<Func<TModel, TProperty1>> property1Selector, TProperty1 value1,
            Expression<Func<TModel, TProperty2>> property2Selector, TProperty2 value2)
        {
            string prop1Name = GetPropertyName(property1Selector);
            string prop2Name = GetPropertyName(property2Selector);
            string condition = $"{prop1Name} = @param0 AND {prop2Name} = @param1";
            return Delete(condition, new object[] { value1, value2 });
        }

        /// <summary>
        /// Deletes records matching three property conditions.
        /// </summary>
        public bool DeleteWhere<TProperty1, TProperty2, TProperty3>(
            Expression<Func<TModel, TProperty1>> property1Selector, TProperty1 value1,
            Expression<Func<TModel, TProperty2>> property2Selector, TProperty2 value2,
            Expression<Func<TModel, TProperty3>> property3Selector, TProperty3 value3)
        {
            string prop1Name = GetPropertyName(property1Selector);
            string prop2Name = GetPropertyName(property2Selector);
            string prop3Name = GetPropertyName(property3Selector);
            string condition = $"{prop1Name} = @param0 AND {prop2Name} = @param1 AND {prop3Name} = @param2";
            return Delete(condition, new object[] { value1, value2, value3 });
        }

        /// <summary>
        /// Deletes records where a property value is in the specified list.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="propertySelector">Expression to select the property</param>
        /// <param name="values">The list of values to match for deletion</param>
        /// <returns>True if the deletion was successful, false otherwise</returns>
        public bool DeleteWhereIn<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, IEnumerable<TProperty> values)
        {
            var valuesList = values.ToList();
            if (valuesList.Count == 0)
                return true; // Nothing to delete

            string propertyName = GetPropertyName(propertySelector);
            
            var paramNames = new List<string>();
            var parameters = new List<object>();
            for (int i = 0; i < valuesList.Count; i++)
            {
                paramNames.Add($"@param{i}");
                parameters.Add(valuesList[i]);
            }
            
            string condition = $"{propertyName} IN ({string.Join(", ", paramNames)})";
            return Delete(condition, parameters.ToArray());
        }

        /// <summary>
        /// Deletes records where the specified property is null.
        /// Note: This method uses the base Delete method which requires a non-empty condition.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="propertySelector">Expression to select the property</param>
        /// <returns>True if the deletion was successful, false otherwise</returns>
        public bool DeleteWhereIsNull<TProperty>(Expression<Func<TModel, TProperty>> propertySelector)
        {
            string propertyName = GetPropertyName(propertySelector);
            string condition = $"{propertyName} IS NULL";
            return Delete(condition, new object[] { });
        }

        /// <summary>
        /// Updates all records where the specified property is null.
        /// Note: This method uses the base Update method which requires a non-empty where clause.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="model">The model with updated values</param>
        /// <param name="propertySelector">Expression to select the property to check for null</param>
        /// <returns>True if the update was successful, false otherwise</returns>
        public bool UpdateWhereIsNull<TProperty>(TModel model, Expression<Func<TModel, TProperty>> propertySelector)
        {
            string propertyName = GetPropertyName(propertySelector);
            string whereClause = $"{propertyName} IS NULL";
            return Update(model, whereClause, new object[] { });
        }

        /// <summary>
        /// Updates records where a property value is in the specified list.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="model">The model with updated values</param>
        /// <param name="propertySelector">Expression to select the property</param>
        /// <param name="values">The list of values to match for update</param>
        /// <returns>True if the update was successful, false otherwise</returns>
        public bool UpdateWhereIn<TProperty>(TModel model, Expression<Func<TModel, TProperty>> propertySelector, IEnumerable<TProperty> values)
        {
            var valuesList = values.ToList();
            if (valuesList.Count == 0)
                return true; // Nothing to update

            string propertyName = GetPropertyName(propertySelector);
            
            var paramNames = new List<string>();
            var parameters = new List<object>();
            for (int i = 0; i < valuesList.Count; i++)
            {
                paramNames.Add($"@whereParam{i}");
                parameters.Add(valuesList[i]);
            }
            
            string whereClause = $"{propertyName} IN ({string.Join(", ", paramNames)})";
            return Update(model, whereClause, parameters.ToArray());
        }

        /// <summary>
        /// Checks if a record exists with the specified property value.
        /// Alias for AnyByProperty for better readability in CRUD context.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="propertySelector">Expression to select the property</param>
        /// <param name="value">The value to check for</param>
        /// <returns>True if a record exists with the specified property value, false otherwise</returns>
        public bool Exists<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)
        {
            return AnyByProperty(propertySelector, value);
        }

        /// <summary>
        /// Inserts the model if it doesn't exist based on the specified property, otherwise updates it.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="model">The model to insert or update</param>
        /// <param name="propertySelector">Expression to select the property to check for existence</param>
        /// <returns>True if the operation was successful, false otherwise</returns>
        public bool InsertOrUpdate<TProperty>(TModel model, Expression<Func<TModel, TProperty>> propertySelector)
        {
            var matchPropInfo = GetPropertyInfo(propertySelector);
            var matchPropName = matchPropInfo.Name;

            var allProps = typeof(TModel).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .ToList();

            var columns = allProps.Select(p => p.Name).ToArray();
            var provider = Utils.Provider;

            // Build upsert SQL using provider's dialect
            string upsertSql = provider.BuildUpsertSql(TableName, columns, matchPropName, provider.ParameterPrefix);

            // Build parameters: p0, p1, ... for columns, then 'match' for the match column
            var parameters = new List<DbParameter>();
            for (int i = 0; i < allProps.Count; i++)
            {
                var val = allProps[i].GetValue(model);
                parameters.Add(provider.CreateParameter($"{provider.ParameterPrefix}p{i}", val ?? (object)DBNull.Value));
            }

            // Add match parameter (used by SqlServer's MERGE USING clause)
            var matchValue = matchPropInfo.GetValue(model);
            parameters.Add(provider.CreateParameter($"{provider.ParameterPrefix}match", matchValue ?? (object)DBNull.Value));

            Utils.Query = upsertSql;
            Utils.SqlParameters = parameters;
            Utils.ExecuteQuery(upsertSql);
            Utils.SqlParameters = null;

            return string.IsNullOrEmpty(Utils.Error);
        }

        /// <summary>
        /// Gets or creates a record. Returns existing record if found by the specified property, otherwise inserts and returns the new record.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="model">The model to insert if not found</param>
        /// <param name="propertySelector">Expression to select the property to check for existence</param>
        /// <returns>The existing or newly created model instance</returns>
        public TModel GetOrCreate<TProperty>(TModel model, Expression<Func<TModel, TProperty>> propertySelector)
        {
            var propertyInfo = GetPropertyInfo(propertySelector);
            var value = (TProperty)propertyInfo.GetValue(model);

            var existing = FirstOrDefaultByProperty(propertySelector, value);
            if (existing != null)
                return existing;

            Insert(model);
            return FirstOrDefaultByProperty(propertySelector, value);
        }

        #endregion

        #region IQueryable and JOIN Support

        /// <summary>
        /// Returns an IQueryable that supports deferred SQL-translated execution.
        /// LINQ methods like Where, OrderBy, Skip, Take are translated to SQL and executed only on enumeration.
        /// </summary>
        /// <returns>A DbQuery instance that implements IQueryable with deferred execution</returns>
        public override IQueryable<TModel> AsQueryable()
        {
            var provider = new DbQueryProvider(
                Utils,
                TableName,
                PrimaryKeyName,
                dataView => MapDataViewToModels(dataView).Cast<object>());
            return new DbQuery<TModel>(provider, TableName);
        }

        /// <summary>
        /// Creates an INNER JOIN query with the specified right table.
        /// Returns a JoinQuery that supports LINQ chaining (Where, OrderBy, Skip, Take).
        /// </summary>
        /// <typeparam name="TRight">The model type for the right (joined) table</typeparam>
        /// <param name="rightTable">The name of the right table to join</param>
        /// <param name="leftKey">Expression selecting the left table's join key (e.g., u => u.Id)</param>
        /// <param name="rightKey">Expression selecting the right table's join key (e.g., o => o.UserId)</param>
        /// <returns>A JoinQuery for further LINQ operations</returns>
        public JoinQuery<TModel, TRight> InnerJoin<TRight>(
            string rightTable,
            Expression<Func<TModel, object>> leftKey,
            Expression<Func<TRight, object>> rightKey)
            where TRight : class, new()
        {
            string leftKeyName = GetPropertyNameFromObjectSelector(leftKey);
            string rightKeyName = GetPropertyNameFromObjectSelector<TRight>(rightKey);

            var provider = new JoinQueryProvider<TModel, TRight>(
                Utils,
                TableName, "t0", leftKeyName,
                rightTable, "t1", rightKeyName,
                "INNER JOIN",
                PrimaryKeyName);

            return new JoinQuery<TModel, TRight>(provider);
        }

        /// <summary>
        /// Creates a LEFT JOIN query with the specified right table.
        /// Returns a JoinQuery where the Right property of JoinResult will be null for non-matching rows.
        /// </summary>
        /// <typeparam name="TRight">The model type for the right (joined) table</typeparam>
        /// <param name="rightTable">The name of the right table to join</param>
        /// <param name="leftKey">Expression selecting the left table's join key (e.g., u => u.Id)</param>
        /// <param name="rightKey">Expression selecting the right table's join key (e.g., o => o.UserId)</param>
        /// <returns>A JoinQuery for further LINQ operations</returns>
        public JoinQuery<TModel, TRight> LeftJoin<TRight>(
            string rightTable,
            Expression<Func<TModel, object>> leftKey,
            Expression<Func<TRight, object>> rightKey)
            where TRight : class, new()
        {
            string leftKeyName = GetPropertyNameFromObjectSelector(leftKey);
            string rightKeyName = GetPropertyNameFromObjectSelector<TRight>(rightKey);

            var provider = new JoinQueryProvider<TModel, TRight>(
                Utils,
                TableName, "t0", leftKeyName,
                rightTable, "t1", rightKeyName,
                "LEFT JOIN",
                PrimaryKeyName);

            return new JoinQuery<TModel, TRight>(provider);
        }

        /// <summary>
        /// Extracts a property name from an Expression&lt;Func&lt;T, object&gt;&gt; selector,
        /// handling the Convert wrapper that appears when selecting value-type properties as object.
        /// </summary>
        private static string GetPropertyNameFromObjectSelector<T>(Expression<Func<T, object>> selector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            var body = selector.Body;

            // Unwrap Convert when the property type is a value type selected as object
            if (body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
                body = unary.Operand;

            if (body is MemberExpression member && member.Member is PropertyInfo)
                return member.Member.Name;

            throw new ArgumentException("Expression must be a property access expression (e.g., x => x.PropertyName)", nameof(selector));
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Extracts the MemberExpression from a property selector expression.
        /// </summary>
        private MemberExpression GetMemberExpression<TProperty>(Expression<Func<TModel, TProperty>> propertySelector)
        {
            if (propertySelector == null)
                throw new ArgumentNullException(nameof(propertySelector));

            MemberExpression memberExpression = null;

            if (propertySelector.Body is MemberExpression expr)
            {
                memberExpression = expr;
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

            return memberExpression;
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
            return GetMemberExpression(propertySelector).Member.Name;
        }

        /// <summary>
        /// Extracts the PropertyInfo from a property selector expression.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="propertySelector">The property selector expression</param>
        /// <returns>The PropertyInfo of the property</returns>
        /// <exception cref="ArgumentException">Thrown when the expression is not a valid property selector</exception>
        private PropertyInfo GetPropertyInfo<TProperty>(Expression<Func<TModel, TProperty>> propertySelector)
        {
            return (PropertyInfo)GetMemberExpression(propertySelector).Member;
        }

        #endregion
    }
}
