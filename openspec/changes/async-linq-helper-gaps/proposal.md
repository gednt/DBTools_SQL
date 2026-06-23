## Why

The `AsyncLinqHelper<T>` library lacks Skip/Take pagination, OrderBy sorting, and a public transaction API — forcing developers to fall back to raw `IAsyncSqlClient` string-interpolated SQL. Additionally, the existing `Linq<T>.AsQueryable()` deferred execution path hardcodes SQL Server paging syntax instead of using the multi-provider `IDbProvider.BuildPagingClause()` infrastructure. These gaps block full LINQ-based repository adoption across all database providers.

## What Changes

- **Fix: Multi-provider paging in DbExpressionTranslator** — `BuildSql()` currently hardcodes SQL Server `OFFSET/FETCH` syntax. Change it to call `IDbProvider.BuildPagingClause()` for correct dialect translation across SQLite, SQL Server, PostgreSQL, MySQL
- **New: Async deferred execution for AsyncLinqHelper** — `WhereAsync()` currently executes immediately. Add an `AsAsyncQueryable()` pattern that returns a deferred `IQueryable<T>` with full Skip/Take/OrderBy support
- **New: Public BeginTransactionAsync on AsyncLinqHelper** — Expose the existing `IDbTransaction`/`DbToolsTransaction` infrastructure that already exists but is not accessible from `AsyncLinqHelper`
- **New: SQLite isolation level mapping** — `IsolationLevel.Serializable` should map to `BEGIN IMMEDIATE` for proper locking behavior

## Capabilities

### New Capabilities
- `async-linq-deferred`: Async deferred execution via IQueryable<T> pattern with full LINQ chain support (Where, OrderBy, Skip, Take)
- `async-linq-transactions`: Public transaction API on AsyncLinqHelper with isolation level support

### Modified Capabilities
- `linq-paging-multi-provider`: `DbExpressionTranslator.BuildSql()` shall use `IDbProvider.BuildPagingClause()` instead of hardcoding SQL Server paging syntax

## Impact

- Affects: `DbExpressionTranslator.BuildSql()`, `AsyncLinqHelper<T>`, `Linq<T>.AsQueryable()` behavior
- New public API surface: `AsyncLinqHelper.BeginTransactionAsync()`, `AsyncLinqHelper.AsAsyncQueryable()`
- Changes `Linq<T>.AsQueryable().Skip().Take()` behavior for non-SQL Server providers (but only to fix incorrect behavior)
- No breaking changes to synchronous `Linq<T>` methods beyond fixing multi-provider SQL generation
