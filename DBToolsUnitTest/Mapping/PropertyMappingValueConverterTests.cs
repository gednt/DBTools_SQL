using DBTools.Controllers;
using DBTools.Core;
using DBTools.Linq;
using DBTools.Mapping;
using DBTools.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace DBToolsUnitTest.Mapping
{
    /// <summary>
    /// Tests for <see cref="PropertyMapping.ValueConverter"/> — the per-property
    /// read converter that handles value-object hydration (e.g. <c>string</c> →
    /// sealed <c>Currency</c> type with private constructor).
    /// </summary>
    [TestClass]
    public class PropertyMappingValueConverterTests
    {
        /// <summary>
        /// Sealed value object with a private parameterless constructor and a
        /// <see cref="Create(string)"/> factory. Mirrors Allied's <c>Currency</c>.
        /// </summary>
        public sealed class Currency
        {
            public string Code { get; }

            private Currency()
            {
                // Required for DBTools's reflection-based fallback path.
                Code = string.Empty;
            }

            public Currency(string code)
            {
                if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Currency code cannot be empty.", nameof(code));
                if (code.Length != 3) throw new ArgumentException("Currency code must be 3 letters.", nameof(code));
                Code = code.ToUpperInvariant();
            }

            public static Currency Create(string code) => new Currency(code);

            public override bool Equals(object obj) => obj is Currency c && c.Code == Code;
            public override int GetHashCode() => Code.GetHashCode();
        }

        public class Account
        {
            public int Id { get; set; }
            public string OwnerName { get; set; }
            public Currency Currency { get; set; }
            public decimal Balance { get; set; }
        }

        public sealed class AccountConfiguration : EntityTypeConfiguration<Account>
        {
            public override void Configure(EntityBuilder<Account> builder)
            {
                builder.ToTable("accounts_currency_test");
                builder.HasKey(a => a.Id);
                builder.Property(a => a.Id).HasColumnName("id");
                builder.Property(a => a.OwnerName).HasColumnName("owner_name");
                builder.Property(a => a.Currency)
                    .HasColumnName("currency")
                    .HasConversion(
                        writeConversion: (Currency c) => c.Code,
                        readConversion: (string raw) => Currency.Create(raw));
                builder.Property(a => a.Balance).HasColumnName("balance");
            }
        }

        private static string _sqliteDbPath;

        [ClassInitialize]
        public static void Setup(TestContext _)
        {
            EntityMappingResolver.ClearCache();
            _sqliteDbPath = Path.Combine(
                Path.GetTempPath(),
                $"dbtools_currency_test_{Guid.NewGuid():N}.db");
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_sqliteDbPath}");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS accounts_currency_test (
                    id INTEGER PRIMARY KEY,
                    owner_name TEXT,
                    currency TEXT,
                    balance REAL
                );
                INSERT OR REPLACE INTO accounts_currency_test (id, owner_name, currency, balance) VALUES
                    (1, 'Alice', 'USD', 100.50),
                    (2, 'Bob', 'EUR', 200.00),
                    (3, 'Charlie', 'BRL', 0.00);";
            cmd.ExecuteNonQuery();
        }

        [ClassCleanup]
        public static void Teardown()
        {
            EntityMappingResolver.ClearCache();
            if (_sqliteDbPath != null && File.Exists(_sqliteDbPath))
            {
                try { File.Delete(_sqliteDbPath); } catch { /* best-effort */ }
            }
        }

        [TestInitialize]
        public void Init()
        {
            EntityMappingResolver.Register<Account>(new AccountConfiguration());
        }

        [TestMethod]
        public void PropertyMapping_ValueConverter_IsNullByDefault()
        {
            var mapping = new PropertyMapping { PropertyName = "OwnerName" };
            Assert.IsNull(mapping.ValueConverter);
        }

        [TestMethod]
        public void PropertyMapping_ValueConverter_CanBeAssigned()
        {
            var mapping = new PropertyMapping { PropertyName = "Currency" };
            mapping.ValueConverter = raw => Currency.Create((string)raw);

            Assert.IsNotNull(mapping.ValueConverter);
        }

        [TestMethod]
        public void HasConversion_OnPropertyBuilder_FlowsIntoPropertyMapping()
        {
            var mapping = EntityMappingResolver.Resolve<Account>();
            var currencyMapping = mapping.Properties.First(p => p.PropertyName == "Currency");
            Assert.IsNotNull(currencyMapping.ValueConverter,
                "HasConversion's read converter should populate PropertyMapping.ValueConverter.");
        }

        [TestMethod]
        public void HasConversion_OnPropertyBuilder_LeavesUnrelatedPropertiesAlone()
        {
            var mapping = EntityMappingResolver.Resolve<Account>();
            var ownerMapping = mapping.Properties.First(p => p.PropertyName == "OwnerName");
            Assert.IsNull(ownerMapping.ValueConverter,
                "Properties without HasConversion should not get a ValueConverter.");
        }

        [TestMethod]
        public void Hydrate_UsesValueConverter_ForValueObjectProperty()
        {
            var client = new AsyncSqlClient($"Data Source={_sqliteDbPath}", new SqliteProvider());
            try
            {
                var helper = new AsyncLinqHelper<Account>(client, "accounts_currency_test", "id", false);

                var results = helper.SelectAsync("id = @param0", new object[] { 1 }).GetAwaiter().GetResult().ToList();

                Assert.AreEqual(1, results.Count);
                Assert.AreEqual("Alice", results[0].OwnerName);
                Assert.IsNotNull(results[0].Currency);
                Assert.AreEqual("USD", results[0].Currency.Code);
                Assert.AreEqual(100.50m, results[0].Balance);
            }
            finally
            {
                client.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        public void Hydrate_PrimitiveConversionFallback_WorksWithoutConverter()
        {
            // Sanity check: properties without ValueConverter still hydrate via Convert.ChangeType
            // (preserves existing behavior). The DB stores balance as REAL/double, the property
            // is decimal — ChangeType handles it.
            var client = new AsyncSqlClient($"Data Source={_sqliteDbPath}", new SqliteProvider());
            try
            {
                var helper = new AsyncLinqHelper<Account>(client, "accounts_currency_test", "id", false);
                var results = helper.SelectAsync("id = @param0", new object[] { 2 }).GetAwaiter().GetResult().ToList();

                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(200.00m, results[0].Balance);
            }
            finally
            {
                client.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        public void Hydrate_NullColumn_SkipsValueConverter()
        {
            // Make sure null values bypass the converter (no NullReferenceException).
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_sqliteDbPath}");
            connection.Open();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "INSERT OR REPLACE INTO accounts_currency_test (id, owner_name, currency, balance) VALUES (99, 'Dave', NULL, 0.0)";
                cmd.ExecuteNonQuery();
            }

            var client = new AsyncSqlClient($"Data Source={_sqliteDbPath}", new SqliteProvider());
            try
            {
                var helper = new AsyncLinqHelper<Account>(client, "accounts_currency_test", "id", false);
                var results = helper.SelectAsync("id = @param0", new object[] { 99 }).GetAwaiter().GetResult().ToList();

                Assert.AreEqual(1, results.Count);
                Assert.AreEqual("Dave", results[0].OwnerName);
                Assert.IsNull(results[0].Currency, "NULL currency column should leave property null without invoking converter.");
            }
            finally
            {
                client.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        public void WriteConverter_AppliedOnInsertAsync()
        {
            // Verifies ExtractFieldsAndValues applies WriteConverter when present.
            // We insert a fresh row via InsertAsync with a Currency value object — the
            // WriteConverter (Currency → string code) should fire, so the DB stores 'GBP'.
            // Then we read it back to verify round-trip behavior.
            var client = new AsyncSqlClient($"Data Source={_sqliteDbPath}", new SqliteProvider());
            try
            {
                var helper = new AsyncLinqHelper<Account>(client, "accounts_currency_test", "id", false);

                var account = new Account
                {
                    Id = 50,
                    OwnerName = "WriteTest",
                    Currency = Currency.Create("GBP"),
                    Balance = 999.99m,
                };

                bool inserted = helper.InsertAsync(account).GetAwaiter().GetResult();
                Assert.IsTrue(inserted, "InsertAsync should succeed when WriteConverter is configured.");

                var roundtrip = helper.SelectAsync("id = @param0", new object[] { 50 })
                    .GetAwaiter().GetResult().ToList();
                Assert.AreEqual(1, roundtrip.Count);
                Assert.AreEqual("WriteTest", roundtrip[0].OwnerName);
                Assert.AreEqual("GBP", roundtrip[0].Currency.Code);
                Assert.AreEqual(999.99m, roundtrip[0].Balance);
            }
            finally
            {
                client.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        public void WriteConverter_FlowsFromPropertyBuilder_ToPropertyMapping()
        {
            var mapping = EntityMappingResolver.Resolve<Account>();
            var currencyMapping = mapping.Properties.First(p => p.PropertyName == "Currency");
            Assert.IsNotNull(currencyMapping.WriteConverter,
                "HasConversion's write converter should populate PropertyMapping.WriteConverter.");
        }

        [TestMethod]
        public void PropertyMapping_WriteConverter_IsNullByDefault()
        {
            var mapping = new PropertyMapping { PropertyName = "Other" };
            Assert.IsNull(mapping.WriteConverter);
        }
    }
}