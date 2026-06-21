---
last_mapped_commit: f9cc45c3b85d4d7645b1a3a5deef92ed3bc9b6c7
generated: 2026-06-21
focus: concerns
---

# Codebase Concerns

**Analysis Date:** 2026-06-21
**Repository:** DBTools_SQL
**Last Mapped Commit:** f9cc45c3b85d4d7645b1a3a5deef92ed3bc9b6c7
**Scope:** Full repository (DBTools + DBToolsUnitTest + scripts/docs)

## Summary

This is a brownfield multi-provider ADO.NET data-access library (`DBTools`). It targets SQL Server, PostgreSQL, MySQL, and SQLite, and exposes both legacy synchronous APIs (`DBTools.Core.DBTools`, `SqlClient`) and modern async APIs (`AsyncSqlClient`, `DbContext`, `DbSet`, `LinqHelper<T>`/`Linq<T>`). The project has had significant security hardening (parameterized queries, identifier validation, mandatory WHERE clauses on UPDATE/DELETE) but several structural, security, and reliability concerns remain. No `TODO`/`FIXME`/`HACK`/`XXX` markers exist in the source — issues below are surfaced from code reading rather than TODO scanning.

Severity legend: **HIGH** (data loss / security / correctness), **MEDIUM** (reliability / maintainability), **LOW** (style / future-proofing).

---

## Tech Debt

### 1. SQL injection surface through string-concatenated query helpers

**Severity:** MEDIUM
**Files:**
- `DBTools/Core/SqlClient.cs:530-548` — `Select(String query_without_select, Object[] parameters)`
- `DBTools/Core/SqlClient.cs:558-576` — `SelectRaw(String fullSql, Object[] parameters)`
- `DBTools/Core/SqlClient.cs:585-630` — static `Select_Query` / `Insert_Query` / `Update_Query` / `Delete_Query` (return raw concatenated SQL)
- `DBTools/Linq/DbQueryProvider.cs:85,157,168` — calls `_utils.SelectRaw(sql, ...)`
- `DBTools/Linq/JoinQuery.cs:119,166` — calls `_utils.SelectRaw(sql, ...)`

**Issue:** Several public methods accept caller-supplied SQL fragments that bypass `SqlValidator` and are not fully parameterized:

- `SqlClient.Select(query_without_select, parameters)` concatenates `"SELECT " + query_without_select`. Even though it is marked `[EditorBrowsable(Never)]` and carries a security warning in its XML doc, it remains a public surface that accepts raw fragments.
- `SqlClient.SelectRaw(fullSql, parameters)` is fully unvalidated. Anything in `fullSql` (including identifiers, comments, multi-statement `;`) goes straight to the server. The IQueryable providers (`DbQueryProvider`, `JoinQueryProvider`) feed LINQ-translated SQL into this method, but if a downstream developer calls it directly with user input, there is no guardrail.
- The static `*_Query` helpers build SQL strings via `String.Format` and quote values manually (`SqlQueryBuilder.UpdateQuery` at `DBTools/Core/SqlQueryBuilder.cs:120-141`). The quoting logic (`'` escaping, comma→period for numbers) is hand-rolled and ad-hoc — easy to bypass with column names containing `\\` or values containing NULL bytes. They are also documented as "for better security, use the non-static Insert method with parameterized queries" but are still publicly callable.

**Impact:** Footgun for users of the library who pick the wrong overload. The async equivalents (`AsyncSqlClient.SelectAsync(queryWithoutSelect, …)`, `SelectRawAsync`) mirror the same shape.
**Fix approach:** Either delete `SelectRaw*` and the static `*_Query` builders entirely (the recommended methods cover all use cases), or validate identifiers in the SQL fragment via `SqlValidator` and refuse anything containing `;`, `--`, `/*`, or balanced-quote mismatches. Add unit tests that attempt classic payloads and verify the methods refuse to execute them.

### 2. Magic-string column names throughout LINQ helper layer

**Severity:** MEDIUM
**Files:**
- `DBTools/Controllers/LinqHelper.cs:78,80` — caller passes `conditionsParametrized` as raw WHERE text
- `DBTools/Controllers/LinqHelper.cs:240-247` — `Count(string conditions, object[] parameters)`
- `DBTools/Controllers/LinqHelper.cs:268-275` — `Find` builds `where` via `string.Format`
- `DBTools/Controllers/LinqHelper.cs:284-287` — `Where(string conditions, object[] parameters)`
- `DBTools/Controllers/LinqHelper.cs:296-313` — `Single`, `SingleOrDefault`
- `DBTools/Controllers/LinqHelper.cs:321-333` — `GetAll`, `AsQueryable`
- `DBTools/Controllers/LinqHelper.cs:447-463` — `SaveChanges`
- `DBTools/Controllers/AsyncLinqHelper.cs:62-66, 89-93, 137-144, 158-167` — same pattern
- `DBTools/Context/DbSet.cs:84-152` — `ToListAsync`, `WhereAsync`, `FindAsync`, `CountAsync`

**Issue:** The "string-condition" overloads (`Select(string, IEnumerable<object>)`, `Where(string, object[])`, etc.) take a WHERE clause as a raw string. There is no enforcement that column names in the string are valid identifiers — the only parameterization is the values array. A typo like `"emial = @param0"` compiles and runs (returning empty results). This pattern is inconsistent with the type-safe `Where(u => u.Email == …)` overloads.

**Impact:** Refactoring model properties (renaming `Email` → `EmailAddress`) is brittle — string overloads silently break at runtime with empty-result sets. No compile-time safety.
**Fix approach:** Either mark these string-condition overloads `[Obsolete]` and migrate users to expression-based ones, or wrap the column names through `ISqlValidator.IsValidIdentifier` before building the WHERE clause.

### 3. Async-over-sync anti-pattern in `DbContext.SaveChanges()`

**Severity:** MEDIUM
**Files:** `DBTools/Context/DbContext.cs:107-110`

**Issue:**
```csharp
public int SaveChanges()
{
    return SaveChangesAsync().GetAwaiter().GetResult();
}
```

This synchronously blocks the calling thread on the async path — a classic deadlock risk in sync-over-async contexts (WinForms, classic ASP.NET, console apps with a `SynchronizationContext`). The body performs database I/O on the calling thread.

