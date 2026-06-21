---
last_mapped_commit: f9cc45c3b85d4d7645b1a3a5deef92ed3bc9b6c7
generated: 2026-06-21
focus: quality
---

# Coding Conventions

**Analysis Date:** 2026-06-21

This document captures the observable coding conventions in the DBTools_SQL codebase — a brownfield C#/.NET 8 multi-provider data-access library. Conventions are described as **observed** patterns, with prescriptive guidance for new code where the convention is consistent across the codebase.

## Language & Runtime

- **Language:** C#, `LangVersion=latest` in both `.csproj` files.
- **Target Framework:** `net8.0` (single TFM).
- **Nullable reference types:** **Disabled** at the project level — `<Nullable>disable</Nullable>` in `DBTools/DBTools.csproj:8` and `DBToolsUnitTest/DBToolsUnitTest.csproj:8`. Nullable annotations are not used; null tolerance is implicit. New code may continue without `?` annotations to match.
- **Implicit `using` directives:** Not enabled. Every file declares its `using` block explicitly (alphabetical System.* / Microsoft.* / DBTools.*).
- **Implicit `var`:** Permitted but not required. The codebase uses `var` heavily for locals (e.g. `var dataExport = new DataExport();`), but explicit types appear for method signatures, public properties, and where clarity matters.

## Brace Style & Layout

- **Braces:** Allman (opening brace on its own line) throughout. See `DBTools/Core/SqlClient.cs:11-15` and `DBToolsUnitTest/Core/DBToolsTests.cs:10-12`.
- **Indentation:** 4 spaces (no tabs).
- **Single-statement `if`:** Braces are always present, even for single statements. From `DBTools/Core/SqlClient.cs:327-330`:
  ```csharp
  if (auto_increment.Equals(true))
  {
      if (!String.IsNullOrEmpty(primary_key_name))
      {
          ...
  ```
- **Field declarations:** One field per line, even when related. See `DBTools/Core/DBTools.cs:17-37` for an example.
- **XML doc comments:** Present on virtually all public types and methods (e.g. `DBTools/Core/SqlValidator.cs`, `DBTools/Models/GenericObject.cs`). Internal/private members generally omit them.
- **Line length:** No strict limit observed; lines commonly exceed 100 characters (e.g. `DBTools/Controllers/Linq.cs:455-460` has 120-char lines).

## Naming Conventions

### Files
- **One type per file**, matching the type name: `SqlClient.cs` → `SqlClient`, `IDbProvider.cs` → `IDbProvider`.
- **Interfaces** prefixed with `I` (e.g. `IDbProvider`, `ISqlValidator`, `IQueryInterceptor`).
- **Attributes** suffixed with `Attribute` (e.g. `TableAttribute`, `ColumnAttribute`) in `DBTools/Mapping/Attributes.cs`.

### Types
- **Classes & Interfaces:** PascalCase. `SqlClient`, `AsyncSqlClient`, `DbQueryProvider`.
- **Generic type parameters:** Single uppercase letter or `T` prefix (`TModel`, `TProperty`, `TEntity`).
- **Enums:** PascalCase members, suffixed with `Option` when wrapping values (`DatabaseGeneratedOption.Identity`).
- **Nested helper types** in source file allowed — `DbExpressionTranslator.cs` declares `JoinClause` and `TranslationResult` in the same file as `DbExpressionTranslator` (lines 13-42).

### Methods & Properties
- **Public methods/properties:** PascalCase. `RetrieveDataSql`, `InsertOrUpdate`, `GetInBdDv`.
- **Async methods:** Suffixed with `Async`. `SelectAsync`, `InsertAsync`, `BeginTransactionAsync`, `DisposeAsync`.
- **Legacy/Java-style methods:** Lowercase camelCase retained for backward compatibility — `setHost`, `getHost`, `setDataBase`, `getQuery`, `connectDB`, `getInBd`, `getInBdDv`, `retrieveDataSql`, `sqlExecuteQuery`. See `DBTools/Core/DBTools.cs:209-258` and `DBTools/Core/SqlClient.cs:182-247`. **Do not introduce new camelCase methods**; new code should use PascalCase. The legacy pair is preserved via wrapper methods (`GetInBd` calls `getInBd`, `SqlClient.cs:212-215`).
- **Local variables & parameters:** camelCase. `autoIncrement`, `primaryKeyName`, `connectionString`, `setClause`.
- **Underscore-prefixed private fields:** Used widely (`_tableName`, `_primaryKeyName`, `_validator`, `_provider`, `_interceptors`). See `DBTools/Controllers/AsyncLinqHelper.cs:23-28`.
- **Hungarian-style underscore for parameters:** Rare; most parameters omit the prefix. `_connectionString` (private) vs `connectionString` (parameter) is observed.
- **Constants:** PascalCase. `TestHost`, `TestDatabase`, `TestUid`, `TestPassword` in `DBToolsUnitTest/TestBase.cs:16-20`.

