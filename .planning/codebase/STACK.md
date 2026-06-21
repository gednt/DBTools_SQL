---
generated: 2026-06-21
last_mapped_commit: f9cc45c3b85d4d7645b1a3a5deef92ed3bc9b6c7
---

# Technology Stack

**Analysis Date:** 2026-06-21

## Languages

**Primary:**
- C# `latest` (LangVersion) — used across both projects in `DBTools.sln`

**Secondary:** None.

## Runtime

**Environment:**
- .NET 8.0 (`net8.0` — `<TargetFramework>net8.0</TargetFramework>` in `DBTools/DBTools.csproj` and `DBToolsUnitTest/DBToolsUnitTest.csproj`)

**Package Manager:**
- NuGet (via `dotnet` SDK)
- Lockfile: not committed (no `packages.lock.json`); restore relies on floating versions within the pinned set
- Source feeds configured in `nuget.config` (clears defaults; adds only `https://api.nuget.org/v3/index.json`)
- Optional GitHub Packages feed documented in `nuget.config.github-packages.example` (source `https://nuget.pkg.github.com/gednt/index.json` with `packageSourceMapping` restricting the `DBTools` package id to GitHub)

**SDK:**
- Pinned by CI via `actions/setup-dotnet@v4` with `dotnet-version: 8.0.x` (`.github/workflows/ci.yml`, `.github/workflows/release.yml`)
- No `global.json` is committed, so the SDK rolls with whatever `8.0.x` GitHub Actions installs.

## Frameworks

**Core Project Type:**
- .NET 8 class library (`Microsoft.NET.Sdk`) packaged as a NuGet package — `DBTools/DBTools.csproj` (`<IsPackable>true</IsPackable>`, `<IsToolPackage>false</IsToolPackage>`)
- Targets `net8.0`, `LangVersion=latest`, `Nullable=disable`, `Deterministic=true`, `GenerateAssemblyInfo=false`
- Package metadata: `PackageId=DBTools`, `Version=1.4.0`, MIT license, owned by "DMF Software" (2016-2026)
- Authoritative assembly identity is hand-pinned in `DBTools/Properties/AssemblyInfo.cs` (`[assembly: AssemblyVersion("1.4.0.0")]`)

**Hosting / UI Framework:** None. The library is consumed by other apps; it does not ship an executable, ASP.NET host, WPF/WinForms UI, or worker service.

**Test Project:**
- MSTest v3 (`Microsoft.NET.Test.Sdk` 17.12.0, `MSTest.TestAdapter` 3.7.0, `MSTest.TestFramework` 3.7.0) — `DBToolsUnitTest/DBToolsUnitTest.csproj`
- Code coverage via `coverlet.collector` 6.0.2 (configured with `PrivateAssets=all`)
- Container orchestration for integration tests via `Testcontainers` 4.4.0 + `Testcontainers.MsSql` 4.4.0

## Key Dependencies

