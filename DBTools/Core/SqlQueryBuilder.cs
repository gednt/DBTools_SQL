using DBTools.Abstractions;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace DBTools.Core
{
    public class SqlQueryBuilder : ISqlQueryBuilder
    {
        private readonly ISqlValidator _validator;

        public SqlQueryBuilder(ISqlValidator validator)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public string SelectQuery(string fields, string table, string conditions)
        {
            if (!_validator.IsValidIdentifier(fields))
                throw new ArgumentException("Invalid field names. Only alphanumeric characters, underscores, dots, brackets, commas, and spaces are allowed.", nameof(fields));

            if (!_validator.IsValidIdentifier(table))
                throw new ArgumentException("Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(table));

            if (conditions != "")
            {
                return String.Format("SELECT {0} FROM {1} WHERE {2}", fields, table, conditions);
            }
            else
            {
                return String.Format("SELECT {0} FROM {1}", fields, table);
            }
        }

        public string InsertQuery(string[] fields, string table, object[] values, string primaryKeyName = "", bool autoIncrement = true)
        {
            if (!_validator.IsValidIdentifier(table))
                throw new ArgumentException("Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(table));

            if (fields == null || fields.Length == 0)
                throw new ArgumentException("Fields array cannot be null or empty.", nameof(fields));

            if (values == null || values.Length == 0)
                throw new ArgumentException("Values array cannot be null or empty.", nameof(values));

            if (fields.Length != values.Length)
                throw new ArgumentException("Field and value arrays must have the same length.");

            if (autoIncrement.Equals(true))
            {
                if (!String.IsNullOrEmpty(primaryKeyName))
                {
                    var fieldsToLower = Array.ConvertAll(fields, field => field.ToLower());
                    int index_of_primary_key = Array.IndexOf(fieldsToLower, primaryKeyName.ToLower(), 0);

                    if (index_of_primary_key >= 0)
                    {
                        values = values.Where((value, index) => index != index_of_primary_key).ToArray();
                        fields = fields.Where((field, index) => index != index_of_primary_key).ToArray();
                    }
                }
            }

            string paramPlaceholders = "";
            for (int cont = 0; cont < fields.Length; cont++)
            {
                string paramName = "@param" + cont;
                paramPlaceholders += paramName + ",";
            }

            foreach (var field in fields)
            {
                if (!_validator.IsValidIdentifier(field))
                    throw new ArgumentException($"Invalid field name '{field}'. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(fields));
            }

            string fieldList = string.Join(",", fields);
            paramPlaceholders = paramPlaceholders.Remove(paramPlaceholders.Length - 1, 1);

            String query = String.Format("INSERT INTO {0}({1}) VALUES({2})", table, fieldList, paramPlaceholders);
            return query;
        }

        public string UpdateQuery(string[] fields, string table, string[] values, string condition = "")
        {
            if (!_validator.IsValidIdentifier(table))
                throw new ArgumentException("Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(table));

            if (fields == null || fields.Length == 0)
                throw new ArgumentException("Fields array cannot be null or empty.", nameof(fields));

            if (values == null || values.Length == 0)
                throw new ArgumentException("Values array cannot be null or empty.", nameof(values));

            if (fields.Length != values.Length)
                throw new ArgumentException("Field and value arrays must have the same length.");

            if (string.IsNullOrEmpty(condition))
                throw new ArgumentException("Condition is required for UPDATE operations for security reasons.", nameof(condition));

            foreach (var field in fields)
            {
                if (!_validator.IsValidIdentifier(field))
                    throw new ArgumentException($"Invalid field name '{field}'. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(fields));
            }

            // Work on a copy to avoid mutating the caller's array
            string[] processedValues = (string[])values.Clone();

            for (int cont = 0; cont < processedValues.Length; cont++)
            {
                if (processedValues[cont] == null)
                {
                    processedValues[cont] = "null";
                    continue;
                }

                double numero;
                if (double.TryParse(processedValues[cont], out numero) == false)
                {
                    if (processedValues[cont].Length == 0)
                    {
                        processedValues[cont] = "null";
                    }
                    else if (processedValues[cont].Substring(0, 1) == "'" && processedValues[cont].Length >= 2)
                    {
                        string escapedValue = processedValues[cont].Substring(1, processedValues[cont].Length - 2).Replace("'", "''");
                        processedValues[cont] = "'" + escapedValue + "'";
                    }
                    else
                    {
                        processedValues[cont] = "'" + processedValues[cont].Replace("'", "''") + "'";
                    }
                }
                else
                {
                    processedValues[cont] = processedValues[cont].Replace(",", ".");
                }
            }

            String setClause = "";
            for (int cont = 0; cont < fields.Length; cont++)
            {
                setClause += fields[cont] + "=" + processedValues[cont] + ",";
            }
            setClause = setClause.Substring(0, setClause.Length - 1);

            String query = String.Format("UPDATE {0} SET {1} WHERE {2}", table, setClause, condition);
            return query;
        }

        public string DeleteQuery(string table, string condition)
        {
            if (!_validator.IsValidIdentifier(table))
                throw new ArgumentException("Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed.", nameof(table));

            if (string.IsNullOrEmpty(condition))
                throw new ArgumentException("Condition is required for DELETE operations for security reasons.", nameof(condition));

            String query = String.Format("DELETE FROM {0} WHERE {1}", table, condition);
            return query;
        }

        /// <summary>
        /// Generates a list of DbParameter instances from the given values.
        /// NOTE: This method creates SqlParameter instances directly because SqlQueryBuilder
        /// is typically used in conjunction with SqlClient (SQL Server). For non-SqlServer providers,
        /// callers should use IDbProvider.CreateParameter() instead.
        /// </summary>
        public List<DbParameter> GenerateSqlParameters(object[] values)
        {
            List<DbParameter> sqlParams = new List<DbParameter>();
            for (int i = 0; i < values.Length; i++)
            {
                string paramName = "@param" + i;
                sqlParams.Add(new SqlParameter(paramName, values[i] ?? (object)DBNull.Value));
            }
            return sqlParams;
        }

        /// <summary>
        /// Generates a list of DbParameter instances from the given values using the specified provider.
        /// This overload creates provider-appropriate parameters and should be used for non-SqlServer providers.
        /// </summary>
        public List<DbParameter> GenerateSqlParameters(object[] values, IDbProvider provider)
        {
            List<DbParameter> sqlParams = new List<DbParameter>();
            for (int i = 0; i < values.Length; i++)
            {
                string paramName = "@param" + i;
                sqlParams.Add(provider.CreateParameter(paramName, values[i] ?? (object)DBNull.Value));
            }
            return sqlParams;
        }
    }
}
