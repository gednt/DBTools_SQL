---
last_mapped_commit: f9cc45c3b85d4d7645b1a3a5deef92ed3bc9b6c7
generated: 2026-06-21
focus: quality
---

# Testing Patterns

**Analysis Date:** 2026-06-21

This document captures the testing strategy, framework, structure, and conventions used in `DBToolsUnitTest/`. The test project is a sibling .NET 8 project targeting the `DBTools` library directly via `<ProjectReference>`.

## Test Framework

### Primary Framework
- **MSTest v3** — `MSTest.TestFramework` and `MSTest.TestAdapter`, version `3.7.0`.
  Defined in `DBToolsUnitTest/DBToolsUnitTest.csproj:17-18`.
- **`Microsoft.NET.Test.Sdk`** version `17.12.0` (line 16) — the standard test SDK adapter.
- **No NUnit, no xUnit.** Every test class uses `[TestClass]` / `[TestMethod]` attributes from `Microsoft.VisualStudio.TestTools.UnitTesting`.

### Namespace & Imports
All test files share the same opening imports:
```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.<Area>;
using System;
using System.Collections.Generic;
// (other System.* / Microsoft.* as needed)
```

Test namespace follows the convention `DBToolsUnitTest.<Area>` (e.g. `DBToolsUnitTest.Core`, `DBToolsUnitTest.Controllers`, `DBToolsUnitTest.Models`, `DBToolsUnitTest.Linq`, `DBToolsUnitTest.Providers`, `DBToolsUnitTest.Export`, `DBToolsUnitTest.Integration`).

### Mocking Library
- **No mocking framework.** The codebase does not depend on Moq, NSubstitute, FakeItEasy, or similar.
- **Hand-rolled fakes** are used. The canonical example is `DBToolsUnitTest/Models/FakeSqlClient.cs:13-108` — an `internal sealed class` implementing `ISqlClient`, with throw-away `NotSupportedException` methods for unused members and recording properties (`InsertCallCount`, `LastInsert`, etc.) for the methods under test:
  ```csharp
  internal sealed class FakeSqlClient : ISqlClient
  {
      public bool Insert(string[] fields, string table, object[] values, ...)
      {
          LastInsert = new InsertCall(fields, table, values, primaryKeyName, autoIncrement);
          InsertCallCount++;
          return InsertResult;
      }
      ...
      public bool InsertResult { get; set; } = true;
      public int InsertCallCount { get; private set; }
      public InsertCall LastInsert { get; private set; }
  }
  ```
  Header comment in `FakeSqlClient.cs:11-12`: `"...Avoids pulling a mocking dependency into the test project."`

### Coverage Tool
- **`coverlet.collector`** version `6.0.2` (`DBToolsUnitTest/DBToolsUnitTest.csproj:19-22`).
- No enforced threshold or `coverage.json` / `coverage.xml` targets configured in the project.
- No `[ExcludeFromCodeCoverage]` attributes observed.

## Test Commands

Standard `dotnet test` invocations from `.github/workflows/ci.yml`:

| Scope | Command |
|-------|---------|
| All unit tests (excludes Integration) | `dotnet test DBTools.sln --no-build --configuration Release --filter "TestCategory!=Integration"` |
| All integration tests | `dotnet test DBTools.sln --no-build --configuration Release --filter "TestCategory=Integration"` |
| All tests | `dotnet test DBTools.sln --no-build --configuration Release` |

Run locally:
```bash
# Unit only — no DB required
dotnet test DBToolsUnitTest/DBToolsUnitTest.csproj --filter "TestCategory!=Integration"

# Full suite (requires SQL Server)
docker compose up -d sqlserver --wait
dotnet test DBToolsUnitTest/DBToolsUnitTest.csproj
```

## Test Project Structure

