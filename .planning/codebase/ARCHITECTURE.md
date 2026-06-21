---
generated: 2026-06-21
last_mapped_commit: f9cc45c3b85d4d7645b1a3a5deef92ed3bc9b6c7
focus: arch
---

# Architecture

**Analysis Date:** 2026-06-21

## System Overview

DBTools_SQL is a **.NET 8 class library** (NuGet package `DBTools`, v1.4.0) that provides a multi-provider data-access facade over `System.Data.Common`. The architecture follows a **Layered + Strategy + Provider-Plugin** pattern: a thin set of abstract contracts (`DBTools.Abstractions`) sits between three consumer-facing surfaces (sync `SqlClient`, async `AsyncSqlClient`, and a DbContext-style API in `DBTools.Context`) and a family of pluggable `IDbProvider` implementations (`SqlServerProvider`, `PostgresProvider`, `MySqlProvider`, `SqliteProvider`).

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│                       Consumer Surfaces (Public API)                        │
│                                                                             │
│  ┌──────────────────────┐  ┌──────────────────────┐  ┌───────────────────┐  │
│  │  SqlClient (sync)    │  │  AsyncSqlClient      │  │  DbContext /      │  │
│  │  `DBTools/Core/      │  │  `DBTools/Core/      │  │  DbSet /          │  │
│  │   SqlClient.cs`      │  │   AsyncSqlClient.cs` │  │  ChangeTracker    │  │
│  │                      │  │                      │  │  `DBTools/Context/`│  │
│  └──────────┬───────────┘  └──────────┬───────────┘  └─────────┬─────────┘  │
│             │  ISqlClient              │  IAsyncSqlClient      │            │
│             │                          │  + IQueryInterceptor  │ uses       │
└─────────────┼──────────────────────────┼────────────────────────┼────────────┘
              │                          │                        │
              ▼                          ▼                        ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│               Controllers (high-level, type-safe APIs)                      │
│  `DBTools/Controllers/`                                                     │
│   LinqHelper<TModel>, Linq<TModel>, AsyncLinqHelper<TModel>,               │
│   DBToolsController, DataExportController                                   │
└──────────────────────────────────┬──────────────────────────────────────────┘
                                   │ uses
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                  LINQ Infrastructure (expression → SQL)                     │
│  `DBTools/Linq/`                                                           │
│   DbQuery<T>, DbQueryProvider, DbExpressionTranslator,                     │
│   JoinQuery<TLeft,TRight>, JoinResult<TLeft,TRight>                        │
└──────────────────────────────────┬──────────────────────────────────────────┘
                                   │ calls SqlClient.SelectRaw
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                   Core Services (cross-cutting helpers)                    │
│  `DBTools/Core/`                                                            │
│   DBTools (legacy base), SqlQueryBuilder, SqlValidator,                     │
│   DbToolsTransaction (DbTransaction)                                        │
└──────────────────────────────────┬──────────────────────────────────────────┘
                                   │ depends on
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│       Configuration & Cross-Cutting                                        │
│  `DBTools/Configuration/` — ServiceCollectionExtensions, DbToolsOptions     │
│  `DBTools/Interceptors/` — Logging / Audit / SoftDelete                     │
│  `DBTools/Mapping/` — EntityMapping, FluentConfiguration, Attributes        │
└──────────────────────────────────┬──────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│       Abstractions (contracts the rest depend on)                           │
│  `DBTools/Abstractions/`                                                    │
│   IDbProvider, ISqlClient, IAsyncSqlClient, IDBTools, IDbConfiguration,     │
│   ISqlQueryBuilder, ISqlValidator, IDbTransaction, IQueryInterceptor       │
└──────────────────────────────────┬──────────────────────────────────────────┘
                                   │ implemented by
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│       Providers (pluggable dialect implementations)                         │
│  `DBTools/Providers/`                                                       │
│   SqlServerProvider (hard dep on Microsoft.Data.SqlClient)                  │
│   PostgresProvider  (reflection-loaded Npgsql)                              │
│   MySqlProvider     (reflection-loaded MySqlConnector / MySql.Data)         │
│   SqliteProvider    (reflection-loaded Microsoft.Data.Sqlite)               │
│   DbProviderFactory  (switch on enum/string → provider)                     │
└──────────────────────────────────┬──────────────────────────────────────────┘
                                   │ via System.Data.Common
                                   ▼
                            ┌─────────────────┐
                            │   SQL Database  │
                            └─────────────────┘