**Impact:** Can deadlock under UI/WinForms/ASP.NET classic; silently drops exceptions wrapped in `AggregateException` if not unwrapped (the `GetAwaiter().GetResult()` unwraps the inner exception, but only for the original `Task.Exception`, not for nested awaited tasks).
**Fix approach:** Document `SaveChanges` as "synchronous stub; prefer SaveChangesAsync", or implement sync paths using `Task.Run(...).GetAwaiter().GetResult()` and pass a `CancellationToken.None`. At minimum, add a `// NOTE: synchronous blocking` warning comment.

### 4. Duplicated LINQ expression parsing — 3 copies, drift inevitable

**Severity:** MEDIUM
**Files:**
- `DBTools/Controllers/LinqHelper.cs:490-579` — `ParseExpression` + `ParseBinaryExpression` + `IsNullParam`
- `DBTools/Controllers/AsyncLinqHelper.cs:264-331` — same algorithm
- `DBTools/Context/DbSet.cs:159-233` — same algorithm
- `DBTools/Linq/DbExpressionTranslator.cs:174-419` — fourth copy (most complete)

**Issue:** Four near-identical expression-tree walkers, each with subtly different handling:
- `LinqHelper.ParseExpression` falls through to `Expression.Lambda(...).Compile().DynamicInvoke()` for unsupported expressions (security/perf concern).
- `DbExpressionTranslator` does NOT compile-and-execute — it throws `NotSupportedException`.
- `DbSet.ParseExpression` also compiles fallback expressions.

**Impact:** Bug fixes land in one and miss the others. Behaviour divergence: `LinqHelper` and `DbSet` will silently compile-and-evaluate arbitrary user-supplied expressions (any `MethodCallExpression`, captured variable, etc.), which can produce SQL that references column names from out-of-scope closures (information disclosure) and is slower than translation. `DbExpressionTranslator` will reject the same input with an exception. Users cannot predict which behaviour they will get.
**Fix approach:** Extract one shared `ExpressionToSqlVisitor` (in `DBTools.Linq`) and have all four call sites use it. Pick one policy for unsupported nodes (compile-and-evaluate vs. throw) and apply consistently.

### 5. Concurrent collection leaks in `AsyncSqlClient` — interceptor lists are not thread-safe in their lifecycle

**Severity:** LOW (MEDIUM if multi-threaded AddInterceptor is used)
**Files:** `DBTools/Core/AsyncSqlClient.cs:26-27,76-89`

**Issue:**
```csharp
private readonly List<IQueryInterceptor> _interceptors = new List<IQueryInterceptor>();
private readonly List<IAsyncQueryInterceptor> _asyncInterceptors = new List<IAsyncQueryInterceptor>();
```

`List<T>` is not thread-safe. `AddInterceptor` mutates both lists; `RunBeforeInterceptorsAsync` (lines 452-458), `RunAfterInterceptorsAsync` (460-466), and `RunErrorInterceptorsAsync` (468-474) iterate them concurrently with execution. If `AddInterceptor` is called from one thread while a query runs on another, the enumeration can throw `InvalidOperationException` ("Collection was modified").

The `DbToolsOptions.Interceptors` lists (`DBTools/Configuration/DbToolsOptions.cs:61-66`) have the same issue and are read during DI scope creation in `ServiceCollectionExtensions.cs:55-58`.

**Fix approach:** Wrap with `ImmutableArray<T>` builder, use `ConcurrentBag<T>`, or document that `AddInterceptor` must complete before the client is shared.

### 6. Global mutable state in `EntityMappingResolver`

**Severity:** MEDIUM
**Files:**
- `DBTools/Mapping/EntityMappingResolver.cs:13-51`
- `DBTools/Mapping/EntityMappingResolver.cs:39-43` — `Register` invalidates cache
- `DBTools/Context/DbContext.cs:320-323` — `ModelBuilder.Entity<T>()` calls `Register`

**Issue:** Static `ConcurrentDictionary` cache lives for the process lifetime. `ModelBuilder.Entity<T>()` registers a new fluent config every time `OnModelCreating` runs (which is called in every `DbContext` constructor — see `DBTools/Context/DbContext.cs:48-49,81-82,92-93`). Once one DbContext subclass registers a fluent config, the cache is poisoned for subsequent DbContext types that may share the same `TEntity`. Tests that share an AppDomain can leak state across runs.

There is no `Clear()` call in `DbContext.Dispose`, only `ClearCache()` (line 48) which is public-and-undocumented.

**Impact:** Cross-context bleed; test interference; subtle "works in isolation, fails in production" bugs.
**Fix approach:** Move mapping storage into the `DbContext` instance, or at minimum snapshot/restore fluent configs in test setup/teardown.

### 7. Regex `IdentifierRegex` accepts semicolon — overly permissive

**Severity:** LOW (security adjacent)
**File:** `DBTools/Core/SqlValidator.cs:9-10`

```csharp
private static readonly Regex IdentifierRegex =
    new Regex(@"^[\w\.\[\]\,\s\*\(\)]+$", RegexOptions.Compiled);
```

`\w` is `[A-Za-z0-9_]` — does NOT include semicolons or hyphens. So `;` and `--` are already blocked. Good. But `*` and `(` `)` are allowed at the IDENTIFIER level — this is correct for `SELECT *` and SQL functions, but means `*; DROP TABLE x` is `false` because of the semicolon (already covered by `IdentifierRegex`)? Let me re-check: `*` is in the allowed set, but `;` is not. So this is fine.

However the validator's `DangerousKeywordRegex` (`SqlValidator.cs:12-14`) only flags certain keywords (`DROP`, `DELETE`, etc.) — it does NOT block `UNION SELECT`, `xp_`, `--comment`, or hex literals. The combination `IsValidIdentifier("a UNION SELECT b")` returns true (no semicolon, all alpha/space). So `SqlClient.Select(fields="a UNION SELECT b", table="users", where="1=1")` would happily build `"SELECT a UNION SELECT b FROM users WHERE 1=1"` — a UNION-based injection that bypasses validator. The parameterization of values saves us from typical injection, but field-list and WHERE-clause string parameters are concatenated raw in `SqlClient.Select(...)` (`SqlClient.cs:293-298`).

