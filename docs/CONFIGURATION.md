<!-- generated-by: gsd-doc-writer -->
# DBTools_SQL Configuration Guide

How to configure database connections, dependency injection options, and local development tooling for DBTools_SQL.

## Table of Contents

1. [Environment Variables](#environment-variables)
2. [Config File Format](#config-file-format)
3. [Required vs Optional Settings](#required-vs-optional-settings)
4. [Defaults](#defaults)
5. [Per-Environment Overrides](#per-environment-overrides)

---

## Environment Variables

DBTools_SQL does **not** read environment variables directly. Connection settings are loaded from `config.json`, an `IConfiguration` instance, or `DbToolsOptions` when using dependency injection.

You can supply settings from environment variables by building an `IConfiguration` and passing it to `DbConfiguration`:

```csharp
using DBTools.Core;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("config.json", optional: true)
    .AddEnvironmentVariables("DBTOOLS_")
    .Build();

var config = new DbConfiguration(configuration);
var db = new SqlClient(config, new SqlValidator(), new SqlQueryBuilder(new SqlValidator()));
```

With the `DBTOOLS_` prefix, map flat environment variables to configuration keys (for example, `DBTOOLS_Host` → `Host`, `DBTOOLS_Password` → `Password`).

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `DBTOOLS_Host` | Optional* | — | Database server host or address (consumer-defined via `IConfiguration`) |
| `DBTOOLS_Database` | Optional* | — | Database name |
| `DBTOOLS_Uid` | Optional* | — | Database username |
| `DBTOOLS_Password` | Optional* | — | Database password |
| `DBTOOLS_Port` | Optional* | — | Database port |
| `DBTOOLS_Provider` | Optional | `SqlServer` | Provider name (`SqlServer`, `PostgreSQL`, `MySQL`, `SQLite`) |
| `ACCEPT_EULA` | Required (Docker only) | — | Must be `"Y"` to run the SQL Server integration-test container (`docker-compose.yml`) |
| `MSSQL_SA_PASSWORD` | Required (Docker only) | `TestStrong!Passw0rd` in repo compose file | SA password for the local SQL Server container used by integration tests |

\*When using the parameterless `DbConfiguration()` constructor (reads `config.json` from the working directory), all connection keys except `Provider` are **required** in the file. When using `DbConfiguration(IConfiguration)`, the same keys are required unless you provide values through another configuration source (such as environment variables).

There is no `.env.example` in this repository. Do not commit real credentials in tracked files.

---

## Config File Format

### `config.json` (primary)

The default configuration file for `SqlClient`, `AsyncSqlClient`, and `DbConfiguration()`. Copy from the example template:

```bash
cp DBTools/config.json.example DBTools/config.json
```

For integration tests:

```bash
cp DBToolsUnitTest/config.json.example DBToolsUnitTest/config.json
```

**Location:** The parameterless `DbConfiguration()` constructor reads `config.json` from the process **current working directory** (`Directory.GetCurrentDirectory()`). The file must also be copied to the build output directory (`CopyToOutputDirectory=Always` in `DBTools/DBTools.csproj` and `DBToolsUnitTest/DBToolsUnitTest.csproj`).

**Schema:**

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

| Key | Type | Description |
|-----|------|-------------|
| `Provider` | string | Database provider. Supported values: `SqlServer` (default), `PostgreSQL` / `postgres`, `MySQL`, `SQLite` |
| `Host` | string | Server hostname, IP, or instance name (for SQL Server, e.g. `localhost\\SQLEXPRESS`) |
| `Database` | string | Database name (for SQLite, this is the file path) |
| `Uid` | string | Database username |
| `Password` | string | Database password |
| `Port` | string | TCP port (typically `1433` for SQL Server, `5432` for PostgreSQL, `3306` for MySQL) |

`config.json` is listed in `.gitignore`; only `config.json.example` is tracked.

**Reload behavior:** When loaded via `DbConfiguration()`, the JSON file is registered with `reloadOnChange: true`, so changes on disk are picked up on subsequent configuration reads through `Microsoft.Extensions.Configuration`.

### `DbToolsOptions` (dependency injection)

When registering services with `AddDbTools`, configure a `DbToolsOptions` instance instead of (or in addition to) a JSON file:

```csharp
services.AddDbTools(options =>
{
    options.Provider = DatabaseProvider.PostgreSQL;
    options.Host = "localhost";
    options.Port = "5432";
    options.Database = "myappdb";
    options.Username = "postgres";
    options.Password = "secret";
    options.TrustServerCertificate = false;
});
```

Alternatively, pass a full connection string (SQL Server by default):

```csharp
services.AddDbTools("Data Source=tcp:localhost,1433;Initial Catalog=MyDb;User ID=sa;Password=secret;");
```

| Property | Type | Description |
|----------|------|-------------|
| `ConnectionString` | string | Full ADO.NET connection string; when set, overrides individual host/database/credential properties |
| `Host` | string | Server host |
| `Database` | string | Database name |
| `Username` | string | Login name (maps to `Uid` on `IDbConfiguration`) |
| `Password` | string | Login password |
| `Port` | string | TCP port |
| `Provider` | `DatabaseProvider` enum | `SqlServer`, `PostgreSQL`, `MySQL`, or `SQLite` |
| `CommandTimeout` | int | Declared timeout in seconds (default `30`; not currently applied to ADO.NET commands) |
| `TrustServerCertificate` | bool | Included in generated SQL Server connection strings (default `true`) |

### `nuget.config` (package restore)

The repository root `nuget.config` points package restore at NuGet.org. For installing the `DBTools` package from GitHub Packages, copy `nuget.config.github-packages.example` to your solution as `nuget.config` and authenticate with a GitHub personal access token (see comments in that file).

### Docker Compose (integration testing)

`docker-compose.yml` at the repository root starts a SQL Server 2022 container and runs schema setup for integration tests. It is not used at runtime by the library itself. See [docs/QUICKSTART.md](QUICKSTART.md) and the compose file for startup commands.

---

## Required vs Optional Settings

### `config.json` / `DbConfiguration(IConfiguration)`

| Setting | Required | Validation error |
|---------|----------|------------------|
| `config.json` file | **Yes** (parameterless constructor) | `FileNotFoundException`: *The configuration file 'config.json' was not found in directory '{basePath}'.* |
| `Host` | **Yes** | `InvalidOperationException`: *The following required configuration keys are missing or empty in 'config.json': Host* (and similarly for other keys) |
| `Database` | **Yes** | Same as above |
| `Uid` | **Yes** | Same as above |
| `Password` | **Yes** | Same as above |
| `Port` | **Yes** | Same as above |
| `Provider` | Optional | Defaults to `SqlServer` when omitted or empty |

If the JSON file is malformed, construction throws `InvalidOperationException` with message *Failed to load database configuration from 'config.json'.*

### `DbToolsOptions` / `AddDbTools`

No automatic validation throws at registration time. An empty or incomplete options object produces connection strings with empty segments. Prefer setting `ConnectionString` directly or ensuring `Host`, `Database`, `Username`, and `Password` are populated before resolving `IAsyncSqlClient`.

### Provider-specific packages

Choosing a non–SQL Server `Provider` requires the corresponding ADO.NET driver in **your** application (only `Microsoft.Data.SqlClient` is a direct dependency of `DBTools`):

| Provider | Consumer package |
|----------|------------------|
| `SqlServer` | Included (`Microsoft.Data.SqlClient`) |
| `PostgreSQL` | `Npgsql` |
| `MySQL` | `MySqlConnector` or `MySql.Data` |
| `SQLite` | `Microsoft.Data.Sqlite` |

Missing provider assemblies surface at runtime when the provider factory loads the connection type.

---

## Defaults

| Setting | Default | Where set |
|---------|---------|-----------|
| `Provider` | `SqlServer` | `DBTools/Core/DbConfiguration.cs` (`configuration["Provider"] ?? "SqlServer"`) |
| `Port` (DI) | `"1433"` | `DBTools/Configuration/DbToolsOptions.cs` |
| `TrustServerCertificate` (DI) | `true` | `DBTools/Configuration/DbToolsOptions.cs` |
| `CommandTimeout` (DI) | `30` | `DBTools/Configuration/DbToolsOptions.cs` |
| `Provider` (DI) | `DatabaseProvider.SqlServer` | `DBTools/Configuration/DbToolsOptions.cs` |

**Connection strings built from `config.json`:**

| Provider | Generated pattern |
|----------|-------------------|
| SqlServer | `Data Source=tcp:{Host},{Port};Initial Catalog={Database};User ID={Uid};Password={Password};TrustServerCertificate=True;` |
| PostgreSQL | `Host={Host};Port={Port};Database={Database};Username={Uid};Password={Password};` |
| MySQL | `Server={Host};Port={Port};Database={Database};Uid={Uid};Pwd={Password};` |
| SQLite | `Data Source={Database};` |

**Connection strings built from `DbToolsOptions`:** Same provider mapping; SQL Server strings use `TrustServerCertificate={TrustServerCertificate}` from options.

**Integration test defaults** (`DBToolsUnitTest/config.json.example`): host `127.0.0.1`, database `testDB`, user `testUser`, password `Integration!123`, port `1433` — matching the Docker Compose setup script.

---

## Per-Environment Overrides

DBTools_SQL has no built-in `Development` / `Staging` / `Production` profiles. Use one of these patterns in consuming applications:

### 1. Multiple JSON files via `IConfiguration`

```csharp
var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var config = new DbConfiguration(configuration);
```

Store shared settings in `appsettings.json` and environment-specific overrides in `appsettings.Development.json`, `appsettings.Staging.json`, etc. Key names must match the flat keys expected by `DbConfiguration` (`Host`, `Database`, `Uid`, `Password`, `Port`, `Provider`).

### 2. Dependency injection per environment

In ASP.NET Core or other DI hosts, configure `AddDbTools` inside environment-specific startup:

```csharp
if (builder.Environment.IsDevelopment())
{
    services.AddDbTools(options =>
    {
        options.Host = "127.0.0.1";
        options.Database = "testDB";
        options.Username = "testUser";
        options.Password = "Integration!123";
        options.Port = "1433";
    });
}
else
{
    services.AddDbTools(builder.Configuration.GetConnectionString("DefaultConnection"));
}
```

### 3. Secrets outside source control

- Keep `config.json` local only (already gitignored in this repo).
- Load passwords from environment variables, Azure Key Vault, or your host's secret manager through `IConfiguration` before constructing `DbConfiguration`.
- See [SECURITY.md](SECURITY.md#configuration-security) for credential handling guidance.

### 4. Local SQL Server for tests

Use Docker Compose from the repository root:

```bash
docker compose up -d sqlserver --wait
docker compose up sqlserver-setup --abort-on-container-exit --exit-code-from sqlserver-setup
```

Then point `DBToolsUnitTest/config.json` (or test code) at `127.0.0.1:1433` with the credentials from `docker-compose.yml` and `DBToolsUnitTest/config.json.example`.

---

## Related Documentation

- [QUICKSTART.md](QUICKSTART.md) — First-time setup and `config.json` copy steps
- [API_REFERENCE.md](API_REFERENCE.md) — `DbConfiguration`, `DbToolsOptions`, and `AddDbTools`
- [SECURITY.md](SECURITY.md) — Protecting credentials and connection security
- [EXAMPLES.md](EXAMPLES.md) — Custom configuration and DI patterns