```

## Component Responsibilities

| Component | Responsibility | File |
|-----------|----------------|------|
| `SqlClient` | Primary sync CRUD facade (SELECT/INSERT/UPDATE/DELETE) + query-building from objects. Parameterized WHERE; identifier validation. | `DBTools/Core/SqlClient.cs` |
| `AsyncSqlClient` | Async CRUD with `CancellationToken`, optional interceptor pipeline, transactions, scalar/read/raw queries. | `DBTools/Core/AsyncSqlClient.cs` |
| `DBTools` (legacy base) | Mutable connection state container (`Host`/`Uid`/`Password`/…) and the first-generation `SqlExecuteQuery` / `RetrieveDataSql` / `RetrieveObjectSql` APIs that build on `_provider`. | `DBTools/Core/DBTools.cs` |
| `SqlQueryBuilder` | Build SELECT/INSERT/UPDATE/DELETE SQL strings + `DbParameter` lists; delegates identifier safety to `SqlValidator`. | `DBTools/Core/SqlQueryBuilder.cs` |
| `SqlValidator` | Regex-based identifier validator; rejects comment markers and dangerous keywords. | `DBTools/Core/SqlValidator.cs` |
| `DbToolsTransaction` | Wraps `DbConnection` + `DbTransaction` with both sync and async commit/rollback; auto-rollback on dispose. | `DBTools/Core/DbTransaction.cs` |
| `IDbProvider` (+ 4 impls + factory) | Database dialect abstraction: connection/command/parameter creation, identifier quoting, paging clause, last-inserted-id SQL, upsert SQL. | `DBTools/Abstractions/IDbProvider.cs`, `DBTools/Providers/*` |
| `DbProviderFactory` | Static factory: enum → `IDbProvider`, or case-insensitive string (`"sqlserver"`, `"postgresql"`, `"postgres"`, `"mysql"`, `"sqlite"`). | `DBTools/Providers/DbProviderFactory.cs` |
| `DbConfiguration` | Loads `config.json` via `Microsoft.Extensions.Configuration`; builds dialect-specific connection strings. | `DBTools/Core/DbConfiguration.cs` |
| `ServiceCollectionExtensions` | `AddDbTools(opts => …)` — DI registration of `IAsyncSqlClient` (scoped), `ISqlValidator`/`ISqlQueryBuilder`/`IDbProvider` (singletons). | `DBTools/Configuration/ServiceCollectionExtensions.cs` |
| `DbToolsOptions` | Strongly-typed options bag; carries provider enum + interceptor lists; builds connection strings via a switch expression. | `DBTools/Configuration/DbToolsOptions.cs` |
| `LinqHelper<TModel>` | LINQ-lambda WHERE predicates over `SqlClient.Select`; CRUD on POCOs via reflection-mapped columns; `InsertRange` runs inside a transaction. | `DBTools/Controllers/LinqHelper.cs` |
| `Linq<TModel>` | Extends `LinqHelper<TModel>` with property-selector queries (`WhereEquals`, `WhereContains`, `WhereBetween`, `WhereIn`, …), JOINs, and `AsQueryable()` returning `DbQuery<T>`. | `DBTools/Controllers/Linq.cs` |
| `AsyncLinqHelper<TModel>` | Async variant of `LinqHelper` that uses `AsyncSqlClient` + `EntityMappingResolver` for attribute-based mapping. | `DBTools/Controllers/AsyncLinqHelper.cs` |
| `DbQuery<T>` / `DbQueryProvider` | `IQueryable<T>` over `SqlClient.SelectRaw`; delegates translation to `DbExpressionTranslator`. | `DBTools/Linq/DbQuery.cs`, `DBTools/Linq/DbQueryProvider.cs` |
| `DbExpressionTranslator` | Walks expression trees (Where/OrderBy/Skip/Take/Count/Any/First/Single/Join) and accumulates SQL fragments + parameters. | `DBTools/Linq/DbExpressionTranslator.cs` |
| `JoinQuery<TLeft,TRight>` / `JoinResult<TLeft,TRight>` | LINQ-style INNER/LEFT JOIN queries; row result is `{ Left, Right }`. | `DBTools/Linq/JoinQuery.cs`, `DBTools/Linq/JoinResult.cs` |
| `DbContext` / `DbSet<T>` / `ChangeTracker` | EF-style unit-of-work; `SaveChangesAsync` flushes tracked entries inside a transaction using `EntityMapping`. | `DBTools/Context/*.cs` |
| `EntityMapping` / `EntityMappingResolver` / `EntityBuilder` / Attributes | Table/column/PK resolution from `[Table]`/`[Column]`/`[Key]`/etc. or fluent `EntityBuilder`. Cached in a `ConcurrentDictionary`. | `DBTools/Mapping/*` |
| `BulkOperations<TEntity>` | High-throughput inserts: `SqlBulkCopy` for SQL Server, batched INSERTs for others; uses `EntityMapping`. | `DBTools/Bulk/BulkOperations.cs` |
| `IQueryInterceptor` / `IAsyncQueryInterceptor` + 3 impls | Cross-cutting hooks (`BeforeExecute`/`AfterExecute`/`OnError`); built-ins: `LoggingInterceptor`, `AuditInterceptor`, `SoftDeleteInterceptor`. | `DBTools/Abstractions/IQueryInterceptor.cs`, `DBTools/Interceptors/*` |
| `DataExport` | CSV serialization of `List<GenericObject>` and CSV → `DataTable` round-trip. | `DBTools/Export/DataExport.cs` |
| `GenericObject` / `GenericObject_Simple` | Plain data carriers: columns/values/valuesString/types + optional `ISqlClient` for `Insert()`/`Update()`. | `DBTools/Models/*` |

## Pattern Overview

**Overall:** Layered library with Strategy (provider), Unit-of-Work (`DbContext`/`ChangeTracker`), Decorator-like interceptors, and Fluent Builder (`EntityBuilder` / `ModelBuilder`).

**Key Characteristics:**

- **Provider Strategy.** `IDbProvider` is the seam; the rest of the library never references `Microsoft.Data.SqlClient`/`Npgsql`/etc. directly. Non-SQL-Server providers are loaded via `Type.GetType("Npgsql.NpgsqlConnection, Npgsql")` so consumers must reference the provider package themselves.
- **Abstraction-first DI.** All public surfaces bind through interfaces (`ISqlClient`, `IAsyncSqlClient`, `IDbProvider`, `ISqlValidator`, `ISqlQueryBuilder`, `IDbConfiguration`, `IDbTransaction`). The DI extension method is the preferred composition root.
- **Sync + Async duality.** A `SqlClient` and an `AsyncSqlClient` exist as separate classes with parallel method shapes; both delegate to the same `SqlQueryBuilder`/`SqlValidator` and the same provider. `DbToolsTransaction` exposes both commit/rollback paths so either surface can share a transaction.
- **LINQ as expression visitor, not IQueryable-as-DB.** `DbQueryProvider` does not generate EF-style navigation SQL; it calls `SqlClient.SelectRaw` with translated SQL + parameters. Joins are explicit (`InnerJoin`/`LeftJoin`) — implicit navigation property joins are not supported.
- **Layered security.** Two layers protect against SQL injection: every public CRUD method builds `@paramN` placeholders; every table/column identifier is validated by `SqlValidator` (regex + keyword blocklist). `SqlValidator` rejects identifiers containing `--`, `;--`, `/*`, `*/`, or `DROP`/`DELETE`/`TRUNCATE`/`INSERT`/`UPDATE`/`EXEC`/`EXECUTE`/`ALTER`/`CREATE`/`GRANT`/`REVOKE`/`MERGE`.
- **Configuration-driven.** `DbConfiguration` reads `config.json` from the working directory at construction time; missing keys throw `InvalidOperationException`. The `Provider` key selects the dialect.

## Layers

**Abstractions (`DBTools/Abstractions/`):**
- Purpose: Define all contracts the rest of the library depends on.
- Location: `DBTools/Abstractions/`
- Contains: `IDbProvider`, `ISqlClient`, `IAsyncSqlClient`, `IDBTools`, `IDbConfiguration`, `ISqlQueryBuilder`, `ISqlValidator`, `IDbTransaction`, `IQueryInterceptor` + `IAsyncQueryInterceptor` + `QueryInterceptionContext` + `QueryOperationType`.
- Depends on: `System.Data.Common`, `System.Threading`, `System.Threading.Tasks`.
- Used by: every other layer.

**Providers (`DBTools/Providers/`):**
- Purpose: Concrete dialect implementations of `IDbProvider` and the factory.
- Location: `DBTools/Providers/`
- Contains: `SqlServerProvider`, `PostgresProvider`, `MySqlProvider`, `SqliteProvider`, `DbProviderFactory`.
- Depends on: `DBTools.Abstractions` and (for SQL Server only) `Microsoft.Data.SqlClient`. Others load via reflection.
- Used by: `SqlClient`, `AsyncSqlClient`, `DbContext`, `BulkOperations`, `ServiceCollectionExtensions`.

**Core (`DBTools/Core/`):**
- Purpose: Concrete implementations of the runtime data-access APIs and shared helpers.
- Location: `DBTools/Core/`
- Contains: `DBTools` (legacy base), `SqlClient`, `AsyncSqlClient`, `DbConfiguration`, `SqlQueryBuilder`, `SqlValidator`, `DbTransaction` (the concrete `DbToolsTransaction`).
- Depends on: `DBTools.Abstractions`, `DBTools.Providers`, `DBTools.Models`.
- Used by: Controllers, Linq infrastructure, consumers via DI.

**Controllers (`DBTools/Controllers/`):**
- Purpose: Higher-level, type-safe APIs (LINQ, JOINs, async LINQ) layered on top of `SqlClient` / `AsyncSqlClient`.
- Location: `DBTools/Controllers/`
- Contains: `LinqHelper<TModel>`, `Linq<TModel>`, `AsyncLinqHelper<TModel>`, `DBToolsController` (legacy), `DataExportController` (stub).
- Depends on: `DBTools.Core`, `DBTools.Linq`, `DBTools.Abstractions`, `DBTools.Mapping`, `DBTools.Models`.
- Used by: end-user consumer code; entry point for ORM-style usage.

**Linq (`DBTools/Linq/`):**
- Purpose: `IQueryable`/`IQueryProvider` infrastructure that translates LINQ expression trees into SQL fragments.
- Location: `DBTools/Linq/`
- Contains: `DbQuery<T>`, `DbQueryProvider`, `DbExpressionTranslator` (+ internal `JoinClause`/`TranslationResult`), `JoinQuery<TLeft,TRight>` (+ `JoinQueryProvider`), `JoinResult<TLeft,TRight>`.
- Depends on: `DBTools.Core`, `System.Linq.Expressions`.
- Used by: `Linq<TModel>.AsQueryable()`, `JoinQuery` returned by `Linq<TModel>.InnerJoin`/`LeftJoin`.

**Models (`DBTools/Models/`):**
- Purpose: Plain DTOs used as carriers between layers (not entity classes — those are user-defined).
- Location: `DBTools/Models/`
- Contains: `GenericObject` (columns/values/valuesString/types/table + `Insert()`/`Update()`), `GenericObject_Simple` (column/value/type triple).
- Used by: `SqlClient.QueryBuilder`, `DataExport`, end-user code that builds values from arbitrary objects.

**Configuration (`DBTools/Configuration/`):**
- Purpose: Microsoft.Extensions.DependencyInjection integration.
- Location: `DBTools/Configuration/`
- Contains: `DbToolsOptions` (POCO + `DatabaseProvider` enum), `ServiceCollectionExtensions.AddDbTools(...)` (two overloads), internal `OptionsDbConfiguration` adapter.
- Depends on: `Microsoft.Extensions.DependencyInjection`, `DBTools.Abstractions`, `DBTools.Core`, `DBTools.Providers`.
- Used by: consumer `Program.cs` / `Startup.cs` to wire the library into an app.

**Mapping (`DBTools/Mapping/`):**
- Purpose: Resolve entity → table/column metadata for attribute- or fluent-configured models.
- Location: `DBTools/Mapping/`
- Contains: `EntityMapping` + `PropertyMapping` + `DatabaseGeneratedOption`, `Attributes.cs` (`[Table]`, `[Column]`, `[Key]`, `[NotMapped]`, `[DatabaseGenerated]`, `[MaxLength]`, `[Required]`, `[ForeignKey]`, `[ConcurrencyCheck]`, `[Timestamp]`, `[DefaultValue]`, `[QueryFilter]`), `FluentConfiguration.cs` (`IEntityConfiguration<T>`, `IEntityConfiguration`, `EntityBuilder<T>`, `PropertyBuilder`), `EntityMappingResolver` (cached, thread-safe).
- Used by: `DbContext`, `DbSet<T>`, `BulkOperations<TEntity>`, `AsyncLinqHelper<T>`.

**Context (`DBTools/Context/`):**
- Purpose: EF-style unit-of-work facade with change tracking and `DbSet<T>` queryables.
- Location: `DBTools/Context/`
- Contains: `DbContext` (abstract) + `ModelBuilder` + `InlineEntityConfiguration` + `EntityTypeConfigurationFromBuilder`, `DbSet<TEntity>`, `ChangeTracker` + `ChangeTrackerEntry` + `EntityState`.
- Depends on: `DBTools.Core`, `DBTools.Mapping`, `DBTools.Configuration`, `DBTools.Abstractions`.
- Used by: consumer code that prefers EF ergonomics over `SqlClient`.

**Interceptors (`DBTools/Interceptors/`):**
- Purpose: Built-in `IQueryInterceptor` implementations for cross-cutting concerns.
- Location: `DBTools/Interceptors/`
- Contains: `LoggingInterceptor`, `AuditInterceptor`, `SoftDeleteInterceptor`.
- Depends on: `DBTools.Abstractions`.
- Used by: registered via `DbToolsOptions.AddInterceptor(...)` and applied inside `AsyncSqlClient`.

**Bulk (`DBTools/Bulk/`):**
- Purpose: High-throughput bulk insert/update.
- Location: `DBTools/Bulk/`
- Contains: `BulkOperations<TEntity>`.
- Depends on: `Microsoft.Data.SqlClient` (for `SqlBulkCopy`), `DBTools.Mapping`, `DBTools.Providers`.

**Export (`DBTools/Export/`):**
- Purpose: CSV serialization / `DataTable` conversion.
- Location: `DBTools/Export/`
- Contains: `DataExport`.
- Depends on: `DBTools.Models`.
- Used by: end-user code that wants CSV export of `List<GenericObject>`.

**Properties (`DBTools/Properties/`):**
- Purpose: Assembly metadata.
- Location: `DBTools/Properties/`
- Contains: `AssemblyInfo.cs` (pinned `1.4.0.0`), `Settings.Designer.cs` (legacy settings).

## Data Flow

### Primary request path — parameterized SELECT

1. **Entry point.** Caller invokes `SqlClient.Select(fields, table, whereClause, parameters)` at `DBTools/Core/SqlClient.cs:271`.
2. **Identifier validation.** `SqlClient.IsValidIdentifier` delegates to `SqlValidator.IsValidIdentifier` (regex + dangerous-keyword check) at `DBTools/Core/SqlClient.cs:68-71` → `DBTools/Core/SqlValidator.cs:16-29`.
3. **Parameter construction.** For each `parameters[i]`, `_provider.CreateParameter("@param" + i, value ?? DBNull.Value)` — `SqlServerProvider.CreateParameter` returns a `SqlParameter` (`DBTools/Providers/SqlServerProvider.cs:29-32`).
4. **SQL assembly.** The query is `String.Format("SELECT {0} FROM {1} WHERE {2}", fields, table, whereClause)` at `DBTools/Core/SqlClient.cs:293`.
5. **Execution.** `RetrieveDataSql(query)` opens a `DbConnection` via `_provider.CreateConnection(ConnectionString)`, binds `SqlParameters`, runs `ExecuteReader()`, and loads results into a `DataTable.DefaultView` at `DBTools/Core/DBTools.cs:415-459`.
6. **Result returned** as `DataView` to the caller.

### Async CRUD with interceptors (INSERT example)

1. Caller invokes `AsyncSqlClient.InsertAsync(fields, table, values, primaryKeyName, autoIncrement, ct)` at `DBTools/Core/AsyncSqlClient.cs:168`.
2. `SqlValidator` validates `table` and field identifiers.
3. Auto-increment PK removal strips the PK field from `fields`/`values` at lines 178-187.
4. SQL built inline: `INSERT INTO {table}({fieldList}) VALUES({@param0,@param1,...})`.
5. `ExecuteNonQueryAsync(sql, values, QueryOperationType.Insert, table, ct)` (lines 399-440) is called.
6. **Interceptor pipeline:**
   - `RunBeforeInterceptorsAsync` invokes every `IQueryInterceptor.BeforeExecute` and `IAsyncQueryInterceptor.BeforeExecuteAsync` (lines 452-458). Built-ins: `LoggingInterceptor` prints `[DBTools] Executing Insert: …`, `AuditInterceptor` stamps `Properties["AuditUser"]`/`["AuditTimestamp"]`, `SoftDeleteInterceptor` leaves Insert unchanged.
   - If `context.IsSuppressed == true` (e.g., a future caching interceptor), execution short-circuits.
7. **Physical execution.** `_provider.CreateConnection(ConnectionString)` → `OpenAsync(ct)` → `connection.CreateCommand()` → `cmd.CommandText = context.Sql` → `AddParameters(cmd, context.Parameters)` (uses `_provider.CreateParameter`, so each provider gets its own `DbParameter` subclass) → `ExecuteNonQueryAsync(ct)`.
8. **Interceptor after:** `context.Duration` and `context.RowsAffected` populated; `RunAfterInterceptorsAsync` fires.
9. On exception, `_error = ex.ToString()`; `RunErrorInterceptorsAsync` fires; method returns `false` and an empty `DataTable` for reads.

### LINQ translation (`Linq<TModel>.AsQueryable().Where(...).ToList()`)

1. `Linq<TModel>.AsQueryable()` returns a new `DbQuery<TModel>` backed by `DbQueryProvider` (`DBTools/Linq/DbQuery.cs:15-72`).
2. `.Where(predicate)` is intercepted by `DbQueryProvider.CreateQuery<T>(expression)` (`DBTools/Linq/DbQueryProvider.cs:52-64`), which constructs a new `DbQuery<T>` carrying the augmented expression tree.
3. `.ToList()` triggers `GetEnumerator()` → `DbQueryProvider.Execute<T>(expression)` (lines 71-…).
4. A `DbExpressionTranslator(tableName, "t0")` (`DBTools/Linq/DbExpressionTranslator.cs:48`) walks the expression tree (Where/OrderBy/Skip/Take/Count/Any/First/Single/GroupBy), producing `TranslationResult { SelectClause, FromClause, WhereClause, OrderByClause, SkipCount, TakeCount, Parameters, IsCountQuery, IsAnyQuery, … }`.
5. The translator emits SQL like `SELECT t0.* FROM Users t0 WHERE t0.Age > @param0 ORDER BY t0.Name OFFSET 10 ROWS FETCH NEXT 5 ROWS ONLY` (for SQL Server; Postgres/SQLite/MySQL get dialect-appropriate paging via the provider's `BuildPagingClause`).
6. `SqlClient.SelectRaw(sql, parameters)` runs it and returns a `DataView`.
7. `DbQueryProvider` maps the `DataView` rows back into `TModel` instances using reflection on properties.

### JOIN flow

1. `Linq<TModel>.InnerJoin<TRight>(rightTable, leftKey, rightKey)` at `DBTools/Controllers/Linq.cs` (around line 670) constructs a `JoinQueryProvider<TLeft,TRight>` with the table names, key column names, alias `t0`/`t1`, and join type.
2. `.Where(j => j.Left.Age > 18)` is translated by the same `DbExpressionTranslator`, which now also walks `JoinResult<,>` member accesses to emit `t0.Age` vs `t1.Total`.
3. `JoinQueryProvider.ExecuteJoinSequence` produces SQL like `SELECT t0.*, t1.* FROM Users t0 INNER JOIN Orders t1 ON t0.Id = t1.UserId WHERE t0.Age > @param0`, runs it via `SqlClient.SelectRaw`, and materializes each row into `JoinResult<TLeft,TRight>`.

### Transactions

1. `AsyncSqlClient.BeginTransaction()` / `BeginTransactionAsync(ct)` opens a `DbConnection` and starts a `DbTransaction` (`DBTools/Core/AsyncSqlClient.cs:98-115`).
2. Wrapped in `DbToolsTransaction` which holds both the `DbConnection` and the underlying `DbTransaction` (`DBTools/Core/DbTransaction.cs:20-35`).
3. Transaction-aware methods (`SelectAsync(DbToolsTransaction, ...)`, `ExecuteInTransactionAsync`) bind `cmd.Transaction = transaction.UnderlyingTransaction` and reuse the open connection.
4. `Commit`/`Rollback` set `_isActive = false`; `Dispose`/`DisposeAsync` auto-rollback if still active (`DBTools/Core/DbTransaction.cs:77-105`).

**State Management:**
- No global mutable state. Each `SqlClient` / `AsyncSqlClient` owns its `IDbConfiguration`/`IDbProvider`/`ISqlValidator`/`ISqlQueryBuilder` and is safe to instantiate per-request when registered `AddScoped`.
- `EntityMappingResolver` uses a `ConcurrentDictionary<Type, EntityMapping>` cache (`DBTools/Mapping/EntityMappingResolver.cs:15-16`); first lookup reflects, subsequent lookups are O(1).
- `DbContext.ChangeTracker` keeps tracked entries in a `List<ChangeTrackerEntry>` (`DBTools/Context/ChangeTracker.cs:14`).

## Key Abstractions

**`IDbProvider`:**
- Purpose: Per-dialect strategy. The single seam that makes the library provider-agnostic.
- Examples: `DBTools/Providers/SqlServerProvider.cs`, `PostgresProvider.cs`, `MySqlProvider.cs`, `SqliteProvider.cs`.
- Pattern: Strategy (one concrete impl per dialect) instantiated by `DbProviderFactory.Create(provider)` from a `DatabaseProvider` enum or string.

**`ISqlClient` / `IAsyncSqlClient`:**
- Purpose: Surface CRUD + `QueryBuilder` for consumers.
- Examples: `DBTools/Abstractions/ISqlClient.cs`, `DBTools/Abstractions/IAsyncSqlClient.cs`.
- Pattern: Interface Segregation; consumers bind against the interface and DI fills the concrete `SqlClient` / `AsyncSqlClient`.

**`ISqlValidator`:**
- Purpose: Single point where SQL identifier safety is enforced.
- Examples: `DBTools/Core/SqlValidator.cs` (regex + keyword blocklist).
- Pattern: Strategy + cross-cutting security gate; called by `SqlClient`, `AsyncSqlClient`, and `SqlQueryBuilder` before any identifier touches SQL.

**`ISqlQueryBuilder`:**
- Purpose: Build the actual SQL strings + parameter lists.
- Examples: `DBTools/Core/SqlQueryBuilder.cs`.
- Pattern: Builder. Pure string-generation (no I/O).

**`IDbConfiguration`:**
- Purpose: Read-only bag of connection settings (Host/Database/Uid/Password/Port/ConnectionString/Provider).
- Examples: `DBTools/Core/DbConfiguration.cs` (loads from `config.json`), internal `OptionsDbConfiguration` (`DBTools/Configuration/ServiceCollectionExtensions.cs:100-120`, reads from `DbToolsOptions`).
- Pattern: Configuration object. Two implementations — file-based and options-based.

**`IDbTransaction` (`DbToolsTransaction`):**
- Purpose: Cross-cutting transaction handle usable from both sync and async code paths.
- Examples: `DBTools/Core/DbTransaction.cs`.
- Pattern: Facade over `System.Data.Common.DbTransaction` with sync+async commit/rollback and auto-rollback disposal.

**`IQueryInterceptor` / `IAsyncQueryInterceptor`:**
- Purpose: Three-method cross-cutting hook (Before/After/OnError) into async CRUD.
- Examples: `DBTools/Interceptors/LoggingInterceptor.cs`, `AuditInterceptor.cs`, `SoftDeleteInterceptor.cs`.
- Pattern: Chain-of-Responsibility. Registered on `DbToolsOptions`, applied at construction inside `AsyncSqlClient`, fired in registration order. `SoftDeleteInterceptor` doubles as a SQL-rewriter (converts `DELETE FROM X WHERE …` → `UPDATE X SET IsDeleted=1 WHERE …`).

**`DbQuery<T>` / `JoinQuery<TLeft,TRight>`:**
- Purpose: IQueryable surfaces that defer SQL execution.
- Examples: `DBTools/Linq/DbQuery.cs`, `JoinQuery.cs`.
- Pattern: Iterator + Expression Visitor (via `DbExpressionTranslator`).

**`EntityMapping` / `EntityBuilder<T>`:**
- Purpose: Entity-to-table mapping metadata, attribute-based or fluent.
- Examples: `DBTools/Mapping/EntityMapping.cs`, `Attributes.cs`, `FluentConfiguration.cs`, `EntityMappingResolver.cs`.
- Pattern: Fluent Builder + Metadata cache.

## Entry Points

**Library composition root (recommended):**
- Location: `DBTools/Configuration/ServiceCollectionExtensions.cs:22-77` — `services.AddDbTools(opts => …)`.
- Triggers: Consumer `Program.cs` / `Startup.cs` calling `AddDbTools`.
- Responsibilities: Registers `DbToolsOptions` (singleton), `ISqlValidator` + `ISqlQueryBuilder` + `IDbProvider` + `IDbConfiguration` (singletons), `IAsyncSqlClient` + `AsyncSqlClient` (scoped), `SqlClient` (transient, for back-compat). Also wires registered interceptors into each new `AsyncSqlClient`.

**Default constructor entry points:**
- `SqlClient()` at `DBTools/Core/SqlClient.cs:23` — instantiates `DbConfiguration` (reads `config.json`), `SqlValidator`, `SqlQueryBuilder`; defaults to `SqlServerProvider`.
- `AsyncSqlClient()` at `DBTools/Core/AsyncSqlClient.cs:39` — same defaults.
- `LinqHelper<TModel>(tableName, pk, autoIncrement)` at `DBTools/Controllers/LinqHelper.cs:46` — internally calls `new SqlClient()`.
- `Linq<TModel>(tableName, pk, autoIncrement)` at `DBTools/Controllers/Linq.cs:27` — extends `LinqHelper<TModel>`.

**DI override entry points:**
- `SqlClient(IDbConfiguration, ISqlValidator, ISqlQueryBuilder)` at `DBTools/Core/SqlClient.cs:37` — override validator/builder while keeping default provider.
- `SqlClient(IDbConfiguration, ISqlValidator, ISqlQueryBuilder, IDbProvider)` at `DBTools/Core/SqlClient.cs:51` — full override.
- `AsyncSqlClient(IDbConfiguration, ISqlValidator, ISqlQueryBuilder, IDbProvider)` at `DBTools/Core/AsyncSqlClient.cs:51`.
- `AsyncSqlClient(string connectionString, IDbProvider)` at `DBTools/Core/AsyncSqlClient.cs:63`.

**Public surface exposed to consumers (no `Main` — this is a library):**
- `DBTools.Core.SqlClient`, `DBTools.Core.AsyncSqlClient`, `DBTools.Core.DbToolsTransaction`, `DBTools.Core.SqlQueryBuilder`, `DBTools.Core.SqlValidator`, `DBTools.Core.DbConfiguration`
- `DBTools.Abstractions.ISqlClient`, `IAsyncSqlClient`, `IDbProvider`, `ISqlValidator`, `ISqlQueryBuilder`, `IDbConfiguration`, `IDbTransaction`, `IDBTools`, `IQueryInterceptor`, `IAsyncQueryInterceptor`
- `DBTools.Providers.DbProviderFactory` (+ the four `XxxProvider` concrete types)
- `DBTools.Configuration.DbToolsOptions`, `DatabaseProvider` enum, `ServiceCollectionExtensions.AddDbTools`
- `DBTools.Controllers.LinqHelper<TModel>`, `Linq<TModel>`, `AsyncLinqHelper<TModel>`
- `DBTools.Linq.DbQuery<T>`, `JoinQuery<TLeft,TRight>`, `JoinResult<TLeft,TRight>`
- `DBTools.Context.DbContext`, `DbSet<TEntity>`, `ChangeTracker`, `ModelBuilder`, `EntityBuilder<T>`, `EntityState`, `ChangeTrackerEntry`
- `DBTools.Mapping.EntityMapping`, `EntityMappingResolver`, plus the attributes in `Attributes.cs`
- `DBTools.Bulk.BulkOperations<TEntity>`
- `DBTools.Interceptors.LoggingInterceptor`, `AuditInterceptor`, `SoftDeleteInterceptor`
- `DBTools.Models.GenericObject`, `GenericObject_Simple`
- `DBTools.Export.DataExport`

## Architectural Constraints

- **Threading:** Library code is mostly synchronous and not internally thread-safe per instance — `SqlClient` exposes mutable state on its `DBTools` base (`Host`, `Uid`, etc. are settable; `SqlParameters` is a settable property). The DI extension registers `SqlClient` as transient and `AsyncSqlClient` as scoped, so each request gets its own. `EntityMappingResolver` is the one shared cache and it uses `ConcurrentDictionary`. `DbQueryProvider.Execute` does not synchronize enumeration.
- **Global state:** `EntityMappingResolver._cache` and `_fluentConfigs` are process-wide. Fluent configs must be registered before the first `Resolve<T>()` call (see `EntityMappingResolver.Register` which invalidates the cache on registration).
- **Circular imports:** None observed. The dependency graph flows downward: `Abstractions` → `Providers` / `Mapping` → `Core` → `Controllers` / `Linq` → `Context`. `Controllers` reference `Core` and `Linq`; `Context` references `Core` + `Mapping`. There is one internal `Core.SqlQueryBuilder` that `using Microsoft.Data.SqlClient;` (line 2) — the comment in the source explicitly acknowledges that for non-SQL-Server providers, callers should use the `IDbProvider.CreateParameter()` overload (`DBTools/Core/SqlQueryBuilder.cs:170-181`).
- **Provider package optionality:** Only SQL Server is a hard package dependency (`<PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.2" />` in `DBTools/DBTools.csproj:37`). Postgres/MySQL/SQLite providers use `Type.GetType(..., "<AssemblyName>")` reflection and throw `InvalidOperationException("… package is not available. Install …")` when the consumer hasn't referenced the right package — see `DBTools/Providers/PostgresProvider.cs:25-28` for the canonical message.
- **Determinism:** `DBTools.csproj` sets `<Deterministic>true</Deterministic>` and pins `AssemblyVersion`/`FileVersion` to `1.4.0.0` in `DBTools/Properties/AssemblyInfo.cs:34-35` (no `*` wildcards).
- **Nullable:** `<Nullable>disable</Nullable>` — the codebase uses null-tolerance annotations, not nullable reference types. Annotations use triple-slash XML docs rather than `?` markers.

