using DbTools.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
namespace DBTools_Utilities
{
    /// <summary>
    /// Utils Class of the Sql DBTools Library
    /// </summary>
    public class Utils : DbTools.DBTools
    {
        public Utils()
        {
            var basePath = Directory.GetCurrentDirectory();
            var configFilePath = Path.Combine(basePath, "config.json");

            if (!File.Exists(configFilePath))
            {
                throw new FileNotFoundException(
                    $"The configuration file 'config.json' was not found in directory '{basePath}'.",
                    configFilePath);
            }

            try
            {
                IConfiguration configuration = new ConfigurationBuilder()
                    .SetBasePath(basePath) // Set the base path for file providers
                    .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                    .Build();

                this.Host = configuration["Host"];
                this.Database = configuration["Database"];
                this.Uid = configuration["Uid"];
                this.Password = configuration["Password"];
                this.Port = configuration["Port"];

                var missingKeys = new List<string>();
                if (string.IsNullOrWhiteSpace(this.Host)) missingKeys.Add("Host");
                if (string.IsNullOrWhiteSpace(this.Database)) missingKeys.Add("Database");
                if (string.IsNullOrWhiteSpace(this.Uid)) missingKeys.Add("Uid");
                if (string.IsNullOrWhiteSpace(this.Password)) missingKeys.Add("Password");
                if (string.IsNullOrWhiteSpace(this.Port)) missingKeys.Add("Port");

                if (missingKeys.Count > 0)
                {
                    throw new InvalidOperationException(
                        "The following required configuration keys are missing or empty in 'config.json': " +
                        string.Join(", ", missingKeys));
                }
            }
            catch (InvalidOperationException)
            {
                // Re-throw our own validation exceptions as-is
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to load database configuration from 'config.json'. See inner exception for details.",
                    ex);
            }
        }
        #region Helper methods for validation
        /// <summary>
        /// Static regex for validating identifiers (compiled once for performance).
        /// Allows: alphanumeric, underscore, dot (schema.table), brackets ([table]), 
        /// comma and space (for field lists like "id, name"), and asterisk (for SELECT *)
        /// </summary>
        private static readonly System.Text.RegularExpressions.Regex IdentifierRegex =
            new System.Text.RegularExpressions.Regex(@"^[\w\.\[\]\,\s\*]+$", System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// Validates an identifier (table name, column name) to prevent SQL injection.
        /// Only allows alphanumeric characters, underscores, dots, brackets, and spaces.
        /// Also checks for forbidden SQL keywords (DROP, DELETE - case-insensitive) and 
        /// common SQL injection patterns (SQL comment patterns: --, ;--, /*, */).
        /// </summary>
        private static bool IsValidIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            // Check for forbidden SQL keywords (case-insensitive)
            string upperIdentifier = identifier.ToUpper();
            if (upperIdentifier.Contains("DROP") || upperIdentifier.Contains("DELETE"))
                return false;

            // Check for common SQL injection patterns (SQL comments)
            if (identifier.Contains("--") || identifier.Contains(";--") || 
                identifier.Contains("/*") || identifier.Contains("*/"))
                return false;

            // Allow alphanumeric, underscore, dot (for schema.table), brackets (for [table]), and comma/space for field lists
            return IdentifierRegex.IsMatch(identifier);
        }
        #endregion