```
DBToolsUnitTest/
├── DBToolsUnitTest.csproj      # MSTest 3.7 + coverlet 6.0.2 + Testcontainers 4.4
├── TestBase.cs                  # Base class for unit tests (no DB)
├── IntegrationTestBase.cs       # Base class for integration tests (DB required)
├── TestDocumentation.md         # Human-readable test catalog (96 tests listed)
├── config.json.example          # Copy target for runtime config
├── Properties/
│   └── AssemblyInfo.cs
├── Controllers/                 # Controllers namespace tests
│   ├── DBToolsControllerTests.cs
│   ├── LinqHelperTests.cs       # [TestCategory("Integration")]
│   ├── LinqHelperLinqTests.cs   # [TestCategory("Integration")]
│   └── LinqTests.cs             # [TestCategory("Integration")]
├── Core/                        # Core namespace tests
│   ├── DBToolsTests.cs
│   ├── QueryBuilderTests.cs
│   ├── SqlClientConnectionTests.cs
│   ├── SqlClientParameterizedQueryTests.cs   # [TestCategory("Integration")]
│   ├── SqlClientQueryBuilderTests.cs
│   ├── SqlClientValidationTests.cs
│   ├── ObsoleteMethodTests.cs   # [TestCategory("Integration")]
│   └── BugRegressionTests.cs    # [TestCategory("Integration")]
├── Export/
│   └── DataExportTests.cs
├── Integration/
│   ├── EdgeCaseTests.cs         # Note: lives in /Integration/ but is not [TestCategory("Integration")]
│   └── WorkflowTests.cs         # Same — see "Test categorization drift" below
├── Linq/
│   └── DbQueryLinqTests.cs      # [TestCategory("Integration")]
├── Models/
│   ├── FakeSqlClient.cs         # Hand-rolled fake (not a test class)
│   ├── GenericObjectTests.cs
│   └── GenericObjectSimpleTests.cs
└── Providers/
    ├── DbProviderFactoryTests.cs
    └── ProviderDialectTests.cs
```

## Base Classes

### `TestBase` (no DB) — `DBToolsUnitTest/TestBase.cs`
- **Abstract class** with shared constants and test-data generators.
- Constants: `TestHost = "127.0.0.1"`, `TestDatabase = "testDB"`, `TestUid = "testUser"`, `TestPassword = "Integration!123"`, `TestPort = "1433"`.
- **Helpers:**
  - `IsDatabaseAvailable()` — opens a `SqlConnection` with 2-second timeout, returns bool. Used to skip integration tests when no DB is reachable.
  - `SkipIfDatabaseUnavailable()` — calls `Assert.Inconclusive("Database is not available. Skipping test.")`.
  - `Surnames()`, `Names()`, `Emails(name, surname)` — random test data factories (used for seed rows).
  - `ExecuteInTempDirectory(Action, string configContent = null)` — sets current dir to a temp folder, optionally writes a `config.json`, runs the action, restores the original directory in `finally`.

### `IntegrationTestBase` (DB required) — `DBToolsUnitTest/IntegrationTestBase.cs`
- Inherits `TestBase`. Adds **container lifecycle management** with three strategies:
  1. Existing database at `127.0.0.1:1433` (set `_lifecycle = ExistingDatabase`).
  2. Docker Compose (walks up to find `docker-compose.yml`, then runs `docker compose up`).
  3. Testcontainers `MsSqlBuilder` (`mcr.microsoft.com/mssql/server:2022-latest`).
- **Usage** documented in the class-level XML doc (lines 9-32):
  ```csharp
  [TestClass]
  [TestCategory("Integration")]
  public class MyIntegrationTests : IntegrationTestBase
  {
      [ClassInitialize]
      public static void ClassSetup(TestContext context) => EnsureContainersReady();

      [TestMethod]
      public void MyTest() { SkipIfDatabaseNotReady(); /* ... */ }
  }
  ```
- `SkipIfDatabaseNotReady()` calls `Assert.Inconclusive(...)` with a descriptive message when the lifecycle is `NotInitialized`.
- **Note:** Despite this rich base class, no test class currently inherits from `IntegrationTestBase`. Integration tests inherit from `TestBase` and call `SkipIfDatabaseUnavailable()` / `SkipIfDatabaseNotReady()` themselves (see Test categorization drift below).

## Test Categorization

### Categories in use
- **`[TestCategory("Integration")]`** — marks tests that require a live SQL Server.
- **No other categories** (no `Unit`, `Regression`, `Smoke`, etc.).

### Category-driven CI split (`.github/workflows/ci.yml`)
1. **`unit-tests` job** — runs all tests except Integration: `--filter "TestCategory!=Integration"`.
2. **`integration-tests` job** — runs only Integration tests after starting SQL Server via Docker Compose: `--filter "TestCategory=Integration"`.
3. **`all-tests` job** — runs everything on tag pushes / `main` branch.