### Variables
- **Public `lowercase` properties on models:** Present on `GenericObject` (`columns`, `values`, `types`, `valuesString`, `table`) — `DBTools/Models/GenericObject.cs:47-51`. This is intentional API surface for the legacy data-carrier model; new POCOs should follow standard PascalCase (`Columns`, `Values`).
- **Loop counters:** `cont`, `i`, `idx`, `paramIndex` (mostly short legacy names from the original Java-style code).

## Code Style Specifics

### String Handling
- **`String.Format` legacy:** Mixed with `$""` interpolated strings. Legacy methods in `SqlClient.cs` and `SqlQueryBuilder.cs` use `String.Format("INSERT INTO {0}({1}) VALUES({2})", table, fieldList, paramPlaceholders)`; newer async methods in `AsyncSqlClient.cs` and `DbContext.cs` use interpolation (`$"INSERT INTO {table}({fieldList}) VALUES({paramPlaceholders})"`).
- **`String.IsNullOrEmpty`** is preferred over `string.IsNullOrEmpty`. Both appear; `String` (capitalized) appears slightly more in legacy paths (`SqlClient.cs:329`, `SqlQueryBuilder.cs:53`).
- **Empty-string sentinel:** `""` used in several places instead of `string.Empty` (e.g. `ExecuteQuery(string query = "")` in `DBTools/Core/DBTools.cs:385`).

### Type Usage
- **`Object` / `String` capitalized aliases:** Used in legacy methods (e.g. `public Object[] values`, `public String[] GetInBd(String query)`). Standard `object`/`string` appear in newer code.
- **`var` vs explicit:** Use `var` when the right-hand side makes the type obvious (`var dataExport = new DataExport();`). Use explicit types for ambiguous or `object` returns.

### Comments
- **XML doc comments:** Comprehensive on public surface (`<summary>`, `<param>`, `<returns>`, `<exception>`, `<remarks>`).
- **Inline comments:** Often demarcate regions with `#region name` / `#endregion`. Examples: `DBTools/Core/SqlClient.cs:66`, `DBTools/Core/DBTools.cs:16`, `DBToolsUnitTest/Models/FakeSqlClient.cs` (no regions here).
- **Inline notes** explain non-obvious design choices — e.g. `DBTools/Core/DBTools.cs:188-190` (`"// NOTE: This auto-generates a SQL Server format connection string."`), `DBTools/Core/DBTools.cs:309-310` (explains why `Count = 1` is hardcoded).

## Architectural Patterns

### Dependency Injection
- **`Microsoft.Extensions.DependencyInjection`** is used. Registration is centralized in `DBTools/Configuration/ServiceCollectionExtensions.cs` via the extension method `AddDbTools(IServiceCollection, Action<DbToolsOptions>)`.
- **Lifetimes:** `AsyncSqlClient` registered as `Scoped`; `ISqlValidator`, `ISqlQueryBuilder`, `IDbProvider`, `IDbConfiguration` as singletons; legacy `SqlClient` registered `Transient` for backward compatibility (lines 35-74).
- **`DbToolsOptions`** in `DBTools/Configuration/DbToolsOptions.cs` is the configuration POCO; populated by caller-supplied `Action<DbToolsOptions>`.

### Repository / Data-Access Pattern
- **Not a strict Repository pattern.** The library exposes:
  - `SqlClient` / `AsyncSqlClient` — direct connection-level CRUD.
  - `LinqHelper<TModel>` / `Linq<TModel>` / `AsyncLinqHelper<TModel>` — typed LINQ-style CRUD per table.
  - `DbContext` / `DbSet<T>` — Entity-Framework-like session in `DBTools/Context/DbContext.cs`.
  - `BulkOperations<TEntity>` — SqlBulkCopy / batched inserts in `DBTools/Bulk/BulkOperations.cs`.
