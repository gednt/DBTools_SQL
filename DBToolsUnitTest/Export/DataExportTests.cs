using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Models;
using DBTools.Export;
using System;
using System.Collections.Generic;
using System.Data;

namespace DBToolsUnitTest.Export
{
    [TestClass]
    public class DataExportTests : TestBase
    {
        [TestMethod]
        public void ToCsv_WithData_ShouldReturnCsvString()
        {
            var dataExport = new DataExport();
            var genericObjects = new List<GenericObject>
            {
                new GenericObject
                {
                    columns = new[] { "id", "name", "email" },
                    values = new object[] { 1, "John", "john@test.com" },
                    types = new[] { "Int32", "String", "String" }
                },
                new GenericObject
                {
                    columns = new[] { "id", "name", "email" },
                    values = new object[] { 2, "Jane", "jane@test.com" },
                    types = new[] { "Int32", "String", "String" }
                }
            };
            string csv = dataExport.ToCsv(genericObjects, ',', true, true);
            Assert.IsNotNull(csv);
            Assert.IsTrue(csv.Contains("Int32,String,String"));
            Assert.IsTrue(csv.Contains("id,name,email"));
            Assert.IsTrue(csv.Contains("John"));
            Assert.IsTrue(csv.Contains("Jane"));
        }

        [TestMethod]
        public void ToCsv_WithEmptyList_ShouldReturnEmptyString()
        {
            var dataExport = new DataExport();
            var genericObjects = new List<GenericObject>();
            string result = dataExport.ToCsv(genericObjects, ',', true, true);
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ToCsv_WithNullList_ShouldThrowArgumentNullException()
        {
            var dataExport = new DataExport();
            dataExport.ToCsv(null, ',', true, true);
        }

        [TestMethod]
        public void ToCsv_WithoutColumnsAndTypes_ShouldReturnDataOnly()
        {
            var dataExport = new DataExport();
            var genericObjects = new List<GenericObject>
            {
                new GenericObject
                {
                    columns = new[] { "id", "name" },
                    values = new object[] { 1, "John" },
                    types = new[] { "Int32", "String" }
                }
            };
            string csv = dataExport.ToCsv(genericObjects, ',', false, false);
            Assert.IsNotNull(csv);
            Assert.IsFalse(csv.Contains("id,name"));
            Assert.IsFalse(csv.Contains("Int32,String"));
            Assert.IsTrue(csv.Contains("John"));
        }

        [TestMethod]
        public void ToCsv_WithDbNullValue_ShouldReturnEmptyField()
        {
            var dataExport = new DataExport();
            var genericObjects = new List<GenericObject>
            {
                new GenericObject
                {
                    columns = new[] { "id", "name" },
                    values = new object[] { 1, DBNull.Value },
                    types = new[] { "Int32", "String" }
                }
            };
            string csv = dataExport.ToCsv(genericObjects, ',', true, true);
            Assert.IsNotNull(csv);
        }

        [TestMethod]
        public void ToDataTable_WithValidCsv_ShouldReturnDataTable()
        {
            var dataExport = new DataExport();
            string csv = "Int32,String\nid,name\n1,John\n2,Jane";
            DataTable dt = dataExport.ToDataTable(csv, ',', true);
            Assert.IsNotNull(dt);
            Assert.AreEqual(2, dt.Columns.Count);
            Assert.AreEqual(2, dt.Rows.Count);
            Assert.AreEqual("1", dt.Rows[0][0].ToString());
            Assert.AreEqual("John", dt.Rows[0][1].ToString());
            Assert.AreEqual("2", dt.Rows[1][0].ToString());
            Assert.AreEqual("Jane", dt.Rows[1][1].ToString());
        }

        [TestMethod]
        public void ToDataTable_RowValuesAreCorrect()
        {
            var dataExport = new DataExport();
            string csv = "id,name,email\n1,Alice,alice@test.com\n2,Bob,bob@test.com";
            DataTable dt = dataExport.ToDataTable(csv, ',', false);
            Assert.AreEqual(3, dt.Columns.Count);
            Assert.AreEqual(2, dt.Rows.Count);
            Assert.AreEqual("Alice", dt.Rows[0][1].ToString());
            Assert.AreEqual("bob@test.com", dt.Rows[1][2].ToString());
        }

        [TestMethod]
        public void ToDataTable_WithCsvNoTypes_ShouldReturnDataTable()
        {
            var dataExport = new DataExport();
            string csv = "id,name\n1,John\n2,Jane";
            DataTable dt = dataExport.ToDataTable(csv, ',', false);
            Assert.IsNotNull(dt);
            Assert.AreEqual(2, dt.Columns.Count);
            Assert.AreEqual(2, dt.Rows.Count);
        }

        [TestMethod]
        public void ToDataTable_WithNullCsv_ShouldReturnEmptyDataTable()
        {
            var dataExport = new DataExport();
            DataTable dt = dataExport.ToDataTable(null, ',', false);
            Assert.IsNotNull(dt);
            Assert.AreEqual(0, dt.Columns.Count);
        }
    }
}
