using DBTools.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace DBTools.Export
{
    public class DataExport
    {
        private static readonly Dictionary<string, Type> _typeMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { "Boolean",  typeof(bool)     },
            { "Byte",     typeof(byte)     },
            { "Int16",    typeof(short)    },
            { "Int32",    typeof(int)      },
            { "Int64",    typeof(long)     },
            { "Single",   typeof(float)    },
            { "Double",   typeof(double)   },
            { "Decimal",  typeof(decimal)  },
            { "DateTime", typeof(DateTime) },
            { "Guid",     typeof(Guid)     },
            { "String",   typeof(string)   },
        };

        public String ToCsv(List<GenericObject> genericObject, char separator, bool showColums = true, bool showTypes = true)
        {
            if (genericObject == null)
                throw new ArgumentNullException(nameof(genericObject));
            if (genericObject.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();

            if (showTypes)
            {
                sb.Append(string.Join(separator.ToString(), genericObject[0].types));
                sb.Append('\n');
            }

            if (showColums)
            {
                sb.Append(string.Join(separator.ToString(), genericObject[0].columns));
                sb.Append('\n');
            }

            for (int rowIndex = 0; rowIndex < genericObject.Count; rowIndex++)
            {
                int colCount = genericObject[0].columns.Length;
                for (int colIndex = 0; colIndex < colCount; colIndex++)
                {
                    if (colIndex > 0)
                        sb.Append(separator);

                    if (genericObject[rowIndex].values[colIndex] != DBNull.Value)
                    {
                        sb.Append(
                            genericObject[rowIndex].values[colIndex]
                                .ToString()
                                .Replace('\\', '/')
                                .Replace(Environment.NewLine, ""));
                    }
                }
                sb.Append('\n');
            }

            if (sb.Length > 0 && sb[sb.Length - 1] == '\n')
                sb.Length -= 1;

            return sb.ToString();
        }

        public DataTable ToDataTable(String csv, char separator, bool specifyColumnTypes = false)
        {
            DataTable dt = new DataTable();

            if (String.IsNullOrEmpty(csv))
                return dt;

            StringReader sr = new StringReader(csv);
            var dataColumns = new List<DataColumn>();

            string firstLine = sr.ReadLine();
            string[] firstTokens = firstLine.Split(separator);

            if (specifyColumnTypes)
            {
                foreach (string typeName in firstTokens)
                {
                    Type colType = _typeMap.TryGetValue(typeName.Trim(), out Type mapped) ? mapped : typeof(string);
                    dataColumns.Add(new DataColumn { DataType = colType, AllowDBNull = true });
                }
            }
            else
            {
                foreach (string _ in firstTokens)
                    dataColumns.Add(new DataColumn { DataType = typeof(string), AllowDBNull = true });
            }

            string[] columnNames = specifyColumnTypes
                ? sr.ReadLine().Split(separator)
                : firstTokens;

            for (int i = 0; i < columnNames.Length; i++)
                dataColumns[i].ColumnName = i + "_" + columnNames[i];

            dt.Columns.AddRange(dataColumns.ToArray());

            while (sr.Peek() > -1)
            {
                string[] rowTokens = sr.ReadLine().Split(separator);
                DataRow row = dt.NewRow();
                int colCount = Math.Min(columnNames.Length, rowTokens.Length);
                for (int colIndex = 0; colIndex < colCount; colIndex++)
                    row[colIndex] = rowTokens[colIndex].Replace(separator, '|');
                dt.Rows.Add(row);
            }

            return dt;
        }
    }
}