- Consumers pick the abstraction they need; there is no enforced interface segregation beyond `ISqlClient`/`IAsyncSqlClient`.

### Factory Pattern
- **`DbProviderFactory.Create(DatabaseProvider)`** and `Create(string)` in `DBTools/Providers/DbProviderFactory.cs:16-45` — switch-expression over enum/string to instantiate `SqlServerProvider`, `PostgresProvider`, `MySqlProvider`, `SqliteProvider`.

### Strategy Pattern
- **`IDbProvider`** abstraction lets each provider encapsulate dialect differences: `QuoteIdentifier`, `BuildPagingClause`, `GetLastInsertedIdSql`, `BuildUpsertSql`. See `DBTools/Abstractions/IDbProvider.cs:9-60`.

### Interceptor Pattern
- **`IQueryInterceptor`** (`DBTools/Abstractions/IQueryInterceptor.cs`) — `BeforeExecute`, `AfterExecute`, `OnError` for sync; `IAsyncQueryInterceptor` with `*Async` variants.
- Three built-in interceptors in `DBTools/Interceptors/`:
  - `LoggingInterceptor` — configurable `Action<string>` log sink.
  - `SoftDeleteInterceptor` — appends `AND IsDeleted = 0`, rewrites `DELETE` to `UPDATE`.
  - `AuditInterceptor` — stamps user/timestamp into `context.Properties`.
- **Registration:** `options.AddInterceptor(...)` on `DbToolsOptions` (lines 71-84); applied inside the `AddDbTools` factory.

### Expression Visitor (LINQ Translation)
- **`DbExpressionTranslator`** in `DBTools/Linq/DbExpressionTranslator.cs` inherits `ExpressionVisitor` to translate expression trees to SQL. Heavy `switch` on `ExpressionType` is the dominant style — see `TranslatePredicate` (lines 174-240).

## Error Handling

### Strategy: Mixed (Exceptions + Error-String Property)
The codebase uses **two parallel error-reporting styles**, depending on the API surface:

1. **Exceptions** — thrown for invalid arguments and invariant violations:
   - `ArgumentNullException(nameof(...))` for null parameters on constructors and required inputs. From `DBTools/Core/SqlClient.cs:39-41`:
     ```csharp
     Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
     _validator = validator ?? throw new ArgumentNullException(nameof(validator));
     ```
   - `ArgumentException` for invalid identifiers / security violations. Example from `DBTools/Core/SqlClient.cs:273-280`:
     ```csharp
     if (!IsValidIdentifier(_fields))
         throw new ArgumentException("Invalid field names. Only alphanumeric characters, underscores, dots, brackets, commas, and spaces are allowed.", nameof(_fields));
     ```
   - `InvalidOperationException` for runtime state violations. Example from `DBTools/Models/GenericObject.cs:78-86`:
     ```csharp
     private void EnsureClient()
     {
         if (_dbTools == null)
         {
             throw new InvalidOperationException(
                 "GenericObject has no ISqlClient configured. Use the GenericObject(ISqlClient) " +
                 "constructor or assign the DbTools property before calling Insert/Update.");
         }
     }
     ```
   - `NotSupportedException` for unsupported expression-tree nodes (e.g. `DBTools/Linq/DbExpressionTranslator.cs:129`: `throw new NotSupportedException($"LINQ method '{methodName}' is not supported for SQL translation.");`).
   - `FileNotFoundException` for missing `config.json` (`DBTools/Core/DbConfiguration.cs:25-29`).

2. **Error-string property (`Error`)** — set inside `try`/`catch`, returned via a public string property. Used in legacy `SqlClient`/`DBTools` flow. From `DBTools/Core/DBTools.cs:386-406`:
   ```csharp
   public void SqlExecuteQuery(String query = "")
   {
       using (DbConnection connection = _provider.CreateConnection(ConnectionString))
       {
           connection.Open();
           try
           {
               DbCommand command = connection.CreateCommand();
               command.CommandText = this.getQuery();
               ...
           }
           catch (Exception ex)
           {
               this.Error = ex.ToString();
           }
       }
   }
   ```
   **The async path uses `_error` field with the same pattern** — see `DBTools/Core/AsyncSqlClient.cs:280-290` (sets `_error = ex.ToString()` then returns null/`false`).

