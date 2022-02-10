using System;

namespace DBTools.Model
{
    public class GenericObject
    {
        public String[] columns { get; set; }
        public Object[] values { get; set; }
        public String[] valuesString { get; set; }
        //  public DataView dataView { get; set; }
        public String[] types { get; set; }
        public String table { get; set; }
        public bool Insert(DBTools_Utilities.Utils dBTools)
        {
            return dBTools.Insert(columns, table, valuesString);
        }

        public bool Update(DBTools_Utilities.Utils dBTools, string conditions)
        {
            return dBTools.Update(columns, table, valuesString, conditions);
        }

        public bool Delete(DBTools_Utilities.Utils dBTools, string conditions)
        {
            return dBTools.Delete(table, conditions);
        }
    }
}