## Anti-Patterns

### String concatenation for SQL fragments (legacy methods)

**What happens:** The non-parameterized `Update(fields, table, values, condition)` overload (no `whereParameters`) at `DBTools/Core/SqlClient.cs:362-410` and the static `SqlQueryBuilder.UpdateQuery` at `DBTools/Core/SqlQueryBuilder.cs:86-152` build the SET clause via string concatenation of `processedValues[cont]` with manual `'` escaping and `null` literal substitution. `Delete_Query` and the parameterless `Update_Query` likewise emit raw strings.

**Why it's wrong:** This is the exact pattern `SqlValidator` is meant to defend against. The static `Insert_Query`/`Update_Query`/`Delete_Query`/`Select_Query` helpers (`DBTools/Core/SqlClient.cs:584-622`) and the non-parameterized `Update` overload are still public for back-compat — they invite callers to bypass parameterized paths.

**Do this instead:** Use the parameterized overloads: `Update(fields, table, values, whereClause, whereParameters)` (`DBTools/Core/SqlClient.cs:421-476`) or `AsyncSqlClient.UpdateAsync(...)` (`DBTools/Core/AsyncSqlClient.cs:200-223`). Reserve `ISqlQueryBuilder` for raw SQL inspection/testing, not for runtime execution.

### Auto-generated connection string assumes SQL Server

