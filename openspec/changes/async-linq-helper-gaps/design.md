## Context

The DBTools library has two LINQ-based query paths:

1. **Sync path**: `Linq<T>.AsQueryable()` → `DbQuery<T>` → `DbExpressionTranslator` → `BuildSql()` with hardcoded SQL Server paging
2. **Async path**: `AsyncLinqHelper<T>` → immediate execution, no Skip/Take/OrderBy support

Additionally, `IDbProvider.BuildPagingClause()` exists with multi-provider implementations (SQLite, SQL Server, PostgreSQL, MySQL) but is **never called** by the expression translator.

This design addresses both: fix the sync path's multi-provider support and add proper async deferred execution.

## Goals / Non-Goals

**Goals:**
- Fix `DbExpressionTranslator.BuildSql()` to use `IDbProvider.BuildPagingClause()` for all paging/ordering
- Add `AsyncLinqHelper.AsAsyncQueryable()` returning deferred `IQueryable<T>` with full Skip/Take/OrderBy
- Expose public `BeginTransactionAsync` on `AsyncLinqHelper`
- Map `IsolationLevel.Serializable` to `BEGIN IMMEDIATE` for SQLite

**Non-Goals:**
- Remove or deprecate existing immediate-execution async methods (WhereAsync, FirstOrDefaultAsync, etc.)
- Add change tracking or EF Core-like behavior
- Migration system integration

## Decisions

### 1. Fix BuildSql() to use IDbProvider

**Decision:** Modify `DbExpressionTranslator.BuildSql()` to call `_provider.BuildPagingClause(skip, take, orderByClause)` instead of hardcoding `OFFSET n ROWS FETCH NEXT m ROWS ONLY`.

**Current code (line 471-483):**
```csharp
// OFFSET / FETCH (SQL Server paging)
if (result.SkipCount.HasValue || result.TakeCount.HasValue)
{
    sql.Append($" OFFSET {result.SkipCount ?? 0} ROWS");
    if (result.TakeCount.HasValue)
        sql.Append($" FETCH NEXT {result.TakeCount.Value} ROWS ONLY");
}
```

**Proposed fix:** Inject `IDbProvider` into `DbExpressionTranslator` and replace with:
```csharp
if (result.SkipCount.HasValue || result.TakeCount.HasValue || !string.IsNullOrEmpty(result.OrderByClause))
{
    sql.Append(_provider.BuildPagingClause(result.SkipCount, result.TakeCount, result.OrderByClause));
}
```

**Note:** `BuildPagingClause` handles the ORDER BY internally, so `BuildSql()` should NOT append `ORDER BY` separately when paging is used.

### 2. Async deferred execution via AsAsyncQueryable

**Decision:** Add `AsAsyncQueryable()` method to `AsyncLinqHelper` that returns an async-enabled `IQueryable<T>` implementation.

**Rationale:** This mirrors the existing `Linq<T>.AsQueryable()` pattern (line 666 of Linq.cs) but uses `AsyncSqlClient` for execution. The async query provider wraps the sync `DbExpressionTranslator` since the SQL generation is identical — only the execution needs to be async.

```
AsyncLinqHelper<T>.AsAsyncQueryable()
    → AsyncDbQueryProvider (new, wraps DbExpressionTranslator)
    → AsyncDbQuery<T> (new)
    → IQueryable<T>
```

### 3. Public BeginTransactionAsync

**Decision:** Add `BeginTransactionAsync(IsolationLevel = ReadCommitted)` to `AsyncLinqHelper` returning `IDbTransaction`.

**Rationale:** The infrastructure already exists — `AsyncSqlClient.BeginTransactionAsync()` returns `DbToolsTransaction` which implements `IDbTransaction`. This just exposes it from `AsyncLinqHelper`.

**Implementation:**
```csharp
public async Task<IDbTransaction> BeginTransactionAsync(
    IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
    CancellationToken ct = default)
{
    var tx = await _client.BeginTransactionAsync(ct);
    if (isolationLevel == IsolationLevel.Serializable && _provider is SqliteProvider)
    {
        // SQLite: BEGIN IMMEDIATE for serializable
        await tx.RollbackAsync(ct);
        // Execute BEGIN IMMEDIATE manually
    }
    return tx;
}
```

### 4. SQLite SERIALIZABLE → BEGIN IMMEDIATE

