using System.Collections.Generic;
using System.Data.Common;

namespace DBTools.Abstractions
{
    public interface ISqlQueryBuilder
    {
        string SelectQuery(string fields, string table, string conditions);
        string InsertQuery(string[] fields, string table, object[] values, string primaryKeyName = "", bool autoIncrement = true);
        string UpdateQuery(string[] fields, string table, string[] values, string condition = "");
        string DeleteQuery(string table, string condition);
        List<DbParameter> GenerateSqlParameters(object[] values);
    }
}
