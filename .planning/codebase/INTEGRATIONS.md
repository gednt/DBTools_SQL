---
generated: 2026-06-21
last_mapped_commit: f9cc45c3b85d4d7645b1a3a5deef92ed3bc9b6c7
---

# External Integrations

**Analysis Date:** 2026-06-21

## APIs & External Services

**None.** The library makes no calls to REST APIs, SaaS services, or third-party HTTP endpoints. All external surface is database-driven (see Data Storage below).

## Data Storage

**Databases (supported providers, selected at runtime via `Provider` in `config.json`):**

| Provider | Connection mechanism | Connection-string template | ADO.NET package |
|----------|---------------------|----------------------------|-----------------|
| SQL Server | Direct — `new SqlConnection(connectionString)` in `DBTools/Providers/SqlServerProvider.cs:21` | `Data Source=tcp:{Host},{Port};Initial Catalog={Database};User ID={Username};Password={Password};TrustServerCertificate={TrustServerCertificate};` (`DBTools/Configuration/DbToolsOptions.cs:96`) | `Microsoft.Data.SqlClient` 5.2.2 — hard dependency in `DBTools/DBTools.csproj:37` |
| PostgreSQL | Reflection — `Type.GetType("Npgsql.NpgsqlConnection, Npgsql")` then `Activator.CreateInstance` (`DBTools/Providers/PostgresProvider.cs:23-28`) | `Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};` (`DBTools/Configuration/DbToolsOptions.cs:97`) | `Npgsql` — consumer-installed (NOT in `DBTools.csproj`) |
| MySQL | Reflection — probes `MySqlConnector.MySqlConnection, MySqlConnector` then `MySql.Data.MySqlClient.MySqlConnection, MySql.Data` (`DBTools/Providers/MySqlProvider.cs:23-24`) | `Server={Host};Port={Port};Database={Database};Uid={Username};Pwd={Password};` (`DBTools/Configuration/DbToolsOptions.cs:98`) | `MySqlConnector` (preferred) or `MySql.Data` — consumer-installed |
| SQLite | Reflection — `Type.GetType("Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite")` (`DBTools/Providers/SqliteProvider.cs:22`) | `Data Source={Database};` (file path; `DBTools/Configuration/DbToolsOptions.cs:99`) | `Microsoft.Data.Sqlite` — consumer-installed |

- **Provider resolution entry point:** `DBTools/Providers/DbProviderFactory.cs` (string or `DatabaseProvider` enum)
- **Reflection-loaded providers throw** `InvalidOperationException("… package is not available. Install the '…' NuGet package.")` if the corresponding ADO.NET package is not present on the consumer — this is the explicit contract documented in the README and in each `*Provider.cs`
- **Hard dep on `Microsoft.Data.SqlClient`** is the only reason `DBTools.csproj` references that package; the others are optional and loaded reflectively
- **Schema bootstrap for integration tests:** `scripts/sqlserver-integration-setup.sql` — creates `dbo.Users` and `dbo.Orders`, seeds Alice/Bob/Charlie, applied by `docker-compose.yml` and by the Testcontainers fallback (`DBToolsUnitTest/IntegrationTestBase.cs:267-286`)
- **High-volume inserts** use `Microsoft.Data.SqlClient.SqlBulkCopy` in `DBTools/Bulk/BulkOperations.cs:189-210` (SQL Server only; other providers fall back to batched `INSERT` statements in a transaction)

**Dialect / SQL Server-specific features exposed via `IDbProvider`:**

| Capability | SQL Server | PostgreSQL | MySQL | SQLite |
|------------|------------|------------|-------|--------|
| Identifier quoting | `[brackets]` (`SqlServerProvider.cs:43`) | `"double quotes"` (`PostgresProvider.cs:57`) | `` `backticks` `` (`MySqlProvider.cs:63`) | `"double quotes"` (`SqliteProvider.cs:56`) |
| Paging | `OFFSET … ROWS FETCH NEXT … ROWS ONLY` (`SqlServerProvider.cs:57-60`) | `LIMIT … OFFSET …` (`PostgresProvider.cs:67-72`) | `LIMIT … OFFSET …` (with `LIMIT 18446744073709551615` sentinel for OFFSET-only; `MySqlProvider.cs:78-83`) | `LIMIT … OFFSET …` (`SqliteProvider.cs:67-71`) |
| Last-inserted ID | `SELECT SCOPE_IDENTITY()` (`SqlServerProvider.cs:67`) | `RETURNING id` (`PostgresProvider.cs:78`) | `SELECT LAST_INSERT_ID()` (`MySqlProvider.cs:90`) | `SELECT last_insert_rowid()` (`SqliteProvider.cs:77`) |
| Upsert | `MERGE INTO … WHEN MATCHED THEN UPDATE … WHEN NOT MATCHED THEN INSERT` (`SqlServerProvider.cs:90-93`) | `INSERT … ON CONFLICT (…) DO UPDATE SET …` (`PostgresProvider.cs:101-102`) | `INSERT … ON DUPLICATE KEY UPDATE …` (`MySqlProvider.cs:113-114`) | `INSERT … ON CONFLICT(…) DO UPDATE SET …` (`SqliteProvider.cs:100-101`) |