**What happens:** `DBTools/Core/DBTools.cs:184-202` `ConnectionString.get` falls back to a hard-coded SQL Server format `"Data Source=tcp:{Host},{Port};Initial Catalog={Database};User ID={Uid};Password={Password};TrustServerCertificate=True;"` when `_connectionString` is null. The XML doc comment at lines 173-177 acknowledges this: *"Users of non-SQL Server providers should set this property directly with a provider-appropriate connection string rather than relying on auto-generation."*

**Why it's wrong:** A consumer who swaps in `PostgresProvider` via the parameterless `new SqlClient()` will silently get a SQL Server connection string. The provider only matters once the connection is actually opened, and `Npgsql.NpgsqlConnection` will reject the SQL Server string.

**Do this instead:** Use `DbConfiguration` (which dispatches on `Provider`) or construct `SqlClient(IDbConfiguration, ISqlValidator, ISqlQueryBuilder, IDbProvider)` and either set `ConnectionString` explicitly or rely on `DbToolsOptions.BuildConnectionString()` (which switches on `DatabaseProvider`, `DBTools/Configuration/DbToolsOptions.cs:94-101`).

### Synchronous I/O in `AsyncSqlClient`

**What happens:** `DbContext.SaveChanges()` at `DBTools/Context/DbContext.cs:107-110` is `SaveChangesAsync().GetAwaiter().GetResult()` — a synchronous-over-async bridge. Similarly `LinqHelper<TModel>.InsertRange` (`DBTools/Controllers/LinqHelper.cs:104-152`) uses `cmd.ExecuteNonQuery()` inside a sync `BeginTransaction()`. `DbToolsTransaction.BeginTransaction()` (non-async) at `DBTools/Core/AsyncSqlClient.cs:98-104` blocks on `connection.Open()`.

