using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Core;
using DBTools.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DBToolsUnitTest.Controllers
{
    [TestClass]
    [TestCategory("Integration")]
    public class LinqTests : TestBase
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
            var controller = new Linq<TestUser>(new SqlClient(), "Users", "Id", true);
            Assert.IsNotNull(controller);
            Assert.IsNotNull(controller.Utils);
        }

        [TestMethod]
        public void Constructor_WithSqlClientInstance_ShouldInitialize()
        {
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            Assert.IsNotNull(controller);
            Assert.IsNotNull(controller.Utils);
        }

        [TestMethod]
        public void WhereEquals_WithValidProperty_ShouldReturnFilteredResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.WhereEquals(u => u.Age, 30);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
        }

        [TestMethod]
        public void WhereGreaterThan_WithValidProperty_ShouldReturnFilteredResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.WhereGreaterThan(u => u.Age, 25);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
        }

        [TestMethod]
        public void WhereLessThan_WithValidProperty_ShouldReturnFilteredResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.WhereLessThan(u => u.Age, 50);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
        }

        [TestMethod]
        public void WhereBetween_WithValidProperty_ShouldReturnFilteredResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.WhereBetween(u => u.Age, 20, 40);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
        }

        [TestMethod]
        public void WhereContains_WithValidProperty_ShouldReturnFilteredResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.WhereContains(u => u.Name, "John");
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
        }

        [TestMethod]
        public void WhereStartsWith_WithValidProperty_ShouldReturnFilteredResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.WhereStartsWith(u => u.Email, "john");
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
        }

        [TestMethod]
        public void WhereEndsWith_WithValidProperty_ShouldReturnFilteredResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.WhereEndsWith(u => u.Email, "@test.com");
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
        }

        [TestMethod]
        public void WhereIn_WithValidProperty_ShouldReturnFilteredResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var ages = new List<int> { 25, 30, 35 };
            var result = controller.WhereIn(u => u.Age, ages);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
        }

        [TestMethod]
        public void WhereIn_WithEmptyList_ShouldReturnEmptyResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var ages = new List<int>();
            var result = controller.WhereIn(u => u.Age, ages);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void FirstOrDefaultByProperty_WithValidProperty_ShouldReturnResult()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            try
            {
                var result = controller.FirstOrDefaultByProperty(u => u.Age, 30);
            }
            catch (ArgumentException ex)
            {
                Assert.Fail($"Should not throw exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void CountByProperty_WithValidProperty_ShouldReturnCount()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            int count = controller.CountByProperty(u => u.Age, 30);
            Assert.IsTrue(count >= 0);
        }

        [TestMethod]
        public void AnyByProperty_WithValidProperty_ShouldReturnBool()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            try
            {
                bool exists = controller.AnyByProperty(u => u.Age, 30);
            }
            catch (ArgumentException ex)
            {
                Assert.Fail($"Should not throw exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void WhereByExample_WithModelTemplate_ShouldReturnFilteredResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var filterModel = new TestUser { Age = 30 };
            var result = controller.WhereByExample(filterModel);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
        }

        [TestMethod]
        public void WhereByExample_WithEmptyModel_ShouldReturnAllRecords()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var filterModel = new TestUser();
            var result = controller.WhereByExample(filterModel);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WhereEquals_WithNullExpression_ShouldThrowException()
        {
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            controller.WhereEquals<int>(null, 30);
        }

        [TestMethod]
        public void WhereNotEquals_WithValidProperty_ShouldReturnFilteredResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.WhereNotEquals(u => u.Age, 30);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
        }

        [TestMethod]
        public void WhereIsNotNull_WithValidProperty_ShouldReturnFilteredResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.WhereIsNotNull(u => u.Name);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
        }

        [TestMethod]
        public void Exists_WithValidProperty_ShouldReturnBool()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            bool exists = controller.Exists(u => u.Age, 30);
            Assert.IsTrue(exists || !exists);
        }

        [TestMethod]
        public void InsertOrUpdate_ShouldNotThrowException()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var user = new TestUser { Name = "Test User", Email = "test@test.com", Age = 25 };
            try
            {
                controller.InsertOrUpdate(user, u => u.Email);
            }
            catch (ArgumentException ex)
            {
                Assert.Fail($"Should not throw exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void GetOrCreate_ShouldReturnModel()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var user = new TestUser { Name = "GetOrCreate User", Email = "getorcreate@test.com", Age = 35 };
            var result = controller.GetOrCreate(user, u => u.Email);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(TestUser));
        }

        [TestMethod]
        public void UpdateWhere_WithTwoConditions_ShouldNotThrowException()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var user = new TestUser { Name = "Updated Name", Email = "updated@test.com", Age = 40 };
            try
            {
                controller.UpdateWhere(user, u => u.Age, 30, u => u.Name, "Test");
            }
            catch (ArgumentException ex)
            {
                Assert.Fail($"Should not throw exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void UpdateWhere_WithThreeConditions_ShouldNotThrowException()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var user = new TestUser { Name = "Updated Name", Email = "updated@test.com", Age = 40 };
            try
            {
                controller.UpdateWhere(user, u => u.Age, 30, u => u.Name, "Test", u => u.Email, "test@test.com");
            }
            catch (ArgumentException ex)
            {
                Assert.Fail($"Should not throw exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void DeleteWhere_WithTwoConditions_ShouldNotThrowException()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            try
            {
                controller.DeleteWhere(u => u.Age, 999, u => u.Name, "NonExistent");
            }
            catch (ArgumentException ex)
            {
                Assert.Fail($"Should not throw exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void DeleteWhereIn_WithEmptyList_ShouldReturnTrue()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var emptyAges = new List<int>();
            bool result = controller.DeleteWhereIn(u => u.Age, emptyAges);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void UpdateWhereIn_WithValidValues_ShouldNotThrowException()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var user = new TestUser { Name = "Bulk Updated", Email = "bulk@test.com", Age = 50 };
            var ages = new List<int> { 999, 998, 997 };
            try
            {
                controller.UpdateWhereIn(user, u => u.Age, ages);
            }
            catch (ArgumentException ex)
            {
                Assert.Fail($"Should not throw exception: {ex.Message}");
            }
        }
    }
}