**Impact:** Bypass of validator via UNION/SELECT stack in field-list parameter.
**Fix approach:** Add `UNION` and `xp_` to the dangerous-keyword regex. Or — better — refuse the string-condition overload entirely and require callers to use the typed overload.

---

## Known Bugs / Suspected Issues

### 1. `BulkOperations<TEntity>(AsyncSqlClient client)` ignores the client's actual provider

**Severity:** MEDIUM (correctness bug)
**File:** `DBTools/Bulk/BulkOperations.cs:41-47`

```csharp
public BulkOperations(AsyncSqlClient client)
{
    if (client == null) throw new ArgumentNullException(nameof(client));
    _connectionString = client.ConnectionString;
    _provider = new Providers.SqlServerProvider();   // ← always SQL Server
    _mapping = EntityMappingResolver.Resolve<TEntity>();
}
```

If the user instantiates `AsyncSqlClient` with a `MySqlProvider` and then constructs `BulkOperations<T>(client)`, the constructor ignores that and hard-codes `SqlServerProvider`. The `BulkInsertAsync` dispatch at `BulkOperations.cs:66-73` will then route to `BulkInsertSqlServerAsync` (`BulkOperations.cs:189-210`), which uses `SqlBulkCopy` directly — producing a runtime error or wrong behaviour when the underlying database is not SQL Server.

**Trigger:** Pass an `AsyncSqlClient` configured for Postgres/MySQL/SQLite to `BulkOperations<T>(AsyncSqlClient)`. BugRegressionTests does not cover this overload.
**Workaround:** Use the `BulkOperations(string connectionString, IDbProvider)` overload explicitly.
**Fix approach:** Expose the provider on `AsyncSqlClient` (it already has `_provider` private — add `public IDbProvider Provider { get; }`), then read it in this constructor.

### 2. `IsNullParam` index calc can match wrong parameter under `OR`

**Severity:** MEDIUM (correctness)
**File:** `DBTools/Controllers/LinqHelper.cs:574-579`

```csharp
private static bool IsNullParam(string paramName, List<object> parameters, int paramsBefore)
{
    if (!int.TryParse(paramName.Substring(6), out int idx))
        return false;
    return idx < parameters.Count && idx >= paramsBefore && parameters[idx] == DBNull.Value;
}
```

`idx` is parsed from the parameter name without scoping; if a `WHERE` clause is `(a = @param0 OR b = @param1) AND c = @param2`, and `@param0` happens to be `DBNull.Value`, the recursive call from `ParseBinaryExpression` for `(a = @param0 OR b = @param1)` will check `idx=0`, see DBNull, and rewrite `a = @param0 OR b = @param1` as `a IS NULL OR b = @param1`. That may or may not be the intent. More importantly, the index-based check can be fooled if the same parameter name (`@param0`) appears in two nested binary expressions.

Same issue in `DBTools/Linq/DbExpressionTranslator.cs:260-264` (`IsNullParameter` there is a no-arg version that parses the same name string and looks up against the shared `_result.Parameters` list — same shared-index hazard).

**Impact:** Subtle correctness: `WHERE a = null OR b = 1` may rewrite to `WHERE a IS NULL OR b = 1` (probably fine) but `WHERE a = null AND b = null` with `paramsBefore=0` and only `@param0` may rewrite inconsistently.
**Fix approach:** Pass the parameter index explicitly (not by parsing the SQL name), or emit `IS NULL` checks by inspecting the expression tree at translate time without going through the param-name round-trip.

### 3. `ConnectivityInterceptor` and "Now hardcoded to 1" comment indicates unfinished refactor

**Severity:** LOW
**Files:**
- `DBTools/Core/DBTools.cs:309-310`
- `DBTools/Core/DBTools.cs:439-440`

```csharp
// Previously set to dataSet.Tables.Count (number of result sets from SqlDataAdapter).
// Now hardcoded to 1 because DbDataReader + DataTable.Load() reads only one result set.
this.Count = 1;
```

The `Count` property is now meaningless — it always equals `1` regardless of the query. The XML doc on `Count` says "Gets or sets the count." Consumers may still read it expecting row count.

**Fix approach:** Mark `Count` `[Obsolete]` with a clear message, or rename to something honest like `IsResultSetAvailable`.

### 4. `SqlToolsController.DBTools` field is a public mutable reference

**Severity:** LOW (defensive-coding concern)
**File:** `DBTools/Controllers/DBToolsController.cs:15-20`

```csharp
public DBTools.Core.DBTools DBTools = new DBTools.Core.DBTools();

public DBToolsController(DBTools.Core.DBTools dbTools)
{
    DBTools = dbTools;
}
```

The field is `public` and mutable. `RetrieveDataSQL()` / `RetrieveObjectSQL()` / `SqlExecuteQuery(...)` delegate straight to it. A caller can replace `DBTools` at any time, bypassing the constructor-injected dependency. Surprising behaviour for DI consumers.

**Fix approach:** Make the field `private` and add a `DBTools` getter; or use an auto-property `{ get; private set; }`.

### 5. `DateTime.Parse(stringValue)` inside `QueryBuilder` is culture-dependent

**Severity:** MEDIUM (correctness, locale-sensitive)
**File:** `DBTools/Core/SqlClient.cs:103`

```csharp
value = rawValue != null
    ? DateTime.Parse(stringValue).ToString("yyyy-MM-dd HH:mm:ss")
    : ""
```

`DateTime.Parse(stringValue)` uses the current culture's date format. On a server with `pt-BR` or `de-DE` culture, a `DateTime` that round-tripped via `ToString()` with `"yyyy-MM-dd HH:mm:ss"` will fail to parse on the way back in. This is asymmetric: the output uses ISO format, but the input parse uses culture defaults.

**Trigger:** Application hosted on non-`en-US` culture sets `user.Name = "test"` then queries via `QueryBuilder` — actually this only fires when `i.PropertyType.Name == "DateTime"`, so `DateTime` properties specifically.
**Fix approach:** Use `DateTime.Parse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None)` and the same `ToString("o")` round-trip format.

### 6. `ConnectivityInterceptor`'s `GetValue(obj, null)` discards `BindingFlags`

**Severity:** LOW
**Files:**
- `DBTools/Core/SqlClient.cs:95` — `obj.GetType().GetProperty(i.Name).GetValue(obj, null)`
- `DBTools/Core/SqlClient.cs:130-131` — has an empty branch `if (i.Name == primaryKeyName) { }` (no-op)

