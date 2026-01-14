using System;

namespace DBTools.Model
{
    public class GenericObject
    {
        /// <summary>
        /// Initiates a new instance of a Generic Object, not able to do operations in the database.
        /// </summary>
        /// <param name="dbTools"></param>
        public GenericObject()
        {

        }

        /// <summary>
        /// Initiates a new instance of a Generic Object, able to do operations in the database.
        /// </summary>
        /// <param name="dbTools"></param>
        public GenericObject(DBTools_Utilities.Utils dbTools)
        {
            this.dbTools = dbTools;
        }
        private DBTools_Utilities.Utils dbTools = new DBTools_Utilities.Utils();
        public String[] columns { get; set; }
        public Object[] values { get; set; }
        public String[] valuesString { get; set; }
        //  public DataView dataView { get; set; }
        public String[] types { get; set; }
        public String table { get; set; }


        public bool Insert()
        {
            return dbTools.Insert(columns, table, valuesString);
        }

        public bool Update(string conditions)
        {
            return dbTools.Update(columns, table, valuesString, conditions);
        }

        public bool Delete(string conditions)
        {
            return dbTools.Delete(table, conditions);
        }
    }
}
