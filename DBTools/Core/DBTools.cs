using DBTools.Abstractions;
using DBTools.Models;
using DBTools.Providers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace DBTools.Core
{
    /// <summary>
    /// DBTools is a Sql Library to manipulate data in Sql Databases
    /// </summary>
    public class DBTools
    {
        #region private variables
        private string host;

        private string uid;

        private string password;

        private string database;

        private string query;

        private string error;

        private string table;
        private string port;

        private string _connectionString;

        private List<DbParameter> sqlParameters;
        public int count;

        protected IDbProvider _provider;
        #endregion

        #region public getters and setters
        public DBTools()
        {
            //STANDARD TCP/IP Port
            port = "1433";
            _provider = new SqlServerProvider();
        }
        /// <summary>
        /// It is used to place the server address of the database <br></br>
        /// Example(localhost:3306 or localhost:4001) <br></br>
        /// It is in this formatting.
        /// Host:Port
        /// </summary>
        public string Host
        {
            get
            {
                return this.host;
            }
            set
            {
                this.host = value;
            }
        }
        /// <summary>
        /// It is used to place the userName of your database <br></br>
        /// Example(root or admin). <br></br>
        /// It is in this formatting
        /// user
        /// </summary>
        public string Uid
        {
            get
            {
                return this.uid;
            }
            set
            {
                this.uid = value;
            }
        }
        /// <summary>
        /// It is used to place the password of the database <br></br>
        ///
        /// </summary>
        public string Password
        {
            get
            {

                return this.password;
            }
            set
            {
                this.password = value;
            }
        }
        /// <summary>
        /// It is used to set the Database name <br></br>
        ///
        /// </summary>
        public string Database
        {
            get
            {
                return this.database;
            }
            set
            {
                this.database = value;
            }
        }
        /// <summary>
        /// It is used to set the Query <br></br>
        ///
        /// </summary>
        public string Query
        {
            get
            {
                return this.query;
            }
            set
            {
                this.query = value;
            }
        }

        public string Error
        {
            get
            {
                return this.error;
            }
            set
            {
                this.error = value;
            }
        }

        public string Table
        {
            get
            {
                return this.table;
            }
            set
            {
                this.table = value;
            }
        }

        public int Count
        {
            get
            {
                return this.count;
            }
            set
            {
                this.count = value;
            }
        }
        /// <summary>
        /// Specify the port of the database: Standard 1433
        ///
        /// </summary>
        public string Port { get => port; set => port = value; }
        public string ConnectionString
        {


            get
            {
                if (_connectionString != null)
                {
                    return _connectionString;
                }

                _connectionString = $"Data Source=tcp:{Host},{Port};Initial Catalog={Database};User ID={Uid};Password={Password};TrustServerCertificate=True;";


                return _connectionString;

            }

            set
            {
                _connectionString = value;
            }

        }

        public List<DbParameter> SqlParameters { get => sqlParameters; set => sqlParameters = value; }
        #endregion

        #region legacy getters and setters
        public void setHost(string host)
        {
            this.Host = host;
        }

        public string getHost()
        {
            return this.Host;
        }

        public void setUid(string uid)
        {
            this.Uid = uid;
        }

        public string getUid()
        {
            return this.Uid;
        }

        public void setPassword(string password)
        {
            this.Password = password;
        }

        public string getPassword()
        {
            return this.Password;
        }

        public void setQuery(string query)
        {
            this.Query = query;
        }

        public string getQuery()
        {
            return this.Query;
        }

        public void setDataBase(string database)
        {
            this.Database = database;
        }

        public string getDatabase()
        {
            return this.Database;
        }
        #endregion
        #region Public Day One Methods
        /// <summary>
        /// Executes the Query in the database.<br/>
        /// This method is deprecated, use <see cref="SqlExecuteQuery(string)"/> instead, for better security and compliance with the standards.
        /// </summary>
        /// 
        [Obsolete("This method is deprecated and should use SqlExecuteQuery instead", false)]
        public void sqlExecuteQuery()
        {
            using (DbConnection connection = _provider.CreateConnection(ConnectionString))
            {
                connection.Open();
                try
                {
                    DbCommand command = connection.CreateCommand();
                    command.CommandText = this.getQuery();
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    this.Error = ex.ToString();
                }
            }
        }

        /// <summary>
        /// Retrieves the DataView Representation in the database.<br/>
        /// This method is deprecated
        /// </summary>
        /// <returns></returns>
        /// 
        [Obsolete("This Method is deprecated, use RetrieveObjectSql or RetrieveDataSql instead", false)]
        public DataView retrieveDataSql()
        {
            DataView defaultView = new DataView();
            try
            {
                using (DbConnection connection = _provider.CreateConnection(ConnectionString))
                {
                    try
                    {
                        connection.Open();
                        DbCommand command = connection.CreateCommand();
                        command.CommandText = this.getQuery();
                        DataTable dataTable = new DataTable();
                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            dataTable.Load(reader);
                        }
                        this.Count = 1;
                        defaultView = dataTable.DefaultView;
                    }
                    catch (DbException e)
                    {
                        Error = e.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Error = ex.ToString();
            }
            return defaultView;
        }
        #endregion

        #region Public improved methods
        /// <summary>
        /// Returns a list of <see cref="GenericObject"/> that can be used in a variety of scenarios
        /// </summary>
        /// <returns></returns>
        public List<GenericObject> RetrieveObjectSql()
        {
            using (DbConnection conn = _provider.CreateConnection(this.ConnectionString))
            {
                DbCommand command = conn.CreateCommand();
                command.CommandText = this.Query;
                if (SqlParameters != null)
                {
                    foreach (var param in SqlParameters)
                        command.Parameters.Add(param);
                }
                DataTable dataTable = new DataTable();
                conn.Open();
                using (DbDataReader reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
                List<GenericObject> lstObject = new List<GenericObject>();

                List<String> columns = new List<string>();
                List<String> types = new List<string>();
                DataView values = dataTable.DefaultView;
                int cont = 0;
                foreach (var column in values.Table.Columns)
                {
                    columns.Add(column.ToString());
                    types.Add(dataTable.Columns[columns[cont]].DataType.Name);
                    cont++;

                }

                for (cont = 0; cont < values.Count; cont++)
                {
                    lstObject.Add(new GenericObject
                    {
                        columns = columns.ToArray(),
                        types = types.ToArray(),
                        values = values[cont].Row.ItemArray
                    });
                }

                return lstObject;
            }

        }



        /// <summary>
        /// Executes a sql query and, if sucessful, returns a void string, if error, returns the error message<br/>
        /// Concatenating the query is not recommendable, use <see cref="SqlParameters"/> to pass your parameters before executing this command.<br/>
        /// </summary>
        /// 
        public void SqlExecuteQuery(String query = "")
        {
            using (DbConnection connection = _provider.CreateConnection(ConnectionString))
            {
                connection.Open();
                try
                {
                    DbCommand command = connection.CreateCommand();
                    command.CommandText = this.getQuery();
                    if (sqlParameters != null)
                    {
                        foreach (var param in SqlParameters)
                            command.Parameters.Add(param);
                    }
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    this.Error = ex.ToString();
                }
            }
        }



        /// <summary>
        /// Retrieves the DataView Representation in the database.<br/>
        /// Concatenating the query is not recommendable, use <see cref="SqlParameters"/> to pass your parameters before executing this command.<br/>
        /// </summary>
        /// <returns></returns>
        public DataView RetrieveDataSql(String query = "")
        {
            DataView defaultView = new DataView();
            try
            {

                using (DbConnection conn = _provider.CreateConnection(this.ConnectionString))
                {
                    try
                    {
                        DbCommand command = conn.CreateCommand();
                        command.CommandText = query != "" ? query : this.Query;
                        if (sqlParameters != null)
                        {
                            foreach (var param in SqlParameters)
                                command.Parameters.Add(param);
                        }
                        DataTable dataTable = new DataTable();
                        conn.Open();
                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            dataTable.Load(reader);
                        }
                        this.Count = 1;
                        defaultView = dataTable.DefaultView;
                    }
                    catch (DbException e)
                    {
                        Error = e.ToString();

                    }

                }



            }
            catch (Exception e)
            {
                Error = e.ToString();
            }
            return defaultView;
        }


        #endregion
    }
}
