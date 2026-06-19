using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Core;
using System;

namespace DBToolsUnitTest.Core
{
    [TestClass]
    [TestCategory("Integration")]
    public class SqlClientParameterizedQueryTests : TestBase
    {
        [TestMethod]
        public void Select_WithParameters_ShouldNotThrowException()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            object[] parameters = { 1, "active" };
            try
            {
                utils.Select("*", "Users", "id = @param0 AND status = @param1", parameters);
            }
            catch (ArgumentException ex)
            {
                Assert.Fail($"Should not throw exception for parameterized query: {ex.Message}");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Select_WithNullParameters_ShouldThrowException()
        {
            var utils = new SqlClient();
            utils.Select("*", "Users", "id = @param0", null);
        }

        [TestMethod]
        public void Update_WithParameterizedWhereClause_ShouldNotThrowException()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            string[] fields = { "name", "email" };
            string[] values = { "John Updated", "john.updated@test.com" };
            object[] whereParams = { 1 };
            try
            {
                utils.Update(fields, "Users", values, "id = @whereParam0", whereParams);
            }
            catch (ArgumentException ex)
            {
                Assert.Fail($"Should not throw exception for parameterized update: {ex.Message}");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Update_WithParameterizedWhereClause_EmptyWhereClause_ShouldThrowException()
        {
            var utils = new SqlClient();
            string[] fields = { "name" };
            string[] values = { "John" };
            object[] whereParams = { };
            utils.Update(fields, "Users", values, "", whereParams);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Update_WithParameterizedWhereClause_NullParameters_ShouldThrowException()
        {
            var utils = new SqlClient();
            string[] fields = { "name" };
            string[] values = { "John" };
            utils.Update(fields, "Users", values, "id = @whereParam0", null);
        }

        [TestMethod]
        public void Delete_WithParameters_ShouldNotThrowException()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            object[] parameters = { 1 };
            try
            {
                utils.Delete("Users", "id = @param0", parameters);
            }
            catch (ArgumentException ex)
            {
                Assert.Fail($"Should not throw exception for parameterized delete: {ex.Message}");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Delete_WithNullParameters_ShouldThrowException()
        {
            var utils = new SqlClient();
            utils.Delete("Users", "id = @param0", null);
        }

        [TestMethod]
        public void Select_WithQueryAndParameters_ShouldNotThrowException()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            object[] parameters = { "John" };
            try
            {
                utils.Select("* FROM Users WHERE name = @param0", parameters);
            }
            catch (ArgumentException ex)
            {
                Assert.Fail($"Should not throw exception for parameterized select query: {ex.Message}");
            }
        }
    }
}
