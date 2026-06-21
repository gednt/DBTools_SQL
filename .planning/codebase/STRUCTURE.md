---
generated: 2026-06-21
last_mapped_commit: f9cc45c3b85d4d7645b1a3a5deef92ed3bc9b6c7
focus: arch
---

# Codebase Structure

**Analysis Date:** 2026-06-21

## Directory Layout

```
DBTools_SQL/                                   # repository root
├── DBTools.sln                                # VS solution (2 projects)
├── README.md                                  # NuGet package readme
├── LICENSE                                    # MIT
├── nuget.config                               # local feed config
├── nuget.config.github-packages.example       # template for GitHub Packages auth
├── docker-compose.yml                         # SQL Server for integration tests
├── docs/                                      # API_REFERENCE.md, EXAMPLES.md (consumer-facing)
├── scripts/                                   # build/release helpers
├── .github/                                   # CI workflows (ci.yml, etc.)
├── .planning/                                 # GSD planning artifacts (incl. this file)
├── .agents/                                   # agent configs
├── .idea/                                     # Rider IDE metadata (gitignored? — committed)
├── DBTools/                                   # ★ main library project
│   ├── DBTools.csproj                         # .NET 8, Microsoft.NET.Sdk, IsPackable=true
│   ├── config.json.example                    # template consumed by DbConfiguration
│   ├── config.json                            # (CopyToOutputDirectory=Always; gitignored)
│   ├── Abstractions/                          # interface contracts
│   ├── Bulk/                                  # SqlBulkCopy + batched INSERT
│   ├── Configuration/                         # DI: AddDbTools, DbToolsOptions
│   ├── Context/                               # EF-style DbContext/DbSet/ChangeTracker
│   ├── Controllers/                           # LinqHelper, Linq, AsyncLinqHelper, DBToolsController
│   ├── Core/                                  # SqlClient, AsyncSqlClient, SqlQueryBuilder, SqlValidator, DbToolsTransaction
│   ├── Export/                                # DataExport (CSV <-> DataTable)
│   ├── Interceptors/                          # Logging / Audit / SoftDelete
│   ├── Linq/                                  # DbQuery, DbQueryProvider, DbExpressionTranslator, JoinQuery, JoinResult
│   ├── Mapping/                               # EntityMapping, EntityMappingResolver, EntityBuilder, Attributes
│   ├── Models/                                # GenericObject, GenericObject_Simple
│   ├── Providers/                             # SqlServer / Postgres / MySQL / Sqlite + DbProviderFactory
│   └── Properties/                            # AssemblyInfo.cs (pinned 1.4.0.0), Settings.Designer.cs
└── DBToolsUnitTest/                           # ★ MSTest project (no NuGet packaging)
    ├── DBToolsUnitTest.csproj                 # MSTest + Testcontainers (MsSql)
    ├── config.json.example
    ├── TestBase.cs                            # shared constants/helpers for unit tests
    ├── IntegrationTestBase.cs                 # Docker/Testcontainers orchestration
    ├── TestDocumentation.md                   # manual notes for running tests
    ├── Controllers/                           # DBToolsControllerTests, LinqHelperTests, LinqHelperLinqTests, LinqTests
    ├── Core/                                  # DBToolsTests, SqlClientConnectionTests, SqlClientParameterizedQueryTests, SqlClientQueryBuilderTests, SqlClientValidationTests, QueryBuilderTests, ObsoleteMethodTests, BugRegressionTests
    ├── Export/                                # DataExportTests
    ├── Integration/                           # EdgeCaseTests, WorkflowTests
    ├── Linq/                                  # DbQueryLinqTests
    ├── Models/                                # GenericObjectTests, GenericObjectSimpleTests, FakeSqlClient
    ├── Providers/                             # DbProviderFactoryTests, ProviderDialectTests
    └── Properties/                            # AssemblyInfo.cs
```

## Directory Purposes

**`DBTools/`:**
- Purpose: The shipped library. Single `Microsoft.NET.Sdk` C# project targeting `net8.0`. `RootNamespace=DBTools`, `AssemblyName=DBTools`, version pinned `1.4.0`.
- Contains: All public types organized in subfolders by namespace/role.
- Key files: `DBTools/DBTools.csproj`, `DBTools/Core/SqlClient.cs`, `DBTools/Core/AsyncSqlClient.cs`, `DBTools/Providers/DbProviderFactory.cs`, `DBTools/Configuration/ServiceCollectionExtensions.cs`, `DBTools/Linq/DbExpressionTranslator.cs`.

