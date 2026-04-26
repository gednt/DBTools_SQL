using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Core;
using DBTools.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DBToolsUnitTest.Controllers
{
    [TestClass]
    public class LinqHelperLinqTests : TestBase
    {
        private class TestUser
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public int Age { get; set; }
        }

        [TestMethod]
        public void GetAll_ShouldReturnAllRecords()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users");
            var result = controller.GetAll();
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(List<TestUser>));
            Assert.IsTrue(result.Count > 0);
        }

        [TestMethod]
        public void AsQueryable_ShouldReturnIQueryable()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users");
            var result = controller.AsQueryable();
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IQueryable<TestUser>));
        }

        [TestMethod]
        public void Add_ShouldInsertRecord()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            var name = Names();
            var surname = Surnames();
            var user = new TestUser { Name = name + " " + surname, Email = Emails(name, surname), Age = new Random().Next(18, 80) };
            bool result = controller.Add(user);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Where_WithLambdaPredicate_ShouldReturnMatchingRecords()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users");
            var result = controller.Where(u => u.Age > 18);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(List<TestUser>));
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result.All(u => u.Age > 18));
        }

        [TestMethod]
        public void Where_WithAndCondition_ShouldReturnMatchingRecords()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users");
            var result = controller.Where(u => u.Age > 18 && u.Age < 80);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.All(u => u.Age > 18 && u.Age < 80));
        }

        [TestMethod]
        public void FirstOrDefault_WithLambdaPredicate_ShouldReturnFirstMatch()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users");
            var result = controller.FirstOrDefault(u => u.Age > 18);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Age > 18);
        }

        [TestMethod]
        public void FirstOrDefault_WithNoMatch_ShouldReturnNull()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users");
            var result = controller.FirstOrDefault(u => u.Id == -999999);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void SingleOrDefault_WithNoMatch_ShouldReturnNull()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users");
            var result = controller.SingleOrDefault(u => u.Id == -999999);
            Assert.IsNull(result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SingleOrDefault_WithMultipleMatches_ShouldThrowException()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users");
            controller.SingleOrDefault(u => u.Age > 18);
        }

        [TestMethod]
        public void Any_WithMatchingPredicate_ShouldReturnTrue()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users");
            bool result = controller.Any(u => u.Age > 18);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Any_WithNoMatchingPredicate_ShouldReturnFalse()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users");
            bool result = controller.Any(u => u.Id == -999999);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Count_WithPredicate_ShouldReturnCorrectCount()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users");
            int countAll = controller.Count();
            int countFiltered = controller.Count(u => u.Age > 18);
            Assert.IsTrue(countAll >= countFiltered);
            Assert.IsTrue(countFiltered >= 0);
        }

        [TestMethod]
        public void Remove_WithLambdaPredicate_ShouldNotThrow()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            var name = Names();
            var surname = Surnames();
            var user = new TestUser { Name = "ToDelete_" + name, Email = Emails(name, surname), Age = 99 };
            controller.Add(user);
            try
            {
                controller.Remove(u => u.Age == 99);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Remove should not throw: {ex.Message}");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Remove_WithNullPredicate_ShouldThrowException()
        {
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users");
            controller.Remove(null);
        }

        [TestMethod]
        public void Update_WithLambdaPredicate_ShouldNotThrow()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            var name = Names();
            var surname = Surnames();
            var user = new TestUser { Name = name + " " + surname, Email = Emails(name, surname), Age = 25 };
            controller.Add(user);
            var updatedUser = new TestUser { Name = "Updated " + name, Email = Emails(name, surname), Age = 26 };
            try
            {
                controller.Update(updatedUser, u => u.Age == 25);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Update with predicate should not throw: {ex.Message}");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SaveChanges_WithoutPrimaryKey_ShouldThrowException()
        {
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users");
            var user = new TestUser { Id = 1, Name = "Test", Email = "test@test.com", Age = 25 };
            controller.SaveChanges(user);
        }

        [TestMethod]
        public void SaveChanges_WithValidPrimaryKey_ShouldNotThrow()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            var name = Names();
            var surname = Surnames();
            var user = new TestUser { Name = name + " " + surname, Email = Emails(name, surname), Age = 30 };
            controller.Add(user);
            var inserted = controller.FirstOrDefault(u => u.Name == user.Name);
            Assert.IsNotNull(inserted);
            inserted.Age = 31;
            try
            {
                controller.SaveChanges(inserted);
            }
            catch (Exception ex)
            {
                Assert.Fail($"SaveChanges should not throw: {ex.Message}");
            }
        }

        [TestMethod]
        public void AsQueryable_AllowsLinqChaining()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users");
            var result = controller.AsQueryable()
                .Where(u => u.Age > 18)
                .OrderBy(u => u.Name)
                .ToList();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.All(u => u.Age > 18));
        }
    }
}
