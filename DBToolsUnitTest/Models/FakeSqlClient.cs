using DBTools.Abstractions;
using System;
using System.Collections.Generic;
using System.Data;

namespace DBToolsUnitTest.Models
{
    /// <summary>
    /// Minimal <see cref="ISqlClient"/> implementation used to verify that
    /// <see cref="DBTools.Models.GenericObject"/> forwards arguments unchanged
    /// to the injected client. Avoids pulling a mocking dependency into the test project.
    /// </summary>
    internal sealed class FakeSqlClient : ISqlClient
    {
        public List<DBTools.Models.GenericObject> QueryBuilder(object obj, string primaryKeyName = "", bool autoIncrement = true)
            => throw new NotSupportedException();

        public DataView Select(string fields, string table, string whereClause, object[] parameters)
            => throw new NotSupportedException();

        public DataView Select(string queryWithoutSelect, object[] parameters)
            => throw new NotSupportedException();

        public bool Insert(string[] fields, string table, object[] values, string primaryKeyName = null, bool autoIncrement = true)
        {
            LastInsert = new InsertCall(fields, table, values, primaryKeyName, autoIncrement);
            InsertCallCount++;
            return InsertResult;
        }

        public bool Update(string[] fields, string table, string[] values, string condition = "")
        {
            LastUpdate = new UpdateCall(fields, table, values, condition, null);
            UpdateCallCount++;
            return UpdateResult;
        }

        public bool Update(string[] fields, string table, string[] values, string whereClause, object[] whereParameters)
        {
            LastUpdate = new UpdateCall(fields, table, values, whereClause, whereParameters);
            UpdateCallCount++;
            return UpdateResult;
        }

        public bool Delete(string table, string whereClause, object[] parameters)
            => throw new NotSupportedException();

        public string[] GetInBd(string query)
            => throw new NotSupportedException();

        public DataView GetInBdDv(string query)
            => throw new NotSupportedException();

        public void ExecuteQuery(string query)
            => throw new NotSupportedException();

        public ISqlQueryBuilder QueryBuilderInstance => throw new NotSupportedException();

        public IDbConfiguration Configuration => throw new NotSupportedException();

        public bool InsertResult { get; set; } = true;

        public bool UpdateResult { get; set; } = true;

        public int InsertCallCount { get; private set; }

        public int UpdateCallCount { get; private set; }

        public InsertCall LastInsert { get; private set; }

        public UpdateCall LastUpdate { get; private set; }

        public sealed class InsertCall
        {
            public InsertCall(string[] fields, string table, object[] values, string primaryKeyName, bool autoIncrement)
            {
                Fields = fields;
                Table = table;
                Values = values;
                PrimaryKeyName = primaryKeyName;
                AutoIncrement = autoIncrement;
            }

            public string[] Fields { get; }
            public string Table { get; }
            public object[] Values { get; }
            public string PrimaryKeyName { get; }
            public bool AutoIncrement { get; }
        }

        public sealed class UpdateCall
        {
            public UpdateCall(string[] fields, string table, string[] values, string condition, object[] whereParameters)
            {
                Fields = fields;
                Table = table;
                Values = values;
                Condition = condition;
                WhereParameters = whereParameters;
            }

            public string[] Fields { get; }
            public string Table { get; }
            public string[] Values { get; }
            public string Condition { get; }
            public object[] WhereParameters { get; }
        }
    }
}