**Why it's wrong:** Blocks the calling thread; can deadlock under synchronization contexts.

**Do this instead:** Use `BeginTransactionAsync(ct)` + `ExecuteInTransactionAsync`/`SelectAsync(transaction, …)` (`DBTools/Core/AsyncSqlClient.cs:191-348`). For unit-of-work, prefer `await dbContext.SaveChangesAsync(ct)`.

### Reflection-driven provider instantiation hides hard dependencies

**What happens:** `PostgresProvider`, `MySqlProvider`, `SqliteProvider` all do `Type.GetType("X.Y, Assembly")` (`DBTools/Providers/PostgresProvider.cs:23-47`, `MySqlProvider.cs:23-53`, `SqliteProvider.cs:22-43`). At runtime, if the package isn't referenced, the call throws `InvalidOperationException`.

**Why it's wrong:** The library's own tests can't exercise these providers without Testcontainers/Docker. The error surfaces at first connection, not at startup, making failure diagnosis harder.

**Do this instead:** Acceptable as a deliberate packaging trade-off (avoid forcing transitive dependencies). Document the requirement loudly (already done in README). For new provider work, consider an interface-only package split (`DBTools.Postgres`, `DBTools.MySQL`, `DBTools.Sqlite`) so the type lookup can be replaced with a compile-time reference.

