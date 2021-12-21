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

    }
}