### Applying the attribute
Tests are decorated at the **class** level — every method inside a class inherits the category. Example from `DBToolsUnitTest/Core/BugRegressionTests.cs:17-18`:
```csharp
[TestClass]
[TestCategory("Integration")]
public class BugRegressionTests : TestBase
```

**No method-level `[TestCategory]` overrides** are used in the codebase.

## Test Structure & Conventions

### Class anatomy
```csharp
[TestClass]
public class SqlClientValidationTests : TestBase
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Insert_WithInvalidTableName_ShouldThrowException()
    {
        var utils = new SqlClient();
        string[] fields = { "name", "email" };
        string[] values = { "John", "john@test.com" };
        utils.Insert(fields, "Users'; DROP TABLE Users--", values);
    }
}
```

### Naming convention
- **Method names use `Method_Scenario_ExpectedBehavior` (or `Subject_ShouldBehavior`):**
  - `Constructor_ShouldSetDefaultPort` (`DBToolsUnitTest/Core/DBToolsTests.cs:15`)
  - `Insert_WithInvalidTableName_ShouldThrowException` (`DBToolsUnitTest/Core/SqlClientValidationTests.cs:12`)
  - `SqlValidator_ShouldAllowDeletedAtColumnName` (`DBToolsUnitTest/Core/BugRegressionTests.cs:39`)
  - `ToCsv_WithSpecialCharacters_ShouldEscapeCorrectly` (`DBToolsUnitTest/Integration/EdgeCaseTests.cs:23`)
  - `AsQueryable_Where_ShouldReturnFilteredResults` (`DBToolsUnitTest/Linq/DbQueryLinqTests.cs:42`)
- PascalCase, underscores separating segments.

### Inner test models
Tests that need a POCO define `private class TestModel` (or `TestUser`, `TestOrder`) at the top of the class:
```csharp
private class TestUser
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
}
```
This pattern appears in `BugRegressionTests.cs:20-26`, `DbQueryLinqTests.cs:14-20`, `LinqTests.cs:14-20`, `LinqHelperTests.cs:14-20`, `LinqHelperLinqTests.cs:14-20`.

### Test regions
Files are sometimes organized with `#region` blocks. Example from `DBToolsUnitTest/Providers/DbProviderFactoryTests.cs:11-148`:
```csharp
#region Create from DatabaseProvider enum
...
#endregion

#region Create from string
...
#endregion

#region Verify factory returns correct provider names
...
#endregion
```

## Assertion Patterns

### MSTest classic assertions
- **`Assert.AreEqual(expected, actual)`** — used heavily, including positional comparisons and `Assert.AreEqual(string, actual, msg)`.
- **`Assert.IsTrue(condition)`** and **`Assert.IsFalse(condition)`** — for booleans.
- **`Assert.IsNotNull(obj)`** / **`Assert.IsNull(obj)`** — null checks.
- **`Assert.IsInstanceOfType(obj, typeof(T))`** — type checks (e.g. `Assert.IsInstanceOfType(provider, typeof(SqlServerProvider));`).
- **`Assert.ThrowsException<T>(action)`** — preferred for new code. Example from `DBToolsUnitTest/Models/GenericObjectTests.cs:30`:
  ```csharp
  Assert.ThrowsException<ArgumentNullException>(() => new GenericObject(null));
  ```
- **`Assert.ThrowsException<T>(action)` returning the exception** for message assertions:
  ```csharp
  var ex = Assert.ThrowsException<InvalidOperationException>(() => obj.Insert());
  StringAssert.Contains(ex.Message, "ISqlClient");
  ```
- **`Assert.Inconclusive(message)`** — used by `SkipIfDatabaseUnavailable` / `SkipIfDatabaseNotReady` when the DB is missing. CI treats inconclusive as a pass but reports it.
- **`Assert.Fail(message)`** — used inside try/catch blocks to fail when an unexpected exception is thrown. Example from `DBToolsUnitTest/Core/SqlClientParameterizedQueryTests.cs:21-24`:
  ```csharp
  try { utils.Select("*", "Users", "id = @param0 AND status = @param1", parameters); }
  catch (ArgumentException ex) { Assert.Fail($"Should not throw exception: {ex.Message}"); }
  ```
