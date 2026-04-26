using DBTools.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace DBTools.Abstractions
{
    public interface ISqlClient
    {
        DataView Select(string fields, string table, string whereClause, object[] parameters);
        DataView Select(string queryWithoutSelect, object[] parameters);
        bool Insert(string[] fields, string table, object[] values, string primaryKeyName = null, bool autoIncrement = true);
        bool Update(string[] fields, string table, string[] values, string condition = "");
        bool Update(string[] fields, string table, string[] values, string whereClause, object[] whereParameters);
        bool Delete(string table, string whereClause, object[] parameters);
        List<GenericObject> QueryBuilder(object obj, string primaryKeyName = "", bool autoIncrement = true);
        string[] GetInBd(string query);
        DataView GetInBdDv(string query);
        void ExecuteQuery(string query);
        ISqlQueryBuilder QueryBuilderInstance { get; }
        IDbConfiguration Configuration { get; }
    }
}