**`DBTools/Abstractions/`:**
- Purpose: Pure interface contracts — no implementations.
- Contains: 9 interface files (`IDbProvider`, `ISqlClient`, `IAsyncSqlClient`, `IDBTools`, `IDbConfiguration`, `ISqlQueryBuilder`, `ISqlValidator`, `IDbTransaction`, `IQueryInterceptor`+`IAsyncQueryInterceptor`+`QueryInterceptionContext`+`QueryOperationType` in one file).
- Key files: `DBTools/Abstractions/IDbProvider.cs`, `IAsyncSqlClient.cs`, `IQueryInterceptor.cs`.

**`DBTools/Providers/`:**
- Purpose: Concrete `IDbProvider` implementations + the factory.
- Contains: `SqlServerProvider` (hard dep on `Microsoft.Data.SqlClient`), `PostgresProvider` (Npgsql via reflection), `MySqlProvider` (MySqlConnector/MySql.Data via reflection), `SqliteProvider` (Microsoft.Data.Sqlite via reflection), `DbProviderFactory`.
- Key files: `DBTools/Providers/SqlServerProvider.cs`, `DbProviderFactory.cs`.

**`DBTools/Core/`:**
- Purpose: The runtime data-access APIs and shared helpers.
- Contains: `DBTools` (legacy base class), `SqlClient` (sync CRUD), `AsyncSqlClient` (async CRUD + interceptors + transactions), `DbConfiguration` (config.json loader), `SqlQueryBuilder` (string SQL generation), `SqlValidator` (identifier regex), `DbToolsTransaction` (concrete `IDbTransaction`).
- Key files: `DBTools/Core/SqlClient.cs`, `AsyncSqlClient.cs`, `DbTransaction.cs`.

**`DBTools/Configuration/`:**
- Purpose: Microsoft.Extensions.DependencyInjection wiring.
- Contains: `ServiceCollectionExtensions` (two `AddDbTools` overloads + internal `OptionsDbConfiguration` adapter), `DbToolsOptions` (POCO + `DatabaseProvider` enum).
- Key files: `DBTools/Configuration/ServiceCollectionExtensions.cs`.

**`DBTools/Controllers/`:**
- Purpose: Higher-level type-safe APIs layered on `SqlClient` / `AsyncSqlClient`.
- Contains: `LinqHelper<TModel>` (LINQ lambdas + CRUD), `Linq<TModel>` (property selectors + JOINs + `AsQueryable()`), `AsyncLinqHelper<TModel>` (async variant using `EntityMappingResolver`), `DBToolsController` (legacy), `DataExportController` (empty stub).
- Key files: `DBTools/Controllers/LinqHelper.cs`, `Linq.cs`, `AsyncLinqHelper.cs`.

**`DBTools/Linq/`:**
- Purpose: IQueryable / IQueryProvider infrastructure for LINQ → SQL translation.
- Contains: `DbQuery<T>`, `DbQueryProvider`, `DbExpressionTranslator` (with internal `JoinClause`/`TranslationResult` types), `JoinQuery<TLeft,TRight>` (+ `JoinQueryProvider<TLeft,TRight>`), `JoinResult<TLeft,TRight>`.
- Key files: `DBTools/Linq/DbExpressionTranslator.cs`, `DbQueryProvider.cs`.

**`DBTools/Context/`:**
- Purpose: EF-style unit-of-work facade.
- Contains: `DbContext` (abstract) + `ModelBuilder` + inline configuration helpers, `DbSet<TEntity>`, `ChangeTracker` + `ChangeTrackerEntry` + `EntityState`.
- Key files: `DBTools/Context/DbContext.cs`, `DbSet.cs`, `ChangeTracker.cs`.

**`DBTools/Mapping/`:**
- Purpose: Entity ↔ table metadata resolution (attribute-based and fluent).
- Contains: `EntityMapping` + `PropertyMapping` + `DatabaseGeneratedOption` enum, `Attributes.cs` (12 attribute types), `FluentConfiguration.cs` (`IEntityConfiguration<T>`, `EntityBuilder<T>`, `PropertyBuilder`), `EntityMappingResolver` (cached, thread-safe).
- Key files: `DBTools/Mapping/EntityMappingResolver.cs`, `Attributes.cs`, `FluentConfiguration.cs`.