**Issue:** `GetValue(obj, null)` returns `null` for value-typed properties — but it's followed immediately by `stringValue = rawValue?.ToString() ?? ""`. If `i.PropertyType` is `int` and the property is 0, that's fine. If it's `int?` and the property is `null`, `rawValue` is `null`, `stringValue` is `""`, and `rawValue != null` is false → branch falls through to `else` (the `switch (autoIncrement)`) and the value is added as `""` — which on insertion gets sent as the string `""`, not as `NULL`. This causes `int?` properties that the user wants to be `NULL` to silently become empty strings (which the DB may reject as type-mismatch or insert as empty string).

**Fix approach:** Preserve `null` vs empty-string distinction: add `null` values to the list as `null`/`DBNull.Value` rather than the string `""`.

### 7. `DbContext.ExecuteDeleteAsync` parameter index hardcoded as `0`

**Severity:** LOW
**File:** `DBTools/Context/DbContext.cs:262-263`

```csharp
string sql = $"DELETE FROM {mapping.TableName} WHERE {mapping.PrimaryKeyColumn} = @param0";
return await _client.ExecuteInTransactionAsync(tx, sql, new object[] { pkValue ?? DBNull.Value }, ct).ConfigureAwait(false);
```

Hard-coded `@param0` is consistent with the single-parameter nature of the statement, but if `ExecuteInTransactionAsync` ever evolves to take additional context-bound parameters (tracing tags, request id, etc.) this would silently collide. Minor fragility.

### 8. `SaveChanges()` sync wrapper does not respect a CancellationToken

**Severity:** LOW
**File:** `DBTools/Context/DbContext.cs:107-110`

There is no overload `SaveChanges(CancellationToken)`. Async path does, but the sync stub does not surface one.

**Fix approach:** Add overload and pass it to `SaveChangesAsync(ct).GetAwaiter().GetResult()`.

---

## Security Considerations

### 1. Connection-string defaults: `TrustServerCertificate=True` hardcoded

**Severity:** MEDIUM (production hardening)
**Files:**
- `DBTools/Core/DBTools.cs:191` — auto-generated connection string always has `TrustServerCertificate=True`
- `DBTools/Core/DbConfiguration.cs:102` — same
- `DBTools/Configuration/DbToolsOptions.cs:51` — `TrustServerCertificate { get; set; } = true` default

**Risk:** Forcing TLS trust without explicit operator consent weakens defence against MITM. On shared or untrusted networks this is a downgrade from best practice. Users who construct `SqlClient` without setting `TrustServerCertificate=false` will be vulnerable by default.

**Current mitigation:** None at the library level.
**Recommendations:**
- Default to `false` for `TrustServerCertificate`.
- Document the trade-off in XML docs at every connection-string emission point.
- Add an opt-in flag like `DbToolsOptions.RequireEncryption` that throws if `TrustServerCertificate=true` is being used in production.

### 2. Password is read into memory and held on `DBTools.Password` for the process lifetime

**Severity:** MEDIUM (sensitive data exposure window)
**Files:**
- `DBTools/Core/DBTools.cs:90-101` — public getter/setter for `Password`
- `DBTools/Core/DbConfiguration.cs:14, 41, 73, 82` — `Password` exposed via `IDbConfiguration`
- `DBTools/Configuration/DbToolsOptions.cs:36` — `Password` auto-property
- `DBTools/Configuration/ServiceCollectionExtensions.cs:115` — `Password = options.Password ?? ""`

**Risk:** Every layer keeps the raw password in memory as `string` (immutable, interned, GC-uncatchable). There is no `SecureString` / no clearing. The `DBTools.Password` getter is public — any consumer can call `dbTools.Password` to exfiltrate. There is no `IDisposable` or finalizer that zeroes the field.

**Current mitigation:** None.
**Recommendations:**
- Replace `string Password` with `SecureString` on `IDbConfiguration` and `DbToolsOptions`, or mark them as `internal` set-only and never expose a getter.
- Mark `DBTools.Password` setter as the only surface; the getter is rarely needed.
- Document that consumers should not log the `DBTools` instance.

### 3. `LoggingInterceptor` parameter values are PII / secret material by default

**Severity:** LOW
**File:** `DBTools/Interceptors/LoggingInterceptor.cs:30-34`

The interceptor accepts a `logParameters` flag (default `false`). Good default. But the log action receives `string.Join(", ", context.Parameters)` which may contain passwords, PII (email, SSN), or even unfiltered exception messages from `OnError` (line 47). Users who plug in `Console.WriteLine` or write to a file via the interceptor will leak sensitive values.

**Recommendation:** Add a parameter-name allowlist to `LoggingInterceptor`, or document that the supplied `logAction` must itself redact.

### 4. `SoftDeleteInterceptor` does string-level surgery on SQL

**Severity:** MEDIUM (fragile + injection-adjacent)
**File:** `DBTools/Interceptors/SoftDeleteInterceptor.cs:32-72`

```csharp
if (context.Sql.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
{
    int orderByIdx = context.Sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
    if (orderByIdx > 0)
    {
        context.Sql = context.Sql.Insert(orderByIdx, $"AND {_columnName} = {_activeValue} ");
    }
    ...
}
```

**Issues:**
- The `_activeValue` is interpolated directly into the SQL string (no parameterization). If a consumer configures `activeValue = "0; DROP TABLE x--"` (e.g. from a config file), it becomes part of every SELECT. Defaults to `"0"` but the API allows arbitrary strings.
- The `Contains("WHERE")` check can be fooled by a column named `WHERE_FOO` (rare) or by a SELECT that contains the literal word `WHERE` in a string literal (rare).
- The DELETE→UPDATE rewrite (line 70) does not handle multi-statement or CTE-prefixed DELETE.

**Fix approach:** Parameterize `_activeValue` as `@softDeleteActiveValue` and use a real DbParameter; for DELETE rewrite, detect prefix more strictly (e.g. `context.Sql.TrimStart().StartsWith("DELETE FROM ", StringComparison.OrdinalIgnoreCase)` already used — but parsing `tablePart` by string index `WHERE` is fragile).

### 5. `Reflection` use to instantiate external provider types

