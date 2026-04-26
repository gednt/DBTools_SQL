using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Core;
using System;

namespace DBToolsUnitTest.Core
{
    [TestClass]
    public class SqlClientQueryBuilderTests : TestBase
    {
        [TestMethod]
        public void Select_Query_WithValidParameters_ShouldReturnQuery()
        {
            string query = SqlClient.Select_Query("id, name", "Users", "id > 10");
            Assert.IsNotNull(query);
            Assert.IsTrue(query.Contains("SELECT"));
            Assert.IsTrue(query.Contains("FROM Users"));
            Assert.IsTrue(query.Contains("WHERE id > 10"));
        }

        [TestMethod]
        public void Select_Query_WithoutConditions_ShouldReturnQueryWithoutWhere()
        {
            string query = SqlClient.Select_Query("*", "Users", "");
            Assert.IsNotNull(query);
            Assert.IsTrue(query.Contains("SELECT *"));
            Assert.IsTrue(query.Contains("FROM Users"));
            Assert.IsFalse(query.Contains("WHERE"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Select_Query_WithInvalidTable_ShouldThrowException()
        {
            SqlClient.Select_Query("*", "Users; DROP TABLE--", "");
        }

        [TestMethod]
        public void Insert_Query_WithValidParameters_ShouldReturnQuery()
        {
            string[] fields = { "name", "email" };
            string[] values = { "John", "john@test.com" };
            string query = SqlClient.Insert_Query(fields, "Users", values);
            Assert.IsNotNull(query);
            Assert.IsTrue(query.Contains("INSERT INTO Users"));
            Assert.IsTrue(query.Contains("(name,email)"));
            Assert.IsTrue(query.Contains("VALUES"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Insert_Query_WithNullFields_ShouldThrowException()
        {
            string[] values = { "John" };
            SqlClient.Insert_Query(null, "Users", values);
        }

        [TestMethod]
        public void Update_Query_WithValidParameters_ShouldReturnQuery()
        {
            string[] fields = { "name", "email" };
            string[] values = { "John Updated", "john.updated@test.com" };
            string query = SqlClient.Update_Query(fields, "Users", values, "id = 1");
            Assert.IsNotNull(query);
            Assert.IsTrue(query.Contains("UPDATE Users"));
            Assert.IsTrue(query.Contains("SET"));
            Assert.IsTrue(query.Contains("WHERE id = 1"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Update_Query_WithoutCondition_ShouldThrowException()
        {
            string[] fields = { "name" };
            string[] values = { "John" };
            SqlClient.Update_Query(fields, "Users", values, "");
        }

        [TestMethod]
        public void Delete_Query_WithValidParameters_ShouldReturnQuery()
        {
            string query = SqlClient.Delete_Query("Users", "id = 1");
            Assert.IsNotNull(query);
            Assert.IsTrue(query.Contains("DELETE FROM Users"));
            Assert.IsTrue(query.Contains("WHERE id = 1"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Delete_Query_WithoutCondition_ShouldThrowException()
        {
            SqlClient.Delete_Query("Users", "");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Delete_Query_WithInvalidTable_ShouldThrowException()
        {
            SqlClient.Delete_Query("Users; DROP TABLE--", "id = 1");
        }
    }
}
