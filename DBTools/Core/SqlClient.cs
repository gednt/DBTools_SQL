using DBTools.Abstractions;
using DBTools.Models;
using DBTools.Providers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace DBTools.Core
{
    /// <summary>
    /// SqlClient Class of the Sql DBTools Library
    /// </summary>
    public class SqlClient : DBTools, ISqlClient
    {
        private readonly ISqlValidator _validator;
        private readonly ISqlQueryBuilder _queryBuilder;
        public IDbConfiguration Configuration { get; }

        public ISqlQueryBuilder QueryBuilderInstance => _queryBuilder;

        public SqlClient()
        {
            Configuration = new DbConfiguration();
            _validator = new SqlValidator();
            _queryBuilder = new SqlQueryBuilder(_validator);

            Host = Configuration.Host;
            Database = Configuration.Database;
            Uid = Configuration.Uid;
            Password = Configuration.Password;
            Port = Configuration.Port;
            ConnectionString = Configuration.ConnectionString;
        }

        public SqlClient(IDbConfiguration configuration, ISqlValidator validator, ISqlQueryBuilder queryBuilder)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _queryBuilder = queryBuilder ?? throw new ArgumentNullException(nameof(queryBuilder));

            Host = Configuration.Host;
            Database = Configuration.Database;
            Uid = Configuration.Uid;
            Password = Configuration.Password;
            Port = Configuration.Port;
            ConnectionString = Configuration.ConnectionString;
        }

        public SqlClient(IDbConfiguration configuration, ISqlValidator validator, ISqlQueryBuilder queryBuilder, IDbProvider provider)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _queryBuilder = queryBuilder ?? throw new ArgumentNullException(nameof(queryBuilder));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));

            Host = Configuration.Host;
            Database = Configuration.Database;
            Uid = Configuration.Uid;
            Password = Configuration.Password;
            Port = Configuration.Port;
            ConnectionString = Configuration.ConnectionString;
        }

        #region Helper methods for validation

        private bool IsValidIdentifier(string identifier)
        {
            return _validator.IsValidIdentifier(identifier);
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
            List<GenericObject> lstReturn = new List<GenericObject>();
            List<String> columns = new List<string>();
            List<Object> valuesReturn = new List<Object>();
            List<String> typesReturn = new List<string>();
            int cont = 0;
            foreach (var i in arrayObject)
            {

                try
                {
                    var rawValue = obj.GetType().GetProperty(i.Name).GetValue(obj, null);
                    string stringValue = rawValue?.ToString() ?? "";

                    if (i.PropertyType.Name.ToString() == "DateTime")
                    {
                        values.Add(new GenericObject_Simple
                        {
                            value = rawValue != null
                                ? DateTime.Parse(stringValue).ToString("yyyy-MM-dd HH:mm:ss")
                                : ""
                            ,
                            column = i.Name
                            ,
                            type = i.PropertyType.Name
                        });

                    }
                    else
                    {
                        switch (autoIncrement)
                        {
                            case false:
                                values.Add(new GenericObject_Simple
                                {
                                    value = stringValue
                                  ,
                                    column = i.Name
                                  ,
                                    type = i.PropertyType.Name
                                });
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
                                        value = stringValue
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
                catch (Exception e)
                {
                    throw new Exception("Error in property: " + i.Name + " - " + e.Message);
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

        public String[] GetInBd(String query)
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

        public String[] getInBd(String query)
        {
            return GetInBd(query);
        }
        /// <summary>
        /// Returns a Dataview of the objects of the Database
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public DataView GetInBdDv(String query)
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
                return null;

            }
            String[] arrayQuery = new String[dv.Count];
            for (int cont = 0; cont < dv.Count; cont++)
            {
                arrayQuery[cont] = dv[cont][0].ToString();
            }
            return dv;
        }

        public DataView getInBdDv(String query)
        {
            return GetInBdDv(query);
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
            if (!IsValidIdentifier(_fields))
                throw new ArgumentException("Invalid field names. Only alphanumeric characters, underscores, dots, brackets, commas, and spaces are allowed.", nameof(_fields));

            if (!IsValidIdentifier(_table))
                throw new ArgumentException("Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_table));

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters), "Parameters array cannot be null. Use empty array for no parameters.");

            String query = "";
            List<DbParameter> sqlParams = new List<DbParameter>();

            for (int i = 0; i < parameters.Length; i++)
            {
                string paramName = "@param" + i;
                sqlParams.Add(_provider.CreateParameter(paramName, parameters[i] ?? (object)DBNull.Value));
            }

            if (!string.IsNullOrEmpty(whereClause))
            {
                query = String.Format("SELECT {0} FROM {1} WHERE {2}", _fields, _table, whereClause);
            }
            else
            {
                query = String.Format("SELECT {0} FROM {1}", _fields, _table);
            }

            SqlParameters = sqlParams;
            DataView result = RetrieveDataSql(query);
            SqlParameters = null;

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
        public bool Insert(String[] _fields, String _table, object[] _values, string primary_key_name = null, bool auto_increment = true)
        {

            List<DbParameter> parameters = new List<DbParameter>();
            string fields = "", paramPlaceholders = "";
            for (int cont = 0; cont < _fields.Length; cont++)
            {
                fields += _fields[cont] + ",";
                string paramName = "@param" + cont;
                paramPlaceholders += paramName + ",";
                parameters.Add(_provider.CreateParameter(paramName, _values[cont] ?? (object)DBNull.Value));
            }

            if (auto_increment.Equals(true))
            {
                if (!String.IsNullOrEmpty(primary_key_name))
                {
                    var fieldsToLower = Array.ConvertAll(_fields, field => field.ToLower());
                    int index_of_primary_key = Array.IndexOf(fieldsToLower, primary_key_name.ToLower(), 0);

                    if (index_of_primary_key >= 0)
                    {
                        _values = _values.Where((value, index) => index != index_of_primary_key).ToArray();
                        _fields = _fields.Where((field, index) => index != index_of_primary_key).ToArray();
                    }
                }

            }

            String query = _queryBuilder.InsertQuery(_fields, _table, _values, primary_key_name, auto_increment);

            SqlParameters = parameters;
            ExecuteQuery(query);
            SqlParameters = null;

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
        public bool Update(String[] _fields, String _table, String[] _values, String condition = "")
        {
            if (!IsValidIdentifier(_table))
                throw new ArgumentException("Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_table));

            if (_fields == null || _fields.Length == 0)
                throw new ArgumentException("Fields array cannot be null or empty.", nameof(_fields));

            if (_values == null || _values.Length == 0)
                throw new ArgumentException("Values array cannot be null or empty.", nameof(_values));

            if (_fields.Length != _values.Length)
                throw new ArgumentException("Field and value arrays must have the same length.");

            if (string.IsNullOrEmpty(condition))
                throw new ArgumentException("Condition is required for UPDATE operations for security reasons.", nameof(condition));

            foreach (var field in _fields)
            {
                if (!IsValidIdentifier(field))
                    throw new ArgumentException($"Invalid field name '{field}'. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_fields));
            }

            String setClause = "";
            List<DbParameter> parameters = new List<DbParameter>();

            for (int cont = 0; cont < _fields.Length; cont++)
            {
                string paramName = "@param" + cont;
                setClause += _fields[cont] + "=" + paramName + ",";
                parameters.Add(_provider.CreateParameter(paramName, _values[cont] ?? (object)DBNull.Value));
            }

            setClause = setClause.Substring(0, setClause.Length - 1);

            String query = String.Format("UPDATE {0} SET {1} WHERE {2}", _table, setClause, condition);

            SqlParameters = parameters;
            ExecuteQuery(query);
            SqlParameters = null;

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
            if (!IsValidIdentifier(_table))
                throw new ArgumentException("Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_table));

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

            foreach (var field in _fields)
            {
                if (!IsValidIdentifier(field))
                    throw new ArgumentException($"Invalid field name '{field}'. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_fields));
            }

            String setClause = "";
            List<DbParameter> parameters = new List<DbParameter>();

            for (int cont = 0; cont < _fields.Length; cont++)
            {
                string paramName = "@param" + cont;
                setClause += _fields[cont] + "=" + paramName + ",";
                parameters.Add(_provider.CreateParameter(paramName, _values[cont] ?? (object)DBNull.Value));
            }

            setClause = setClause.Substring(0, setClause.Length - 1);

            for (int i = 0; i < whereParameters.Length; i++)
            {
                string paramName = "@whereParam" + i;
                parameters.Add(_provider.CreateParameter(paramName, whereParameters[i] ?? (object)DBNull.Value));
            }

            String query = String.Format("UPDATE {0} SET {1} WHERE {2}", _table, setClause, whereClause);

            SqlParameters = parameters;
            ExecuteQuery(query);
            SqlParameters = null;

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
            if (!IsValidIdentifier(_table))
                throw new ArgumentException("Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(_table));

            if (string.IsNullOrEmpty(whereClause))
                throw new ArgumentException("Condition is required for DELETE operations for security reasons.", nameof(whereClause));

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters), "Parameters array cannot be null. Use empty array for no parameters.");

            List<DbParameter> sqlParams = new List<DbParameter>();

            for (int i = 0; i < parameters.Length; i++)
            {
                string paramName = "@param" + i;
                sqlParams.Add(_provider.CreateParameter(paramName, parameters[i] ?? (object)DBNull.Value));
            }

            String query = String.Format("DELETE FROM {0} WHERE {1}", _table, whereClause);

            SqlParameters = sqlParams;
            ExecuteQuery(query);
            SqlParameters = null;

            if (Error != null)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns a DataView based on the query without the select clause using parameterized queries.<br/>
        /// This method uses parameterized queries to prevent SQL injection attacks.<br/>
        /// <para><strong>Security:</strong> The <paramref name="query_without_select"/> fragment is not validated.
        /// Never pass raw user input as this argument. Use the structured overload
        /// <see cref="Select(string, string, string, object[])"/> for user-controlled filter values.</para>
        /// </summary>
        /// <param name="query_without_select">Query without SELECT keyword (e.g., "* FROM users WHERE id = @param0"). Must not contain raw user input.</param>
        /// <param name="parameters">Array of parameter values corresponding to the placeholders in the query</param>
        /// <returns>DataView with the query results</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public DataView Select(String query_without_select, Object[] parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters), "Parameters array cannot be null. Use empty array for no parameters.");

            List<DbParameter> sqlParams = new List<DbParameter>();

            for (int i = 0; i < parameters.Length; i++)
            {
                string paramName = "@param" + i;
                sqlParams.Add(_provider.CreateParameter(paramName, parameters[i] ?? (object)DBNull.Value));
            }

            SqlParameters = sqlParams;
            DataView result = getInBdDv("SELECT " + query_without_select);
            SqlParameters = null;

            return result;
        }

        /// <summary>
        /// Executes a complete, pre-built SQL query string with parameterized values and returns a DataView.<br/>
        /// Unlike <see cref="Select(string, object[])"/>, this method does NOT prepend "SELECT ".
        /// Used by the LINQ providers that build their own full SQL via BuildSql.
        /// </summary>
        /// <param name="fullSql">The complete SQL query string (must start with SELECT)</param>
        /// <param name="parameters">Array of parameter values corresponding to the placeholders in the query</param>
        /// <returns>DataView with the query results</returns>
        public DataView SelectRaw(String fullSql, Object[] parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters), "Parameters array cannot be null. Use empty array for no parameters.");

            List<DbParameter> sqlParams = new List<DbParameter>();

            for (int i = 0; i < parameters.Length; i++)
            {
                string paramName = "@param" + i;
                sqlParams.Add(_provider.CreateParameter(paramName, parameters[i] ?? (object)DBNull.Value));
            }

            SqlParameters = sqlParams;
            DataView result = getInBdDv(fullSql);
            SqlParameters = null;

            return result;
        }
        #endregion

        #region Static query builder methods (backward compatibility)
        /// <summary>
        /// Returns a string based on the parameters given<br/>
        /// Note: This method returns a query string with string concatenation. For better security, use the non-static Select method with parameterized queries.
        /// </summary>
        public static string Select_Query(String _fields, String _table, String _conditions)
        {
            var validator = new SqlValidator();
            var builder = new SqlQueryBuilder(validator);
            return builder.SelectQuery(_fields, _table, _conditions);
        }

        /// <summary>
        /// Returns an insert query based on the parameters given<br/>
        /// Note: This method returns a query string with escaped values. For better security, use the non-static Insert method with parameterized queries.
        /// </summary>
        public static string Insert_Query(String[] _fields, String _table, object[] _values, string primary_key_name = "", bool auto_increment = true)
        {
            var validator = new SqlValidator();
            var builder = new SqlQueryBuilder(validator);
            return builder.InsertQuery(_fields, _table, _values, primary_key_name, auto_increment);
        }

        /// <summary>
        /// Returns an Update query<br/>
        /// Note: This method returns a query string with escaped values. For better security, use the non-static Update method with parameterized queries.
        /// </summary>
        public static string Update_Query(String[] _fields, String _table, String[] _values, String condition = "")
        {
            var validator = new SqlValidator();
            var builder = new SqlQueryBuilder(validator);
            return builder.UpdateQuery(_fields, _table, _values, condition);
        }

        /// <summary>
        /// Returns a Delete query<br/>
        /// For security reasons, the use of a condition is mandatory.
        /// </summary>
        public static string Delete_Query(String _table, String condition)
        {
            var validator = new SqlValidator();
            var builder = new SqlQueryBuilder(validator);
            return builder.DeleteQuery(_table, condition);
        }

        /// <summary> Generates a list of SQL parameters from an array of values </summary>
        public static List<DbParameter> GenerateSqlParameters(object[] values)
        {
            var validator = new SqlValidator();
            var builder = new SqlQueryBuilder(validator);
            return builder.GenerateSqlParameters(values);
        }
        #endregion
    }
}