        #region query utilities
        /// <summary>
        /// Returns a list of a representation of any given object to be used into the Insert and select clauses of this library.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public List<GenericObject> QueryBuilder(Object obj, String primaryKeyName = "", bool autoIncrement = true)
        {
            connectDB();
            var arrayObject = obj.GetType().GetProperties();
            List<GenericObject_Simple> values = new List<GenericObject_Simple>();
            //List<String> fields = new List<string>();
            //List<String> type = new List<string>();
            List<GenericObject> lstReturn = new List<GenericObject>();
            List<String> columns = new List<string>();
            List<Object> valuesReturn = new List<Object>();
            List<String> typesReturn = new List<string>();
            int cont = 0;
            foreach (var i in arrayObject)
            {

                try
                {
                    if (i.PropertyType.Name.ToString() == "DateTime")
                    {
                        values.Add(new GenericObject_Simple
                        {
                            value = DateTime.Parse((string)obj.GetType().GetProperty(i.Name).GetValue(obj, null).ToString()).ToString("yyyy-MM-dd HH:mm:ss")
                            ,
                            column = i.Name
                            ,
                            type = i.PropertyType.Name
                        });
                        // type.Add(i.PropertyType.Name);

                    }
                    else
                    {
                        switch (autoIncrement)
                        {
                            case false:
                                //  fields.Add(i.Name);
                                values.Add(new GenericObject_Simple
                                {
                                    value = (string)obj.GetType().GetProperty(i.Name).GetValue(obj, null).ToString()
                                  ,
                                    column = i.Name
                                  ,
                                    type = i.PropertyType.Name
                                });
                                // type.Add(i.PropertyType.Name);
                                cont++;
                                break;
                            case true:
                                if (i.Name == primaryKeyName)
                                {

                                }
                                else
                                {
                                    values.Add(new GenericObject_Simple
                                    {
                                        value = (string)obj.GetType().GetProperty(i.Name).GetValue(obj, null).ToString()
                                        ,
                                        column = i.Name
                                        ,
                                        type = i.PropertyType.Name
                                    });
                                    cont++;
                                }
                                break;
                        }






                    }

                }
                catch
                {

                }



            }
            List<String> valueString = new List<string>();
            foreach (var value in values)
            {
                valueString.Add(value.value.ToString());
            }

            for (cont = 0; cont < values.Count; cont++)
            {
                columns.Add(values[cont].column);
                valuesReturn.Add(values[cont].value);
                typesReturn.Add(values[cont].type);
            }
            lstReturn.Add(new GenericObject { columns = columns.ToArray(), values = valuesReturn.ToArray(), types = typesReturn.ToArray(), valuesString = valueString.ToArray() });





            return lstReturn;


        }
        public void connectDB()
        {
            setDataBase(Database);
            setHost(Host);
            setPassword(Password);
            setUid(Uid);
        }
        /// <summary>
        /// Returns a string array of the objects of the Database
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        /// 

        public String[] getInBd(String query)
        {
            setQuery(query);

            DataView dv = new DataView();
            dv = RetrieveDataSql();
            String[] arrayQuery = new String[dv.Count];
            for (int cont = 0; cont < dv.Count; cont++)
            {
                arrayQuery[cont] = dv[cont][0].ToString();
            }
            return arrayQuery;


        }
        /// <summary>
        /// Returns a Dataview of the objects of the Database
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public DataView getInBdDv(String query)
        {
            setQuery(query);

            DataView dv = new DataView();
            try
            {
                dv = RetrieveDataSql();
            }
            catch (Exception e)
            {
                Error = e.ToString();
                // System.Windows.MessageBox.Show(error);
                return null;

            }
            String[] arrayQuery = new String[dv.Count];
            for (int cont = 0; cont < dv.Count; cont++)
            {
                arrayQuery[cont] = dv[cont][0].ToString();
            }
            return dv;
        }
        /// <summary>
        /// Executes any sql query that returns no value
        /// </summary>
        /// <param name="query"></param>
        public void ExecuteQuery(String query)
        {
            setQuery(query);
            SqlExecuteQuery();


        }
        #endregion