**ORM / Data Layer:**
- No Entity Framework, Dapper, or other third-party ORM
- The library is itself a thin ADO.NET wrapper on `System.Data.Common` (`DbConnection`, `DbCommand`, `DbParameter`) — see `DBTools/Abstractions/IDbProvider.cs`
- LINQ-to-SQL translation is hand-rolled in `DBTools/Linq/DbExpressionTranslator.cs`, `DBTools/Linq/DbQueryProvider.cs`, `DBTools/Linq/DbQuery.cs`, `DBTools/Linq/JoinQuery.cs`, `DBTools/Linq/JoinResult.cs`
- EF-style change tracking is re-implemented in `DBTools/Context/{DbContext,DbSet,ChangeTracker}.cs`

**File Storage:**
- No cloud blob/file storage integration
- SQLite "database" file is read/written through `Microsoft.Data.Sqlite` (local file path passed in connection string)
- CSV export writes to local filesystem via `System.IO.File` (`DBTools/Export/DataExport.cs`)
- Spreadsheet export (xls/xlsx) uses `NPOI` 2.7.3 (`DBTools/DBTools.csproj:48`)

**Caching:**
- None. No `IDistributedCache`, `IMemoryCache`, or Redis/Memcached integration
- Listed in README Roadmap (`README.md:1399`) as a future enhancement

## Authentication & Identity

- **No identity provider** (no Auth0, no Azure AD, no IdentityServer, no JWT/OIDC integration)
- Database authentication is delegated to the underlying provider's connection-string credentials (`Uid`, `Password` keys in `config.json`)
- `AuditInterceptor` (`DBTools/Interceptors/AuditInterceptor.cs:11-64`) accepts a caller-supplied `Func<string>` to derive a "current user" string for audit columns — but does not authenticate the user itself

## Message Brokers

- None. No RabbitMQ, Kafka, Service Bus, Redis pub/sub, NATS, or other broker client packages referenced

## Search / Indexing

- None. No Elasticsearch, Lucene, or full-text search wrapper

## Observability / Monitoring

**Error Tracking:**
- None. No Sentry, Application Insights, OpenTelemetry, or Datadog SDK referenced in either csproj
- Errors are surfaced as `_error` strings on `SqlClient`/`AsyncSqlClient` (e.g. `DBTools/Core/AsyncSqlClient.cs:34`) and via interceptor callbacks (`DBTools/Abstractions/IQueryInterceptor.cs`)

**Logging:**
- No `Microsoft.Extensions.Logging` / `ILogger<T>` integration. `grep` for `using Microsoft.Extensions.Logging` returns no hits under `DBTools/`
- `DBTools/Interceptors/LoggingInterceptor.cs` accepts a caller-supplied `Action<string>` log sink (`DBTools/Interceptors/LoggingInterceptor.cs:11-49`) — caller wires up `Console.WriteLine` or any other sink they want. Parameter values are off by default (`logParameters = false`)
- Interceptors fire on three lifecycle hooks: `BeforeExecute`, `AfterExecute`, `OnError` (`DBTools/Abstractions/IQueryInterceptor.cs`)

**Tracing:**
- `System.Diagnostics.Stopwatch` is used internally to time queries (`DBTools/Core/AsyncSqlClient.cs:264, 367, 412`); the duration is exposed via `QueryInterceptionContext.Duration` for interceptor consumers
- No `Activity`/`ActivitySource` (OpenTelemetry) integration

## CI/CD & Deployment

**Hosting:**
- None. The library is not hosted; consumers bring their own host (Console, ASP.NET, worker, etc.)
- No Dockerfile for the library itself (only a `docker-compose.yml` for spinning up test infrastructure)