- **`[ExpectedException(typeof(T))]`** — older MSTest attribute still used in many tests (e.g. `DBToolsUnitTest/Core/SqlClientQueryBuilderTests.cs:30-35`):
  ```csharp
  [ExpectedException(typeof(ArgumentException))]
  public void Select_Query_WithInvalidTable_ShouldThrowException()
  ```
- **`StringAssert.Contains(actual, substring, message)`** — for substring checks. Example `DBToolsUnitTest/Core/BugRegressionTests.cs:154`:
  ```csharp
  StringAssert.Contains(sql, "Users", "ToString() should produce SQL containing the table name");
  ```
- **`CollectionAssert.AreEqual(expected, actual)`** — for collection equality (`DBToolsUnitTest/Models/GenericObjectTests.cs:112`).

### Recommended modern pattern (mixed in codebase)
**New tests should use `Assert.ThrowsException<T>` instead of `[ExpectedException]`.** Both styles currently coexist.

## What's Tested vs. What's Not

### Well-covered (deep unit tests, no DB needed)
- **`SqlValidator`** — word-boundary matching, dangerous keywords, identifier regex. See `DBToolsUnitTest/Core/BugRegressionTests.cs:39-83`.
- **`DbProviderFactory`** — enum and string overloads, case-insensitivity, alias mapping, invalid provider rejection. See `DBToolsUnitTest/Providers/DbProviderFactoryTests.cs`.
- **`IDbProvider` dialect differences** — `QuoteIdentifier`, `BuildPagingClause`, `GetLastInsertedIdSql`, `BuildUpsertSql`, `ParameterPrefix`, `ProviderName`, `SupportsMerge`, `CreateConnection` for all four providers. See `DBToolsUnitTest/Providers/ProviderDialectTests.cs:1-401`.
- **`SqlQueryBuilder`** (static methods) — `Select_Query`, `Insert_Query`, `Update_Query`, `Delete_Query` with valid and invalid inputs. See `DBToolsUnitTest/Core/SqlClientQueryBuilderTests.cs`.
- **`SqlValidator`** — see BugRegressionTests above.
- **`GenericObject` model behavior** — constructors, property setters, behavior with/without an injected `ISqlClient`, exception when client is null. Uses `FakeSqlClient` to verify forwarding. See `DBToolsUnitTest/Models/GenericObjectTests.cs`.
- **`DataExport`** — `ToCsv` and `ToDataTable` for null/empty/valid inputs, DBNull handling, header toggling. See `DBToolsUnitTest/Export/DataExportTests.cs`.
- **`DBToolsController`** — constructor + `SqlExecuteQuery`. See `DBToolsUnitTest/Controllers/DBToolsControllerTests.cs`.

### Integration-tested (require SQL Server at `127.0.0.1:1433`)
- **`SqlClient` parameterized queries** (`DBToolsUnitTest/Core/SqlClientParameterizedQueryTests.cs`) — `Select`, `Update`, `Delete` with parameters.
- **`LinqHelper<TModel>`** (`DBToolsUnitTest/Controllers/LinqHelperTests.cs`) — Insert, InsertRange (1000 rows), Update, Delete, Select, Where, Count, Any, SingleOrDefault, Find, SaveChanges.
- **`LinqHelper<TModel>` LINQ lambda methods** (`DBToolsUnitTest/Controllers/LinqHelperLinqTests.cs`) — `Where` predicate, `FirstOrDefault`, `SingleOrDefault` (with multiple-match exception), `Any`, `Count`, `Remove`, `Update`, `SaveChanges`, `AsQueryable`.
- **`Linq<TModel>`** (`DBToolsUnitTest/Controllers/LinqTests.cs`) — `WhereEquals`, `WhereGreaterThan`, `WhereBetween`, `WhereContains`, `WhereStartsWith`, `WhereEndsWith`, `WhereIn`, `FirstOrDefaultByProperty`, `CountByProperty`, `AnyByProperty`, `WhereByExample`, `UpdateWhere`, `DeleteWhere`, `DeleteWhereIn`, `UpdateWhereIn`, `InsertOrUpdate`, `GetOrCreate`, `Exists`.
- **`DbQuery<TModel>` / AsQueryable + JOINs** (`DBToolsUnitTest/Linq/DbQueryLinqTests.cs`) — `Where`, `OrderBy`/`OrderByDescending`/`ThenBy`, `Skip`/`Take`, `FirstOrDefault`, `Count`, `Any`, complex chains, `WhereContains` → LIKE, `InnerJoin`/`LeftJoin`.
- **Bug regressions** (`DBToolsUnitTest/Core/BugRegressionTests.cs`) — bug-numbered regression tests for `SqlValidator` substring matching, `QueryBuilder` null handling, `WhereByExample` value-type zero, `DeleteWhereIsNull`, `DbQuery.ToString`, `AsQueryable.Single` TOP 2, JOIN alias resolution, `InsertRange` atomicity, null `=` rewriting, `ConnectionString` TrustServerCertificate.
- **`ObsoleteMethodTests`** — backward-compat for deprecated methods.

