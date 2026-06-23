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

- [Docker](https://www.docker.com/) and Docker Compose — **all tests must run inside Docker**
- No .NET SDK installation required on the host; the `test-runner` container includes it

---

## Running tests

**All tests must be run inside Docker using the `test-runner` service.** Do not run `dotnet test` directly on the host machine.

### Build the test runner

```bash
docker compose build test-runner
```

### Full test suite (unit + integration) — SQL Server

```bash
docker compose up -d sqlserver --wait
docker compose up sqlserver-setup --abort-on-container-exit --exit-code-from sqlserver-setup
docker compose run --rm test-runner
```

### Unit tests only (no database containers needed)

```bash
docker compose run --rm test-runner -- --filter "TestCategory!=Integration"
```

Extra arguments after `--` are passed through to `dotnet test`.

### Integration tests only

```bash
docker compose up -d sqlserver --wait
docker compose up sqlserver-setup --abort-on-container-exit --exit-code-from sqlserver-setup
docker compose run --rm test-runner -- --filter "TestCategory=Integration"
```

### Filter by class or method name

```bash
docker compose run --rm test-runner -- --filter "FullyQualifiedName~SqlClientValidationTests"
```

### Code coverage (optional)

```bash
docker compose run --rm test-runner -- --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

Coverage reports are written to `./TestResults/` on the host (mounted via volume).

---

## Testing against different providers

The `test-runner` container uses the `TEST_PROVIDER` environment variable to select which database to connect to. It automatically configures `config.json` with the correct Docker service hostname and port.

| `TEST_PROVIDER` value | Host in container | Port | Database container | Setup container |
|-----------------------|-------------------|------|--------------------|-----------------|
| `SqlServer` (default) | `sqlserver` | `1433` | `dbtools-sqlserver` | `dbtools-sqlserver-setup` |
| `PostgreSQL` | `postgres` | `5432` | `dbtools-postgres` | `dbtools-postgres-setup` |
| `MySQL` | `mysql` | `3306` | `dbtools-mysql` | `dbtools-mysql-setup` |
| `SQLite` | — | — | None (file-based) | None |

### SQL Server (default)

```bash
docker compose build test-runner
docker compose up -d sqlserver --wait
docker compose up sqlserver-setup --abort-on-container-exit --exit-code-from sqlserver-setup
docker compose run --rm test-runner
```

### PostgreSQL

```bash
docker compose build test-runner
docker compose up -d postgres --wait
docker compose up postgres-setup --abort-on-container-exit --exit-code-from postgres-setup
docker compose run --rm -e TEST_PROVIDER=PostgreSQL test-runner
```

### MySQL

```bash
docker compose build test-runner
docker compose up -d mysql --wait
docker compose up mysql-setup --abort-on-container-exit --exit-code-from mysql-setup
docker compose run --rm -e TEST_PROVIDER=MySQL test-runner
```

### SQLite (no database container needed)

```bash
docker compose build test-runner
docker compose run --rm -e TEST_PROVIDER=SQLite test-runner
```

### All providers sequentially

```bash
docker compose build test-runner
docker compose up -d sqlserver postgres mysql --wait
docker compose up setup-all --abort-on-container-exit --exit-code-from setup-all

docker compose run --rm -e TEST_PROVIDER=SqlServer test-runner
docker compose run --rm -e TEST_PROVIDER=PostgreSQL test-runner
docker compose run --rm -e TEST_PROVIDER=MySQL test-runner
docker compose run --rm -e TEST_PROVIDER=SQLite test-runner
```

### How TEST_PROVIDER works

The `test-runner` entrypoint runs `scripts/switch-provider.sh` before `dotnet test`. This script writes the correct `DBToolsUnitTest/config.json` based on the `TEST_PROVIDER` value, using Docker service hostnames (`sqlserver`, `postgres`, `mysql`) instead of `127.0.0.1`. For SQLite, it also creates the `testDB.db` file from `scripts/sqlite-integration-setup.sql`.

---

## Database containers

The `docker-compose.yml` defines containers for **SQL Server**, **PostgreSQL**, and **MySQL**. **SQLite** is file-based and does not require a container.

### Connection details (inside Docker network)

| Provider | Host | Port | User | Password | Database |
|----------|------|------|------|----------|----------|
| SQL Server | `sqlserver` | `1433` | `testUser` | `Integration!123` | `testDB` |
| PostgreSQL | `postgres` | `5432` | `testUser` | `Integration!123` | `testDB` |
| MySQL | `mysql` | `3306` | `testUser` | `Integration!123` | `testDB` |
| SQLite | — | — | — | — | `testDB.db` file |

### Connection details (from host for debugging)

| Provider | Host | Port | User | Password | Database |
|----------|------|------|------|----------|----------|
| SQL Server | `127.0.0.1` | `1433` | `testUser` | `Integration!123` | `testDB` |
| PostgreSQL | `127.0.0.1` | `5432` | `testUser` | `Integration!123` | `testDB` |
| MySQL | `127.0.0.1` | `3306` | `testUser` | `Integration!123` | `testDB` |

### Start databases only (without running tests)

```bash
# All providers
docker compose up -d sqlserver postgres mysql --wait
docker compose up setup-all --abort-on-container-exit --exit-code-from setup-all

# Single provider
docker compose up -d sqlserver --wait
docker compose up sqlserver-setup --abort-on-container-exit --exit-code-from sqlserver-setup
```

### Stop and clean up

```bash
docker compose down -v --remove-orphans
rm -f testDB.db
```

---

## How the test runner works

The `test-runner` service in `docker-compose.yml`:

1. Builds from `Dockerfile.test` using the `mcr.microsoft.com/dotnet/sdk:8.0` image
2. Restores NuGet packages and builds `DBTools.sln` in Release configuration
3. At runtime, executes `scripts/switch-provider.sh` which writes `DBToolsUnitTest/config.json` based on the `TEST_PROVIDER` environment variable
4. Uses Docker service hostnames (`sqlserver`, `postgres`, `mysql`) so the test container can reach the database containers
5. Runs `dotnet test DBTools.sln` — any extra arguments after `--` in `docker compose run` are forwarded to `dotnet test`

### Dockerfiles and scripts

| File | Purpose |
|------|---------|
| `Dockerfile.test` | Multi-stage build: restores, builds, and prepares the test runner |
| `scripts/switch-provider.sh` | Writes `config.json` with the correct host/port for the selected provider |
| `scripts/sqlserver-integration-setup.sql` | Creates tables and seed data for SQL Server |
| `scripts/postgres-integration-setup.sql` | Creates tables and seed data for PostgreSQL |
| `scripts/mysql-integration-setup.sql` | Creates tables and seed data for MySQL |
| `scripts/sqlite-integration-setup.sql` | Creates tables and seed data for SQLite |

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
| Integration | Yes — live database | `[TestCategory("Integration")]` on the test class | `TestBase` or `IntegrationTestBase` |

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
| **Unit Tests** | Every push / PR | None | `docker compose run --rm test-runner -- --filter "TestCategory!=Integration"` |
| **Integration Tests** | Every push / PR | Docker Compose (SQL Server) | `docker compose run --rm test-runner` (default provider) |
| **All Tests (Full)** | Push to `main` or version tags (`v*`) | Docker Compose (SQL Server) | `docker compose run --rm test-runner` (no filter) |

Each job:

1. Checks out the repository
2. Builds the `test-runner` Docker image
3. Starts the required database containers
4. Runs tests inside the `test-runner` container
5. Tears down containers

The **Unit Tests** job also verifies deterministic build output by comparing SHA-256 hashes of `DBTools.dll` across two consecutive builds.

Release builds ([`.github/workflows/release.yml`](../.github/workflows/release.yml)) run unit tests only (`TestCategory!=Integration`) before packing the NuGet package.

---

## Docker image management

Before pulling images, check which are already cached locally:

```bash
docker images
```

Only pull images you don't have. To free disk space from unused images:

```bash
docker image prune -a
```

Common images for this project:

| Image | Purpose |
|-------|---------|
| `mcr.microsoft.com/dotnet/sdk:8.0` | .NET SDK (test-runner build) |
| `mcr.microsoft.com/mssql/server:2022-latest` | SQL Server |
| `postgres:16-alpine` | PostgreSQL |
| `mysql:8.0` | MySQL |
| `alpine:3.19` | Setup coordination (`setup-all` service) |

---

## Next steps

- [CONFIGURATION.md](CONFIGURATION.md) — test database connection settings in `config.json`
- [ARCHITECTURE.md](ARCHITECTURE.md) — library structure that test directories mirror
- [QUICKSTART.md](QUICKSTART.md) — first-run setup for the library itself