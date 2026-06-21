<!-- generated-by: gsd-doc-writer -->
# Testing

How to run, filter, and extend the DBTools_SQL test suite.

## Test framework and setup

DBTools_SQL uses **MSTest** (`MSTest.TestFramework` 3.7.0) with the **Microsoft Test SDK** (17.12.0). Tests live in the `DBToolsUnitTest` project, which targets **.NET 8.0** and references the main `DBTools` library.

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.NET.Test.Sdk` | 17.12.0 | Test runner host |
| `MSTest.TestFramework` | 3.7.0 | `[TestClass]`, `[TestMethod]`, `Assert` |
| `MSTest.TestAdapter` | 3.7.0 | Visual Studio / `dotnet test` adapter |
| `coverlet.collector` | 6.0.2 | Code coverage collection (optional) |
| `Testcontainers` / `Testcontainers.MsSql` | 4.4.0 | Local SQL Server fallback for integration tests |

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Unit tests only:** no database required
- **Integration tests:** Docker (for `docker compose`) or an existing SQL Server instance at `127.0.0.1:1433`

### One-time setup

Copy the example configuration files before running tests. CI performs the same step:

```bash
cp DBTools/config.json.example DBTools/config.json
cp DBToolsUnitTest/config.json.example DBToolsUnitTest/config.json
dotnet restore DBTools.sln
dotnet build DBTools.sln --configuration Release
```

The test project copies `DBToolsUnitTest/config.json` (created from `config.json.example` during setup) to the output directory automatically. It points at the integration test database (`testDB` on `127.0.0.1:1433` with user `testUser`).

---

## Running tests

### Full suite

```bash
dotnet test DBTools.sln --no-build --configuration Release --verbosity normal
```

Integration tests require SQL Server. Start containers first (see [Integration test setup](#integration-test-setup)) or they will be marked **Inconclusive**.

### Unit tests only (no database)

Excludes tests tagged with `[TestCategory("Integration")]`:

```bash
dotnet test DBTools.sln \
  --no-build \
  --configuration Release \
  --verbosity normal \
  --filter "TestCategory!=Integration"
```

This is the fast path used by CI on every push and pull request.

### Integration tests only

Requires a running SQL Server with the integration schema applied:

```bash
dotnet test DBTools.sln \
  --no-build \
  --configuration Release \
  --verbosity normal \
  --filter "TestCategory=Integration"
```

### Single test project

```bash
dotnet test DBToolsUnitTest/DBToolsUnitTest.csproj --configuration Release
```

### Filter by class or method name

```bash
# Run one test class
dotnet test DBToolsUnitTest/DBToolsUnitTest.csproj \
  --filter "FullyQualifiedName~DBToolsUnitTest.Core.SqlClientValidationTests"

# Run one test method
dotnet test DBToolsUnitTest/DBToolsUnitTest.csproj \
  --filter "FullyQualifiedName~Constructor_ShouldSetDefaultPort"
```

### Code coverage (optional)

`coverlet.collector` is referenced but no coverage thresholds are enforced in CI. To collect coverage locally:

```bash
dotnet test DBToolsUnitTest/DBToolsUnitTest.csproj \
  --configuration Release \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults
```

Coverage reports are written under `./TestResults/` as Cobertura XML.

### Visual Studio

Open `DBTools.sln`, build the solution, then use **Test Explorer** to run or debug individual tests and classes.

No watch-mode test script is configured in the repository.

---

## Integration test setup

Integration tests connect to SQL Server at `127.0.0.1:1433` using credentials from `DBToolsUnitTest/config.json.example` (`testUser` / `Integration!123`, database `testDB`).

### Docker Compose (recommended — matches CI)

From the repository root:

```bash
docker compose up -d sqlserver --wait
docker compose up sqlserver-setup --abort-on-container-exit --exit-code-from sqlserver-setup
```

The `sqlserver-setup` service creates `testDB`, the `testUser` login, and applies `scripts/sqlserver-integration-setup.sql`.

When finished:

```bash
docker compose down -v --remove-orphans
```

### Automatic fallbacks (`IntegrationTestBase`)

Classes that inherit from `IntegrationTestBase` can call `EnsureContainersReady()` to attempt, in order:

1. Connect to an already-running database at `127.0.0.1:1433`
2. Start containers via `docker compose`
3. Start a disposable SQL Server via **Testcontainers**

Most integration test classes today inherit from `TestBase` and call `SkipIfDatabaseUnavailable()` per test method instead.

---

## Writing new tests

### File and class conventions

| Convention | Detail |
|------------|--------|
| Project | `DBToolsUnitTest/DBToolsUnitTest.csproj` |
| File naming | `{Component}Tests.cs` (e.g. `SqlClientValidationTests.cs`) |
| Class naming | `{Component}Tests` decorated with `[TestClass]` |
| Method naming | `[TestMethod]` with descriptive `Action_ExpectedResult` names |
| Directory layout | Mirrors the library: `Core/`, `Controllers/`, `Models/`, `Export/`, `Providers/`, `Linq/`, `Integration/` |

### Base classes

**`TestBase`** (`DBToolsUnitTest/TestBase.cs`) — shared constants (`TestHost`, `TestDatabase`, `TestUid`, `TestPassword`), random test-data generators (`Names()`, `Surnames()`, `Emails()`), and helpers:

- `IsDatabaseAvailable()` — probes SQL Server with a 2-second timeout
- `SkipIfDatabaseUnavailable()` — marks the test **Inconclusive** when no database is reachable
- `ExecuteInTempDirectory(Action, string configContent)` — runs code in an isolated temp directory with an optional `config.json`

**`IntegrationTestBase`** (`DBToolsUnitTest/IntegrationTestBase.cs`) — extends `TestBase` with Docker Compose / Testcontainers lifecycle management. Use for new integration test classes that need automatic container startup.

### Unit vs integration

| Type | Database required | Attribute | Base class |
|------|-------------------|-----------|------------|
| Unit | No | *(none)* | `TestBase` |
| Integration | Yes — live SQL Server | `[TestCategory("Integration")]` on the test class | `TestBase` or `IntegrationTestBase` |

Place offline validation and query-building tests anywhere under `DBToolsUnitTest/` without a category attribute. Tests that execute real SQL against `testDB` must carry `[TestCategory("Integration")]` so CI can filter them.

Example integration test class:

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Core;

namespace DBToolsUnitTest.Core
{
    [TestClass]
    [TestCategory("Integration")]
    public class MyFeatureTests : TestBase
    {
        [TestMethod]
        public void Insert_WithValidRow_ShouldReturnTrue()
        {
            SkipIfDatabaseUnavailable();
            var client = new SqlClient();
            // ...
        }
    }
}
```

Example unit test (no database):

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Core;

namespace DBToolsUnitTest.Core
{
    [TestClass]
    public class MyValidationTests : TestBase
    {
        [TestMethod]
        public void Select_WithInvalidTableName_ShouldThrowException()
        {
            Assert.ThrowsException<Exception>(() =>
                SqlClient.Select_Query("*", "Users; DROP TABLE--", null));
        }
    }
}
```

### What to test

The existing suite covers:

- **Security** — SQL injection prevention, mandatory `WHERE` clauses on `UPDATE`/`DELETE`, parameterized queries
- **Query building** — static `SqlClient.*_Query` methods and CRUD execution
- **Models and controllers** — `GenericObject`, `LinqHelper<T>`, `DBToolsController`
- **Providers** — dialect differences and `DbProviderFactory` resolution
- **Export** — CSV import/export via `DataExport`
- **Regression** — known bug fixes in `BugRegressionTests`

See `DBToolsUnitTest/TestDocumentation.md` for a detailed inventory of existing test classes.

---

## Coverage requirements

No coverage threshold is configured. The test project includes `coverlet.collector` for optional local collection, but CI does not fail on coverage percentages.

| Type | Threshold |
|------|-----------|
| Lines | Not configured |
| Branches | Not configured |
| Functions | Not configured |
| Statements | Not configured |

---

## CI integration

Tests run in [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) on push and pull request to `main`, `master`, `develop`, and `dotnet-core`.

| Job | Trigger | Database | Command |
|-----|---------|----------|---------|
| **Unit Tests** | Every push / PR | None | `dotnet test DBTools.sln --no-build --configuration Release --filter "TestCategory!=Integration"` |
| **Integration Tests** | Every push / PR | Docker Compose SQL Server | Same build, then `--filter "TestCategory=Integration"` after `docker compose up` |
| **All Tests (Full)** | Push to `main` or version tags (`v*`) | Docker Compose SQL Server | `dotnet test DBTools.sln --no-build --configuration Release` (no filter) |

Each job:

1. Checks out the repository
2. Installs .NET 8.x
3. Copies `config.json.example` files for `DBTools` and `DBToolsUnitTest`
4. Restores and builds `DBTools.sln` in Release configuration

The **Unit Tests** job also verifies deterministic build output by comparing SHA-256 hashes of `DBTools.dll` across two consecutive builds.

Release builds ([`.github/workflows/release.yml`](../.github/workflows/release.yml)) run unit tests only (`TestCategory!=Integration`) before packing the NuGet package.

---

## Next steps

- [CONFIGURATION.md](CONFIGURATION.md) — test database connection settings in `config.json`
- [ARCHITECTURE.md](ARCHITECTURE.md) — library structure that test directories mirror
- [QUICKSTART.md](QUICKSTART.md) — first-run setup for the library itself