**`DBTools/Interceptors/`:**
- Purpose: Built-in `IQueryInterceptor` implementations.
- Contains: `LoggingInterceptor`, `AuditInterceptor`, `SoftDeleteInterceptor`.
- Key files: `DBTools/Interceptors/LoggingInterceptor.cs`, `SoftDeleteInterceptor.cs`.

**`DBTools/Bulk/`:**
- Purpose: High-throughput bulk operations.
- Contains: `BulkOperations<TEntity>`.
- Key files: `DBTools/Bulk/BulkOperations.cs`.

**`DBTools/Export/`:**
- Purpose: CSV serialization / `DataTable` conversion.
- Contains: `DataExport` (static type map + `ToCsv` + `ToDataTable`).
- Key files: `DBTools/Export/DataExport.cs`.

**`DBTools/Models/`:**
- Purpose: Plain DTOs used as cross-layer carriers.
- Contains: `GenericObject` (columns/values/valuesString/types/table + `Insert()`/`Update()` with injectable `ISqlClient`), `GenericObject_Simple` (column/value/type triple).
- Key files: `DBTools/Models/GenericObject.cs`.

**`DBTools/Properties/`:**
- Purpose: Assembly metadata.
- Contains: `AssemblyInfo.cs` (pinned version `1.4.0.0`, GUID `39d6736a-…`), `Settings.Designer.cs` (legacy auto-generated settings designer file).

**`DBToolsUnitTest/`:**
- Purpose: MSTest unit + integration tests for `DBTools`. Not packaged.
- Contains: `TestBase.cs` (shared helpers), `IntegrationTestBase.cs` (Docker/Testcontainers), per-feature folders mirroring `DBTools/`'s namespace layout (`Controllers/`, `Core/`, `Export/`, `Integration/`, `Linq/`, `Models/`, `Providers/`).
- Key files: `DBToolsUnitTest/IntegrationTestBase.cs`, `DBToolsUnitTest/Core/BugRegressionTests.cs`, `DBToolsUnitTest/Providers/ProviderDialectTests.cs`.

**`docs/`:**
- Purpose: Consumer-facing long-form docs.
- Contains: `API_REFERENCE.md`, `EXAMPLES.md`. Linked from the main `README.md`.
- Generated: No — hand-authored Markdown.

**`scripts/`:**
- Purpose: Build/release helpers (e.g., publishing the `.nupkg` to GitHub Packages).
- Generated: No.

**`.github/`:**
- Purpose: GitHub Actions workflows (CI, release). `ci.yml` runs `dotnet build` + `dotnet test` with optional integration tests via Docker/Testcontainers.

**`.planning/`:**
- Purpose: GSD (this toolchain) planning artifacts. This file and its siblings (`CONVENTIONS.md`, `TESTING.md`) live under `.planning/codebase/`. **Do not edit unless by a GSD command.**

## Key File Locations

**Entry points (library — no `Program.cs`, no `Main`):**
- `DBTools/DBTools.csproj` — the package definition (`PackageId=DBTools`, `Version=1.4.0`).
- `DBTools/Configuration/ServiceCollectionExtensions.cs:22-77` — `AddDbTools(Action<DbToolsOptions>)` — recommended composition root.
- `DBTools/Configuration/ServiceCollectionExtensions.cs:82-89` — `AddDbTools(string connectionString)` overload (defaults to SQL Server).
- `DBTools/Core/SqlClient.cs:23-35` — parameterless `SqlClient()` constructor reads `config.json`.
- `DBTools/Core/AsyncSqlClient.cs:39-46` — parameterless `AsyncSqlClient()` constructor.

**Configuration:**
- `DBTools/DBTools.csproj` — project file (`<TargetFramework>net8.0</TargetFramework>`, package metadata, `<Deterministic>true</Deterministic>`, `config.json` CopyToOutputDirectory=Always).
- `DBTools/config.json.example` — template for the file-based configuration.
- `DBTools/config.json` — runtime config (created by copying the `.example`).
- `DBToolsUnitTest/config.json.example` — template for the test project.
- `DBTools/Configuration/DbToolsOptions.cs` — strongly-typed options bag.
- `DBTools/Core/DbConfiguration.cs` — loads `config.json` via `Microsoft.Extensions.Configuration`.
- `nuget.config` — local feed config.
- `nuget.config.github-packages.example` — template for GitHub Packages auth.

