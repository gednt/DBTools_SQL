using DBTools.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;

namespace DBToolsUnitTest.Linq
{
    [TestClass]
    public class DataRowRecordAdapterTests
    {
        private static DataTable CreateTable()
        {
            var table = new DataTable();
            table.Columns.Add("id", typeof(int));
            table.Columns.Add("name", typeof(string));
            table.Columns.Add("amount", typeof(decimal));
            table.Columns.Add("created_at", typeof(DateTime));
            return table;
        }

        [TestMethod]
        public void DataRowRecordAdapter_FieldCount_MatchesTableColumnCount()
        {
            var table = CreateTable();
            var row = table.NewRow();
            var adapter = new DataRowRecordAdapter(row);

            Assert.AreEqual(4, adapter.FieldCount);
        }

        [TestMethod]
        public void DataRowRecordAdapter_GetName_ReturnsColumnName()
        {
            var table = CreateTable();
            var row = table.NewRow();
            var adapter = new DataRowRecordAdapter(row);

            Assert.AreEqual("id", adapter.GetName(0));
            Assert.AreEqual("name", adapter.GetName(1));
            Assert.AreEqual("amount", adapter.GetName(2));
            Assert.AreEqual("created_at", adapter.GetName(3));
        }

        [TestMethod]
        public void DataRowRecordAdapter_GetOrdinal_ReturnsColumnIndex()
        {
            var table = CreateTable();
            var row = table.NewRow();
            var adapter = new DataRowRecordAdapter(row);

            Assert.AreEqual(0, adapter.GetOrdinal("id"));
            Assert.AreEqual(1, adapter.GetOrdinal("name"));
            Assert.AreEqual(2, adapter.GetOrdinal("amount"));
            Assert.AreEqual(3, adapter.GetOrdinal("created_at"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DataRowRecordAdapter_NullRow_Throws()
        {
            new DataRowRecordAdapter(null);
        }

        [TestMethod]
        public void DataRowRecordAdapter_IndexerByInt_ReturnsValue()
        {
            var table = CreateTable();
            var row = table.NewRow();
            row["id"] = 7;
            row["name"] = "Alice";
            row["amount"] = 12.34m;
            var adapter = new DataRowRecordAdapter(row);

            Assert.AreEqual(7, adapter[0]);
            Assert.AreEqual("Alice", adapter[1]);
            Assert.AreEqual(12.34m, adapter[2]);
        }

        [TestMethod]
        public void DataRowRecordAdapter_IndexerByName_ReturnsValue()
        {
            var table = CreateTable();
            var row = table.NewRow();
            row["id"] = 7;
            row["name"] = "Alice";
            var adapter = new DataRowRecordAdapter(row);

            Assert.AreEqual(7, adapter["id"]);
            Assert.AreEqual("Alice", adapter["name"]);
        }

        [TestMethod]
        public void DataRowRecordAdapter_GetValue_ReturnsRawValue()
        {
            var table = CreateTable();
            var row = table.NewRow();
            row["name"] = "Bob";
            var adapter = new DataRowRecordAdapter(row);

            Assert.AreEqual("Bob", adapter.GetValue(1));
        }

        [TestMethod]
        public void DataRowRecordAdapter_IsDBNull_TrueForNullAndDbNull()
        {
            var table = CreateTable();
            var row = table.NewRow();
            // row["name"] is null by default
            var adapter = new DataRowRecordAdapter(row);

            Assert.IsTrue(adapter.IsDBNull(0));
            Assert.IsTrue(adapter.IsDBNull(1));

            row["name"] = DBNull.Value;
            Assert.IsTrue(adapter.IsDBNull(1));
        }

        [TestMethod]
        public void DataRowRecordAdapter_IsDBNull_FalseForActualValue()
        {
            var table = CreateTable();
            var row = table.NewRow();
            row["id"] = 1;
            var adapter = new DataRowRecordAdapter(row);

            Assert.IsFalse(adapter.IsDBNull(0));
        }

        [TestMethod]
        public void DataRowRecordAdapter_GetString_ReturnsStringValue()
        {
            var table = CreateTable();
            var row = table.NewRow();
            row["name"] = "Carol";
            var adapter = new DataRowRecordAdapter(row);

            Assert.AreEqual("Carol", adapter.GetString(1));
        }

        [TestMethod]
        public void DataRowRecordAdapter_GetInt32_ReturnsIntValue()
        {
            var table = CreateTable();
            var row = table.NewRow();
            row["id"] = 42;
            var adapter = new DataRowRecordAdapter(row);

            Assert.AreEqual(42, adapter.GetInt32(0));
        }

        [TestMethod]
        public void DataRowRecordAdapter_GetDecimal_ReturnsDecimalValue()
        {
            var table = CreateTable();
            var row = table.NewRow();
            row["amount"] = 99.95m;
            var adapter = new DataRowRecordAdapter(row);

            Assert.AreEqual(99.95m, adapter.GetDecimal(2));
        }

        [TestMethod]
        public void DataRowRecordAdapter_GetDateTime_ReturnsDateTimeValue()
        {
            var table = CreateTable();
            var row = table.NewRow();
            var when = new DateTime(2024, 6, 15, 12, 30, 0, DateTimeKind.Utc);
            row["created_at"] = when;
            var adapter = new DataRowRecordAdapter(row);

            Assert.AreEqual(when, adapter.GetDateTime(3));
        }

        [TestMethod]
        public void DataRowRecordAdapter_GetGuid_ParsesStringValue()
        {
            var table = new DataTable();
            table.Columns.Add("id", typeof(string));
            var row = table.NewRow();
            var guid = Guid.NewGuid();
            row["id"] = guid.ToString();
            var adapter = new DataRowRecordAdapter(row);

            Assert.AreEqual(guid, adapter.GetGuid(0));
        }

        [TestMethod]
        public void DataRowRecordAdapter_GetValues_FillsArray()
        {
            var table = CreateTable();
            var row = table.NewRow();
            row["id"] = 1;
            row["name"] = "X";
            var adapter = new DataRowRecordAdapter(row);

            var values = new object[4];
            int count = adapter.GetValues(values);

            Assert.AreEqual(4, count);
            Assert.AreEqual(1, values[0]);
            Assert.AreEqual("X", values[1]);
        }

        [TestMethod]
        public void DataRowRecordAdapter_GetDataTypeName_ReturnsColumnTypeName()
        {
            var table = CreateTable();
            var row = table.NewRow();
            var adapter = new DataRowRecordAdapter(row);

            Assert.AreEqual("Int32", adapter.GetDataTypeName(0));
            Assert.AreEqual("String", adapter.GetDataTypeName(1));
        }

        [TestMethod]
        public void DataRowRecordAdapter_GetFieldType_ReturnsColumnType()
        {
            var table = CreateTable();
            var row = table.NewRow();
            var adapter = new DataRowRecordAdapter(row);

            Assert.AreEqual(typeof(int), adapter.GetFieldType(0));
            Assert.AreEqual(typeof(string), adapter.GetFieldType(1));
        }
    }
}