**Severity:** LOW (supply-chain + load-time risk)
**Files:**
- `DBTools/Providers/PostgresProvider.cs:23-29,33-37,42-46` — `Type.GetType("Npgsql.NpgsqlConnection, Npgsql")`
- `DBTools/Providers/MySqlProvider.cs:23-30,34-41,46-52` — `Type.GetType("MySqlConnector.MySqlConnection, MySqlConnector")`
- `DBTools/Providers/SqliteProvider.cs:22-28,32-36,41-45` — `Type.GetType("Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite")`

**Issue:** `Type.GetType(assemblyQualifiedName)` is brittle — it depends on the exact assembly version being loaded. If Npgsql is loaded transitively at a different version (e.g. via a transitive package), `Type.GetType("Npgsql.NpgsqlConnection, Npgsql")` returns `null` even though Npgsql *is* loaded — it just doesn't match the unqualified assembly name. The error message ("Npgsql package is not available") is misleading in that case.

**Fix approach:** Iterate loaded assemblies via `AppDomain.CurrentDomain.GetAssemblies()` and look up by `Type.Name == "NpgsqlConnection"`. Or wrap the lookup in a small helper.

### 6. No connection-string secret management helper

**Severity:** LOW
**Files:** entire `Configuration/` namespace

There is no integration with `IConfiguration` or environment variables for secrets. Users who put `Password = "…"` directly in `config.json` ship the secret in the repo (the example config at `DBTools/config.json.example:6` literally has `"Password": "YourPassword"` as a placeholder). The library neither warns nor helps.

**Recommendation:** Document an `IDbConfiguration` implementation that reads `Password` from `Environment.GetEnvironmentVariable("DBTOOLS_PASSWORD")` or a `SecretClient`. Add a `Microsoft.Extensions.Configuration.Binder` integration.

---

## Performance Bottlenecks

### 1. Per-call `Type.GetProperties()` reflection in `MapDataViewToModels`

**Severity:** MEDIUM (hot-path allocation)
**Files:**
- `DBTools/Controllers/LinqHelper.cs:612-614` — `var properties = typeof(TModel).GetProperties(...).ToDictionary(...)`
- `DBTools/Context/DbSet.cs:258-260` — same
- `DBTools/Controllers/AsyncLinqHelper.cs:376-378` — same
- `DBTools/Linq/JoinQuery.cs:209-215` — same per-row dictionary rebuild

**Issue:** Every `Select` call rebuilds the property→column dictionary. For a 100-row table that's 100 reflection scans. Property lookups via `Type.GetProperties()` allocate arrays, `BindingFlags` filtering allocates enumerators, and `.ToDictionary` allocates a fresh dictionary each call.

**Impact:** 5–10% CPU on reflection-heavy workloads; observable in tight loops or high-RPS services.
**Fix approach:** Cache the `Dictionary<string, PropertyInfo>` per `TModel` in a `ConcurrentDictionary<Type, PropertyInfo[]>`. Already done in `EntityMappingResolver._cache`, but not surfaced to the mapping code paths.

### 2. `Convert.ChangeType` for every cell during mapping

**Severity:** LOW
**Files:**
- `DBTools/Controllers/LinqHelper.cs:629-642`
- `DBTools/Controllers/AsyncLinqHelper.cs:392-403`
- `DBTools/Context/DbSet.cs:274-285`
- `DBTools/Linq/JoinQuery.cs:291-302`

**Issue:** Each cell goes through `Convert.ChangeType(value, propertyType)` even when types already match. `Convert.ChangeType` boxes, looks up `IConvertible.ToType` via reflection (`Int32 → Double → Object`), and is one of the slowest paths in BCL.

**Fix approach:** Short-circuit when types match (`if (propertyType == value.GetType()) property.SetValue(model, value); return;`) — the existing code DOES this short-circuit but only compares `property.PropertyType` to `value.GetType()` (line 629). For nullable<int> with DB returning `int`, the short-circuit fails because `Nullable<int>` ≠ `int` — and we then fall into `Convert.ChangeType(value, underlyingType)`. Performance can be improved by fast-pathing common DB-to-CLR pairs (Int32↔Int32, String↔String, etc.) with explicit `(T)value` casts.

### 3. `BulkOperations.BulkInsertGenericAsync` issues one INSERT per entity (not batched INSERT VALUES)

**Severity:** MEDIUM
**File:** `DBTools/Bulk/BulkOperations.cs:212-254`

For non-SQL-Server providers, "batch" actually means one `INSERT` per entity inside a single transaction — not `INSERT INTO t (cols) VALUES (…), (…), (…);`. Each row is its own round trip. For 10k rows that's 10k round trips even though `batchSize` is 1000.

**Impact:** Bulk insert performance on PostgreSQL/MySQL/SQLite is roughly 100–1000x slower than it should be.
**Fix approach:** Construct a single multi-row `INSERT` per batch with `string.Join(",", paramPlaceholders)`; send one statement per batch within the transaction.

### 4. `ExecuteReaderAsync` reads entire `DataTable` into memory before returning

**Severity:** MEDIUM (large-result-set risk)
**File:** `DBTools/Core/AsyncSqlClient.cs:354-397`

`dt.Load(reader)` materializes the full result set. For a SELECT returning 10M rows this loads all 10M into memory before any caller can act. There is no streaming API.

**Fix approach:** Expose an `IAsyncEnumerable<DataRow>` variant or a callback `Func<DbDataReader, Task>` that streams.

### 5. `EntityMappingResolver._fluentConfigs` dict allows only one config per type

**Severity:** LOW
**File:** `DBTools/Mapping/EntityMappingResolver.cs:39-43`

```csharp
_fluentConfigs[typeof(TModel)] = configuration;
_cache.TryRemove(typeof(TModel), out _);
```

Re-registering replaces the previous config silently. Two DbContexts mapping the same entity differently will conflict.

---

## Fragile Areas

### 1. `DbExpressionTranslator.BuildSql` is one large method (~80 lines) with many string mutations

**Severity:** MEDIUM (cyclomatic complexity)
**File:** `DBTools/Linq/DbExpressionTranslator.cs:424-515`