**Core Logic:**
- `DBTools/Core/SqlClient.cs` — sync CRUD + query-building.
- `DBTools/Core/AsyncSqlClient.cs` — async CRUD + interceptors + transactions.
- `DBTools/Core/DBTools.cs` — legacy base class with connection-state properties and first-gen query APIs.
- `DBTools/Core/SqlQueryBuilder.cs` — SQL string + parameter generation.
- `DBTools/Core/SqlValidator.cs` — identifier regex + keyword blocklist.
- `DBTools/Core/DbTransaction.cs` — concrete `IDbTransaction` (the type named `DbToolsTransaction`).
- `DBTools/Providers/DbProviderFactory.cs` — provider resolution.
- `DBTools/Linq/DbExpressionTranslator.cs` — LINQ expression tree walker.
- `DBTools/Mapping/EntityMappingResolver.cs` — entity metadata cache.

**Testing:**
- `DBToolsUnitTest/DBToolsUnitTest.csproj` — MSTest + Testcontainers.MsSql.
- `DBToolsUnitTest/TestBase.cs` — base class for unit tests (constants, helpers).
- `DBToolsUnitTest/IntegrationTestBase.cs` — base class for integration tests (Docker orchestration fallback).
- `DBToolsUnitTest/TestDocumentation.md` — manual notes for running tests locally and in CI.
- `docker-compose.yml` — SQL Server container for integration tests.
- `.github/workflows/ci.yml` — CI pipeline (build + test).

## Naming Conventions

**Files:**
- One public type per file, named identically to the type: `SqlClient.cs` → `class SqlClient`.
- Generic type names include the arity as a comment-style suffix (e.g., `DbQueryProvider.cs` contains `DbQueryProvider`, but the generic `DbQuery<TModel>` is also in `DbQuery.cs`).
- Sub-namespaces map 1:1 to subdirectories: `DBTools.Abstractions` ↔ `DBTools/Abstractions/`, `DBTools.Providers` ↔ `DBTools/Providers/`, etc.
- Test files end in `Tests.cs` (`SqlClientConnectionTests.cs`, `LinqHelperTests.cs`).
- Stub/placeholder files exist (`DBTools/Controllers/DataExportController.cs` is empty; `DBTools/Properties/Settings.Designer.cs` is auto-generated).

**Directories:**
- PascalCase, no separators: `Abstractions`, `Providers`, `Core`, `Controllers`, `Configuration`, `Linq`, `Models`, `Interceptors`, `Mapping`, `Context`, `Bulk`, `Export`, `Properties`.
- Plural for collections of implementations (`Providers`, `Interceptors`, `Models`).
- Singular for the abstract core (`Core`).
- Test folders mirror the source folder (`DBToolsUnitTest/Core/`, `DBToolsUnitTest/Controllers/`, `DBToolsUnitTest/Providers/`, etc.).

**Namespaces:**
- Top-level namespace is always `DBTools` (per `RootNamespace` in `.csproj`).
- Sub-namespace matches the folder: `DBTools.Abstractions`, `DBTools.Providers`, `DBTools.Core`, `DBTools.Controllers`, `DBTools.Configuration`, `DBTools.Linq`, `DBTools.Models`, `DBTools.Interceptors`, `DBTools.Mapping`, `DBTools.Context`, `DBTools.Bulk`, `DBTools.Export`.
- `Properties` is NOT its own namespace (its files compile into `DBTools.Properties` by convention but contain only assembly-level attributes).
- One quirk: `DBTools.Linq.JoinResult` lives in `DBTools.Linq`, not `DBTools.Controllers` even though it's returned by `Linq<TModel>.InnerJoin` (which is in `DBTools.Controllers`).

**Types:**
- Interfaces prefixed `I`: `IDbProvider`, `ISqlClient`, `IDbConfiguration`, `ISqlValidator`, `ISqlQueryBuilder`, `IDbTransaction`, `IQueryInterceptor`, `IAsyncQueryInterceptor`, `IEntityConfiguration<T>`, `IDBTools`, `IAsyncSqlClient`.
- Provider implementations suffixed `Provider`: `SqlServerProvider`, `PostgresProvider`, `MySqlProvider`, `SqliteProvider`.
- Generic controllers use `TModel` as the entity placeholder: `LinqHelper<TModel>`, `Linq<TModel>`, `AsyncLinqHelper<TModel>`.
- `DbSet` and `BulkOperations` use `TEntity`: `DbSet<TEntity>`, `BulkOperations<TEntity>`.
- The transaction type is named `DbToolsTransaction` (NOT `DbTransaction`) to avoid collision with `System.Data.Common.DbTransaction`.
- Async counterpart is suffixed `Async`: `AsyncSqlClient`, `AsyncLinqHelper<TModel>`. The interface is `IAsyncSqlClient` (no `Async` suffix on the noun).

