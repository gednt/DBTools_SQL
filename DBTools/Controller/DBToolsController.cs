using DbTools.Model;
using DbTools;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbTools.Controller
{
    public class DBToolsController : Interfaces.IDBTools
    {
        public DbTools.DBTools DBTools = new DbTools.DBTools();

        public DBToolsController(DbTools.DBTools dbTools)
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