### Catch Block Conventions
- **`catch (Exception e)`** for broad swallow-and-store: see `DBTools/Core/SqlClient.cs:226-235` (catch wraps an inner query execution).
- **`catch (DbException e)`** for DB-specific errors (`DBTools/Core/DBTools.cs:313-316`).
- **`catch { }`** empty catch is **not used** for swallow-on-dispose paths but does appear in cleanup helpers like `DBTools/Core/DbTransaction.cs:84,99` (`try { _transaction.Rollback(); } catch { /* Swallow on dispose */ }`).
- **`catch (Exception ex) when (!(ex is InvalidOperationException))`** — re-throw wrapper pattern in `DBTools/Core/DbConfiguration.cs:59-64`.

### Result-Returning Methods
- **Mutation-style APIs return `bool`** (`Insert`, `Update`, `Delete`) — `true` on success, `false` if `Error != null` after execution. See `DBTools/Core/SqlClient.cs:349-353`:
  ```csharp
  if (Error != null)
  {
      return false;
  }
  return true;
  ```
- **No `Result<T>` / `Try*` pattern** is used. Methods either throw on validation failures or return `bool` on execution failures.

### Validation Pre-conditions
- **Guard clauses at top of methods.** Every public mutator in `SqlClient` opens with `if (!IsValidIdentifier(...)) throw new ArgumentException(...)` followed by null-checks. This is the dominant pattern.
- **Validators** (`SqlValidator` in `DBTools/Core/SqlValidator.cs`) enforce a regex + dangerous-keyword blacklist:
  ```csharp
  private static readonly Regex DangerousKeywordRegex =
      new Regex(@"\b(DROP|DELETE|TRUNCATE|INSERT|UPDATE|EXEC|EXECUTE|ALTER|CREATE|GRANT|REVOKE|MERGE)\b",
          RegexOptions.Compiled | RegexOptions.IgnoreCase);
  ```

## Logging

- **No `ILogger` / `Microsoft.Extensions.Logging` dependency.** Logging is pluggable via `Action<string>` delegate passed into `LoggingInterceptor` (`DBTools/Interceptors/LoggingInterceptor.cs:21-25`).
- **Default sink is `Console.WriteLine`** — see consumer code:
  ```csharp
  options.AddInterceptor(new LoggingInterceptor(Console.WriteLine));
  ```
- **No use of `Console.WriteLine` directly inside library code** — all logging flows through interceptors. The exception is `IntegrationTestBase.cs` which prints diagnostic lines during container lifecycle management (`Console.WriteLine("[IntegrationTestBase] ...")`), but that's test infrastructure.
- **`#if DEBUG` traces:** Not present in the library; logging is opt-in via interceptor.

## Async / Await Patterns

- **Naming:** Suffix `Async` on all async methods (`SelectAsync`, `InsertAsync`, `BeginTransactionAsync`, `DisposeAsync`).
- **Cancellation:** Every async public method accepts `CancellationToken ct = default` as the last parameter.
- **ConfigureAwait:** All internal awaits use `.ConfigureAwait(false)` (e.g. `DBTools/Core/AsyncSqlClient.cs:139, 274, 322`).
- **`using var`:** Used in modern async methods (`DBTools/Core/AsyncSqlClient.cs:267, 270, 321, 338`); legacy methods use `using (...)` blocks.

## Disposal Patterns

- **IDisposable + IAsyncDisposable** on classes that own connections/transactions: `AsyncSqlClient`, `DbContext`, `DbToolsTransaction`.
- **Disposal idempotency:** `_disposed` flag checked at entry. From `DBTools/Core/AsyncSqlClient.cs:480-491`:
  ```csharp
  public void Dispose()
  {
      if (_disposed) return;
      _disposed = true;
  }
  ```
- **`await using`** for async disposal in newer code (`DBTools/Context/DbContext.cs:122`, `DBTools/Bulk/BulkOperations.cs:94`).

## Imports / Using Organization

`using` directives appear in this order (consistent across files):
1. `System` and `System.*` namespaces (alphabetical: `System`, `System.Collections`, `System.Collections.Generic`, `System.Data`, `System.Data.Common`, `System.Diagnostics`, `System.IO`, `System.Linq`, `System.Linq.Expressions`, `System.Reflection`, `System.Text`, `System.Threading`, `System.Threading.Tasks`).
2. `Microsoft.*` (`Microsoft.Data.SqlClient`, `Microsoft.Extensions.Configuration`, etc.).
3. `DBTools.*` (alphabetical: `DBTools.Abstractions`, `DBTools.Configuration`, `DBTools.Core`, `DBTools.Mapping`, `DBTools.Models`, `DBTools.Providers`).