## Error Handling

**Strategy:** `DBTools` base sets a mutable `Error` string property; methods return `bool` or `DataView`/`List<GenericObject>` and let callers check `db.Error`. `AsyncSqlClient` exposes `Error` as a `string` property too but the async methods `return false / new DataTable()` on failure rather than throwing — exceptions are surfaced through the interceptor pipeline (`OnError`/`OnErrorAsync`) instead.

**Patterns:**
- **Validation throws `ArgumentException`/`ArgumentNullException`** — all `SqlClient`/`AsyncSqlClient` public methods validate arguments before touching I/O (`SqlClient.cs:273-281`, `SqlClient.cs:364-380`, `AsyncSqlClient.cs:126-131`, `AsyncSqlClient.cs:170-176`).
- **Identifier validation throws** via `SqlValidator.IsValidIdentifier` — no silent fallback (`SqlValidator.cs:16-29`).
- **I/O failures are caught** in `DBTools.RetrieveDataSql`/`SqlExecuteQuery` (`DBTools/Core/DBTools.cs:385-406`, `415-459`) and stored in `Error`. The exception type narrowing is `catch (DbException e)` for `DbException` and a generic `catch (Exception e)` for the outer scope — a non-DbException around connection creation can mask real configuration errors.
- **`AsyncSqlClient`** logs `_error = ex.ToString()` and continues; readers return `new DataTable()`, writers return `false`. Combined with the interceptor pipeline, this means callers must opt-in to exception semantics via a custom interceptor.
- **`DbToolsTransaction`** enforces `InvalidOperationException` on `Commit`/`Rollback` after disposal (lines 71-75) and auto-rolls-back on `Dispose` if still active (lines 82-86, 98-100).
- **`GenericObject`** throws `InvalidOperationException` when `Insert()`/`Update()` are called without a client injected (`DBTools/Models/GenericObject.cs:78-86`).

