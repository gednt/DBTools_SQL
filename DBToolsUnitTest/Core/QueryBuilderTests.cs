using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Core;
using DBTools.Models;
using System;
using System.Linq;

namespace DBToolsUnitTest.Core
{
    [TestClass]
    public class QueryBuilderTests : TestBase
    {
        private class TestModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public DateTime CreatedDate { get; set; }
        }

        [TestMethod]
        public void QueryBuilder_WithObject_ShouldReturnGenericObjectList()
        {
            var utils = new SqlClient();
            var model = new TestModel
            {
                Id = 1,
                Name = "John",
                Email = "john@test.com",
                CreatedDate = new DateTime(2024, 1, 1, 10, 30, 0)
            };
            var result = utils.QueryBuilder(model, "Id", true);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0].columns.Length > 0);
            Assert.IsTrue(result[0].values.Length > 0);
        }

        [TestMethod]
        public void QueryBuilder_WithAutoIncrement_ShouldExcludePrimaryKey()
        {
            var utils = new SqlClient();
            var model = new TestModel
            {
                Id = 1,
                Name = "John",
                Email = "john@test.com",
                CreatedDate = DateTime.Now
            };
            var result = utils.QueryBuilder(model, "Id", true);
            Assert.IsNotNull(result);
            bool hasId = false;
            foreach (var col in result[0].columns)
            {
                if (col == "Id") { hasId = true; break; }
            }
            Assert.IsFalse(hasId, "Id should be excluded when autoIncrement is true");
        }

        [TestMethod]
        public void QueryBuilder_WithoutAutoIncrement_ShouldIncludePrimaryKey()
        {
            var utils = new SqlClient();
            var model = new TestModel
            {
                Id = 1,
                Name = "John",
                Email = "john@test.com",
                CreatedDate = DateTime.Now
            };
            var result = utils.QueryBuilder(model, "Id", false);
            Assert.IsNotNull(result);
            bool hasId = false;
            foreach (var col in result[0].columns)
            {
                if (col == "Id") { hasId = true; break; }
            }
            Assert.IsTrue(hasId, "Id should be included when autoIncrement is false");
        }

        [TestMethod]
        public void QueryBuilder_WithDateTime_ShouldFormatCorrectly()
        {
            var utils = new SqlClient();
            var model = new TestModel
            {
                Id = 1,
                Name = "John",
                Email = "john@test.com",
                CreatedDate = new DateTime(2024, 1, 15, 10, 30, 45)
            };
            var result = utils.QueryBuilder(model, "Id", true);
            Assert.IsNotNull(result);
            bool hasCorrectDateFormat = result[0].valuesString
                .Any(val => val.Contains("2024-01-15"));
            Assert.IsTrue(hasCorrectDateFormat, "DateTime should be formatted as yyyy-MM-dd HH:mm:ss");
        }
    }
}