Example from `DBTools/Core/AsyncSqlClient.cs:1-12`:
```csharp
using DBTools.Abstractions;
using DBTools.Models;
using DBTools.Providers;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
```

**No `#region` for using blocks.** Regions are used inside the type to group methods (`#region Helper methods for validation`, `#region query utilities`, `#region Data Manipulation modules`, etc.).

## Module / Class Design

### Public Surface Discipline
- **Public types are XML-documented.** Examples: `SqlClient`, `SqlValidator`, `DbConfiguration`, `AsyncSqlClient`, `Linq<TModel>`, `EntityMapping`.
- **Implementation classes are `public`** even when intended for internal use (e.g. `SqlServerProvider`, `PostgresProvider`). Only a handful of types are `internal` (e.g. `JoinClause`, `TranslationResult`, `InlineEntityConfiguration` in `DBTools/Context/DbContext.cs:335`).
- **No `InternalsVisibleTo`.** Tests access internals via `public` types only — verified by the absence of an `AssemblyInfo` `InternalsVisibleTo` attribute in `DBTools/Properties/AssemblyInfo.cs`.

### Constructors
- **Multiple constructors for testability.** `SqlClient` has three (`DBTools/Core/SqlClient.cs:23, 37, 51`): parameterless (reads `config.json`), DI-aware (`IDbConfiguration, ISqlValidator, ISqlQueryBuilder`), and provider-injected (adds `IDbProvider`).
- **Null-checks at the top of every multi-arg constructor** using `?? throw new ArgumentNullException(nameof(...))`.

### Properties
- **Mixed `private set` / `public set`.** Read-only configuration objects use private setters or `get;` only; mutable model/POCO types use `public set` or lowercase `set`.
- **Property expression-bodied syntax** is used for short getters/setters — `DBTools/Core/DBTools.cs:172`: `public string Port { get => port; set => port = value; }`.

## Where to Add New Code

### New public type
- Place in the matching `DBTools/<Area>/` folder: `Abstractions/`, `Bulk/`, `Configuration/`, `Context/`, `Controllers/`, `Core/`, `Export/`, `Interceptors/`, `Linq/`, `Mapping/`, `Models/`, `Providers/`.
- File name = type name (`MyNewType.cs`).
- XML-document the public surface.
- If the type owns a connection, implement `IDisposable` + `IAsyncDisposable` with the `_disposed` guard pattern.

### New CRUD method on `SqlClient` / `AsyncSqlClient`
- Add XML docs (`<summary>`, `<param>`, `<returns>`, `<exception>`).
- Validate inputs at the top with `IsValidIdentifier` for table/field names, then `ArgumentNullException` for null parameter arrays.
- Use `String.IsNullOrEmpty(condition)` for required WHERE clauses (security invariant for UPDATE/DELETE).
- Build `@param{i}` placeholders; create parameters via `_provider.CreateParameter(name, value ?? DBNull.Value)`.
- Sync methods return `bool` and consult `Error` post-execution. Async methods return `Task<bool>` / `Task<T>` and set `_error`.

### New interceptor
- Implement `IQueryInterceptor` for sync; also `IAsyncQueryInterceptor` if any work awaits.
- Use `QueryInterceptionContext` (`DBTools/Abstractions/IQueryInterceptor.cs:47-88`) — never read SQL or params directly from a private field.
- Register via `options.AddInterceptor(...)` on `DbToolsOptions`.

### New LINQ method (in `Linq.cs` / `AsyncLinqHelper.cs`)
- For property-selector methods, follow the `WhereEquals`/`WhereGreaterThan` pattern (`DBTools/Controllers/Linq.cs:52-57`): extract column name via `GetPropertyName`, build parameterized condition, dispatch to `Select(condition, parameters)`.
- For LINQ-translated methods (in `DbExpressionTranslator.cs`), add a new `case` to the `switch` over `node.Method.Name` (lines 84-130).

### New provider
- Add class to `DBTools/Providers/`, implement `IDbProvider`.
- Register in `DbProviderFactory.Create(DatabaseProvider)` and `Create(string)` switch expressions.
- If new package required, add to `DBTools/DBTools.csproj` `<PackageReference>` block.

---

*Convention analysis: 2026-06-21*