**Decision:** When `IsolationLevel.Serializable` is requested on SQLite, execute `BEGIN IMMEDIATE` instead of the default `BEGIN`.

**Rationale:** SQLite's default deferred transaction doesn't provide serializable semantics. `BEGIN IMMEDIATE` acquires a write lock immediately, matching user expectations.

## Architecture

```
                    ┌─────────────────────────────────────────────┐
                    │           AsyncLinqHelper<T>                │
                    │                                             │
                    │  WhereAsync(...) → Task<List<T>> (immediate)│
                    │  AsAsyncQueryable() → IQueryable<T> (deferred)
                    │  BeginTransactionAsync() → IDbTransaction   │
                    └─────────────────────────────────────────────┘
                                        │
                    ┌───────────────────┴───────────────────┐
                    ▼                                       ▼
    ┌──────────────────────────┐        ┌──────────────────────────┐
    │   AsyncDbQueryProvider   │        │   AsyncSqlClient         │
    │   (new)                  │        │   (existing)             │
    │   Wraps DbExpression     │        │                          │
    │   Translator + async     │        │   BeginTransactionAsync()│
    │   execution              │        │   → DbToolsTransaction   │
    └──────────────────────────┘        └──────────────────────────┘
                    │                                       │
                    ▼                                       ▼
    ┌──────────────────────────┐        ┌──────────────────────────┐
    │   DbExpressionTranslator │        │   IDbProvider           │
    │   (existing, modified)   │        │   BuildPagingClause()   │
    │                          │        │   (existing, wired now)  │
    └──────────────────────────┘        └──────────────────────────┘
```

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| `BuildSql()` now depends on `IDbProvider` | Inject via constructor; `DbExpressionTranslator` already has `IQueryProvider` context |
| AsyncDbQueryProvider duplicates DbQueryProvider logic | Share translation logic; only execution differs (sync vs async DataView) |
| Breaking change for users relying on SQL Server paging from `Linq<T>.AsQueryable()` | This is a bug fix — users on other providers were getting incorrect SQL |
| SQLite SERIALIZABLE requires separate connection per transaction | Document this limitation; standard SQLite behavior |
| Dropping `new()` constraint on `AsyncLinqHelper<T>` / `AsyncDbQueryProvider<T>` | Existing consumers using `new TModel()` still work — the constraint is only relaxed, not removed. Document the new constructor requirement clearly. |
| `ModelFactory` runs per-row; adds a delegate invocation | Acceptable for typical workloads; per-row delegate cost is dwarfed by network/DB latency. |
| `DataRowRecordAdapter` adds a layer of indirection | Trivial adapter class; no measurable impact. |

## Open Questions

1. **Take(0) behavior**: `BuildPagingClause` generates `LIMIT 0` for `Take(0)`. Should `BuildSql()` detect this and return empty result set without executing query?
2. **Skip(0) with Take(n)**: `BuildPagingClause` generates `OFFSET 0` for `Skip(0)`. Some providers may not need this. Should we omit OFFSET entirely when skip is 0 or null?

---

## Extension: Custom Mapper Extensibility

### Context

Two patterns are common in domain-driven consumer codebases (e.g. Allied) and are incompatible with the current `new TModel()` hydration path in `AsyncLinqHelper<T>` / `AsyncDbQueryProvider<T>`:

1. **Private parameterless constructor** — entities are constructed via static `Create(...)` factories; the parameterless ctor is `private`. The `new()` constraint on `AsyncLinqHelper<TModel>` blocks compilation; `RuntimeHelpers.GetUninitializedObject` is the only workaround.
2. **Value-object properties** — sealed value objects with private constructors (e.g. `Currency`, `WalletName`) cannot be hydrated by `Convert.ChangeType` because there is no conversion path from `string` → sealed class.

The library needs to support both without forcing consumers to abandon factory-only construction or value-object types.

### Decision: Two extensibility points on EntityMapping / PropertyMapping

**Choice:** Add two optional delegates:

- `PropertyMapping.ValueConverter : Func<object, object>` — invoked during property hydration to convert the raw column value to the property's CLR type.
- `EntityMapping.ModelFactory : Func<IDataRecord, object>` — invoked once per row during entity hydration, replacing `new TModel()` + property setters.

Both are optional. When null, the existing behavior is preserved.

