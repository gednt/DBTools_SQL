using DBTools.Model;
using System;
using System.Collections.Generic;
using System.Data;
namespace DBTools_Utilities
{
    /// <summary>
    /// Utils Class of the Sql DBTools Library
    /// </summary>
    public class Utils : DBToolsDll.DBTools_SQL
    {
        public Utils(String Host, String Database, String Uid, String pwd, String port)
        {
            this.Host = Host;
            this.Database = Database;
            this.Uid = Uid;
            this.Password = pwd;
            this.Port = port;
        }
        public Utils()
        {

        }
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
        public DataView Select(String _fields, String _table, String _conditions)
        {

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
        /// Inserts the data into the database based on the parameters given<br/>
        /// This class can and should be used with the <see cref="QueryBuilder(object)">QueryBuilder Command</see>
        /// </summary>
        /// <param name="_fields"></param>
        /// <param name="_table"></param>
        /// <param name="_conditions"></param>
        /// <returns></returns>
        public bool Insert(String[] _fields, String _table, String[] _values)
        {
            String fields = "", values = "";
            //MONTA OS CAMPOS
            String query = "INSERT INTO " + _table + "(";

            for (int cont = 0; cont < _fields.Length; cont++)
            {
                fields += _fields[cont] + ",";
            }
            fields = fields.Remove(fields.Length - 1, 1);
            fields += ") VALUES(";
            //VALORES
            for (int cont = 0; cont < _fields.Length; cont++)
            {
                int numero;
                if (int.TryParse(_values[cont], out numero) == false)
                {
                    values += "'" + _values[cont] + "',";
                }
                else
                {
                    values += _values[cont] + ",";
                }

            }
            values = values.Remove(values.Length - 1, 1);

            //FINALIZA A QUERY
            query += fields;
            query += values;

            query += ")";
            //EXECUTA A QUERY
            ExecuteQuery(query);
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
            String fields = "", values = "";
            //MONTA OS CAMPOS
            String query = "UPDATE  " + _table + " SET ";

            for (int cont = 0; cont < _fields.Length; cont++)
            {
                fields += _fields[cont] + "=" + _values[cont] + ",";
            }
            //fields = fields.Remove(fields.Length - 1, 1);
            fields = fields.Substring(0, fields.Length - 1);
            fields += " WHERE " + condition;
            query += fields;
            //EXECUTA A QUERY
            ExecuteQuery(query);
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
        public bool Delete(String _table, String condition)
        {

            //MONTA OS CAMPOS
            String query = "DELETE  FROM " + _table + " WHERE " + condition;
            //EXECUTA A QUERY
            ExecuteQuery(query);
            if (Error != null)
            {
                return false;
            }
            return true;


        }
        //MODULOS DE MANIPULAÇAO DE DADOS
        /// <summary>
        /// Returns a DataView based on the query without the select clause<br/>
        /// 
        /// </summary>
        /// <param name="_fields"></param>
        /// <param name="_table"></param>
        /// <param name="_conditions"></param>
        /// <returns></returns>
        public DataView Select(String query_without_select)
        {



            return getInBdDv("SELECT " + query_without_select);


        }
        #endregion



    }
}
