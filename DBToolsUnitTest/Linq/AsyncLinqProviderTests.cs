using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Abstractions;
using DBTools.Controllers;
using DBTools.Core;
using DBTools.Linq;
using DBTools.Providers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DBToolsUnitTest.Linq
{
    [TestClass]
    public class AsyncLinqProviderTests : TestBase
    {
        private class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
            public string Status { get; set; }
        }

        private static AsyncSqlClient CreateSqliteClient()
        {
            var provider = new SqliteProvider();
            var connectionString = $"Data Source={Path.Combine(GetSolutionDirectory(), "testDB.db")}";
            return new AsyncSqlClient(connectionString, provider);
        }

        private static void EnsureSqliteDatabase()
        {
            var dbPath = Path.Combine(GetSolutionDirectory(), "testDB.db");
            if (!File.Exists(dbPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath) ?? ".");
                using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS users (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT,
                        email TEXT,
                        age INTEGER NOT NULL,
                        status TEXT
                    );
                    CREATE TABLE IF NOT EXISTS orders (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        userid INTEGER NOT NULL,
                        product TEXT,
                        amount REAL NOT NULL
                    );
                    INSERT OR IGNORE INTO users (id, name, email, age, status) VALUES
                        (1, 'Alice', 'alice@test.com', 25, 'active'),
                        (2, 'Bob', 'bob@test.com', 30, 'active'),
                        (3, 'Charlie', 'charlie@test.com', 16, 'inactive');
                    INSERT OR IGNORE INTO orders (userid, product, amount) VALUES
                        (1, 'Widget', 10.00),
                        (1, 'Gadget', 20.00),
                        (2, 'Service', 15.50);";
                cmd.ExecuteNonQuery();
            }
        }

        [ClassInitialize]
        public static void SetupDatabase(TestContext context)
        {
            EnsureSqliteDatabase();
        }

        [TestMethod]
        public void AsyncLinqHelper_AsAsyncQueryable_SkipTake_ReturnsResults()
        {
            EnsureSqliteDatabase();
            var client = CreateSqliteClient();
            var helper = new AsyncLinqHelper<TestEntity>(client, "users", "id", true);

            var results = helper.AsAsyncQueryable()
                .Skip(1)
                .Take(2)
                .ToList();

            Assert.IsNotNull(results);
        }

        [TestMethod]
        public void AsyncLinqHelper_AsAsyncQueryable_OrderBy_ReturnsOrderedResults()
        {
            EnsureSqliteDatabase();
            var client = CreateSqliteClient();
            var helper = new AsyncLinqHelper<TestEntity>(client, "users", "id", true);

            var results = helper.AsAsyncQueryable()
                .OrderBy(e => e.Age)
                .ToList();

            Assert.IsNotNull(results);
            Assert.IsTrue(results.Count > 0);
            Assert.AreEqual(16, results[0].Age);
        }

        [TestMethod]
        public void AsyncLinqHelper_AsAsyncQueryable_OrderByDescending_ReturnsOrderedResults()
        {
            EnsureSqliteDatabase();
            var client = CreateSqliteClient();
            var helper = new AsyncLinqHelper<TestEntity>(client, "users", "id", true);

            var results = helper.AsAsyncQueryable()
                .OrderByDescending(e => e.Age)
                .ToList();

            Assert.IsNotNull(results);
            Assert.IsTrue(results.Count > 0);
            Assert.AreEqual(30, results[0].Age);
        }

        [TestMethod]
        public void AsyncLinqHelper_AsAsyncQueryable_Where_ReturnsFilteredResults()
        {
            EnsureSqliteDatabase();
            var client = CreateSqliteClient();
            var helper = new AsyncLinqHelper<TestEntity>(client, "users", "id", true);

            var results = helper.AsAsyncQueryable()
                .Where(e => e.Status == "active")
                .ToList();

            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);
        }

        [TestMethod]
        public void AsyncLinqHelper_AsAsyncQueryable_SkipOnly_ReturnsSkippedResults()
        {
            EnsureSqliteDatabase();
            var client = CreateSqliteClient();
            var helper = new AsyncLinqHelper<TestEntity>(client, "users", "id", true);

            var results = helper.AsAsyncQueryable()
                .Skip(2)
                .ToList();

            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count);
        }

        [TestMethod]
        public void AsyncLinqHelper_AsAsyncQueryable_TakeOnly_ReturnsLimitedResults()
        {
            EnsureSqliteDatabase();
            var client = CreateSqliteClient();
            var helper = new AsyncLinqHelper<TestEntity>(client, "users", "id", true);

            var results = helper.AsAsyncQueryable()
                .Take(2)
                .ToList();

            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);
        }

        [TestMethod]
        public void AsyncLinqHelper_AsAsyncQueryable_Where_Skip_Take_CombinesCorrectly()
        {
            EnsureSqliteDatabase();
            var client = CreateSqliteClient();
            var helper = new AsyncLinqHelper<TestEntity>(client, "users", "id", true);

            var results = helper.AsAsyncQueryable()
                .Where(e => e.Age > 15)
                .Skip(1)
                .Take(1)
                .ToList();

            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(30, results[0].Age);
        }

        [TestMethod]
        public void AsyncLinqHelper_AsAsyncQueryable_ThenBy_ReturnsSecondarySort()
        {
            EnsureSqliteDatabase();
            var client = CreateSqliteClient();
            var helper = new AsyncLinqHelper<TestEntity>(client, "users", "id", true);

            var results = helper.AsAsyncQueryable()
                .OrderBy(e => e.Status)
                .ThenBy(e => e.Age)
                .ToList();

            Assert.IsNotNull(results);
            Assert.IsTrue(results.Count > 0);
        }

        [TestMethod]
        public async Task AsyncLinqHelper_BeginTransactionAsync_DefaultIsolation_Works()
        {
            EnsureSqliteDatabase();
            var client = CreateSqliteClient();
            var helper = new AsyncLinqHelper<TestEntity>(client, "users", "id", true);

            var tx = await helper.BeginTransactionAsync();
            Assert.IsNotNull(tx);
            Assert.IsTrue(tx.IsActive);

            await tx.RollbackAsync();
            Assert.IsFalse(tx.IsActive);
        }

        [TestMethod]
        public async Task AsyncLinqHelper_BeginTransactionAsync_SerializableIsolation_Works()
        {
            EnsureSqliteDatabase();
            var client = CreateSqliteClient();
            var helper = new AsyncLinqHelper<TestEntity>(client, "users", "id", true);

            var tx = await helper.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            Assert.IsNotNull(tx);
            Assert.IsTrue(tx.IsActive);

            await tx.RollbackAsync();
            Assert.IsFalse(tx.IsActive);
        }

        [TestMethod]
        public async Task AsyncLinqHelper_CountAsync_ReturnsCorrectCount()
        {
            EnsureSqliteDatabase();
            var client = CreateSqliteClient();
            var helper = new AsyncLinqHelper<TestEntity>(client, "users", "id", true);

            var count = await helper.CountAsync();
            Assert.AreEqual(3, count);
        }

        [TestMethod]
        public async Task AsyncLinqHelper_AnyAsync_ReturnsCorrectResult()
        {
            EnsureSqliteDatabase();
            var client = CreateSqliteClient();
            var helper = new AsyncLinqHelper<TestEntity>(client, "users", "id", true);

            var anyActive = await helper.AnyAsync(e => e.Status == "active");
            var anyInactive = await helper.AnyAsync(e => e.Status == "inactive");

            Assert.IsTrue(anyActive);
            Assert.IsTrue(anyInactive);
        }

        [TestMethod]
        public async Task AsyncLinqHelper_WhereAsync_ReturnsMatchingResults()
        {
            EnsureSqliteDatabase();
            var client = CreateSqliteClient();
            var helper = new AsyncLinqHelper<TestEntity>(client, "users", "id", true);

            var results = await helper.WhereAsync(e => e.Status == "active");
            Assert.AreEqual(2, results.Count);
        }

        [TestMethod]
        public async Task AsyncLinqHelper_FirstOrDefaultAsync_ReturnsFirstMatch()
        {
            EnsureSqliteDatabase();
            var client = CreateSqliteClient();
            var helper = new AsyncLinqHelper<TestEntity>(client, "users", "id", true);

            var result = await helper.FirstOrDefaultAsync(e => e.Age > 20);
            Assert.IsNotNull(result);
            Assert.AreEqual(25, result.Age);
        }

        [TestMethod]
        public void AsyncDbQuery_ToString_ReturnsSql()
        {
            EnsureSqliteDatabase();
            var provider = new SqliteProvider();
            var client = CreateSqliteClient();
            var asyncProvider = new AsyncDbQueryProvider<TestEntity>(client, provider, "users", "id");
            var query = new AsyncDbQuery<TestEntity>(asyncProvider, "users");

            var sql = query.ToString();
            Assert.IsNotNull(sql);
        }

        [TestMethod]
        public async Task AsyncLinqHelper_InsertAndFind_RoundTrips()
        {
            EnsureSqliteDatabase();
            var client = CreateSqliteClient();
            var helper = new AsyncLinqHelper<TestEntity>(client, "users", "id", true);

            var newEntity = new TestEntity { Name = "Test User", Age = 99, Status = "active" };
            var inserted = await helper.InsertAsync(newEntity);
            Assert.IsTrue(inserted);

            var found = await helper.FirstOrDefaultAsync(e => e.Name == "Test User");
            Assert.IsNotNull(found);
            Assert.AreEqual(99, found.Age);

            await helper.RemoveAsync(e => e.Id == found.Id);
        }

        [TestMethod]
        public void AsyncLinqHelper_AsAsyncQueryable_FirstOrDefault_ReturnsFirst()
        {
            EnsureSqliteDatabase();
            var client = CreateSqliteClient();
            var helper = new AsyncLinqHelper<TestEntity>(client, "users", "id", true);

            var result = helper.AsAsyncQueryable().FirstOrDefault();

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void AsyncLinqHelper_AsAsyncQueryable_Count_ReturnsCount()
        {
            EnsureSqliteDatabase();
            var client = CreateSqliteClient();
            var helper = new AsyncLinqHelper<TestEntity>(client, "users", "id", true);

            var count = helper.AsAsyncQueryable().Count();

            Assert.AreEqual(3, count);
        }

        [TestMethod]
        public void AsyncLinqHelper_AsAsyncQueryable_Any_ReturnsBool()
        {
            EnsureSqliteDatabase();
            var client = CreateSqliteClient();
            var helper = new AsyncLinqHelper<TestEntity>(client, "users", "id", true);

            var any = helper.AsAsyncQueryable().Any();

            Assert.IsTrue(any);
        }
    }
}
