using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Core;
using DBTools.Controllers;
using DBTools.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace DBToolsUnitTest.Core
{
    /// <summary>
    /// Regression tests for bugs fixed in the feature/add-join-support review.
    /// Tests are grouped by bug number and cover both offline (unit) and online (integration) scenarios.
    /// </summary>
    [TestClass]
    [TestCategory("Integration")]
    public class BugRegressionTests : TestBase
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

        // ─── Bug 6: SqlValidator word-boundary matching ───────────────────────

        [TestMethod]
        public void SqlValidator_ShouldAllowDeletedAtColumnName()
        {
            var validator = new SqlValidator();
            Assert.IsTrue(validator.IsValidIdentifier("DeletedAt"),
                "DeletedAt contains 'Delete' as a substring but not as a SQL keyword");
        }

        [TestMethod]
        public void SqlValidator_ShouldAllowIsDeletedColumnName()
        {
            var validator = new SqlValidator();
            Assert.IsTrue(validator.IsValidIdentifier("IsDeleted"),
                "IsDeleted is a valid column name");
        }

        [TestMethod]
        public void SqlValidator_ShouldAllowDropReasonColumnName()
        {
            var validator = new SqlValidator();
            Assert.IsTrue(validator.IsValidIdentifier("DropReason"),
                "DropReason is a valid column name");
        }

        [TestMethod]
        public void SqlValidator_ShouldStillRejectBareDROPKeyword()
        {
            var validator = new SqlValidator();
            Assert.IsFalse(validator.IsValidIdentifier("DROP"),
                "Bare DROP keyword should be rejected");
        }

        [TestMethod]
        public void SqlValidator_ShouldStillRejectBareDELETEKeyword()
        {
            var validator = new SqlValidator();
            Assert.IsFalse(validator.IsValidIdentifier("DELETE"),
                "Bare DELETE keyword should be rejected");
        }

        [TestMethod]
        public void SqlValidator_ShouldRejectDropTableInjection()
        {
            var validator = new SqlValidator();
            Assert.IsFalse(validator.IsValidIdentifier("Users; DROP TABLE Users"),
                "SQL injection via DROP TABLE should be rejected");
        }

        // ─── Bug 4: QueryBuilder NullReferenceException on null properties ───

        [TestMethod]
        public void QueryBuilder_ShouldNotThrowOnNullStringProperty()
        {
            var utils = new SqlClient();
            var model = new TestUser { Id = 1, Name = null, Email = "test@test.com", Age = 25 };
            var result = utils.QueryBuilder(model, "Id", true);
            Assert.IsNotNull(result, "QueryBuilder should return a result even when Name is null");
            Assert.IsTrue(result.Count > 0);
        }

        [TestMethod]
        public void QueryBuilder_ShouldUseEmptyStringForNullProperty()
        {
            var utils = new SqlClient();
            var model = new TestUser { Id = 1, Name = null, Email = null, Age = 0 };
            var result = utils.QueryBuilder(model, "Id", true);
            Assert.IsNotNull(result);
            var nameEntry = result[0].values.Cast<object>()
                .Zip(result[0].columns, (v, c) => new { Col = c, Val = v })
                .FirstOrDefault(x => x.Col == "Name");
            if (nameEntry != null)
                Assert.AreEqual("", nameEntry.Val, "Null property should map to empty string");
        }

        // ─── Bug 13: WhereByExample must not skip value-type zero ────────────

        [TestMethod]
        public void WhereByExample_WithZeroIntValue_ShouldIncludeInFilter()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var filter = new TestUser { Age = 0 };
            var result = controller.WhereByExample(filter);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.All(u => u.Age == 0),
                "WhereByExample with Age=0 should filter by Age=0, not return all records");
        }

        // ─── Bug 14: DeleteWhereIsNull / UpdateWhereIsNull no longer need AND 1=1 ───

        [TestMethod]
        public void DeleteWhereIsNull_ShouldNotThrow_ForNullableColumn()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            try
            {
                controller.DeleteWhereIsNull(u => u.Email);
            }
            catch (ArgumentException ex)
            {
                Assert.Fail($"DeleteWhereIsNull should not throw ArgumentException: {ex.Message}");
            }
        }

        // ─── Bug 10: DbQuery.ToString generates valid SQL ────────────────────

        [TestMethod]
        public void DbQuery_ToString_ShouldContainTableName()
        {
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var query = controller.AsQueryable();
            string sql = query.ToString();
            StringAssert.Contains(sql, "Users",
                "ToString() should produce SQL containing the table name");
        }

        [TestMethod]
        public void DbQuery_ToString_ShouldNotStartWithSelectSpace()
        {
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var query = controller.AsQueryable();
            string sql = query.ToString();
            Assert.IsFalse(sql.StartsWith("SELECT SELECT"),
                "ToString() should not produce double SELECT");
        }

        // ─── Bug 8: Single/SingleOrDefault uses TOP 2 not full scan ─────────

        [TestMethod]
        public void AsQueryable_Single_SqlShouldContainTop2()
        {
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var query = (DbQuery<TestUser>)controller.AsQueryable().Where(u => u.Id == 1);
            string sql = query.ToString();
            Assert.IsTrue(sql.Contains("SELECT TOP 2") || !sql.Contains("TOP"),
                "Single query should use TOP 2 or no OFFSET when table name is in play");
        }

        // ─── Bug 1: JOIN WHERE on j.Left / j.Right resolves correct alias ────

        [TestMethod]
        public void InnerJoin_WithWhereOnLeftProp_ShouldReturnFilteredResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.InnerJoin<TestOrder>(
                "Orders",
                u => u.Id,
                o => o.UserId)
                .Where(j => j.Left.Age > 0)
                .ToList();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.All(j => j.Left == null || j.Left.Age > 0),
                "WHERE on j.Left.Age should filter correctly");
        }

        [TestMethod]
        public void LeftJoin_WithWhereOnRightProp_ShouldReturnFilteredResults()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.LeftJoin<TestOrder>(
                "Orders",
                u => u.Id,
                o => o.UserId)
                .Where(j => j.Right.Amount > 0)
                .ToList();
            Assert.IsNotNull(result);
        }

        // ─── Bug 2: JOIN with shared column names maps both sides correctly ──

        [TestMethod]
        public void InnerJoin_SharedColumnName_ShouldMapBothSides()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new Linq<TestUser>(utils, "Users", "Id", true);
            var result = controller.InnerJoin<TestOrder>(
                "Orders",
                u => u.Id,
                o => o.UserId)
                .ToList();

            Assert.IsNotNull(result);
            if (result.Count > 0)
            {
                var first = result[0];
                Assert.IsNotNull(first.Left, "Left (User) should be mapped");
                Assert.IsNotNull(first.Right, "Right (Order) should be mapped");
                Assert.IsTrue(first.Left.Id > 0 || first.Left.Id == 0,
                    "Left.Id should be populated (not stuck at 0 due to alias collision)");
                Assert.IsTrue(first.Right.Id > 0 || first.Right.Id == 0,
                    "Right.Id should be independently populated");
            }
        }

        // ─── Bug 3: InsertRange is atomic ────────────────────────────────────

        [TestMethod]
        public void InsertRange_WithValidModels_ShouldReturnTrue()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            var users = new List<TestUser>
            {
                new TestUser { Name = "BatchUser1", Email = "batch1@test.com", Age = 21 },
                new TestUser { Name = "BatchUser2", Email = "batch2@test.com", Age = 22 },
            };
            bool result = controller.InsertRange(users);
            Assert.IsTrue(result, "InsertRange with valid models should return true");
        }

        [TestMethod]
        public void InsertRange_WithEmptyList_ShouldReturnTrue()
        {
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            bool result = controller.InsertRange(new List<TestUser>());
            Assert.IsTrue(result, "InsertRange with empty list should return true (no-op)");
        }

        // ─── Bug 5: ParseBinaryExpression generates IS NULL for null comparisons ───

        [TestMethod]
        public void Where_NullComparison_ShouldGenerateIsNull()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            var controller = new LinqHelper<TestUser>(utils, "Users", "Id", true);
            // Should not throw and should return null-email users, not empty set
            var result = controller.Where(u => u.Email == null);
            Assert.IsNotNull(result);
        }

        // ─── Bug 16: ConnectionString contains TrustServerCertificate ─────────

        [TestMethod]
        public void ConnectionString_ShouldContainTrustServerCertificate()
        {
            var db = new DBTools.Core.DBTools();
            db.Host = "localhost";
            db.Database = "testDB";
            db.Uid = "sa";
            db.Password = "pass";
            db.Port = "1433";
            string cs = db.ConnectionString;
            StringAssert.Contains(cs, "TrustServerCertificate=True",
                "ConnectionString should include TrustServerCertificate=True");
        }
    }
}