**Rationale:** Splits concerns — `ValueConverter` is per-property (single-column conversion), `ModelFactory` is per-entity (whole-row construction). Consumers register either or both via `PropertyBuilder.HasConversion` / `EntityBuilder.HasModelFactory`.

### Decision: Drop the `new()` constraint

**Choice:** Relax `where TModel : class, new()` to `where TModel : class` on both `AsyncLinqHelper<TModel>` and `AsyncDbQueryProvider<TModel>`.

**Rationale:** The constraint was needed for `new TModel()`. With `ModelFactory` as the supported alternative, the constraint is no longer necessary. Consumers who do not register a `ModelFactory` still need a public parameterless constructor — but the library no longer enforces it at the type system level.

### Decision: DataRowRecordAdapter wraps DataRow as IDataRecord

**Choice:** Provide an internal `DataRowRecordAdapter` that implements `System.Data.IDataRecord` over a wrapped `DataRow`. `ModelFactory` consumes the adapter via the `IDataRecord` interface, not `DataRow` directly.

**Rationale:** `IDataRecord` is the lowest-common-denominator data-access interface — works with any data source. Consumers that need `IDataReader` (e.g. streaming) can write their own adapter. The DataRow adapter is the minimum surface needed for `ModelFactory` to work with the existing `DataTable` / `DataRow` hydration path.

### Implementation Notes

#### `PropertyMapping.ValueConverter`

```csharp
public class PropertyMapping
{
    // existing fields...
    public Func<object, object> ValueConverter { get; set; }
}
```

Wired up from `PropertyBuilder.ReadConverter` inside `EntityTypeConfiguration<T>.Apply`.

#### `EntityMapping.ModelFactory`

```csharp
public class EntityMapping
{
    // existing fields...
    public Func<IDataRecord, object> ModelFactory { get; set; }
}
```

Wired up from `EntityBuilder<TModel>.HasModelFactory(...)` inside `EntityTypeConfiguration<T>.Apply`.

#### `AsyncDbQueryProvider.MapDataViewToModels` changes

```csharp
foreach (DataRow row in dt.Rows)
{
    if (_mapping.ModelFactory != null)
    {
        var record = new DataRowRecordAdapter(row);
        var model = _mapping.ModelFactory(record);
        models.Add(model);
        continue;
    }

    var entity = new TModel(); // existing path
    foreach (DataColumn column in dt.Columns)
    {
        if (columnLookup.TryGetValue(column.ColumnName, out var propMapping))
        {
            var raw = row[column.ColumnName];
            object value;
            if (raw == null || raw == DBNull.Value) continue;
            if (propMapping.ValueConverter != null)
                value = propMapping.ValueConverter(raw);
            else
                value = Convert.ChangeType(raw, propMapping.PropertyInfo.PropertyType);
            propMapping.PropertyInfo.SetValue(entity, value);
        }
    }
    models.Add(entity);
}
```

The same logic applies in `AsyncLinqHelper.MapDataTableToModels`.

#### `DataRowRecordAdapter`

```csharp
internal sealed class DataRowRecordAdapter : IDataRecord
{
    private readonly DataRow _row;
    private readonly DataTable _table;

    public DataRowRecordAdapter(DataRow row)
    {
        _row = row;
        _table = row.Table;
    }

    public object this[int i] => _row[i];
    public object this[string name] => _row[name];
    public int FieldCount => _table.Columns.Count;
    public string GetName(int i) => _table.Columns[i].ColumnName;
    public int GetOrdinal(string name) => _table.Columns.IndexOf(name);
    public object GetValue(int i) => _row[i];
    public bool IsDBNull(int i) => _row.IsNull(i);

    // explicit accessors delegate via Convert.ChangeType
    public string GetString(int i) => Convert.ToString(_row[i]);
    public int GetInt32(int i) => Convert.ToInt32(_row[i]);
    public Guid GetGuid(int i) => Guid.Parse(Convert.ToString(_row[i]) ?? "");
    public DateTime GetDateTime(int i) => Convert.ToDateTime(_row[i]);
    public decimal GetDecimal(int i) => Convert.ToDecimal(_row[i]);
    public bool GetBoolean(int i) => Convert.ToBoolean(_row[i]);

    // remaining IDataRecord members throw NotImplementedException
}
```
