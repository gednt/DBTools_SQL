using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DBTools.Controller
{
    /// <summary>
    /// Generic controller for LINQ-style database manipulation compatible with any model type.
    /// Provides Insert, Update, Delete, and Select operations similar to Entity Framework.
    /// </summary>
    /// <typeparam name="TModel">The model type that represents a database table record</typeparam>
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
        public UtilsController(string host, string database, string uid, string password, string tableName, string port = "1433", string primaryKeyName = "", bool autoIncrement = true)
        {
            _utils = new DBTools_Utilities.Utils(host, database, uid, password, port);
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
        /// Selects all records from the table and maps them to model instances.
        /// This is a LINQ-style operation that returns IEnumerable for deferred execution.
        /// </summary>
        /// <returns>An enumerable collection of TModel instances</returns>
        public IEnumerable<TModel> Select()
        {
            return Select("");
        }

        /// <summary>
        /// Selects records from the table based on conditions and maps them to model instances.
        /// This is a LINQ-style operation that returns IEnumerable for deferred execution.
        /// </summary>
        /// <param name="conditions">WHERE clause conditions (e.g., "Age > 18 AND Active = 1")</param>
        /// <returns>An enumerable collection of TModel instances</returns>
        public IEnumerable<TModel> Select(string conditions)
        {
            DataView dataView = _utils.Select("*", _tableName, conditions);
            return MapDataViewToModels(dataView);
        }

        /// <summary>
        /// Selects records from the table based on a predicate function.
        /// This provides LINQ-style filtering capability.
        /// Note: This loads all records first, then filters in memory. For large datasets, use the string-based Select with SQL conditions.
        /// </summary>
        /// <param name="predicate">A function to filter the records</param>
        /// <returns>An enumerable collection of filtered TModel instances</returns>
        public IEnumerable<TModel> Where(Func<TModel, bool> predicate)
        {
            return Select().Where(predicate);
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
            return _utils.Insert(genericObj.columns, _tableName, genericObj.valuesString);
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
        /// Deletes records from the database based on conditions.
        /// </summary>
        /// <param name="conditions">WHERE clause conditions to identify which records to delete (required for security)</param>
        /// <returns>True if the deletion was successful, false otherwise</returns>
        public bool Delete(string conditions)
        {
            if (string.IsNullOrEmpty(conditions))
                throw new ArgumentException("Conditions are required for DELETE operations for security reasons.", nameof(conditions));

            return _utils.Delete(_tableName, conditions);
        }

        /// <summary>
        /// Gets the first record that matches the conditions.
        /// </summary>
        /// <param name="conditions">WHERE clause conditions (optional)</param>
        /// <returns>The first matching model instance or null if not found</returns>
        public TModel FirstOrDefault(string conditions = "")
        {
            return Select(conditions).FirstOrDefault();
        }

        /// <summary>
        /// Gets the first record that matches the predicate.
        /// Note: This loads all records first, then filters in memory. For large datasets, use the string-based FirstOrDefault.
        /// </summary>
        /// <param name="predicate">A function to filter the records</param>
        /// <returns>The first matching model instance or null if not found</returns>
        public TModel FirstOrDefault(Func<TModel, bool> predicate)
        {
            return Select().FirstOrDefault(predicate);
        }

        /// <summary>
        /// Gets all records from the table as a list.
        /// </summary>
        /// <returns>A list of all model instances</returns>
        public List<TModel> ToList()
        {
            return Select().ToList();
        }

        /// <summary>
        /// Gets all records that match the conditions as a list.
        /// </summary>
        /// <param name="conditions">WHERE clause conditions</param>
        /// <returns>A list of matching model instances</returns>
        public List<TModel> ToList(string conditions)
        {
            return Select(conditions).ToList();
        }

        /// <summary>
        /// Counts the number of records that match the conditions using a database-level COUNT query for efficiency.
        /// </summary>
        /// <param name="conditions">WHERE clause conditions (optional)</param>
        /// <returns>The count of matching records</returns>
        public int Count(string conditions = "")
        {
            // Use database-level COUNT for better performance
            DataView result = _utils.Select("COUNT(*) AS RecordCount", _tableName, conditions);
            if (result != null && result.Count > 0)
            {
                return Convert.ToInt32(result[0]["RecordCount"]);
            }
            return 0;
        }

        /// <summary>
        /// Checks if any records exist that match the conditions using a database-level query for efficiency.
        /// </summary>
        /// <param name="conditions">WHERE clause conditions (optional)</param>
        /// <returns>True if any matching records exist, false otherwise</returns>
        public bool Any(string conditions = "")
        {
            // Use database-level COUNT for better performance
            return Count(conditions) > 0;
        }

        /// <summary>
        /// Checks if any records exist that match the predicate.
        /// Note: This loads all records first, then filters in memory. For large datasets, use the string-based Any.
        /// </summary>
        /// <param name="predicate">A function to filter the records</param>
        /// <returns>True if any matching records exist, false otherwise</returns>
        public bool Any(Func<TModel, bool> predicate)
        {
            return Select().Any(predicate);
        }

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
                            // Skip properties that can't be cast to the target type
                        }
                        catch (FormatException)
                        {
                            // Skip properties with invalid format
                        }
                        catch (OverflowException)
                        {
                            // Skip properties where the value is outside the range of the target type
                        }
                        catch (ArgumentException)
                        {
                            // Skip properties with invalid arguments during conversion
                        }
                    }
                }
                models.Add(model);
            }

            return models;
        }
    }
}