The method handles SELECT/CONST/ANY/FIRST/SINGLE/SKIP/TAKE branches and rewrites SQL with `Clear()/Append(...)` and `Substring(7)` slicing. The `Substring(7)` slice ("Skip 'SELECT '") will silently corrupt SQL if a future feature inserts whitespace, casing, or `SELECT` keyword inside a CTE. Hard to test exhaustively.

**Fix approach:** Refactor into discrete helpers (`BuildCountSql`, `BuildSingleSql`, etc.) that return strings. Or build the SQL from an AST in a single forward pass.

### 2. `LinqHelper.ParseExpression` and friends compile-and-execute user expressions

**Severity:** MEDIUM (security-adjacent + perf)
**Files:**
- `DBTools/Controllers/LinqHelper.cs:528-538`
- `DBTools/Context/DbSet.cs:204-213`
- `DBTools/Linq/DbExpressionTranslator.cs:231-239, 287-303, 356-365, 411-419`
- `DBTools/Controllers/AsyncLinqHelper.cs:301-311`

For any expression tree node the visitor doesn't recognise, the code does `Expression.Lambda(expression).Compile().DynamicInvoke()` and treats the result as a constant. This:
- compiles a new delegate per call (allocation + JIT);
- silently transforms `u => SomeStaticMethod(u.Email)` into "evaluate method, pass scalar" — meaning any code reachable from the captured closure runs inside the SQL builder;
- if the expression references a column not in scope (typo `u.Emial` instead of `u.Email` for a value-type, where `Emial` is `int 0`), the result is silently treated as constant `0`.

**Impact:** Reflection-based code execution on every unsupported LINQ predicate. Performance penalty in hot loops. Information disclosure if a captured variable contains sensitive material (it is materialised into a SQL parameter).
**Fix approach:** Reject unsupported nodes with `NotSupportedException` (the safer default — already the policy of `DbExpressionTranslator`); require users to extract closure values into local variables first.

### 3. `BulkOperations.CreateDataTable` uses reflection to enumerate entities — full scan + boxing

**Severity:** LOW
**File:** `DBTools/Bulk/BulkOperations.cs:257-280`

Per-entity `prop.PropertyInfo.GetValue(entity)` boxes value types. Combined with per-cell DBNull checks, allocation cost is significant for 100k-row bulks.

**Fix approach:** Cache a compiled `Func<TEntity, object[]>` once per `TEntity` that returns the column values as an object array, then iterate via the cached delegate.

### 4. `DbContext.ExecuteInsertAsync` and friends depend on global static `EntityMappingResolver` cache

**Severity:** MEDIUM (test isolation + lifecycle)
**File:** `DBTools/Context/DbContext.cs:200, 224, 255`

Cache poisoning concern already documented under "Tech Debt #6". Additionally, `SaveChangesAsync` (line 116-154) iterates `_changeTracker.GetPendingEntries()` and uses the cached mapping — if the cached mapping changes between Add() and SaveChangesAsync(), the SQL generated is inconsistent with the model state.

### 5. `UpdateAsync(TModel model, Expression<Func<TModel, bool>> predicate, CancellationToken)` in `AsyncLinqHelper` uses `values.Select(v => v?.ToString() ?? "")` — value is lost

**Severity:** MEDIUM (data corruption)
**File:** `DBTools/Controllers/AsyncLinqHelper.cs:178-181`

```csharp
var (fields, values) = ExtractFieldsAndValues(model, excludePrimaryKey: false);
var stringValues = values.Select(v => v?.ToString() ?? "").ToArray();
return await _client.UpdateAsync(fields, _tableName, stringValues, result.WhereClause, result.Parameters.ToArray(), ct).ConfigureAwait(false);
```

The `AsyncSqlClient.UpdateAsync` overload (line 200-223) takes `string[] values` and forwards them as `DbNull.Value` if null, but for non-null values it passes the *string* — not the original typed object. If the model has `double` 3.14, this serialises to `"3,14"` (or `"3.14"` depending on culture), and the string is then passed as a *string parameter* to the SQL update. SQL Server coerces `"3.14"` to numeric 3.14 for numeric columns — but only if the connection uses a compatible culture. On a `de-DE` server this can silently write `314` or fail.

**Impact:** Numeric/DateTime fields in async `UpdateAsync` are culture-sensitive round-trips through `string`.
**Fix:** The `AsyncSqlClient.UpdateAsync(string[] values)` overload has the wrong type signature. It should accept `object[]` and forward directly. Track this as a typed-overload bug.

### 6. `Linq.cs` file is 812 lines with mixed concerns

**Severity:** LOW (maintainability)
**File:** `DBTools/Controllers/Linq.cs:1-812`

Contains: WhereEquals/WhereContains/WhereStartsWith/WhereEndsWith (×4), WhereIn/WhereNotIn, FirstOrDefaultByProperty/SingleByProperty/SingleOrDefaultByProperty/CountByProperty/AnyByProperty/DeleteByProperty/UpdateByProperty, WhereByExample, InsertAndFind, UpdateWhere/DeleteWhere (×3 arities each), InsertOrUpdate, GetOrCreate, AsQueryable override, InnerJoin/LeftJoin, helper static methods. Total of ~30+ public methods.

**Fix approach:** Split into `LinqFilterExtensions.cs`, `LinqJoinExtensions.cs`, `LinqMutationExtensions.cs`, keep `Linq<T>` as the orchestrator.

### 7. `SqlClient.Update` overload (deprecated path) at line 362-410 still builds SQL via `String.Format` with manually-formatted SET clause

**Severity:** LOW (security, correctness)
**File:** `DBTools/Core/SqlClient.cs:385-397`

```csharp
for (int cont = 0; cont < _fields.Length; cont++)
{
    string paramName = "@param" + cont;
    setClause += _fields[cont] + "=" + paramName + ",";
    parameters.Add(_provider.CreateParameter(paramName, _values[cont] ?? (object)DBNull.Value));
}
setClause = setClause.Substring(0, setClause.Length - 1);
String query = String.Format("UPDATE {0} SET {1} WHERE {2}", _table, setClause, condition);
```

The `condition` is still raw (validated only for table). Field names are validated (`IsValidIdentifier`). But the SET-clause uses positional `@param0..N` while the WHERE can use `@param0..N` too — the two namespaces collide if the WHERE clause references `@param0` (e.g. `id = @param0`) because `parameters` only contains SET values. The newer overload at line 421-476 separates `@param*` and `@whereParam*` — the older one does not.

