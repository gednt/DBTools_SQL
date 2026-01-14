using DBTools.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace DBTools_Utilities
{
    public class DataExport
    {
        public String ToCsv(List<GenericObject> genericObject, char separator, bool showColums = true, bool showTypes = true)
        {
            string strReturn = "";

            if (showTypes == true)
            {
                foreach (var tipo in genericObject[0].types)
                {
                    strReturn += tipo + separator;
                }
                strReturn = strReturn.Substring(0, strReturn.Length - 1) + "\n";
            }


            if (showColums == true)
            {
                foreach (var coluna in genericObject[0].columns)
                {

                    strReturn += coluna + separator;
                }
                strReturn = strReturn.Substring(0, strReturn.Length - 1) + "\n";
            }

            for (int cont = 0; cont < genericObject.Count; cont++)
            {
                for (int columns = 0; columns < genericObject[0].columns.Length; columns++)
                {
                    if (genericObject[cont].values[columns] == DBNull.Value)
                    {
                        strReturn += "" + separator;
                    }
                    else
                    {
                        strReturn += genericObject[cont].values[columns].ToString().Replace('\\', '/').Replace(System.Environment.NewLine, "") + separator.ToString();
                    }
                }
                strReturn += "\n";

            }

            strReturn = strReturn.Substring(0, strReturn.Length - 1);






            return strReturn;
        }

        public DataTable ToDataTable(String csv, char separator, bool specifyColumnTypes = false)
        {
            DataTable dt = new DataTable();
            List<DataColumn> dataColumns = new List<DataColumn>();

            if (String.IsNullOrEmpty(csv))
                return dt;

            StringReader sr = new StringReader(csv);
            //Mount the columns
            ///Data types must be specified in the first line of the csv file and are optional.
            ///The column names must be specified in the first line of the csv file if Column types are not set.
            ///If column types are set, columns must be specified in the second line of the csv file.
            if (csv != null)
            {
                var columns = sr.ReadLine().ToString().Split(separator).ToArray();


                //Data Types
                if (specifyColumnTypes == true)
                {
                    foreach (var type in columns)
                    {
                        //dataColumns.Add(new DataColumn
                        //{
                        //    DataType = Type.GetType("System."+type),
                        //    AllowDBNull = true
                        //});
                        dataColumns.Add(new DataColumn
                        {
                            DataType = typeof(string),
                            AllowDBNull = true
                        });
                    }
                }
                else
                {
                    foreach (var type in columns)
                    {
                        //dataColumns.Add(new DataColumn
                        //{
                        //    DataType = Type.GetType("System."+type),
                        //    AllowDBNull = true
                        //});
                        dataColumns.Add(new DataColumn
                        {
                            DataType = typeof(string),
                            AllowDBNull = true
                        });
                    }
                }
                sr = new StringReader(csv);
                //Column names
                columns = sr.ReadLine().ToString().Split(separator).ToArray();
                for (int cont = 0; cont < columns.Length; cont++)
                {
                    dataColumns[cont].ColumnName = cont + "_" + columns[cont].ToString();
                }
                dt.Columns.AddRange(dataColumns.ToArray());
                //Rows
                while (sr.Peek() > -1)
                {
                    var rowDetails = sr.ReadLine().ToString().Split(separator).ToArray().ToList();
                    dt.Rows.Add();
                    rowDetails.ToList().RemoveAt(rowDetails.Count - 1);
                    for (int cont = dt.Rows.Count - 1; ;)
                    {
                        for (int columnCount = 0; columnCount < columns.Length; columnCount++)
                        {
                            dt.Rows[cont][columnCount] = rowDetails[columnCount].Replace(separator, '|');

                        }
                        break;

                    }





                }


            }



            return dt;
        }
    }
}