**CI Pipeline (GitHub Actions):**
- `.github/workflows/ci.yml` — `actions/checkout@v4`, `actions/setup-dotnet@v4` (`dotnet-version: 8.0.x`), `docker compose up` for the integration matrix, `dotnet restore` / `dotnet build` / `dotnet test`
- `.github/workflows/release.yml` — same checkout + setup, then `dotnet pack`, `actions/upload-artifact@v4`, `dotnet nuget push` to GitHub Packages (uses `${{ secrets.GITHUB_TOKEN }}`), `softprops/action-gh-release@v2`

**Package Registry:**
- GitHub Packages (`https://nuget.pkg.github.com/gednt/index.json`) — see `nuget.config.github-packages.example:18` and `.github/workflows/release.yml:84-87`
- Authenticated via the workflow's `GITHUB_TOKEN`; consumer-side authentication uses a PAT with `read:packages` scope (`README.md:59-67`)
- nuget.org is the dev/consumer fallback (`nuget.config:5`)

## Containerized Test Infrastructure

**`docker-compose.yml` (root):**
- Service `sqlserver` — `mcr.microsoft.com/mssql/server:2022-latest`, exposes `1433:1433`, `ACCEPT_EULA=Y`, `MSSQL_SA_PASSWORD=TestStrong!Passw0rd` (test-only credential), healthcheck via `sqlcmd`
- Service `sqlserver-setup` — same image; runs `scripts/sqlserver-integration-setup.sql` to create `testDB` and a `testUser` login with `Integration!123` password and `sysadmin` role
- Orchestrated by `.github/workflows/ci.yml:84-85` and `.github/workflows/ci.yml:142-145`

**Testcontainers fallback (`DBToolsUnitTest/IntegrationTestBase.cs:193-235`):**
- Uses `Testcontainers.MsSql.MsSqlBuilder` (from `Testcontainers.MsSql` 4.4.0) to start `mcr.microsoft.com/mssql/server:2022-latest` with port binding `1433:1433` when Docker Compose is unavailable
- Bootstraps `testDB` and `testUser` via the SA connection string returned by `testcontainer.GetConnectionString()`
- Lifecycle strategy (`DBToolsUnitTest/IntegrationTestBase.cs:105-148`): (1) existing DB at `127.0.0.1:1433` → (2) `docker compose up` → (3) Testcontainers

**PostgreSQL/MySQL/SQLite integration testing:**
- Not yet wired up in `docker-compose.yml` or `IntegrationTestBase.cs`. `README.md:1402` marks "Testcontainers-based integration tests for multi-provider CI" as completed in the roadmap, but the actual `docker-compose.yml` only defines the SQL Server container
- The `Testcontainers.PostgreSql`, `Testcontainers.MySql`, and `Testcontainers.MsSql` modules would need to be added to `DBToolsUnitTest.csproj` for additional provider matrices

## Environment Configuration

**Required env vars (consumers):** None — configuration is JSON, not env-var-driven. Credentials live in `config.json`, which is `.gitignore`d except for `config.json.example`.

**Secrets location:**
- Local: `DBTools/config.json` and `DBToolsUnitTest/config.json` (gitignored)
- CI: `docker-compose.yml` defines test credentials inline (`TestStrong!Passw0rd`, `Integration!123`) — these are not real secrets
- Release: `${{ secrets.GITHUB_TOKEN }}` is the only secret used by `.github/workflows/release.yml`
- `README.md:1079` recommends environment variables or a secret manager (Azure Key Vault) for production deployments; the library does not read env vars itself
- `nuget.config.github-packages.example` documents the PAT (with `read:packages` scope) consumers must supply; the actual PAT is supplied via the `dotnet nuget add source … --password …` command — never committed

**Configuration schema (`DBTools/config.json.example` and `DBToolsUnitTest/config.json.example`):**
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
Parsed by `DBTools/Core/DbConfiguration.cs` via `Microsoft.Extensions.Configuration.Json` (bound to the `IDbConfiguration` interface in `DBTools/Abstractions/IDbConfiguration.cs`)

## Webhooks & Callbacks

**Incoming:** None.

**Outgoing:** None.

The only "callback" surface is in-process query interception (`IQueryInterceptor` / `IAsyncQueryInterceptor`), which is internal to the consumer process — not an HTTP/webhook mechanism.

---

*Integration audit: 2026-06-21*