### Gap: weakly tested / not unit-tested
- **`AsyncSqlClient`** — no dedicated test file in `DBToolsUnitTest/`. Most of its public surface (`SelectAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`, `ExecuteScalarAsync`, `BeginTransactionAsync`, interceptor pipeline) is exercised only through the existing integration tests that go via `SqlClient` / `LinqHelper` / `Linq` paths. **Recommended: add `DBToolsUnitTest/Core/AsyncSqlClientTests.cs`.**
- **`DbContext` / `DbSet` / `ChangeTracker`** (`DBTools/Context/`) — no tests. SaveChanges/transactional behavior is uncovered.
- **`BulkOperations<TEntity>`** (`DBTools/Bulk/`) — no tests.
- **`EntityMappingResolver`** (`DBTools/Mapping/EntityMappingResolver.cs`) — attribute-driven mapping has no test coverage.
- **`FluentConfiguration` / `EntityBuilder`** (`DBTools/Mapping/FluentConfiguration.cs`) — no tests.
- **`AsyncLinqHelper<TModel>`** — no dedicated tests (separate from `LinqHelper`).
- **`DbToolsTransaction`** (`DBTools/Core/DbTransaction.cs`) — commit/rollback/Dispose paths are not exercised.
- **Built-in interceptors** (`LoggingInterceptor`, `SoftDeleteInterceptor`, `AuditInterceptor`) — not directly tested.
- **PostgresProvider / MySqlProvider / SqliteProvider `CreateConnection`** — `ProviderDialectTests.cs:376-397` explicitly expects `InvalidOperationException` because the package references aren't loaded in the test project. Connection strings and other provider-specific behaviors are not covered.

### Test categorization drift (warning)
- `DBToolsUnitTest/Integration/EdgeCaseTests.cs` and `WorkflowTests.cs` live in the `Integration` namespace but **do not** carry `[TestCategory("Integration")]`. They will run in the default unit-tests job (where they pass because they use the offline `SqlClient.Insert_Query` / `Update_Query` / `Delete_Query` static methods). This is intentional for those particular tests, but the file location is misleading.
- The `Integration/` folder is otherwise unused by other category-true integration tests — `BugRegressionTests.cs`, `DbQueryLinqTests.cs`, `LinqHelperTests.cs`, `LinqTests.cs`, `LinqHelperLinqTests.cs`, `SqlClientParameterizedQueryTests.cs`, and `ObsoleteMethodTests.cs` all sit in `Core/`, `Linq/`, or `Controllers/` instead.
- **Recommendation:** Move `EdgeCaseTests.cs` / `WorkflowTests.cs` to a more appropriate folder (e.g. `Core/` or `Export/`), since they are not DB-dependent.

## How to Add a New Test

### Step-by-step

1. **Identify the test category.**
   - No DB required → put in any folder; **no** `[TestCategory]` attribute.
   - Requires SQL Server at `127.0.0.1:1433` → add `[TestCategory("Integration")]`.

