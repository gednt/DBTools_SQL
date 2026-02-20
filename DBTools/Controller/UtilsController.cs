using DBTools_Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DbTools.Controller
{
    /// <summary>
    /// Helper class to hold WHERE clause parsing results for LINQ expression queries.
    /// </summary>
    internal class WhereClauseResult
    {
        public string WhereClause { get; set; }
        public List<object> Parameters { get; set; }
    }

    /// <summary>
    /// Generic controller for LINQ-style database manipulation compatible with any model type.
    /// Provides Insert, Update, Delete, and Select operations similar to Entity Framework.
    /// </summary>
    /// <typeparam name="TModel">The model type that represents a database table record. 
    /// Must be a reference type (class) with a parameterless constructor. 
    /// Model properties should be public with getters and setters, and property names should match database column names.</typeparam>
    public class UtilsController<TModel> where TModel : class, new()
    {
        private readonly DBTools_Utilities.Utils _utils;
        private readonly string _tableName;
        private readonly string _primaryKeyName;
        private readonly bool _autoIncrement;

        /// <summary>
        /// Initializes a new instance of the UtilsController with database connection settings.
        /// </summary>
        /// <param name="host">Database server address</param>
        /// <param name="database">Database name</param>
        /// <param name="uid">Database user ID</param>
        /// <param name="password">Database password</param>
        /// <param name="tableName">The name of the database table</param>
        /// <param name="port">Database port (default: 1433)</param>
        /// <param name="primaryKeyName">The name of the primary key column (optional)</param>
        /// <param name="autoIncrement">Whether the primary key is auto-incremented (default: true)</param>
        public UtilsController(string tableName, string primaryKeyName = "", bool autoIncrement = true)
        {
            _utils = new DBTools_Utilities.Utils();
            _tableName = tableName;
            _primaryKeyName = primaryKeyName;
            _autoIncrement = autoIncrement;
        }

        /// <summary>
        /// Initializes a new instance of the UtilsController with an existing Utils instance.
        /// </summary>
        /// <param name="utils">An existing Utils instance with database connection configured</param>
        /// <param name="tableName">The name of the database table</param>
        /// <param name="primaryKeyName">The name of the primary key column (optional)</param>
        /// <param name="autoIncrement">Whether the primary key is auto-incremented (default: true)</param>
        public UtilsController(DBTools_Utilities.Utils utils, string tableName, string primaryKeyName = "", bool autoIncrement = true)
        {
            _utils = utils;
            _tableName = tableName;
            _primaryKeyName = primaryKeyName;
            _autoIncrement = autoIncrement;
        }


        /// <summary>
        /// Selects records from the table based on conditions and maps them to model instances.
        /// This is a LINQ-style operation that returns IEnumerable for deferred execution.
        /// </summary>
        /// <param name="conditions">WHERE clause conditions (e.g., "Age > @param0 AND Active = @param1")</param>
        /// <returns>An enumerable collection of TModel instances</returns>
        public IEnumerable<TModel> Select(string conditionsParametrized, IEnumerable<object> parameters )
        {
            DataView dataView = _utils.Select("*", _tableName, conditionsParametrized, parameters.ToArray());
            return MapDataViewToModels(dataView);
        }

        /// <summary>
        /// Inserts a new record into the database from the model instance.
        /// Properties are automatically mapped to database columns.
        /// </summary>
        /// <param name="model">The model instance to insert</param>
        /// <returns>True if the insertion was successful, false otherwise</returns>
        public bool Insert(TModel model)
        {
            var genericObjects = _utils.QueryBuilder(model, _primaryKeyName, _autoIncrement);
            if (genericObjects == null || genericObjects.Count == 0)
                return false;

            var genericObj = genericObjects[0];
            return _utils.Insert(genericObj.columns, _tableName, genericObj.values, _primaryKeyName, _autoIncrement);
        }

        ///<summary>
        ///Inserts a list of model instances into the database.
        ///</summary>
        public bool InsertRange(IEnumerable<TModel> models)
        {
            //Creates a list of sql insert statements
            //And then executes them in a transaction
            var sqlStatements = new List<string>();
            var rollbackStatements = new List<string>();
            List<IEnumerable<SqlParameter>> sqlParameters = new List<IEnumerable<SqlParameter>>();
            foreach (var model in models)
            {
                var genericObjects = _utils.QueryBuilder(model, _primaryKeyName, _autoIncrement);
                if (genericObjects == null || genericObjects.Count == 0)
                    continue;
                var genericObj = genericObjects[0];
   


                string sql = Utils.Insert_Query(genericObj.columns, _tableName, genericObj.valuesString, "Id", true) + ";";
                sqlParameters.Add(Utils.GenerateSqlParameters(genericObj.values));
                sqlStatements.Add(sql);
            }

            for (var cont = 0; cont < sqlStatements.Count;cont++)
            {
                _utils.Query = sqlStatements[cont];
                _utils.SqlParameters = sqlParameters[cont].ToList();
                _utils.ExecuteQuery(sqlStatements[cont]);
            }

            if (_utils.Error != null)
            {
                for (var cont = 0; cont < sqlStatements.Count; cont++)
                {
                    _utils.Query = sqlStatements[cont];
                    _utils.SqlParameters = sqlParameters[cont].ToList();
                    _utils.ExecuteQuery(sqlStatements[cont]);
                }
            }
            if (String.IsNullOrEmpty(_utils.Error))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Updates records in the database based on the model instance and conditions.
        /// Properties are automatically mapped to database columns.
        /// </summary>
        /// <param name="model">The model instance with updated values</param>
        /// <param name="conditions">WHERE clause conditions to identify which records to update (required for security)</param>
        /// <returns>True if the update was successful, false otherwise</returns>
        public bool Update(TModel model, string conditions)
        {
            if (string.IsNullOrEmpty(conditions))
                throw new ArgumentException("Conditions are required for UPDATE operations for security reasons.", nameof(conditions));

            var genericObjects = _utils.QueryBuilder(model, _primaryKeyName, false);
            if (genericObjects == null || genericObjects.Count == 0)
                return false;

            var genericObj = genericObjects[0];
            return _utils.Update(genericObj.columns, _tableName, genericObj.valuesString, conditions);
        }

        /// <summary>
        /// Updates records in the database based on the model instance using parameterized WHERE clause.
        /// This is the recommended method for update operations as it provides better security against SQL injection.
        /// Properties are automatically mapped to database columns.
        /// </summary>
        /// <param name="model">The model instance with updated values</param>
        /// <param name="whereClause">WHERE clause with parameter placeholders (e.g., "id = @whereParam0 AND status = @whereParam1")</param>
        /// <param name="whereParameters">Array of parameter values corresponding to the placeholders in whereClause</param>
        /// <returns>True if the update was successful, false otherwise</returns>
        public bool Update(TModel model, string whereClause, object[] whereParameters)
        {
            if (string.IsNullOrEmpty(whereClause))
                throw new ArgumentException("WHERE clause is required for UPDATE operations for security reasons.", nameof(whereClause));

            if (whereParameters == null)
                throw new ArgumentNullException(nameof(whereParameters), "WHERE parameters array cannot be null. Use empty array for no parameters.");

            var genericObjects = _utils.QueryBuilder(model, _primaryKeyName, false);
            if (genericObjects == null || genericObjects.Count == 0)
                return false;

            var genericObj = genericObjects[0];
            return _utils.Update(genericObj.columns, _tableName, genericObj.valuesString, whereClause, whereParameters);
        }

        /// <summary>
        /// Deletes records from the database based on conditions.
        /// </summary>
        /// <param name="conditions">WHERE clause conditions to identify which records to delete (required for security)</param>
        /// <returns>True if the deletion was successful, false otherwise</returns>
        public bool Delete(string conditions, object[] parameters)
        {
            if (string.IsNullOrEmpty(conditions))
                throw new ArgumentException("Conditions are required for DELETE operations for security reasons.", nameof(conditions));

            return _utils.Delete(_tableName, conditions, parameters);
        }

        /// <summary>
        /// Gets the first record that matches the conditions.
        /// </summary>
        /// <param name="conditions">WHERE clause conditions (optional, empty for all records)</param>
        /// <param name="parameters">Array of parameter values corresponding to the placeholders in conditions</param>
        /// <returns>The first matching model instance or null if not found</returns>
        public TModel FirstOrDefault(string conditions = "", object[] parameters = null)
        {
            return Select(conditions, parameters ?? new object[] { }).FirstOrDefault();
        }

        /// <summary>
        /// Returns all records from the table.
        /// This is a LINQ-style operation that returns IEnumerable for deferred execution.
        /// </summary>
        /// <returns>An enumerable collection of all TModel instances in the table</returns>
        public IEnumerable<TModel> All()
        {
            return Select("", new object[] { });
        }

        /// <summary>
        /// Counts the total number of records in the table that match the given conditions.
        /// </summary>
        /// <param name="conditions">WHERE clause conditions (optional, empty for all records)</param>
        /// <param name="parameters">Array of parameter values corresponding to the placeholders in conditions</param>
        /// <returns>The number of records matching the conditions</returns>
        public int Count(string conditions = "", object[] parameters = null)
        {
            DataView dataView = _utils.Select("COUNT(1) AS RecordCount", _tableName, conditions, parameters ?? new object[] { });
            if (dataView != null && dataView.Count > 0)
            {
                return Convert.ToInt32(dataView[0]["RecordCount"]);
            }
            return 0;
        }

        /// <summary>
        /// Determines whether any records exist that match the specified conditions.
        /// This is more efficient than Count when you only need to check existence.
        /// </summary>
        /// <param name="conditions">WHERE clause conditions (optional, empty to check if table has any records)</param>
        /// <param name="parameters">Array of parameter values corresponding to the placeholders in conditions</param>
        /// <returns>True if any records exist matching the conditions, false otherwise</returns>
        public bool Any(string conditions = "", object[] parameters = null)
        {
            return Count(conditions, parameters) > 0;
        }

        /// <summary>
        /// Finds a record by its primary key value.
        /// This method requires that the primary key name was specified in the constructor.
        /// </summary>
        /// <param name="primaryKeyValue">The value of the primary key to search for</param>
        /// <returns>The model instance if found, null otherwise</returns>
        /// <exception cref="InvalidOperationException">Thrown when primary key name is not specified in the constructor</exception>
        public TModel Find(object primaryKeyValue)
        {
            if (string.IsNullOrEmpty(_primaryKeyName))
                throw new InvalidOperationException("Primary key name must be specified in the constructor to use the Find method.");

            string conditions = $"{_primaryKeyName} = @param0";
            return Select(conditions, new object[] { primaryKeyValue }).FirstOrDefault();
        }

        /// <summary>
        /// Filters records based on the specified conditions.
        /// This is a LINQ-style Where operation that returns IEnumerable for method chaining.
        /// </summary>
        /// <param name="conditions">WHERE clause conditions (e.g., "Age > @param0 AND Active = @param1")</param>
        /// <param name="parameters">Array of parameter values corresponding to the placeholders in conditions</param>
        /// <returns>An enumerable collection of TModel instances matching the conditions</returns>
        public IEnumerable<TModel> Where(string conditions, object[] parameters)
        {
            return Select(conditions, parameters);
        }

        /// <summary>
        /// Gets a single record that matches the conditions. Throws an exception if zero or more than one record is found.
        /// </summary>
        /// <param name="conditions">WHERE clause conditions</param>
        /// <param name="parameters">Array of parameter values corresponding to the placeholders in conditions</param>
        /// <returns>The single matching model instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when zero or more than one record matches the conditions</exception>
        public TModel Single(string conditions, object[] parameters)
        {
            return Select(conditions, parameters).Single();
        }

        /// <summary>
        /// Gets a single record that matches the conditions, or null if no records match.
        /// Throws an exception if more than one record matches.
        /// When called without conditions, returns the only record in the table or throws if there are multiple records.
        /// </summary>
        /// <param name="conditions">WHERE clause conditions (optional, empty to check if table has exactly one record)</param>
        /// <param name="parameters">Array of parameter values corresponding to the placeholders in conditions</param>
        /// <returns>The single matching model instance, or null if no match</returns>
        /// <exception cref="InvalidOperationException">Thrown when more than one record matches the conditions</exception>
        public TModel SingleOrDefault(string conditions = "", object[] parameters = null)
        {
            return Select(conditions, parameters ?? new object[] { }).SingleOrDefault();
        }

        #region LINQ Expression-Based Methods (MySQLDBTools-compatible standards)

        /// <summary>
        /// Returns all records from the table as a list.
        /// </summary>
        /// <returns>A list of all TModel instances in the table</returns>
        public List<TModel> GetAll()
        {
            return Select("", new object[] { }).ToList();
        }

        /// <summary>
        /// Returns all records as IQueryable for advanced LINQ operations such as OrderBy, Skip, Take.
        /// </summary>
        /// <returns>An IQueryable of all TModel instances in the table</returns>
        public IQueryable<TModel> AsQueryable()
        {
            return GetAll().AsQueryable();
        }

        /// <summary>
        /// Inserts a new record into the database. Alias for <see cref="Insert(TModel)"/> following LINQ naming conventions.
        /// </summary>
        /// <param name="entity">The model instance to insert</param>
        /// <returns>True if the insertion was successful, false otherwise</returns>
        public bool Add(TModel entity)
        {
            return Insert(entity);
        }

        /// <summary>
        /// Filters records using a LINQ lambda expression.
        /// Example: <c>Where(u => u.Name == "John")</c>
        /// </summary>
        /// <param name="predicate">A lambda expression representing the WHERE condition</param>
        /// <returns>A list of TModel instances matching the predicate</returns>
        public List<TModel> Where(Expression<Func<TModel, bool>> predicate)
        {
            var result = ParseWhereExpression(predicate);
            return Select(result.WhereClause, result.Parameters).ToList();
        }

        /// <summary>
        /// Gets the first record matching the predicate, or null if not found.
        /// Example: <c>FirstOrDefault(u => u.Id == 1)</c>
        /// </summary>
        /// <param name="predicate">A lambda expression representing the WHERE condition</param>
        /// <returns>The first matching TModel instance, or null if no match</returns>
        public TModel FirstOrDefault(Expression<Func<TModel, bool>> predicate)
        {
            return Where(predicate).FirstOrDefault();
        }

        /// <summary>
        /// Gets a single record matching the predicate, or null if no records match.
        /// Throws an exception if more than one record matches.
        /// Example: <c>SingleOrDefault(u => u.Id == 1)</c>
        /// </summary>
        /// <param name="predicate">A lambda expression representing the WHERE condition</param>
        /// <returns>The single matching TModel instance, or null if no match</returns>
        /// <exception cref="InvalidOperationException">Thrown when more than one record matches the predicate</exception>
        public TModel SingleOrDefault(Expression<Func<TModel, bool>> predicate)
        {
            var results = Where(predicate);
            if (results.Count > 1)
                throw new InvalidOperationException("Sequence contains more than one element");
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Checks if any records exist that match the specified predicate.
        /// Example: <c>Any(u => u.Name == "John")</c>
        /// </summary>
        /// <param name="predicate">A lambda expression representing the WHERE condition</param>
        /// <returns>True if any records match the predicate, false otherwise</returns>
        public bool Any(Expression<Func<TModel, bool>> predicate)
        {
            return Count(predicate) > 0;
        }

        /// <summary>
        /// Counts records matching the specified predicate.
        /// Example: <c>Count(u => u.Age > 18)</c>
        /// </summary>
        /// <param name="predicate">A lambda expression representing the WHERE condition</param>
        /// <returns>The number of records matching the predicate</returns>
        public int Count(Expression<Func<TModel, bool>> predicate)
        {
            var result = ParseWhereExpression(predicate);
            DataView dataView = _utils.Select("COUNT(1) AS RecordCount", _tableName, result.WhereClause, result.Parameters.ToArray());
            if (dataView != null && dataView.Count > 0)
                return Convert.ToInt32(dataView[0]["RecordCount"]);
            return 0;
        }

        /// <summary>
        /// Updates records in the database using a LINQ lambda expression to identify records.
        /// Example: <c>Update(updatedUser, u => u.Id == 1)</c>
        /// </summary>
        /// <param name="model">The model instance with updated values</param>
        /// <param name="predicate">A lambda expression representing the WHERE condition</param>
        /// <returns>True if the update was successful, false otherwise</returns>
        public bool Update(TModel model, Expression<Func<TModel, bool>> predicate)
        {
            var result = ParseWhereExpression(predicate);
            if (string.IsNullOrEmpty(result.WhereClause))
                throw new ArgumentException("WHERE clause is required for UPDATE operations for security reasons.", nameof(predicate));
            return Update(model, result.WhereClause, result.Parameters.ToArray());
        }

        /// <summary>
        /// Deletes records from the database using a LINQ lambda expression.
        /// Example: <c>Remove(u => u.Id == 1)</c>
        /// </summary>
        /// <param name="predicate">A lambda expression representing the WHERE condition</param>
        /// <returns>True if the deletion was successful, false otherwise</returns>
        public bool Remove(Expression<Func<TModel, bool>> predicate)
        {
            var result = ParseWhereExpression(predicate);
            if (string.IsNullOrEmpty(result.WhereClause))
                throw new ArgumentException("WHERE clause is required for DELETE operations for security reasons.", nameof(predicate));
            return Delete(result.WhereClause, result.Parameters.ToArray());
        }

        /// <summary>
        /// Saves changes to an entity by updating the record identified by its primary key.
        /// Requires that the primary key name was specified in the constructor.
        /// Example: <c>SaveChanges(user)</c>
        /// </summary>
        /// <param name="entity">The model instance with updated values; its primary key property identifies the record to update</param>
        /// <returns>True if the update was successful, false otherwise</returns>
        /// <exception cref="InvalidOperationException">Thrown when primary key name is not specified or the primary key value is null</exception>
        public bool SaveChanges(TModel entity)
        {
            if (string.IsNullOrEmpty(_primaryKeyName))
                throw new InvalidOperationException("Primary key name must be specified in the constructor to use SaveChanges.");

            var pkProperty = typeof(TModel).GetProperty(_primaryKeyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (pkProperty == null)
                throw new InvalidOperationException($"Primary key property '{_primaryKeyName}' not found on type {typeof(TModel).Name}.");

            var pkValue = pkProperty.GetValue(entity);
            if (pkValue == null)
                throw new InvalidOperationException("Primary key value cannot be null for SaveChanges.");

            string whereClause = $"{_primaryKeyName} = @whereParam0";
            return Update(entity, whereClause, new object[] { pkValue });
        }

        #endregion

        #region Expression Parsing Helpers

        /// <summary>
        /// Parses a LINQ lambda expression into a SQL WHERE clause with parameter values.
        /// </summary>
        private WhereClauseResult ParseWhereExpression(Expression<Func<TModel, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var parameters = new List<object>();
            string whereClause = ParseExpression(predicate.Body, parameters);
            return new WhereClauseResult
            {
                WhereClause = whereClause,
                Parameters = parameters
            };
        }

        /// <summary>
        /// Recursively parses an expression tree node into a SQL fragment.
        /// </summary>
        private string ParseExpression(Expression expression, List<object> parameters)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.AndAlso:
                    var andExp = (BinaryExpression)expression;
                    return $"({ParseExpression(andExp.Left, parameters)}) AND ({ParseExpression(andExp.Right, parameters)})";

                case ExpressionType.OrElse:
                    var orExp = (BinaryExpression)expression;
                    return $"({ParseExpression(orExp.Left, parameters)}) OR ({ParseExpression(orExp.Right, parameters)})";

                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    return ParseBinaryExpression((BinaryExpression)expression, parameters);

                case ExpressionType.Not:
                    var notExp = (UnaryExpression)expression;
                    return $"NOT ({ParseExpression(notExp.Operand, parameters)})";

                case ExpressionType.MemberAccess:
                    var memberExp = (MemberExpression)expression;
                    if (memberExp.Expression != null && memberExp.Expression.NodeType == ExpressionType.Parameter)
                        return memberExp.Member.Name;
                    break;

                case ExpressionType.Constant:
                    var constExp = (ConstantExpression)expression;
                    string paramName = $"@param{parameters.Count}";
                    parameters.Add(constExp.Value ?? DBNull.Value);
                    return paramName;
            }

            // For complex expressions (captured variables, method calls, etc.), evaluate them
            try
            {
                var value = Expression.Lambda(expression).Compile().DynamicInvoke();
                string paramName = $"@param{parameters.Count}";
                parameters.Add(value ?? DBNull.Value);
                return paramName;
            }
            catch (Exception ex)
            {
                throw new NotSupportedException($"Expression type '{expression.NodeType}' is not supported.", ex);
            }
        }

        /// <summary>
        /// Parses a binary comparison expression into a SQL condition.
        /// </summary>
        private string ParseBinaryExpression(BinaryExpression expression, List<object> parameters)
        {
            string left = ParseExpression(expression.Left, parameters);
            string right = ParseExpression(expression.Right, parameters);

            string op = expression.NodeType switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "!=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                _ => throw new NotSupportedException($"Binary operator '{expression.NodeType}' is not supported.")
            };

            return $"{left} {op} {right}";
        }

        #endregion

        /// <summary>
        /// Provides access to the underlying Utils instance for advanced operations.
        /// </summary>
        public DBTools_Utilities.Utils Utils => _utils;

        /// <summary>
        /// Gets the last error message from the database operations.
        /// </summary>
        public string Error => _utils.Error;

        /// <summary>
        /// Maps a DataView to a collection of model instances using reflection.
        /// </summary>
        private IEnumerable<TModel> MapDataViewToModels(DataView dataView)
        {
            var models = new List<TModel>();
            if (dataView == null || dataView.Count == 0)
                return models;

            var properties = typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            foreach (DataRowView row in dataView)
            {
                var model = new TModel();
                foreach (DataColumn column in dataView.Table.Columns)
                {
                    if (properties.TryGetValue(column.ColumnName, out PropertyInfo property))
                    {
                        try
                        {
                            var value = row[column.ColumnName];
                            if (value != null && value != DBNull.Value)
                            {
                                // Handle type conversion
                                if (property.PropertyType != value.GetType())
                                {
                                    if (property.PropertyType.IsGenericType &&
                                        property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                    {
                                        // Handle nullable types
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
                        catch (InvalidCastException)
                        {
                            // Skip properties that can't be cast to the target type.
                            // This is expected behavior when database columns don't map perfectly to model properties.
                            // Users should ensure property types match column types for critical fields.
                        }
                        catch (FormatException)
                        {
                            // Skip properties with invalid format.
                            // This can occur when string values can't be parsed into numeric or date types.
                        }
                        catch (OverflowException)
                        {
                            // Skip properties where the value is outside the range of the target type.
                            // For example, a BIGINT value that's too large for an Int32 property.
                        }
                        catch (ArgumentException)
                        {
                            // Skip properties with invalid arguments during conversion.
                            // This can occur with incompatible type conversions.
                        }
                    }
                }
                models.Add(model);
            }

            return models;
        }
    }
}