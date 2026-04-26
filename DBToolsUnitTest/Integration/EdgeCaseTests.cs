using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Core;
using DBTools.Models;
using DBTools.Export;
using System;
using System.Collections.Generic;

namespace DBToolsUnitTest.Integration
{
    [TestClass]
    public class EdgeCaseTests : TestBase
    {
        [TestMethod]
        public void Update_Query_WithNumericString_ShouldNotAddQuotes()
        {
            string[] fields = { "age" };
            string[] values = { "25" };
            string query = SqlClient.Update_Query(fields, "Users", values, "id = 1");
            Assert.IsTrue(query.Contains("age=25") || query.Contains("age=25.0"));
        }

        [TestMethod]
        public void ToCsv_WithSpecialCharacters_ShouldEscapeCorrectly()
        {
            var dataExport = new DataExport();
            var genericObjects = new List<GenericObject>
            {
                new GenericObject
                {
                    columns = new[] { "path" },
                    values = new object[] { "C:\\Users\\Test\r\nNewLine" },
                    types = new[] { "String" }
                }
            };
            string csv = dataExport.ToCsv(genericObjects, ',', false, false);
            Assert.IsNotNull(csv);
            Assert.IsTrue(csv.Contains("C:/Users/Test"));
            Assert.IsFalse(csv.Contains("\r\n"));
        }
    }
}