## Cross-Cutting Concerns

**Logging:** No logging framework is wired in. The `LoggingInterceptor` (`DBTools/Interceptors/LoggingInterceptor.cs`) takes an `Action<string>` delegate — callers plug in `Console.WriteLine`, `ILogger.LogInformation`, NLog, Serilog, etc. Logging is opt-in: register on `DbToolsOptions` via `AddInterceptor(new LoggingInterceptor(Console.WriteLine))`. The interceptor prints `[DBTools] Executing {Operation}: {Sql}` before and `[DBTools] Completed {Operation} in {ms}ms | Rows: {n}` after.

**Validation:** Centralized in `SqlValidator` (`DBTools/Core/SqlValidator.cs`). Two layers of regex:
- `IdentifierRegex = ^[\w\.\[\]\,\s\*\(\)]+$` — allowlist for the character set (alphanumerics, underscores, dots, brackets, commas, spaces, `*`, parens).
- `DangerousKeywordRegex` — blocklist of `DROP|DELETE|TRUNCATE|INSERT|UPDATE|EXEC|EXECUTE|ALTER|CREATE|GRANT|REVOKE|MERGE`.
- Comment markers `--`, `;--`, `/*`, `*/` are explicitly rejected.

Called from every public `SqlClient`/`AsyncSqlClient`/`SqlQueryBuilder` method that accepts an identifier.

