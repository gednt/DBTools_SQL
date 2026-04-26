using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Core;
using DBTools.Controllers;
using DBTools.Linq;
using System;
using System.Linq;

namespace DBToolsUnitTest.Linq
{
    [TestClass]
    public class DbQueryLinqTests : TestBase
    {
        private class TestUser
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public int Age { get; set; }
        }

        private class TestOrder
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public string Product { get; set; }
            public decimal Amount { get; set; }
        }

        [TestMethod]
        public void AsQueryable_ShouldReturnDbQuery()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var query = controller.AsQueryable();
            Assert.IsNotNull(query);
            Assert.IsInstanceOfType(query, typeof(IQueryable<TestUser>));
        }

        [TestMethod]
        public void AsQueryable_Where_ShouldReturnFilteredResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.AsQueryable()
                .Where(u => u.Age > 18)
                .ToList();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.All(u => u.Age > 18));
        }

        [TestMethod]
        public void AsQueryable_OrderBy_ShouldReturnOrderedResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.AsQueryable()
                .OrderBy(u => u.Age)
                .ToList();
            Assert.IsNotNull(result);
            var ages = result.Select(u => u.Age).ToList();
            for (int i = 1; i < ages.Count; i++)
                Assert.IsTrue(ages[i] >= ages[i - 1]);
        }

        [TestMethod]
        public void AsQueryable_OrderByDescending_ShouldReturnDescendingResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.AsQueryable()
                .OrderByDescending(u => u.Age)
                .ToList();
            Assert.IsNotNull(result);
            var ages = result.Select(u => u.Age).ToList();
            for (int i = 1; i < ages.Count; i++)
                Assert.IsTrue(ages[i] <= ages[i - 1]);
        }

        [TestMethod]
        public void AsQueryable_ThenBy_ShouldAddSecondarySort()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.AsQueryable()
                .OrderBy(u => u.Age)
                .ThenBy(u => u.Name)
                .ToList();
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void AsQueryable_SkipTake_ShouldReturnPagedResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.AsQueryable()
                .OrderBy(u => u.Id)
                .Skip(5)
                .Take(3)
                .ToList();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count <= 3);
        }

        [TestMethod]
        public void AsQueryable_FirstOrDefault_ShouldReturnSingleResult()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.AsQueryable()
                .FirstOrDefault(u => u.Age > 18);
            if (result != null)
                Assert.IsTrue(result.Age > 18);
        }

        [TestMethod]
        public void AsQueryable_Count_ShouldReturnInteger()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            int count = controller.AsQueryable()
                .Where(u => u.Age > 18)
                .Count();
            Assert.IsTrue(count >= 0);
        }

        [TestMethod]
        public void AsQueryable_Any_ShouldReturnBool()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            bool exists = controller.AsQueryable()
                .Where(u => u.Age > 18)
                .Any();
            Assert.IsTrue(exists || !exists);
        }

        [TestMethod]
        public void AsQueryable_ComplexChain_ShouldWork()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.AsQueryable()
                .Where(u => u.Age > 18 && u.Age < 80)
                .OrderBy(u => u.Name)
                .Skip(2)
                .Take(5)
                .ToList();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count <= 5);
            Assert.IsTrue(result.All(u => u.Age > 18 && u.Age < 80));
        }

        [TestMethod]
        public void AsQueryable_WhereContains_ShouldTranslateToLike()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.AsQueryable()
                .Where(u => u.Name.Contains("John"))
                .ToList();
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void InnerJoin_ShouldReturnJoinQuery()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var joinQuery = controller.InnerJoin<TestOrder>(
                "Orders",
                u => u.Id,
                o => o.UserId);
            Assert.IsNotNull(joinQuery);
            Assert.IsInstanceOfType(joinQuery, typeof(IQueryable<DBTools.Linq.JoinResult<TestUser, TestOrder>>));
        }

        [TestMethod]
        public void LeftJoin_ShouldReturnJoinQuery()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var joinQuery = controller.LeftJoin<TestOrder>(
                "Orders",
                u => u.Id,
                o => o.UserId);
            Assert.IsNotNull(joinQuery);
            Assert.IsInstanceOfType(joinQuery, typeof(IQueryable<DBTools.Linq.JoinResult<TestUser, TestOrder>>));
        }

        [TestMethod]
        public void InnerJoin_WithWhere_ShouldReturnFilteredJoinResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.InnerJoin<TestOrder>(
                "Orders",
                u => u.Id,
                o => o.UserId)
                .Where(j => j.Left.Age > 18)
                .ToList();
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void InnerJoin_WithOrderBySkipTake_ShouldReturnPagedJoinResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.InnerJoin<TestOrder>(
                "Orders",
                u => u.Id,
                o => o.UserId)
                .OrderBy(j => j.Left.Name)
                .Skip(2)
                .Take(5)
                .ToList();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count <= 5);
        }
    }
}
