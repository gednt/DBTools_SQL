<!-- generated-by: gsd-doc-writer -->
# Getting Started

Get DBTools_SQL built locally and run your first query in a few minutes.

## Prerequisites

Before you begin, install the following:

| Requirement | Version | Notes |
|-------------|---------|-------|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | `8.0.x` | Required to build and test the solution (`TargetFramework`: `net8.0`) |
| Git | Any recent version | Clone the repository |
| SQL Server (or Docker) | Optional | Required only to run integration tests or query a live database |
| [Docker](https://docs.docker.com/get-docker/) and Docker Compose | Optional | Starts the SQL Server container used by integration tests (`docker-compose.yml`) |
| GitHub personal access token | Optional | Required only when installing the `DBTools` NuGet package from GitHub Packages (see [README.md](../README.md#installation)) |

No Node.js, Python, or other runtimes are required.

---

## Installation steps

### Option A — Build from source (contributors and local development)

1. Clone the repository:

```bash
git clone https://github.com/gednt/DBTools_SQL.git
cd DBTools_SQL
```

2. Restore NuGet packages:

```bash
dotnet restore DBTools.sln
```

3. Copy the example configuration files (required for build and tests):

```bash
cp DBTools/config.json.example DBTools/config.json
cp DBToolsUnitTest/config.json.example DBToolsUnitTest/config.json
```

4. Edit `DBTools/config.json` with your database connection details, or leave the example values if you only need to build and run unit tests.

5. Build the solution:

```bash
dotnet build DBTools.sln --configuration Release
```

### Option B — Install as a NuGet package (application consumers)

If you are adding DBTools to an existing .NET 8 application rather than working in this repository, install the published package instead:

1. Authenticate with GitHub Packages (see `nuget.config.github-packages.example` or [README.md](../README.md#installation)).
2. Add the package to your project:

```bash
dotnet add package DBTools --version 1.4.0 --source github
```

3. Copy `DBTools/config.json.example` into your application as `config.json` and set **Copy to Output Directory → Copy always** in your IDE (or equivalent MSBuild setting).

4. Add optional provider packages if you use PostgreSQL, MySQL, or SQLite (`Npgsql`, `MySqlConnector`, or `Microsoft.Data.Sqlite`).

---

## First run

### Verify the build (from source)

After installation steps above, confirm the library compiles:

```bash
dotnet build DBTools.sln --configuration Release
```

A successful build produces `DBTools/bin/Release/net8.0/DBTools.dll`.

### Run unit tests (no database required)

Unit tests exclude the `Integration` category and do not need Docker or a live database:

```bash
dotnet test DBTools.sln \
  --configuration Release \
  --filter "TestCategory!=Integration"
```

You should see MSTest results with passing tests.

### Run integration tests (requires Docker)

To exercise SQL Server integration tests locally:

```bash
docker compose up -d sqlserver --wait
docker compose up sqlserver-setup --abort-on-container-exit --exit-code-from sqlserver-setup

dotnet test DBTools.sln \
  --configuration Release \
  --filter "TestCategory=Integration"

docker compose down -v --remove-orphans
```

The Docker setup creates database `testDB` and login `testUser` with password `Integration!123` on port `1433`, matching `DBToolsUnitTest/config.json.example`.

### Run your first query (consumer application)

In a .NET 8 project that references `DBTools`, place `config.json` in the output directory and run:

```csharp
using DBTools.Core;
using System.Data;

var db = new SqlClient();

DataView users = db.Select(
    "*",
    "Users",
    "Age > @param0",
    new object[] { 18 }
);

foreach (DataRowView row in users)
{
    Console.WriteLine($"{row["Name"]}, age {row["Age"]}");
}
```

Example `config.json`:

```json
{
  "Provider": "SqlServer",
  "Host": "localhost\\SQLEXPRESS",
  "Database": "YourDatabaseName",
  "Uid": "YourUsername",
  "Password": "YourPassword",
  "Port": "1433"
}
```

For a step-by-step tutorial with CRUD and LINQ examples, see [QUICKSTART.md](QUICKSTART.md).

---

## Common setup issues

### `config.json` not found

**Symptom:** `FileNotFoundException: The configuration file 'config.json' was not found in directory '...'`

**Cause:** `DbConfiguration()` reads `config.json` from the process current working directory, and the file must be present in the build output folder.

**Fix:**

1. Copy `DBTools/config.json.example` to `config.json` in your project.
2. In Visual Studio: right-click `config.json` → Properties → set **Copy to Output Directory** to **Copy always**.
3. When building from this repository, run the `cp` commands in [Installation steps](#installation-steps) before `dotnet build` or `dotnet test`.

### Missing required configuration keys

**Symptom:** `InvalidOperationException: The following required configuration keys are missing or empty in 'config.json': Host` (or `Database`, `Uid`, `Password`, `Port`)

**Fix:** Ensure every required key in `config.json` has a non-empty value. Only `Provider` is optional (defaults to `SqlServer`). See [CONFIGURATION.md](CONFIGURATION.md#required-vs-optional-settings).

### Cannot restore or install `DBTools` from GitHub Packages

**Symptom:** NuGet restore fails with 401/403 when resolving `DBTools`

**Fix:**

1. Create a GitHub PAT with `read:packages` scope.
2. Register the GitHub Packages feed:

```bash
dotnet nuget add source "https://nuget.pkg.github.com/gednt/index.json" \
  --name github \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_GITHUB_PAT \
  --store-password-in-clear-text
```

3. Install with `--source github`, or copy `nuget.config.github-packages.example` to your solution as `nuget.config`.

### Provider assembly not found at runtime

**Symptom:** Exception when connecting with `PostgreSQL`, `MySQL`, or `SQLite` provider

**Fix:** Add the matching ADO.NET driver to **your** application project (`Npgsql`, `MySqlConnector`, or `Microsoft.Data.Sqlite`). SQL Server support is bundled via `Microsoft.Data.SqlClient` in the `DBTools` package.

### Integration tests fail — SQL Server not ready

**Symptom:** Connection timeouts or login failures when running `TestCategory=Integration` tests

**Fix:**

1. Confirm Docker is running.
2. Start the container and wait for setup to finish:

```bash
docker compose up -d sqlserver --wait
docker compose up sqlserver-setup --abort-on-container-exit --exit-code-from sqlserver-setup
```

3. Verify `DBToolsUnitTest/config.json` matches `DBToolsUnitTest/config.json.example` (host `127.0.0.1`, database `testDB`, user `testUser`, port `1433`).
4. Ensure port `1433` is not already in use by another SQL Server instance.

---

## Next steps

- [QUICKSTART.md](QUICKSTART.md) — Hands-on tutorial: CRUD, LINQ, JOINs, and troubleshooting
- [CONFIGURATION.md](CONFIGURATION.md) — `config.json`, dependency injection, and environment overrides
- [ARCHITECTURE.md](ARCHITECTURE.md) — Library structure and component overview
- [EXAMPLES.md](EXAMPLES.md) — Comprehensive code samples
- [API_REFERENCE.md](API_REFERENCE.md) — Method-level API documentation
- [DEVELOPMENT.md](DEVELOPMENT.md) — Local development workflow, build scripts, and code style
- [TESTING.md](TESTING.md) — Test framework, coverage, and CI integration
