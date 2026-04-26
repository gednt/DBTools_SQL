using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Core;
using DBTools.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DBToolsUnitTest.Controllers
{
    [TestClass]
    public class LinqHelperTests : TestBase
    {
        private class TestUser
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public int Age { get; set; }
        }

        [TestMethod]
        public void Constructor_WithConnectionParameters_ShouldInitialize()
        {
            var controller = new LinqHelper<TestUser>(new SqlClient(), "Users", "Id", true);
            Assert.IsNotNull(controller);
            Assert.IsNotNull(controller.Utils);
        }

        [TestMethod]
        public void Constructor_WithSqlClientInstance_ShouldInitialize()
        {
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            Assert.IsNotNull(controller);
            Assert.IsNotNull(controller.Utils);
        }

        [TestMethod]
        public void Insert_ShouldCallSqlClientInsertAndReturnTrue()
        {
            SkipIfDatabaseUnavailable();
            var randomAge = new Random().Next(18, 80);
            var name = Names();
            var surname = Surnames();
            var email = Emails(name, surname);
            string completeName = name + " " + surname;
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            var user = new TestUser { Name = completeName, Email = email, Age = randomAge };
            Assert.IsTrue(controller.Insert(user));
        }

        [TestMethod]
        public void InsertBatch_ShouldCallSqlClientInsertAndReturnTrue()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            var users = new List<TestUser>();
            for (int i = 0; i < 1000; i++)
            {
                System.Threading.Thread.Sleep(15);
                var randomAge = new Random().Next(18, 80);
                var name = Names();
                var surname = Surnames();
                var email = Emails(name, surname);
                string completeName = name + " " + surname;
                users.Add(new TestUser { Name = completeName, Email = email, Age = randomAge });
            }
            Assert.IsTrue(controller.InsertRange(users));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Update_WithoutCondition_ShouldThrowException()
        {
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users");
            var user = new TestUser { Id = 1, Name = "John" };
            controller.Update(user, "");
        }

        [TestMethod]
        public void SelectWithConditions_ShouldReturnList()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users");
            string conditions = "age > @param0";
            IEnumerable<object> parameters = new object[] { 25 };
            var result = controller.Select(conditions, parameters);
            Assert.IsTrue(result.Count() > 0);
            Assert.IsInstanceOfType(result, typeof(List<TestUser>));
        }

        [TestMethod]
        public void Error_Property_ShouldReflectSqlClientError()
        {
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users");
            utils.Error = "Test error";
            string error = controller.Error;
            Assert.AreEqual("Test error", error);
        }

        [TestMethod]
        public void Update_WithParameterizedWhereClause_ShouldNotThrowException()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            var user = new TestUser { Id = 1, Name = "Updated Name", Email = "updated@test.com", Age = 30 };
            object[] whereParams = { 1 };
            try
            {
                controller.Update(user, "Id = @whereParam0", whereParams);
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
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            var user = new TestUser { Id = 1, Name = "Test" };
            object[] whereParams = { };
            controller.Update(user, "", whereParams);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Update_WithParameterizedWhereClause_NullParameters_ShouldThrowException()
        {
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            var user = new TestUser { Id = 1, Name = "Test" };
            controller.Update(user, "Id = @whereParam0", null);
        }

        [TestMethod]
        public void All_ShouldReturnAllRecords()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            var result = controller.All();
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
        }

        [TestMethod]
        public void Count_WithoutConditions_ShouldReturnTotalCount()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            int count = controller.Count();
            Assert.IsTrue(count >= 0);
        }

        [TestMethod]
        public void Count_WithConditions_ShouldReturnFilteredCount()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            string conditions = "Age > @param0";
            object[] parameters = { 25 };
            int count = controller.Count(conditions, parameters);
            Assert.IsTrue(count >= 0);
        }

        [TestMethod]
        public void Any_WithMatchingConditions_ShouldReturnTrue()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            string conditions = "Age > @param0";
            object[] parameters = { 0 };
            bool exists = controller.Any(conditions, parameters);
            Assert.IsTrue(exists);
        }

        [TestMethod]
        public void Any_WithNoMatchingConditions_ShouldReturnFalse()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            string conditions = "Age > @param0";
            object[] parameters = { 999999 };
            bool exists = controller.Any(conditions, parameters);
            Assert.IsFalse(exists);
        }

        [TestMethod]
        public void Find_WithValidPrimaryKey_ShouldReturnModel()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            var name = Names();
            var surname = Surnames();
            var email = Emails(name, surname);
            var user = new TestUser { Name = name + " " + surname, Email = email, Age = 30 };
            controller.Insert(user);
            var firstUser = controller.FirstOrDefault();
            if (firstUser != null)
            {
                var result = controller.Find(firstUser.Id);
                Assert.IsNotNull(result);
                Assert.AreEqual(firstUser.Id, result.Id);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Find_WithoutPrimaryKeyName_ShouldThrowException()
        {
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users");
            controller.Find(1);
        }

        [TestMethod]
        public void Where_WithConditions_ShouldReturnFilteredResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            string conditions = "Age > @param0";
            object[] parameters = { 25 };
            var result = controller.Where(conditions, parameters);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
        }

        [TestMethod]
        public void SingleOrDefault_WithSingleMatch_ShouldReturnModel()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            var firstUser = controller.FirstOrDefault();
            if (firstUser != null)
            {
                string conditions = "Id = @param0";
                object[] parameters = { firstUser.Id };
                var result = controller.SingleOrDefault(conditions, parameters);
                Assert.IsNotNull(result);
                Assert.AreEqual(firstUser.Id, result.Id);
            }
        }

        [TestMethod]
        public void SingleOrDefault_WithNoMatch_ShouldReturnNull()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            string conditions = "Id = @param0";
            object[] parameters = { -999999 };
            var result = controller.SingleOrDefault(conditions, parameters);
            Assert.IsNull(result);
        }
    }
}
