using System;
using System.Data;

namespace DBTools.Linq
{
    /// <summary>
    /// Wraps a <see cref="DataRow"/> as an <see cref="IDataRecord"/> so that
    /// <see cref="DBTools.Mapping.EntityMapping.ModelFactory"/> can consume rows
    /// from a <see cref="DataTable"/> without depending on <see cref="DataTable.Load(IDataReader)"/>
    /// (which can re-enforce SQL UNIQUE constraints as <see cref="DataTable"/> constraints).
    ///
    /// The adapter is intentionally minimal: it implements the <see cref="IDataRecord"/>
    /// surface that hydrate methods typically need (<c>GetName</c>, <c>GetOrdinal</c>,
    /// indexer access, <c>IsDBNull</c>, <c>FieldCount</c>, <c>GetValue</c>, and the
    /// common typed accessors). Members that aren't used by current consumers throw
    /// <see cref="NotImplementedException"/>; expand the adapter if a new consumer
    /// needs them.
    /// </summary>
    public sealed class DataRowRecordAdapter : IDataRecord
    {
        private readonly DataRow _row;
        private readonly DataTable _table;

        public DataRowRecordAdapter(DataRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            _row = row;
            _table = row.Table;
        }

        public object this[int i] => _row[i];

        public object this[string name] => _row[name];

        public int FieldCount => _table?.Columns.Count ?? 0;

        public bool GetBoolean(int i) => Convert.ToBoolean(_row[i]);

        public byte GetByte(int i) => Convert.ToByte(_row[i]);

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException("GetBytes is not implemented by DataRowRecordAdapter.");
        }

        public char GetChar(int i) => Convert.ToChar(_row[i]);

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException("GetChars is not implemented by DataRowRecordAdapter.");
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException("GetData is not implemented by DataRowRecordAdapter.");
        }

        public string GetDataTypeName(int i) => _table.Columns[i].DataType.Name;

        public DateTime GetDateTime(int i) => Convert.ToDateTime(_row[i], System.Globalization.CultureInfo.InvariantCulture);

        public decimal GetDecimal(int i) => Convert.ToDecimal(_row[i], System.Globalization.CultureInfo.InvariantCulture);

        public double GetDouble(int i) => Convert.ToDouble(_row[i], System.Globalization.CultureInfo.InvariantCulture);

        public Type GetFieldType(int i) => _table.Columns[i].DataType;

        public float GetFloat(int i) => (float)Convert.ToDouble(_row[i], System.Globalization.CultureInfo.InvariantCulture);

        public Guid GetGuid(int i)
        {
            var value = _row[i];
            if (value is Guid g) return g;
            return Guid.Parse(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
        }

        public short GetInt16(int i) => Convert.ToInt16(_row[i]);

        public int GetInt32(int i) => Convert.ToInt32(_row[i]);

        public long GetInt64(int i) => Convert.ToInt64(_row[i]);

        public string GetName(int i) => _table.Columns[i].ColumnName;

        public int GetOrdinal(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return _table.Columns.IndexOf(name);
        }

        public string GetString(int i) => Convert.ToString(_row[i], System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;

        public object GetValue(int i) => _row[i];

        public int GetValues(object[] values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            int count = Math.Min(values.Length, FieldCount);
            for (int i = 0; i < count; i++)
                values[i] = _row[i];
            return count;
        }

        public bool IsDBNull(int i)
        {
            var value = _row[i];
            return value == null || value == DBNull.Value;
        }
    }
}