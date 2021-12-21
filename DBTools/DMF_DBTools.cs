using DBTools.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DBToolsDll
{
    /// <summary>
    /// DBTools is a Sql Library to manipulate data in Sql Databases
    /// </summary>
    public class DBTools_SQL
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

        private List<SqlParameter> sqlParameters;
        public int count;
        #endregion

        #region public getters and setters
        public DBTools_SQL()
        {
            //STANDARD TCP/IP Port
            port = "1433";
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
        /// Specify the port of the database: Standard 3306
        ///
        /// </summary>
        public string Port { get => port; set => port = value; }
        public string ConnectionString
        {


            get
            {
                if (port != null)
                {
                    _connectionString = String.Format("Server={0},{1};Database={2};User Id={3}Password={4};", host, port, database, uid, password);
                }
                else
                {
                    _connectionString = String.Format("Server={0};Database={2};User Id={3};Password={4};", host, port, database, uid, password);
                }

                return _connectionString;

            }

        }

        public List<SqlParameter> SqlParameters { get => sqlParameters; set => sqlParameters = value; }
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

            SqlConnection SqlConnection = new SqlConnection(ConnectionString);
            SqlConnection.Open();
            try
            {
                SqlCommand SqlCommand = SqlConnection.CreateCommand();
                SqlCommand.CommandText = this.getQuery();
                SqlCommand.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                this.Error = ex.ToString();
            }
            finally
            {
                bool flag = SqlConnection.State == ConnectionState.Open;
                if (flag)
                {
                    SqlConnection.Close();
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

                SqlCommand SqlCommand = new SqlCommand();
                SqlConnection SqlConnection = new SqlConnection(ConnectionString);
                //Verifica se a conexão foi aberta com sucesso
                try
                {
                    SqlConnection.Open();
                    SqlCommand = SqlConnection.CreateCommand();
                    SqlCommand.CommandText = this.getQuery();
                    SqlDataAdapter SqlDataAdapter = new SqlDataAdapter(SqlCommand);
                    DataSet dataSet = new DataSet();
                    SqlDataAdapter.Fill(dataSet);
                    this.Count = dataSet.Tables.Count;
                    defaultView = dataSet.Tables[0].DefaultView;
                    SqlConnection.Close();
                }
                catch (SqlException e)
                {
                    Error = e.ToString();

                }



            }
            catch (Exception)
            {
                DataSet dataSet2 = new DataSet();
                defaultView = dataSet2.Tables[0].DefaultView;
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
            using (SqlConnection conn = new SqlConnection(this.ConnectionString))
            {
                SqlCommand command = new SqlCommand(this.Query, conn);
                if (SqlParameters != null)
                    command.Parameters.AddRange(SqlParameters.ToArray());
                SqlDataAdapter SqlDataAdapter = new SqlDataAdapter(command);
                DataSet dataSet = new DataSet();
                SqlDataAdapter.Fill(dataSet);
                List<GenericObject> lstObject = new List<GenericObject>();

                List<String> columns = new List<string>();
                List<String> types = new List<string>();
                DataView values = dataSet.Tables[0].DefaultView;
                int cont = 0;
                foreach (var column in values.Table.Columns)
                {
                    columns.Add(column.ToString());
                    types.Add(dataSet.Tables[0].Columns[columns[cont]].DataType.Name);
                    cont++;

                }

                for (cont = 0; cont < values.Count; cont++)
                {
                    lstObject.Add(new GenericObject
                    {
                        columns = columns.ToArray(),
                        types = types.ToArray(),
                        values = values[cont].Row.ItemArray
                        //  valuesString = values[cont].DataView
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

            using (SqlConnection SqlConnection = new SqlConnection(ConnectionString))
            {
                SqlConnection.Open();
                try
                {
                    SqlCommand SqlCommand = SqlConnection.CreateCommand();
                    SqlCommand.CommandText = this.getQuery();
                    if (sqlParameters != null)
                        SqlCommand.Parameters.AddRange(SqlParameters.ToArray());
                    SqlCommand.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    this.Error = ex.ToString();
                }
                finally
                {
                    bool flag = SqlConnection.State == ConnectionState.Open;
                    if (flag)
                    {
                        SqlConnection.Close();
                    }
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

                using (SqlConnection conn = new SqlConnection(this.ConnectionString))
                {
                    try
                    {
                        SqlCommand SqlCommand = new SqlCommand(query != "" ? query : this.Query, conn);
                        if (sqlParameters != null)
                            SqlCommand.Parameters.AddRange(SqlParameters.ToArray());
                        SqlDataAdapter SqlDataAdapter = new SqlDataAdapter(SqlCommand);
                        DataSet dataSet = new DataSet();
                        SqlDataAdapter.Fill(dataSet);
                        this.Count = dataSet.Tables.Count;
                        defaultView = dataSet.Tables[0].DefaultView;
                    }
                    catch (SqlException e)
                    {
                        Error = e.ToString();

                    }

                }



            }
            catch (Exception e)
            {
                DataSet dataSet2 = new DataSet();
                defaultView = dataSet2.Tables[0].DefaultView;
            }
            return defaultView;
        }


        #endregion
    }
}