**Methods:**
- PascalCase verbs: `Select`, `Insert`, `Update`, `Delete`, `ExecuteQuery`, `GetInBd`, `QueryBuilder`, `BeginTransaction`.
- Async methods suffixed `Async`: `SelectAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`, `ExecuteQueryAsync`, `ExecuteScalarAsync`, `SelectRawAsync`, `BeginTransactionAsync`.
- Property-selector query helpers are named `Where<Op>`: `WhereEquals`, `WhereNotEquals`, `WhereGreaterThan`, `WhereGreaterThanOrEquals`, `WhereLessThan`, `WhereLessThanOrEquals`, `WhereBetween`, `WhereContains`, `WhereStartsWith`, `WhereEndsWith`, `WhereIn`, `WhereNotIn`, `WhereIsNull`, `WhereIsNotNull`, `WhereByExample`.
- LINQ provider hooks on the translator use prefix `Translate`: `TranslateWhere`, `TranslateOrderBy`, `TranslateThenBy`.

**Variables & parameters:**
- camelCase locals, `_camelCase` for private fields (consistent throughout — e.g., `private string _connectionString;`, `private readonly IDbProvider _provider;` in `DBTools/Core/DBTools.cs:32-37`).
- Loop indices are short: `i`, `cont`, `numero` (Spanish for "number", legacy).
- Parameter names use leading underscore for legacy methods on `SqlClient` (`String[] _fields, String _table`) but the newer async methods on `AsyncSqlClient` dropped the underscore (`string[] fields, string table`).

**Connection-string placeholders:**
- `@param0`, `@param1`, … — used in parameterized SQL. The mapping index is always `i` (loop counter) and the placeholder is built as `"@param" + i`.
- The provider abstraction's `BuildUpsertSql` uses `@p0`, `@p1`, … (`parameterPrefix + "p" + i`).

## Where to Add New Code

**New SQL client feature / CRUD operation:**
- Sync surface: Add to `DBTools/Core/SqlClient.cs` (mirror `Select`/`Insert`/`Update`/`Delete` patterns). Add the corresponding interface method to `DBTools/Abstractions/ISqlClient.cs`.
- Async surface: Add to `DBTools/Core/AsyncSqlClient.cs` and `DBTools/Abstractions/IAsyncSqlClient.cs`. Wrap in `Before/After/OnError` interceptor calls.
- Validation: Reuse `_validator.IsValidIdentifier(...)` for any new identifier-typed parameter.

**New SQL helper / static query method:**
- Add to `DBTools/Core/SqlQueryBuilder.cs` (and the corresponding interface in `DBTools/Abstractions/ISqlQueryBuilder.cs`).
- Always run `_validator.IsValidIdentifier` on identifier-typed parameters before string-building.

**New LINQ expression translator case (e.g., support for `GroupBy` or `Select` projections):**
- Add a `case` block in the `VisitMethodCall` switch inside `DBTools/Linq/DbExpressionTranslator.cs:74-…`.
- Update `TranslationResult` (`DBTools/Linq/DbExpressionTranslator.cs:26-42`) if a new SQL fragment is needed.
- For join variants: add a `JoinClause` to the result and implement in `JoinQueryProvider<TLeft,TRight>.ExecuteJoinSequence` (`DBTools/Linq/JoinQuery.cs`).

**New property-based LINQ query method (e.g., `WhereStartsWith`):**
- Add to `DBTools/Controllers/Linq.cs` — they all share the `Select(condition, params)` skeleton and a `GetPropertyName(propertySelector)` helper.
- The base CRUD/SELECT logic lives in `LinqHelper<TModel>` (`DBTools/Controllers/LinqHelper.cs`); no need to touch it for property selectors.

**New entity-mapping attribute (e.g., `[Index]`):**
- Add the attribute class to `DBTools/Mapping/Attributes.cs` (one file holds all attribute types).
- Wire it into `EntityMappingResolver.BuildMapping` (`DBTools/Mapping/EntityMappingResolver.cs:53-…`).
- If fluent equivalents are needed, add to `DBTools/Mapping/FluentConfiguration.cs` (`EntityBuilder<T>` / `PropertyBuilder`).