**`DBTools` project (`DBTools/DBTools.csproj`):**

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.Bcl.AsyncInterfaces` | 10.0.1 | Polyfill for `IAsyncDisposable` / async interfaces on the net8.0 surface |
| `Microsoft.Data.SqlClient` | 5.2.2 | SQL Server ADO.NET provider (hard dep — used by `SqlServerProvider`, `BulkOperations` SqlBulkCopy, and `SqlQueryBuilder`) |
| `Microsoft.Extensions.Configuration` | 10.0.1 | Configuration framework root |
| `Microsoft.Extensions.Configuration.Abstractions` | 10.0.1 | `IConfiguration` abstractions |
| `Microsoft.Extensions.Configuration.FileExtensions` | 10.0.1 | File-based configuration providers |
| `Microsoft.Extensions.Configuration.Json` | 10.0.1 | JSON provider that backs `DBTools.Core.DbConfiguration` reading `config.json` |
| `Microsoft.Extensions.DependencyInjection` | 10.0.1 | DI container used by `Configuration/ServiceCollectionExtensions.cs` (`AddDbTools`) |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.1 | DI abstractions |
| `Microsoft.Extensions.FileProviders.Abstractions` | 10.0.1 | File-provider abstractions |
| `Microsoft.Extensions.FileProviders.Physical` | 10.0.1 | Physical file-system provider |
| `Microsoft.Extensions.FileSystemGlobbing` | 10.0.1 | Glob support for file providers |
| `Microsoft.Extensions.Primitives` | 10.0.1 | `StringValues` / change-token primitives |
| `NPOI` | 2.7.3 | Spreadsheet (xls/xlsx) support for `DBTools.Export` |
| `Portable.BouncyCastle` | 1.9.0 | Crypto support (used alongside SharpZipLib for export compression/encryption helpers) |
| `SharpZipLib` | 1.4.2 | Zip compression used by export utilities |
| `System.Configuration.ConfigurationManager` | 10.0.1 | Legacy `System.Configuration.ConfigurationManager` interop (carried in addition to the new `Microsoft.Extensions.Configuration` stack) |

**Optional (consumer-side) dependencies — referenced by `Type.GetType(..., "<Assembly>")` but NOT in the package's `<PackageReference>` list:**

| Provider | Consumer package required | Notes |
|----------|---------------------------|-------|
| PostgreSQL | `Npgsql` (latest) | Loaded reflectively by `DBTools/Providers/PostgresProvider.cs:23`; throws `InvalidOperationException("Npgsql package is not available…")` if absent |
| MySQL | `MySqlConnector` (preferred) or `MySql.Data` (fallback) | Probed in this order by `DBTools/Providers/MySqlProvider.cs:23-24` |
| SQLite | `Microsoft.Data.Sqlite` | Loaded by `DBTools/Providers/SqliteProvider.cs:22` |

The README at `README.md:83-101` instructs consumers to install these directly so the library does not pay for unused providers.

**`DBToolsUnitTest` project (`DBToolsUnitTest/DBToolsUnitTest.csproj`):**

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.NET.Test.Sdk` | 17.12.0 | Test host / VSTest runtime |
| `MSTest.TestAdapter` | 3.7.0 | MSTest v3 adapter discovery |
| `MSTest.TestFramework` | 3.7.0 | `[TestClass]`, `[TestMethod]`, `Assert`, `[TestCategory("Integration")]` |
| `coverlet.collector` | 6.0.2 | Coverage data collector (private assets) |
| `Testcontainers` | 4.4.0 | Docker container orchestration for integration tests (fallback path) |
| `Testcontainers.MsSql` | 4.4.0 | Pre-configured `MsSqlBuilder` used by `DBToolsUnitTest/IntegrationTestBase.cs:199-203` |
| `ProjectReference` | `..\DBTools\DBTools.csproj` | The library under test |

## Configuration

**Project files:**
- `DBTools/DBTools.csproj` — main library, sets TargetFramework, LangVersion, deterministic builds, NuGet metadata
- `DBToolsUnitTest/DBToolsUnitTest.csproj` — MSTest project, sets `IsTestProject=true`, references `Microsoft.NET.Test.Sdk`
- No `Directory.Build.props`, no `Directory.Build.targets`, no `.editorconfig` are committed

**Runtime configuration:**
- `DBTools/config.json` is copied to the output directory (`CopyToOutputDirectory=Always` in `DBTools/DBTools.csproj:55-57`); same for `DBToolsUnitTest/config.json` in `DBToolsUnitTest/DBToolsUnitTest.csproj:32-34`
- `DBTools/config.json.example` documents the schema consumed by `DBTools.Core.DbConfiguration`: `Provider`, `Host`, `Database`, `Uid`, `Password`, `Port`
- `nuget.config` — single feed (`https://api.nuget.org/v3/index.json`); the GitHub Packages example is shipped as `nuget.config.github-packages.example`

**Build configuration:**
- `Deterministic=true` on both projects; CI verifies byte-identical Release rebuilds (`.github/workflows/ci.yml:38-47`)
- Release builds additionally pass `-p:ContinuousIntegrationBuild=true` (`.github/workflows/release.yml:41`) for source-link determinism
- `GenerateAssemblyInfo=false` — version metadata is hand-written in `DBTools/Properties/AssemblyInfo.cs` and `DBToolsUnitTest/Properties/AssemblyInfo.cs`
- `RootNamespace=DBTools` / `DBToolsUnitTest` per csproj

