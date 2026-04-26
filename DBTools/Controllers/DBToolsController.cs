using DBTools.Models;
using DBTools.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBTools.Abstractions;

namespace DBTools.Controllers
{
    public class DBToolsController : IDBTools
    {
        public DBTools.Core.DBTools DBTools = new DBTools.Core.DBTools();

        public DBToolsController(DBTools.Core.DBTools dbTools)
        {
            DBTools = dbTools;
        }

        public DataView RetrieveDataSQL()
        {
            return DBTools.RetrieveDataSql();
        }

        public List<GenericObject> RetrieveObjectSQL()
        {
            return DBTools.RetrieveObjectSql();
        }

        public void SqlExecuteQuery(String query = "")
        {
            DBTools.Query = query;
            DBTools.SqlExecuteQuery();
        }
    }
}