**New `IDbProvider` (e.g., Oracle):**
- Implement `DBTools/Abstractions/IDbProvider.cs` in a new file `DBTools/Providers/OracleProvider.cs` (model on `DBTools/Providers/SqlServerProvider.cs`).
- Register in `DBTools/Providers/DbProviderFactory.cs` (`Create(DatabaseProvider)` switch + `Create(string)` lowercase switch) — both overloads.
- Add `DatabaseProvider.Oracle` to `DBTools/Configuration/DbToolsOptions.cs` (enum + `BuildConnectionString` switch + `Provider.ToString()` round-trip).
- Update the README provider table.

**New `IQueryInterceptor`:**
- Implement `DBTools/Abstractions/IQueryInterceptor.cs` in a new file under `DBTools/Interceptors/` (model on `LoggingInterceptor.cs`).
- If it needs to mutate SQL, modify `context.Sql` inside `BeforeExecute`. To suppress execution entirely, set `context.IsSuppressed = true`.
- Consumers register via `services.AddDbTools(opts => opts.AddInterceptor(new MyInterceptor()))`.

**New test:**
- Unit test: `DBToolsUnitTest/<NamespaceMirror>/<Subject>Tests.cs` — instantiate directly, no DB needed.
- Integration test (DB required): same path, mark the class or methods with `[TestCategory("Integration")]` so they can be filtered with `dotnet test --filter "TestCategory!=Integration"`.
- For tests that need Docker/Testcontainers: inherit from `DBToolsUnitTest/IntegrationTestBase.cs` (already wired to `Testcontainers.MsSql` per `DBToolsUnitTest/DBToolsUnitTest.csproj:23-24`).
- Use `DBToolsUnitTest/Models/FakeSqlClient.cs` (already in tree) for `ISqlClient` doubles.

**New configuration option (e.g., `MaxPoolSize`):**
- Add the property to `DBTools/Configuration/DbToolsOptions.cs`.
- Add the connection-string fragment to `BuildConnectionString()` in the same file (provider-specific switch).
- If it requires loading from `config.json`, add the key to `DBTools/Core/DbConfiguration.cs` constructors (lines 38-43 and 71-76) and to `DBTools/config.json.example`.

**New DbContext-style capability (e.g., lazy loading):**
- Add to `DBTools/Context/DbContext.cs` (and `DbSet<TEntity>` at `DBTools/Context/DbSet.cs` if it operates on a single entity type).
- All DbSet operations delegate to `_client.SelectAsync`/`InsertAsync`/etc., so prefer extending `AsyncSqlClient` over re-implementing provider calls.

## Special Directories

**`DBTools/Properties/`:**
- Purpose: Assembly metadata and (legacy) app settings.
- Contains: `AssemblyInfo.cs` (pinned `1.4.0.0`), `Settings.Designer.cs` (auto-generated, low value).
- Generated: Partially (`Settings.Designer.cs`); `AssemblyInfo.cs` is hand-edited.
- Committed: Yes.

**`.planning/`:**
- Purpose: GSD planning artifacts (PROJECT.md, ROADMAP.md, codebase/, phases/).
- Contains: This file and its siblings (`CONVENTIONS.md`, `TESTING.md`); future roadmap/phase docs.
- Generated: Yes — by GSD commands (`/gsd-map-codebase`, `/gsd-plan-phase`, etc.).
- Committed: Yes (the GSD workflow expects these in git for state continuity).

**`DBTools/bin/` and `DBTools/obj/`:**
- Purpose: Build outputs and intermediate files.
- Generated: Yes (by `dotnet build`/`dotnet pack`).
- Committed: No — listed in `.gitignore`.

**`DBToolsUnitTest/bin/` and `DBToolsUnitTest/obj/`:**
- Purpose: Test build outputs.
- Generated: Yes.
- Committed: No.

**`artifacts/` (created by `dotnet pack`):**
- Purpose: Output `.nupkg` and `.snupkg` files (per README §"Building the package locally": `dotnet pack DBTools/DBTools.csproj -c Release -o ./artifacts`).
- Generated: Yes.
- Committed: No.

**`DBTools/config.json` and `DBToolsUnitTest/config.json`:**
- Purpose: Runtime/test database connection settings (loaded by `DbConfiguration`).
- Generated: No (hand-created from `*.example`).
- Committed: **NO** — only the `.example` templates are checked in. Listed in `.gitignore`.

---

*Structure analysis: 2026-06-21*