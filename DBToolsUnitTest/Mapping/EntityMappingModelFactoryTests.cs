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
    /// Tests for <see cref="EntityMapping.ModelFactory"/> — the per-entity factory
    /// that hydrates entities from an <see cref="IDataRecord"/> directly, bypassing
    /// <c>new TModel()</c>. This is the supported path for entities with private
    /// parameterless constructors.
    /// </summary>
    [TestClass]
    public class EntityMappingModelFactoryTests
    {
        /// <summary>
        /// Entity with a private parameterless constructor and factory-only
        /// construction. Mirrors Allied's <c>Wallet</c> shape.
        /// </summary>
        public sealed class PrivateCtorEntity
        {
            public int Id { get; private set; }
            public string Name { get; private set; }
            public decimal Balance { get; private set; }

            private PrivateCtorEntity()
            {
                Name = string.Empty;
            }

            public static PrivateCtorEntity Hydrate(IDataRecord record)
            {
                if (record == null) throw new ArgumentNullException(nameof(record));
                return new PrivateCtorEntity
                {
                    Id = Convert.ToInt32(record["id"]),
                    Name = record["name"] == DBNull.Value ? null : Convert.ToString(record["name"]),
                    Balance = record["balance"] == DBNull.Value ? 0m : Convert.ToDecimal(record["balance"]),
                };
            }
        }

        public sealed class PrivateCtorEntityConfiguration : EntityTypeConfiguration<PrivateCtorEntity>
        {
            public override void Configure(EntityBuilder<PrivateCtorEntity> builder)
            {
                builder.ToTable("private_ctor_entities");
                builder.HasKey(e => e.Id);
                builder.Property(e => e.Id).HasColumnName("id");
                builder.Property(e => e.Name).HasColumnName("name");
                builder.Property(e => e.Balance).HasColumnName("balance");
                builder.HasModelFactory(record => PrivateCtorEntity.Hydrate(record));
            }
        }

        private static string _sqliteDbPath;

        [ClassInitialize]
        public static void Setup(TestContext _)
        {
            EntityMappingResolver.ClearCache();
            _sqliteDbPath = Path.Combine(
                Path.GetTempPath(),
                $"dbtools_modelfactory_test_{Guid.NewGuid():N}.db");
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_sqliteDbPath}");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS private_ctor_entities (
                    id INTEGER PRIMARY KEY,
                    name TEXT,
                    balance REAL
                );
                INSERT OR REPLACE INTO private_ctor_entities (id, name, balance) VALUES
                    (1, 'Alice', 100.50),
                    (2, 'Bob', 200.00),
                    (3, 'Charlie', 0.00);";
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
            EntityMappingResolver.Register<PrivateCtorEntity>(new PrivateCtorEntityConfiguration());
        }

        [TestMethod]
        public void EntityMapping_ModelFactory_IsNullByDefault()
        {
            var mapping = new EntityMapping();
            Assert.IsNull(mapping.ModelFactory);
        }

        [TestMethod]
        public void EntityMapping_ModelFactory_CanBeAssigned()
        {
            var mapping = new EntityMapping();
            mapping.ModelFactory = record => PrivateCtorEntity.Hydrate(record);

            Assert.IsNotNull(mapping.ModelFactory);
        }

        [TestMethod]
        public void HasModelFactory_FlowsIntoEntityMapping()
        {
            var mapping = EntityMappingResolver.Resolve<PrivateCtorEntity>();
            Assert.IsNotNull(mapping.ModelFactory,
                "HasModelFactory on EntityBuilder should populate EntityMapping.ModelFactory.");
        }

        [TestMethod]
        public void HasModelFactory_NullArgument_Throws()
        {
            var builder = new EntityBuilder<PrivateCtorEntity>();
            Assert.ThrowsException<ArgumentNullException>(
                () => builder.HasModelFactory(null));
        }

        [TestMethod]
        public void Hydrate_WithModelFactory_PrivateConstructorEntity()
        {
            // The whole point: this entity has a private parameterless constructor.
            // Without ModelFactory, AsyncLinqHelper<PrivateCtorEntity> would fail to
            // compile (no new() constraint) or fail at runtime (Activator.CreateInstance).
            var client = new AsyncSqlClient($"Data Source={_sqliteDbPath}", new SqliteProvider());
            try
            {
                var helper = new AsyncLinqHelper<PrivateCtorEntity>(client, "private_ctor_entities", "id", false);

                var results = helper.SelectAsync("id = @param0", new object[] { 1 }).GetAwaiter().GetResult().ToList();

                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(1, results[0].Id);
                Assert.AreEqual("Alice", results[0].Name);
                Assert.AreEqual(100.50m, results[0].Balance);
            }
            finally
            {
                client.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        public void Hydrate_AsAsyncQueryable_WithModelFactory()
        {
            // Exercise the deferred LINQ path (AsyncDbQueryProvider) end-to-end with
            // a ModelFactory. This verifies both the new() constraint drop and the
            // ModelFactory branch in AsyncDbQueryProvider.MapDataViewToModels.
            var client = new AsyncSqlClient($"Data Source={_sqliteDbPath}", new SqliteProvider());
            try
            {
                var helper = new AsyncLinqHelper<PrivateCtorEntity>(client, "private_ctor_entities", "id", false);

                var results = helper.AsAsyncQueryable()
                    .Where(e => e.Balance > 50m)
                    .ToList();

                Assert.AreEqual(2, results.Count);
                Assert.IsTrue(results.All(e => e.Balance > 50m));
            }
            finally
            {
                client.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        public void Hydrate_ModelFactory_CalledPerRow()
        {
            // Verify the factory is invoked once per row by counting with a custom
            // factory that wraps the real one and increments a counter.
            var callCount = 0;
            EntityMappingResolver.Register<PrivateCtorEntity>(
                new CountingEntityConfiguration(record =>
                {
                    callCount++;
                    return PrivateCtorEntity.Hydrate(record);
                }));

            var client = new AsyncSqlClient($"Data Source={_sqliteDbPath}", new SqliteProvider());
            try
            {
                var helper = new AsyncLinqHelper<PrivateCtorEntity>(client, "private_ctor_entities", "id", false);
                var results = helper.SelectAsync("", Array.Empty<object>()).GetAwaiter().GetResult().ToList();

                Assert.AreEqual(3, results.Count);
                Assert.AreEqual(3, callCount, "ModelFactory should be called once per row.");
            }
            finally
            {
                client.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }

        private sealed class CountingEntityConfiguration : EntityTypeConfiguration<PrivateCtorEntity>
        {
            private readonly Func<IDataRecord, PrivateCtorEntity> _factory;
            public CountingEntityConfiguration(Func<IDataRecord, PrivateCtorEntity> factory) { _factory = factory; }
            public override void Configure(EntityBuilder<PrivateCtorEntity> builder)
            {
                builder.ToTable("private_ctor_entities");
                builder.HasKey(e => e.Id);
                builder.Property(e => e.Id).HasColumnName("id");
                builder.Property(e => e.Name).HasColumnName("name");
                builder.Property(e => e.Balance).HasColumnName("balance");
                builder.HasModelFactory(_factory);
            }
        }

        [TestMethod]
        public void Hydrate_NoModelFactory_NoPublicCtor_ThrowsClearError()
        {
            // Entity has no public parameterless constructor and no ModelFactory registered.
            // The runtime Activator.CreateInstance path should throw InvalidOperationException
            // with a clear message rather than MissingMethodException.
            EntityMappingResolver.ClearCache();
            EntityMappingResolver.Register<PrivateCtorEntity>(new NoFactoryConfiguration());

            var client = new AsyncSqlClient($"Data Source={_sqliteDbPath}", new SqliteProvider());
            try
            {
                var helper = new AsyncLinqHelper<PrivateCtorEntity>(client, "private_ctor_entities", "id", false);

                var ex = Assert.ThrowsException<InvalidOperationException>(
                    () => helper.SelectAsync("id = @param0", new object[] { 1 }).GetAwaiter().GetResult().ToList(),
                    "Expected clear InvalidOperationException when entity has no public ctor and no ModelFactory.");

                StringAssert.Contains(ex.Message, "ModelFactory",
                    "Error message should mention ModelFactory as the recommended fix.");
            }
            finally
            {
                client.DisposeAsync().AsTask().GetAwaiter().GetResult();
                // Restore the working configuration for subsequent tests.
                EntityMappingResolver.ClearCache();
                EntityMappingResolver.Register<PrivateCtorEntity>(new PrivateCtorEntityConfiguration());
            }
        }

        private sealed class NoFactoryConfiguration : EntityTypeConfiguration<PrivateCtorEntity>
        {
            public override void Configure(EntityBuilder<PrivateCtorEntity> builder)
            {
                builder.ToTable("private_ctor_entities");
                builder.HasKey(e => e.Id);
                builder.Property(e => e.Id).HasColumnName("id");
                builder.Property(e => e.Name).HasColumnName("name");
                builder.Property(e => e.Balance).HasColumnName("balance");
                // Deliberately NO HasModelFactory.
            }
        }
    }
}