using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace DBTools.Abstractions
{
    public interface ISqlQueryBuilder
    {
        string SelectQuery(string fields, string table, string conditions);
        string InsertQuery(string[] fields, string table, object[] values, string primaryKeyName = "", bool autoIncrement = true);
        string UpdateQuery(string[] fields, string table, string[] values, string condition = "");
        string DeleteQuery(string table, string condition);
        List<SqlParameter> GenerateSqlParameters(object[] values);
    }
}