2. **Create the file** in the folder matching the namespace under test:
   - `DBTools/Core/*` → `DBToolsUnitTest/Core/<TypeName>Tests.cs`
   - `DBTools/Controllers/*` → `DBToolsUnitTest/Controllers/<TypeName>Tests.cs`
   - `DBTools/Models/*` → `DBToolsUnitTest/Models/<TypeName>Tests.cs`
   - `DBTools/Providers/*` → `DBToolsUnitTest/Providers/<TypeName>Tests.cs`
   - `DBTools/Linq/*` → `DBToolsUnitTest/Linq/<TypeName>Tests.cs`
   - `DBTools/Export/*` → `DBToolsUnitTest/Export/<TypeName>Tests.cs`
   - `DBTools/Abstractions/*` → `DBToolsUnitTest/Abstractions/<TypeName>Tests.cs` (folder does not exist yet — create it).

3. **Pick the base class.**
   - Unit tests (no DB): inherit `TestBase` to inherit the `TestHost`/`TestUid`/etc. constants and the data generators.
   - Integration tests: still inherit `TestBase` (no test currently uses `IntegrationTestBase`); call `SkipIfDatabaseUnavailable()` at the top of every DB-touching method.

4. **Write `[TestClass]` with `[TestMethod]` methods.** Use the `Method_Scenario_ExpectedBehavior` naming convention.

5. **Use `Assert.ThrowsException<T>` for exception assertions** in new code. Reserve `[ExpectedException]` only for matching existing style in a file you're extending.

6. **For cross-cutting test data**, add helpers to `TestBase.cs` (e.g. new random-data factories, new constants).

### Example template (offline unit test)
```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.<Area>;
using System;

namespace DBToolsUnitTest.<Area>
{
    [TestClass]
    public class <TypeName>Tests : TestBase
    {
        [TestMethod]
        public void Method_Scenario_ExpectedBehavior()
        {
            // Arrange
            var sut = new <TypeName>(/* deps */);
            // Act
            var result = sut.DoThing();
            // Assert
            Assert.AreEqual(expected, result);
        }
    }
}
```

### Example template (integration test)
```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.<Area>;
using System;

namespace DBToolsUnitTest.<Area>
{
    [TestClass]
    [TestCategory("Integration")]
    public class <TypeName>IntegrationTests : TestBase
    {
        [TestMethod]
        public void DbMethod_WithX_ShouldDoY()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            // ... call utils.DbMethod ...
            Assert.IsTrue(condition);
        }
    }
}
```

### Verifying locally
```bash
# Compile + unit-only run (no DB needed)
dotnet test DBToolsUnitTest/DBToolsUnitTest.csproj --filter "TestCategory!=Integration"

# Full run (requires Docker for SQL Server)
docker compose up -d sqlserver --wait
dotnet test DBToolsUnitTest/DBToolsUnitTest.csproj
```

## Special Considerations

### Determinism in CI
- `.github/workflows/ci.yml:38-47` explicitly verifies deterministic build output by building twice and diffing `DBTools.dll` via `sha256sum`. Tests should not depend on non-deterministic build artifacts.
- Tests that hit the database should **not** assume row counts (e.g. `Assert.IsTrue(count >= 0)` rather than `Assert.AreEqual(N, count)`).

### Cross-platform / container awareness
- The CI runner is `ubuntu-latest`. Tests must work on Linux.
- `IntegrationTestBase.cs:91-97` demonstrates `docker compose` v2 vs `docker-compose` v1 probing — any new test orchestration should follow the same pattern.

### Pre-merge test discipline (observed in `BugRegressionTests.cs`)
- Each bug fix is followed by a regression test grouped under a `// ─── Bug N: <description> ────────` header comment (lines 36-289). New bug fixes should follow the same convention for traceability.

### Security regression tests
- The codebase maintains a comprehensive set of SQL-injection-prevention tests:
  - `DBToolsUnitTest/Core/SqlClientValidationTests.cs` (5 tests, validation rejection).
  - `DBToolsUnitTest/Core/SqlClientQueryBuilderTests.cs` (4 tests, query-string rejection).
  - `DBToolsUnitTest/Core/SqlClientParameterizedQueryTests.cs` (4 tests, parameter validation).
  - `DBToolsUnitTest/Core/BugRegressionTests.cs:39-83` (6 tests, validator substring matching).
  - `DBToolsUnitTest/Integration/WorkflowTests.cs:30-83` (SQL injection pattern sweep).
- **Convention:** New SQL-builder entry points must ship with at least one positive (valid input) and one negative (injection pattern) test.

---

*Testing analysis: 2026-06-21*