**Impact:** If a user calls the legacy `Update(fields, table, values, "id = @param0", new object[]{1})` shape, they would expect the WHERE `@param0` to be added to parameters — but the legacy overload does not accept WHERE parameters; it uses positional `@param*` only.
**Fix:** Mark `[Obsolete]` with migration message (some legacy methods already are; this one is not).

---

## Dependencies at Risk

### 1. Reflection-based loading of Npgsql/MySqlConnector/Microsoft.Data.Sqlite without declared dependencies

**Severity:** MEDIUM (build-time confusion)
**Files:**
- `DBTools/DBTools.csproj:36-51` — only `Microsoft.Data.SqlClient` is a direct dependency; no Npgsql/MySqlConnector/Microsoft.Data.Sqlite reference.
- `DBTools/Providers/PostgresProvider.cs:24-29` etc. — relies on reflection to find Npgsql.

**Risk:** A user installs `DBTools` and gets a runtime exception when they try to use Postgres, because `Npgsql` isn't a transitive dependency. The error message ("Npgsql package is not available") is helpful, but the wiring is fragile:
- Reflection lookup is by string assembly name `Npgsql` (see Security #5).
- Version conflicts are not handled (multiple Npgsql versions in the dependency graph).
- The package metadata `<PackageTags>` advertises `postgresql` support (DBTools.csproj:26) but the project does not actually reference the driver.

**Migration plan:** Either:
(a) Move provider implementations into separate packages (DBTools.PostgreSQL, DBTools.MySQL, DBTools.SQLite) that depend on the driver, or
(b) Add `<PackageReference Include="Npgsql" Version="..." />` etc. as `<PrivateAssets>all</PrivateAssets>` optional references using `<IncludeAssets>`.

### 2. `Microsoft.Extensions.*` packages pinned to version `10.0.1`

**Severity:** LOW (future-proofing)
**File:** `DBTools/DBTools.csproj:36-47`

`Microsoft.Extensions.Configuration`, `Microsoft.Extensions.DependencyInjection`, etc. are pinned to a non-existent major version (`10.0.1`). As of this analysis date (2026-06-21), .NET 10 is the LTS released Nov 2025; the version is plausible. But pulling in 8 such `Microsoft.Extensions.*` packages at the major version means any consumer using an older `Microsoft.Extensions.Configuration` (e.g. 8.0.0) will see binding redirects or package downgrade warnings.

**Migration plan:** Either (a) depend on `Microsoft.Extensions.DependencyInjection.Abstractions` only and let the consumer pick their version, or (b) explicitly mark the major version expected (`.NET 8.0` runtime packages).

### 3. `NPOI 2.7.3` referenced but unused in source

**Severity:** LOW
**File:** `DBTools/DBTools.csproj:48` — `<PackageReference Include="NPOI" Version="2.7.3" />`
**File:** `DBTools/DBTools.csproj:49-50` — `Portable.BouncyCastle`, `SharpZipLib` also referenced

`grep` across the source shows no `using NPOI;` or `using org.apache.poi;`. These packages are dead weight — they bloat the NuGet package and create supply-chain attack surface.

**Migration plan:** Remove the unused package references. If they're reserved for an unfinished Excel export feature, mark with a comment and a future-version tag.

### 4. `Microsoft.Data.SqlClient 5.2.2`

**Severity:** LOW
**File:** `DBTools/DBTools.csproj:37`

Latest stable 5.2.x as of mid-2026 is fine. No known CVEs at this version. **Recommendation:** Subscribe to GitHub Security Advisories for `Microsoft.Data.SqlClient` and bump on minor releases.

---

## Test Coverage Gaps

| Area | Test Coverage | Risk |
|------|---------------|------|
| `SqlClient` parameterized CRUD (Select/Insert/Update/Delete with parameters) | `SqlClientParameterizedQueryTests.cs` (114 lines) — high | LOW |
| `SqlClient` validation | `SqlClientValidationTests.cs` (60 lines) — medium | LOW |
| `SqlClient` static query builders | `SqlClientQueryBuilderTests.cs` (101 lines) — high | LOW |
| `SqlClient` connection lifecycle | `SqlClientConnectionTests.cs` (97 lines) — medium | LOW |
| `Linq<T>` (modern) | `LinqTests.cs` (355 lines) — medium | MEDIUM (only happy paths) |
| `LinqHelper<T>` (legacy) | `LinqHelperTests.cs` (274 lines) — medium | MEDIUM |
| `AsyncLinqHelper<T>` | `LinqHelperLinqTests.cs` (252 lines) — minimal | **HIGH** (whole async layer under-tested) |
| `DbExpressionTranslator` (LINQ→SQL) | indirect via `LinqTests` and `DbQueryLinqTests.cs` (238 lines) | MEDIUM |
| `DbContext` + `DbSet` + `ChangeTracker` | **NOT FOUND** in test files (only mentioned in `TestDocumentation.md`) | **HIGH** |
| `BulkOperations` (SqlServer + generic) | **NOT FOUND** | **HIGH** |
| `BulkOperations(AsyncSqlClient)` constructor (the bug from Concern §Known Bugs #1) | **NOT FOUND** | **HIGH** |
| `DbToolsTransaction` (sync + async commit/rollback, dispose patterns) | **NOT FOUND** | HIGH |
| `Interceptors/*` (Audit / Logging / SoftDelete) | **NOT FOUND** | HIGH |
| `EntityMappingResolver` (attribute + fluent cache + concurrency) | **NOT FOUND** | MEDIUM |
| `FluentConfiguration` / `EntityBuilder<T>` / `EntityTypeConfiguration<T>` | **NOT FOUND** | MEDIUM |
| `DataExport.ToCsv` / `ToDataTable` | `DataExportTests.cs` (141 lines) — medium | LOW |
| `ProviderDialectTests` (per-provider Upsert/Paging/Quote) | `ProviderDialectTests.cs` (401 lines) — good | LOW |
| `DbProviderFactory` | `DbProviderFactoryTests.cs` (152 lines) — good | LOW |
| Bug regression | `BugRegressionTests.cs` (298 lines) — covers 7 known regressions | LOW |

**Key gaps requiring test coverage:**

1. **`DbContext.SaveChangesAsync` lifecycle** — concurrency, change tracking, FK detection, ordering of inserts/updates/deletes.
2. **`DbContext.Dispose` / `DisposeAsync` and `_disposed` flag interactions** — the `_disposed` guard at `DbContext.cs:289-303` is not tested.
3. **`BulkOperations(AsyncSqlClient client)`** — the bug from §Known Bugs #1 is not caught because the constructor that ignores the client's provider has no tests.
4. **`SoftDeleteInterceptor`** — the string surgery on SQL is fragile and has zero test coverage. The bypass case "WHERE in column name or string literal" is not exercised.
5. **`AsyncSqlClient.ExecuteScalarAsync` / `BeginTransactionAsync`** — async-only paths under-tested.
6. **`EntityMappingResolver` cache + fluent config race conditions** — concurrent registration tests are absent.
7. **`IDbProvider.QuoteIdentifier` for each provider** — partial coverage; only the table-vs-field distinction is tested.
8. **`PostgresProvider`/`MySqlProvider`/`SqliteProvider` create-on-reflection failure paths** — what happens when the assembly is not loaded? Is the exception message actionable?

---

## Missing Critical Features

### 1. No built-in connection-pool statistics / metrics

**Severity:** LOW (observability)
There is no interceptor that surfaces Npgsql / SqlClient pool stats (idle/busy connections). For production tuning this is a gap. The `LoggingInterceptor` covers only per-query duration; the request-scoped pool state is invisible.

### 2. No retry policy / transient-fault handling

**Severity:** MEDIUM (production resilience)
Transient errors (connection drops, deadlocks, throttling in Azure SQL) are not retried. The library propagates exceptions to callers, who must wrap their own retry. For SqlClient this typically means `Microsoft.Data.SqlClient.SqlConnection.SqlConnectionStringBuilder.ConnectRetryCount` — which is NOT set anywhere in the connection-string builders in this project (see `DBTools.cs:191`, `DbToolsOptions.cs:96-101`).

### 3. No bulk upsert (only bulk insert/update/delete separately)

**Severity:** LOW
`BulkOperations` exposes `BulkInsertAsync`, `BulkUpdateAsync`, `BulkDeleteAsync`, but not `BulkUpsertAsync`. The `Linq<T>.InsertOrUpdate` method (line 602-635) handles upsert one-record-at-a-time; a bulk equivalent is missing.

### 4. No `CancellationToken` propagation on legacy `SqlClient` and `LinqHelper<T>` paths

**Severity:** MEDIUM
`SqlClient.Select(...)`, `SqlClient.Insert(...)`, `SqlClient.Update(...)`, etc. all block the calling thread on a network operation with no way to cancel. The async equivalents (`AsyncSqlClient`) accept `CancellationToken`. But the **most common path** (`SqlClient` → `DataView` return) cannot be cancelled. For long-running queries on UI threads or in worker pools this is a hazard.

### 5. No savepoints / nested transactions

`DbContext.SaveChangesAsync` opens one transaction. There is no API to nest savepoints within a caller-supplied outer transaction (EF Core has `SaveChanges(acceptAllChangesOnSuccess: false)` and `UseTransaction` — this library does not).

### 6. No multi-result-set reading

`DataTable.Load(reader)` (per the comment at `DBTools.cs:309-310`) reads only the first result set. Stored procedures returning multiple result sets are silently truncated.

---

## Scaling Limits

### 1. Connection lifecycle is per-call (no pooling customisation)

**Severity:** LOW
Each `SqlClient.Select(...)` opens and closes a connection in a `using` block. The underlying ADO.NET pool reuses TCP connections, but the library does not expose pool sizing knobs (`Min Pool Size`, `Max Pool Size`, `Load Balance Timeout`). For high-concurrency deployments these defaults may be suboptimal.

### 2. `BulkOperations` with batched inserts is single-threaded

**Severity:** MEDIUM
`BulkInsertGenericAsync` (`BulkOperations.cs:212-254`) loops sequentially. No parallelism, no `Parallel.ForEachAsync`. For 1M-row inserts on Postgres this is many minutes.

---

## Recommended Priorities

If the team has time to address concerns, here is a suggested ordering by **risk reduction per hour of work**:

1. **Add `BulkOperations(AsyncSqlClient)` test** + fix provider-ignoring bug (Known Bugs #1) — ~1 hour, prevents silent data corruption.
2. **Mark `SqlClient.SelectRaw` and `Select(query_without_select, …)` `[Obsolete]`** — ~30 min, removes dangerous footguns.
3. **Default `TrustServerCertificate` to `false`** + add documentation — ~1 hour, improves security posture.
4. **Add a "no-block" `SaveChanges(CancellationToken)` overload** — ~30 min, removes deadlock surface.
5. **Add tests for `SoftDeleteInterceptor`** — ~2 hours, the SQL-string surgery is the highest-risk un-tested code.
6. **Extract shared `ExpressionToSqlVisitor`** — ~4 hours, prevents drift across the 4 copies.
7. **Remove unused `NPOI`/`BouncyCastle`/`SharpZipLib`** — ~15 min, supply-chain attack-surface reduction.
8. **Cache property mappings per `TModel`** — ~2 hours, measurable perf win.
9. **Convert `BulkInsertGenericAsync` to true multi-row INSERTs** — ~3 hours, big perf win.
10. **Default `LoggingInterceptor` to redact sensitive parameter values** — ~1 hour.

---

## Notes

- **Forbidden files** (`.env`, `*.pem`, etc.) were never read. `DBTools/config.json.example` and `DBToolsUnitTest/config.json.example` exist but contain placeholder values ("YourPassword", "YourUsername"), not real secrets.
- **No TODO/FIXME/HACK/XXX markers** found anywhere in the codebase. All concerns above are derived from code reading.
- **Recent activity**: the bug-regression test file (`BugRegressionTests.cs`) and the migration of providers to `IDbProvider` (SQL Server → Postgres/MySQL/SQLite) are recent (small file count, clean naming) but the underlying `DBTools`/`SqlClient` legacy API is preserved. The library is in transition between legacy and modern shapes.
- The `TestDocumentation.md` documents 96 tests; the actual test count in the repository is significantly higher (we count ~24 test files with multiple test methods each — closer to ~150 tests).