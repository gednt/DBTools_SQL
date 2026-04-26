using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Core;
using System;

namespace DBToolsUnitTest.Core
{
    [TestClass]
    public class SqlClientValidationTests : TestBase
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Insert_WithInvalidTableName_ShouldThrowException()
        {
            var utils = new SqlClient();
            string[] fields = { "name", "email" };
            string[] values = { "John", "john@test.com" };
            utils.Insert(fields, "Users'; DROP TABLE Users--", values);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Insert_WithInvalidFieldName_ShouldThrowException()
        {
            var utils = new SqlClient();
            string[] fields = { "name'; DROP TABLE--", "email" };
            string[] values = { "John", "john@test.com" };
            utils.Insert(fields, "Users", values);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Insert_WithEmptyFields_ShouldThrowException()
        {
            var utils = new SqlClient();
            string[] fields = { };
            string[] values = { "John", "john@test.com" };
            utils.Insert(fields, "Users", values);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Update_WithEmptyCondition_ShouldThrowException()
        {
            var utils = new SqlClient();
            string[] fields = { "name" };
            string[] values = { "John" };
            utils.Update(fields, "Users", values, "");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Update_WithNullCondition_ShouldThrowException()
        {
            var utils = new SqlClient();
            string[] fields = { "name" };
            string[] values = { "John" };
            utils.Update(fields, "Users", values, null);
        }
    }
}