        #region Data Manipulation modules
        //MODULOS DE MANIPULAÇAO DE DADOS
        /// <summary>
        /// Returns a DataView based on the parameters given<br/>
        /// This class can and should be used with the <see cref="QueryBuilder(object)">QueryBuilder Command</see>
        /// </summary>
        /// <param name="_fields"></param>
        /// <param name="_table"></param>
        /// <param name="_conditions"></param>
        /// <returns></returns>
        [Obsolete("This method is deprecated. Use the overload with parameterized conditions for better security.", false)]
        public DataView Select(String _fields, String _table, String _conditions)
        {
            // Validate identifiers to prevent SQL injection
            if (!IsValidIdentifier(_fields))
                throw new ArgumentException("Invalid field names. Only alphanumeric characters, underscores, dots, brackets, commas, and spaces are allowed.", nameof(_fields));

            if (!IsValidIdentifier(_table))
                throw new ArgumentException("Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_table));

            String query = "";
            if (_conditions != "")
            {
                query = String.Format("SELECT {0} FROM {1} WHERE {2}", _fields, _table, _conditions);
            }
            else
            {
                query = String.Format("SELECT {0} FROM {1}", _fields, _table);
            }

            return getInBdDv(query);


        }

        /// <summary>
        /// Returns a DataView based on the parameters given with parameterized WHERE clause<br/>
        /// This method uses parameterized queries to prevent SQL injection attacks.<br/>
        /// </summary>
        /// <param name="_fields">Field names to select (e.g., "id, name" or "*")</param>
        /// <param name="_table">Table name</param>
        /// <param name="whereClause">WHERE clause with parameter placeholders (e.g., "id = @param0 AND status = @param1")</param>
        /// <param name="parameters">Array of parameter values corresponding to the placeholders in whereClause</param>
        /// <returns>DataView with the query results</returns>
        public DataView Select(String _fields, String _table, String whereClause, Object[] parameters)
        {
            // Validate identifiers to prevent SQL injection
            if (!IsValidIdentifier(_fields))
                throw new ArgumentException("Invalid field names. Only alphanumeric characters, underscores, dots, brackets, commas, and spaces are allowed.", nameof(_fields));

            if (!IsValidIdentifier(_table))
                throw new ArgumentException("Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_table));

            // Validate parameters array
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters), "Parameters array cannot be null. Use empty array for no parameters.");

            String query = "";
            List<SqlParameter> sqlParams = new List<SqlParameter>();

            // Build parameter list
            for (int i = 0; i < parameters.Length; i++)
            {
                string paramName = "@param" + i;
                sqlParams.Add(new SqlParameter(paramName, parameters[i] ?? (object)DBNull.Value));
            }

            // Build the query
            if (!string.IsNullOrEmpty(whereClause))
            {
                query = String.Format("SELECT {0} FROM {1} WHERE {2}", _fields, _table, whereClause);
            }
            else
            {
                query = String.Format("SELECT {0} FROM {1}", _fields, _table);
            }

            // Set parameters and execute query
            SqlParameters = sqlParams;
            DataView result = getInBdDv(query);
            SqlParameters = null; // Clear parameters after use

            return result;
        }
        /// <summary>
        /// Inserts the data into the database based on the parameters given<br/>
        /// This class can and should be used with the <see cref="QueryBuilder(object)">QueryBuilder Command</see>
        /// </summary>
        /// <param name="_fields">Array of field names to insert into</param>
        /// <param name="_table">Table name</param>
        /// <param name="_values">Array of values to insert corresponding to the fields</param>
        /// <returns></returns>
        public bool Insert(String[] _fields, String _table, String[] _values, string primary_key_name = null, bool auto_increment = true)
        {
            // Validate table name to prevent SQL injection
            if (!IsValidIdentifier(_table))
                throw new ArgumentException("Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_table));

            // Validate arrays are not empty
            if (_fields == null || _fields.Length == 0)
                throw new ArgumentException("Fields array cannot be null or empty.", nameof(_fields));

            if (_values == null || _values.Length == 0)
                throw new ArgumentException("Values array cannot be null or empty.", nameof(_values));

            if (_fields.Length != _values.Length)
                throw new ArgumentException("Field and value arrays must have the same length.");

            if (String.IsNullOrEmpty(primary_key_name))
            {
                throw new ArgumentException("Primary key name must be provided.", nameof(primary_key_name));

            }

            if (auto_increment.Equals(false))
            {
                var fieldsToLower = Array.ConvertAll(_fields, field => field.ToLower());
                int index_of_primary_key = Array.IndexOf(fieldsToLower, primary_key_name, 0);
                //Excludes the primary_key name if it is filled and the corresponding value
                _values = Array.FindAll(_values, value => value != _values[index_of_primary_key]);
                _fields = Array.FindAll(_fields, field => field != _fields[index_of_primary_key]);
            }
            // Validate field names to prevent SQL injection
            foreach (var field in _fields)
            {
                if (!IsValidIdentifier(field))
                    throw new ArgumentException($"Invalid field name '{field}'. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_fields));
            }

            String fields = "";
            String paramPlaceholders = "";
            List<SqlParameter> parameters = new List<SqlParameter>();

            // Build field list and parameter placeholders
            for (int cont = 0; cont < _fields.Length; cont++)
            {
                fields += _fields[cont] + ",";
                string paramName = "@param" + cont;
                paramPlaceholders += paramName + ",";
                parameters.Add(new SqlParameter(paramName, _values[cont] ?? (object)DBNull.Value));
            }

            fields = fields.Remove(fields.Length - 1, 1);
            paramPlaceholders = paramPlaceholders.Remove(paramPlaceholders.Length - 1, 1);

            // Build the query
            String query = String.Format("INSERT INTO {0}({1}) VALUES({2})", _table, fields, paramPlaceholders);

            // Set parameters and execute query
            SqlParameters = parameters;
            ExecuteQuery(query);
            SqlParameters = null; // Clear parameters after use

            if (Error != null)
            {
                return false;
            }
            return true;


        }
        /// <summary>
        /// Updates the database
        /// </summary>
        /// <param name="_fields"></param>
        /// <param name="_table"></param>
        /// <param name="_values"></param>
        /// <returns></returns>
        [Obsolete("This method is deprecated. Use the overload with parameterized WHERE clause for better security.", false)]
        public bool Update(String[] _fields, String _table, String[] _values, String condition = "")
        {
            // Validate table name to prevent SQL injection
            if (!IsValidIdentifier(_table))
                throw new ArgumentException("Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_table));

            // Validate arrays are not empty
            if (_fields == null || _fields.Length == 0)
                throw new ArgumentException("Fields array cannot be null or empty.", nameof(_fields));

            if (_values == null || _values.Length == 0)
                throw new ArgumentException("Values array cannot be null or empty.", nameof(_values));

            if (_fields.Length != _values.Length)
                throw new ArgumentException("Field and value arrays must have the same length.");

            if (string.IsNullOrEmpty(condition))
                throw new ArgumentException("Condition is required for UPDATE operations for security reasons.", nameof(condition));

            // Validate field names to prevent SQL injection
            foreach (var field in _fields)
            {
                if (!IsValidIdentifier(field))
                    throw new ArgumentException($"Invalid field name '{field}'. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_fields));
            }

            String setClause = "";
            List<SqlParameter> parameters = new List<SqlParameter>();

            // Build SET clause with parameters
            for (int cont = 0; cont < _fields.Length; cont++)
            {
                string paramName = "@param" + cont;
                setClause += _fields[cont] + "=" + paramName + ",";
                parameters.Add(new SqlParameter(paramName, _values[cont] ?? (object)DBNull.Value));
            }

            setClause = setClause.Substring(0, setClause.Length - 1);

            // Build the query
            String query = String.Format("UPDATE {0} SET {1} WHERE {2}", _table, setClause, condition);

            // Set parameters and execute query
            SqlParameters = parameters;
            ExecuteQuery(query);
            SqlParameters = null; // Clear parameters after use

            if (Error != null)
            {
                return false;
            }
            return true;


        }

        /// <summary>
        /// Updates the database with parameterized WHERE clause for better security
        /// </summary>
        /// <param name="_fields">Array of field names to update</param>
        /// <param name="_table">Table name</param>
        /// <param name="_values">Array of values to update corresponding to the fields</param>
        /// <param name="whereClause">WHERE clause with parameter placeholders (e.g., "id = @whereParam0")</param>
        /// <param name="whereParameters">Array of parameter values for the WHERE clause</param>
        /// <returns>True if successful, false if error occurred</returns>
        public bool Update(String[] _fields, String _table, String[] _values, String whereClause, Object[] whereParameters)
        {
            // Validate table name to prevent SQL injection
            if (!IsValidIdentifier(_table))
                throw new ArgumentException("Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_table));

            // Validate arrays are not empty
            if (_fields == null || _fields.Length == 0)
                throw new ArgumentException("Fields array cannot be null or empty.", nameof(_fields));

            if (_values == null || _values.Length == 0)
                throw new ArgumentException("Values array cannot be null or empty.", nameof(_values));

            if (_fields.Length != _values.Length)
                throw new ArgumentException("Field and value arrays must have the same length.");

            if (string.IsNullOrEmpty(whereClause))
                throw new ArgumentException("WHERE clause is required for UPDATE operations for security reasons.", nameof(whereClause));

            if (whereParameters == null)
                throw new ArgumentNullException(nameof(whereParameters), "WHERE parameters array cannot be null. Use empty array for no parameters.");

            // Validate field names to prevent SQL injection
            foreach (var field in _fields)
            {
                if (!IsValidIdentifier(field))
                    throw new ArgumentException($"Invalid field name '{field}'. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_fields));
            }

            String setClause = "";
            List<SqlParameter> parameters = new List<SqlParameter>();

            // Build SET clause with parameters
            for (int cont = 0; cont < _fields.Length; cont++)
            {
                string paramName = "@param" + cont;
                setClause += _fields[cont] + "=" + paramName + ",";
                parameters.Add(new SqlParameter(paramName, _values[cont] ?? (object)DBNull.Value));
            }

            setClause = setClause.Substring(0, setClause.Length - 1);

            // Add WHERE clause parameters
            for (int i = 0; i < whereParameters.Length; i++)
            {
                string paramName = "@whereParam" + i;
                parameters.Add(new SqlParameter(paramName, whereParameters[i] ?? (object)DBNull.Value));
            }

            // Build the query
            String query = String.Format("UPDATE {0} SET {1} WHERE {2}", _table, setClause, whereClause);

            // Set parameters and execute query
            SqlParameters = parameters;
            ExecuteQuery(query);
            SqlParameters = null; // Clear parameters after use

            if (Error != null)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Deletes the row from the database.<br/>
        /// For security reasons, the use of a condition is mandatory.
        /// </summary>
        /// <param name="_table"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        [Obsolete("This method is deprecated. Use the overload with parameterized conditions for better security.", false)]
        public bool Delete(String _table, String condition)
        {
            // Validate table name to prevent SQL injection
            if (!IsValidIdentifier(_table))
                throw new ArgumentException("Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_table));

            if (string.IsNullOrEmpty(condition))
                throw new ArgumentException("Condition is required for DELETE operations for security reasons.", nameof(condition));

            // Build the query
            String query = String.Format("DELETE FROM {0} WHERE {1}", _table, condition);

            // Execute query
            ExecuteQuery(query);

            if (Error != null)
            {
                return false;
            }
            return true;


        }

        /// <summary>
        /// Deletes rows from the database using parameterized WHERE clause.<br/>
        /// For security reasons, the use of a condition is mandatory.<br/>
        /// This method uses parameterized queries to prevent SQL injection attacks.
        /// </summary>
        /// <param name="_table">Table name</param>
        /// <param name="whereClause">WHERE clause with parameter placeholders (e.g., "id = @param0")</param>
        /// <param name="parameters">Array of parameter values corresponding to the placeholders in whereClause</param>
        /// <returns>True if successful, false if error occurred</returns>
        public bool Delete(String _table, String whereClause, Object[] parameters)
        {
            // Validate table name to prevent SQL injection
            if (!IsValidIdentifier(_table))
                throw new ArgumentException("Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_table));

            if (string.IsNullOrEmpty(whereClause))
                throw new ArgumentException("Condition is required for DELETE operations for security reasons.", nameof(whereClause));

            // Validate parameters array
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters), "Parameters array cannot be null. Use empty array for no parameters.");

            List<SqlParameter> sqlParams = new List<SqlParameter>();

            // Build parameter list
            for (int i = 0; i < parameters.Length; i++)
            {
                string paramName = "@param" + i;
                sqlParams.Add(new SqlParameter(paramName, parameters[i] ?? (object)DBNull.Value));
            }

            // Build the query
            String query = String.Format("DELETE FROM {0} WHERE {1}", _table, whereClause);

            // Set parameters and execute query
            SqlParameters = sqlParams;
            ExecuteQuery(query);
            SqlParameters = null; // Clear parameters after use

            if (Error != null)
            {
                return false;
            }
            return true;
        }
        //MODULOS DE MANIPULAÇAO DE DADOS
        /// <summary>
        /// Returns a DataView based on the query without the select clause<br/>
        /// Note: This method is deprecated. Use the overload with parameterized queries for better security.
        /// </summary>
        /// <param name="query_without_select"></param>
        /// <returns></returns>
        [Obsolete("This method is deprecated. Use the overload with parameters for better security.", false)]
        public DataView Select(String query_without_select)
        {



            return getInBdDv("SELECT " + query_without_select);


        }

        /// <summary>
        /// Returns a DataView based on the query without the select clause using parameterized queries<br/>
        /// This method uses parameterized queries to prevent SQL injection attacks.<br/>
        /// </summary>
        /// <param name="query_without_select">Query without SELECT keyword (e.g., "* FROM users WHERE id = @param0")</param>
        /// <param name="parameters">Array of parameter values corresponding to the placeholders in the query</param>
        /// <returns>DataView with the query results</returns>
        public DataView Select(String query_without_select, Object[] parameters)
        {
            // Validate parameters array
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters), "Parameters array cannot be null. Use empty array for no parameters.");

            List<SqlParameter> sqlParams = new List<SqlParameter>();

            // Build parameter list
            for (int i = 0; i < parameters.Length; i++)
            {
                string paramName = "@param" + i;
                sqlParams.Add(new SqlParameter(paramName, parameters[i] ?? (object)DBNull.Value));
            }

            // Set parameters and execute query
            SqlParameters = sqlParams;
            DataView result = getInBdDv("SELECT " + query_without_select);
            SqlParameters = null; // Clear parameters after use

            return result;
        }
        #endregion

        #region métodos helpers de queries de banco de dados
        //MODULOS DE MANIPULAÇAO DE DADOS
        /// <summary>
        /// Returns a string based on the parameters given<br/>
        /// Note: This method returns a query string with string concatenation. For better security, use the non-static Select method with parameterized queries.
        /// </summary>
        /// <param name="_fields"></param>
        /// <param name="_table"></param>
        /// <param name="_conditions"></param>
        /// <returns></returns>
        public static string Select_Query(String _fields, String _table, String _conditions)
        {
            // Validate identifiers to prevent SQL injection
            if (!IsValidIdentifier(_fields))
                throw new ArgumentException("Invalid field names. Only alphanumeric characters, underscores, dots, brackets, commas, and spaces are allowed.", nameof(_fields));

            if (!IsValidIdentifier(_table))
                throw new ArgumentException("Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_table));

            String query = "";
            if (_conditions != "")
            {
                query = String.Format("SELECT {0} FROM {1} WHERE {2}", _fields, _table, _conditions);
            }
            else
            {
                query = String.Format("SELECT {0} FROM {1}", _fields, _table);
            }

            return query;


        }
        /// <summary>
        /// Returns an insert query based on the parameters given<br/>
        /// Note: This method returns a query string with escaped values. For better security, use the non-static Insert method with parameterized queries.
        /// </summary>
        /// <param name="_fields">Array of field names to insert into</param>
        /// <param name="_table">Table name</param>
        /// <param name="_values">Array of values to insert corresponding to the fields</param>
        /// <returns></returns>
        public static string Insert_Query(String[] _fields, String _table, String[] _values, string primary_key_name = "", bool auto_increment = true)
        {
            // Validate table name to prevent SQL injection
            if (!IsValidIdentifier(_table))
                throw new ArgumentException("Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_table));

            // Validate arrays are not empty
            if (_fields == null || _fields.Length == 0)
                throw new ArgumentException("Fields array cannot be null or empty.", nameof(_fields));

            if (_values == null || _values.Length == 0)
                throw new ArgumentException("Values array cannot be null or empty.", nameof(_values));

            if (_fields.Length != _values.Length)
                throw new ArgumentException("Field and value arrays must have the same length.");

            // Validate field names to prevent SQL injection
            foreach (var field in _fields)
            {
                if (!IsValidIdentifier(field))
                    throw new ArgumentException($"Invalid field name '{field}'. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_fields));
            }



            if (!String.IsNullOrEmpty(primary_key_name))
            {
                if (auto_increment.Equals(false))
                {
                    var fieldsToLower = Array.ConvertAll(_fields, field => field.ToLower());
                    int index_of_primary_key = Array.IndexOf(fieldsToLower, primary_key_name, 0);
                    //Excludes the primary_key name if it is filled and the corresponding value
                    _values = Array.FindAll(_values, value => value != _values[index_of_primary_key]);
                    _fields = Array.FindAll(_fields, field => field != _fields[index_of_primary_key]);
                }
            }
            String fields = "";
            String values = "";

            // Build field list
            for (int cont = 0; cont < _fields.Length; cont++)
            {
                fields += _fields[cont] + ",";
            }
            fields = fields.Remove(fields.Length - 1, 1);

            // Build values list with proper escaping
            for (int cont = 0; cont < _fields.Length; cont++)
            {
                // Check if value is null first
                if (_values[cont] == null)
                {
                    values += "null,";
                    continue;
                }

                double numero;
                if (double.TryParse(_values[cont], out numero) == false)
                {
                    // Non-numeric value - needs quotes and escaping
                    if (_values[cont].Length == 0)
                    {
                        values += "null,";
                    }
                    else if (_values[cont].Substring(0, 1) == "'" && _values[cont].Length >= 2)
                    {
                        // Value already has quotes, but still escape internal quotes
                        string escapedValue = _values[cont].Substring(1, _values[cont].Length - 2).Replace("'", "''");
                        values += "'" + escapedValue + "',";
                    }
                    else
                    {
                        // Escape single quotes to prevent SQL injection
                        values += "'" + _values[cont].Replace("'", "''") + "',";
                    }
                }
                else
                {
                    // Numeric value - use as is with decimal point
                    values += _values[cont].Replace(",", ".") + ",";
                }
            }
            values = values.Remove(values.Length - 1, 1);

            // Build the query
            String query = String.Format("INSERT INTO {0}({1}) VALUES({2})", _table, fields, values);

            return query;


        }
        /// <summary>
        /// Returns an Update query
        /// Note: This method returns a query string with escaped values. For better security, use the non-static Update method with parameterized queries.
        /// </summary>
        /// <param name="_fields">Array of field names to update</param>
        /// <param name="_table">Table name</param>
        /// <param name="_values">Array of values to update corresponding to the fields</param>
        /// <param name="condition">WHERE clause condition (mandatory for security)</param>
        /// <returns></returns>
        public static string Update_Query(String[] _fields, String _table, String[] _values, String condition = "")
        {
            // Validate table name to prevent SQL injection
            if (!IsValidIdentifier(_table))
                throw new ArgumentException("Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_table));

            // Validate arrays are not empty
            if (_fields == null || _fields.Length == 0)
                throw new ArgumentException("Fields array cannot be null or empty.", nameof(_fields));

            if (_values == null || _values.Length == 0)
                throw new ArgumentException("Values array cannot be null or empty.", nameof(_values));

            if (_fields.Length != _values.Length)
                throw new ArgumentException("Field and value arrays must have the same length.");

            if (string.IsNullOrEmpty(condition))
                throw new ArgumentException("Condition is required for UPDATE operations for security reasons.", nameof(condition));

            // Validate field names to prevent SQL injection
            foreach (var field in _fields)
            {
                if (!IsValidIdentifier(field))
                    throw new ArgumentException($"Invalid field name '{field}'. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_fields));
            }

            String fields = "";

            // Process values with proper escaping
            for (int cont = 0; cont < _fields.Length; cont++)
            {
                // Check if value is null first
                if (_values[cont] == null)
                {
                    _values[cont] = "null";
                    continue;
                }

                double numero;
                if (double.TryParse(_values[cont], out numero) == false)
                {
                    // Non-numeric value - needs quotes and escaping
                    if (_values[cont].Length == 0)
                    {
                        _values[cont] = "null";
                    }
                    else if (_values[cont].Substring(0, 1) == "'" && _values[cont].Length >= 2)
                    {
                        // Value already has quotes, but still escape internal quotes
                        string escapedValue = _values[cont].Substring(1, _values[cont].Length - 2).Replace("'", "''");
                        _values[cont] = "'" + escapedValue + "'";
                    }
                    else
                    {
                        // Escape single quotes to prevent SQL injection
                        _values[cont] = "'" + _values[cont].Replace("'", "''") + "'";
                    }
                }
                else
                {
                    // Numeric value - use as is with decimal point
                    _values[cont] = _values[cont].Replace(",", ".");
                }
            }

            // Build SET clause
            for (int cont = 0; cont < _fields.Length; cont++)
            {
                fields += _fields[cont] + "=" + _values[cont] + ",";
            }
            fields = fields.Substring(0, fields.Length - 1);

            // Build the query
            String query = String.Format("UPDATE {0} SET {1} WHERE {2}", _table, fields, condition);

            return query;


        }
        /// <summary>
        /// Returns a Delete query<br/>
        /// For security reasons, the use of a condition is mandatory.
        /// </summary>
        /// <param name="_table"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static string Delete_Query(String _table, String condition)
        {
            // Validate table name to prevent SQL injection
            if (!IsValidIdentifier(_table))
                throw new ArgumentException("Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_table));

            if (string.IsNullOrEmpty(condition))
                throw new ArgumentException("Condition is required for DELETE operations for security reasons.", nameof(condition));

            // Build the query
            String query = String.Format("DELETE FROM {0} WHERE {1}", _table, condition);

            return query;


        }
        #endregion

    }
}
