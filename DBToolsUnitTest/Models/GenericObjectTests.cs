using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Core;
using DBTools.Models;
using System;

namespace DBToolsUnitTest.Models
{
    [TestClass]
    public class GenericObjectTests : TestBase
    {
        [TestMethod]
        public void Constructor_Default_ShouldCreateInstance()
        {
            var obj = new GenericObject();
            Assert.IsNotNull(obj);
        }

        [TestMethod]
        public void Constructor_WithSqlClient_ShouldCreateInstance()
        {
            var utils = new SqlClient();
            var obj = new GenericObject(utils);
            Assert.IsNotNull(obj);
        }

        [TestMethod]
        public void Properties_ShouldStoreValues()
        {
            var obj = new GenericObject
            {
                columns = new[] { "id", "name" },
                values = new object[] { 1, "John" },
                types = new[] { "Int32", "String" },
                table = "Users"
            };
            Assert.AreEqual("Users", obj.table);
            Assert.AreEqual(2, obj.columns.Length);
            Assert.AreEqual(2, obj.values.Length);
        }
    }
}
