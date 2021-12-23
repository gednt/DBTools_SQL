using DBTools.Model;
using DBToolsDll;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBTools.Controller
{
    public class DBToolsController : Interfaces.IDBTools
    {
        public DBToolsDll.DBTools_SQL DBTools = new DBToolsDll.DBTools_SQL();

        public DBToolsController(DBToolsDll.DBTools_SQL dbTools)
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
            DBTools.SqlExecuteQuery();
        }
    }
}
