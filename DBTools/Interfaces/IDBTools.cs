using DBTools.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBTools.Interfaces
{
    interface IDBTools
    {
        List<GenericObject> RetrieveObjectSQL();

        DataView RetrieveDataSQL();
        void SqlExecuteQuery(string query = "");



    }
}