**Authentication:** No built-in auth. Credentials are read from `config.json` (file mode) or `DbToolsOptions` (DI mode). The library is agnostic to credential stores — consumers compose with Azure Key Vault / env vars / secret managers before constructing `DbConfiguration`. `OPTIONS_FOR_SQL_SERVER_DEV` (TrustServerCertificate=True) defaults to true to ease local development (`DBTools/Configuration/DbToolsOptions.cs:51`).

**Transactions:** Single class `DbToolsTransaction` (`DBTools/Core/DbTransaction.cs`) wrapping a `DbConnection` + `DbTransaction`. Both sync (`BeginTransaction()`/`Commit`/`Rollback`) and async (`BeginTransactionAsync(ct)`/`CommitAsync(ct)`/`RollbackAsync(ct)`) entry points on `AsyncSqlClient`. `LinqHelper.InsertRange` (`DBTools/Controllers/LinqHelper.cs:124-151`) and `DbContext.SaveChangesAsync` (`DBTools/Context/DbContext.cs:121-153`) both open their own connections and run their batched statements inside a `DbTransaction` for atomicity.

**Mapping:** `DBTools.Mapping.EntityMappingResolver` resolves table/column metadata from `[Table]`/`[Column]`/`[Key]`/`[NotMapped]`/`[DatabaseGenerated]` attributes or from `EntityBuilder<T>` fluent configuration registered via `EntityMappingResolver.Register<T>(configuration)`. Cache invalidates on fluent registration. Used by `DbSet<T>`, `BulkOperations<T>`, `AsyncLinqHelper<T>`.

**Change tracking:** `DBTools.Context.ChangeTracker` (`DBTools/Context/ChangeTracker.cs`) — opt-in tracking: entities added via `DbSet.Add/AddRange/Update/Remove` or `DbContext.Attach` enter the tracker; `DbContext.SaveChangesAsync` flushes pending entries in a single transaction (`DBTools/Context/DbContext.cs:107-154`). Supports `EntityState { Added, Modified, Deleted, Unchanged, Detached }`.

**Bulk operations:** `DBTools.Bulk.BulkOperations<TEntity>` (`DBTools/Bulk/BulkOperations.cs`) — `SqlBulkCopy` for SQL Server (typed directly, hard dependency), batched INSERTs wrapped in a transaction for other providers.

---

*Architecture analysis: 2026-06-21*