**Package metadata (`DBTools/DBTools.csproj:14-32`):**
- `PackageId=DBTools`, `Version=1.4.0`, `PackageLicenseExpression=MIT`
- `PackageTags`: `sql-server;postgresql;mysql;sqlite;database;orm;crud;linq;query-builder;multi-provider;dbtools;dapper;ado.net`
- `RepositoryUrl=https://github.com/gednt/DBTools_SQL`, `PackageReadmeFile=README.md` (linked from repo root)
- `IncludeSymbols=false` (no symbol/snupkg published; the README explains that .nupkg zip metadata is non-deterministic even when the embedded assembly is deterministic)

## Platform Requirements

**Development:**
- .NET 8 SDK (CI pins `8.0.x`); the library targets `net8.0`
- Docker (for integration tests via Docker Compose or the Testcontainers fallback in `DBToolsUnitTest/IntegrationTestBase.cs`)
- `mcr.microsoft.com/mssql/server:2022-latest` — the SQL Server 2022 image used by `docker-compose.yml` and Testcontainers
- `sqlcmd` client inside the SQL Server container for the bootstrap script
- `gh` CLI / GitHub Actions runner for CI/CD; no third-party CI service

**Production:**
- The library runs on any environment that supports .NET 8 (`net8.0` TFM); no specific hosting target — it is consumed by caller apps
- Database backend selected at runtime via `Provider` in `config.json` (`SqlServer`, `PostgreSQL`, `MySQL`, `SQLite`); consumers must install the corresponding ADO.NET package

## Static Analysis / Linting / Formatting

- **None configured.** No `.editorconfig`, no `dotnet format` config, no StyleCop/FxCop analyzers, no Roslyn analyzer packages in either csproj
- The `.gitignore` retains StyleCop and Coverlet sections (`StyleCopReport.xml`, `coverage*.json/xml/info`) but no related tooling is wired into the build
- Test categorization (`[TestCategory("Integration")]`) is the only project-level quality gate, enforced by the CI filter `--filter "TestCategory!=Integration"` on the fast path and `--filter "TestCategory=Integration"` on the integration path

## CI/CD Toolchain

- **CI:** `.github/workflows/ci.yml` — runs on `push`/`pull_request` to `dotnet-core`, `main`, `master`, `develop`
  - `unit-tests` job — restore, build Release, deterministic-build check, run all non-Integration tests
  - `integration-tests` job — same build + `docker compose up -d sqlserver --wait`, `sqlserver-setup` bootstrap, wait-loop for SQL readiness, run `--filter "TestCategory=Integration"`
  - `all-tests` job — same plus full test run; only on `push` to `main` or `refs/tags/v*`
- **Release:** `.github/workflows/release.yml` — triggers on `v*` tag push or `workflow_dispatch`
  - Packs with `dotnet pack DBTools/DBTools.csproj -c Release -o ./artifacts`
  - Publishes to GitHub Packages via `dotnet nuget push` with `${{ secrets.GITHUB_TOKEN }}` (URL `https://nuget.pkg.github.com/${{ github.repository_owner }}/index.json`)
  - Uploads `artifacts/*.nupkg` and creates a GitHub Release via `softprops/action-gh-release@v2`
- **No third-party CI service** (no AppVeyor, no Travis, no Azure Pipelines)

## Build Artifacts and Outputs

- `DBTools/bin/{Debug,Release}/net8.0/` — `DBTools.dll`, `DBTools.xml` (XML doc), `config.json`, `README.md` (linked)
- `artifacts/DBTools.1.4.0.nupkg` — produced by `dotnet pack` and uploaded by the release workflow
- `artifacts/` is gitignored at the repo root (`/home/felipecoelhosilva/Repos/DBTools_SQL/.gitignore:64`)
- `**/config.json` is gitignored (only `**/config.json.example` is tracked)

---

*Stack analysis: 2026-06-21*
