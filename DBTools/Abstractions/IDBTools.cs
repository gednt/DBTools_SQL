using DBTools.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace DBTools.Abstractions
{
    public interface IDBTools
    {
        List<GenericObject> RetrieveObjectSQL();
        DataView RetrieveDataSQL();
        void SqlExecuteQuery(string query = "");
    }
}
