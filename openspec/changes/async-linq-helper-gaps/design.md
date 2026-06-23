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

## Open Questions

1. **Take(0) behavior**: `BuildPagingClause` generates `LIMIT 0` for `Take(0)`. Should `BuildSql()` detect this and return empty result set without executing query?
2. **Skip(0) with Take(n)**: `BuildPagingClause` generates `OFFSET 0` for `Skip(0)`. Some providers may not need this. Should we omit OFFSET entirely when skip is 0 or null?
