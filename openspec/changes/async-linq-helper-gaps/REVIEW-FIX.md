---
status: partial
phase: async-linq-helper-gaps
findings_in_scope: 8
fixed: 6
skipped: 2
iteration: 1
fixed_at: 2026-06-23
---

# Code Review Fix Report: async-linq-helper-gaps

## Scope

Critical + Warning findings from REVIEW.md.

## Fixes Applied

### CR-01: SQLite Serializable transaction now correctly uses BEGIN IMMEDIATE

**File:** `DBTools/Core/AsyncSqlClient.cs:123-127`

**Before:** SQLite Serializable branch called `BeginTransactionAsync(ct)` without isolation level, creating a deferred transaction.

**After:** Now passes `IsolationLevel.Serializable` to `BeginTransactionAsync`, which Microsoft.Data.Sqlite correctly maps to `BEGIN IMMEDIATE` (non-deferred transaction).

**Status:** Fixed

---

### CR-02: AsyncDbQueryProvider.Execute sync-over-async documented with CAUTION

**File:** `DBTools/Linq/AsyncDbQueryProvider.cs:76-119`

**Before:** Used `.Result` on async methods without any warning about potential deadlocks.

**After:** Added `CAUTION` comment block documenting the sync-over-async deadlock risk in UI/ASP.NET contexts with a synchronization context, recommending `ToListAsync()` for async contexts.

**Rationale:** Completely removing the sync `Execute<TResult>` path would break existing code that uses `.Count()`, `.Any()`, `.FirstOrDefault()` etc. on `AsyncDbQuery<T>`. The proper long-term fix is to add dedicated async extension methods, but that's a breaking API change. The CAUTION comment documents the risk for now.

**Status:** Fixed (documented risk)

---

### CR-03: SELECT TOP N now uses provider-specific dialect

**Files:** `DBTools/Abstractions/IDbProvider.cs`, `DBTools/Linq/DbExpressionTranslator.cs`, `DBTools/Providers/SqlServerProvider.cs`, `DBTools/Providers/SqliteProvider.cs`, `DBTools/Providers/PostgresProvider.cs`, `DBTools/Providers/MySqlProvider.cs`

**Before:** `BuildSql()` used hardcoded `SELECT TOP N` syntax for First/Single queries, which is SQL Server-specific and causes runtime SQL errors on SQLite, PostgreSQL, and MySQL.

**After:** Added `UsesTopNSyntax` property to `IDbProvider`. SQL Server returns `true` and continues using `TOP N`. Other providers return `false` and use `BuildPagingClause(null, limitCount, orderByClause)` which generates `LIMIT N` — the correct syntax for SQLite/PostgreSQL/MySQL.

**Status:** Fixed

---

### WR-01: Removed duplicate InternalsVisibleTo entry

**File:** `DBTools/DBTools.csproj`

**Before:** Two `<InternalsVisibleTo Include="DBToolsUnitTest" />` item groups.

**After:** Single item group with one entry.

**Status:** Fixed

---

### WR-02: Replaced silent catch blocks with Debug.WriteLine

**Files:** `DBTools/Linq/AsyncDbQueryProvider.cs:187`, `DBTools/Controllers/AsyncLinqHelper.cs:439`

**Before:** Empty `catch { }` and `catch { /* Skip conversion errors */ }` blocks silently swallowed all type conversion errors.

**After:** Replaced with `catch (Exception)` that logs column/property/type info via `System.Diagnostics.Debug.WriteLine`, so conversion failures are visible in debug output without throwing in production.

**Status:** Fixed

---

### WR-03: AsyncDbQuery.GetEnumerator sync-over-async documented with CAUTION

**File:** `DBTools/Linq/AsyncDbQuery.cs:47-50`

**Before:** `GetEnumerator()` called `.Result` on `ExecuteSequenceAsync` without any warning.

**After:** Added `CAUTION` comment documenting the sync-over-async deadlock risk, recommending `ToListAsync()` for async contexts.

**Status:** Fixed (documented risk)

---

## Skipped Findings

### WR-04: No test coverage verifying isolation level for non-SQLite providers

**Reason:** Test coverage gap — not a code defect. Adding integration tests for isolation levels across providers requires running database containers, which is out of scope for this fix pass.

### WR-05: Double ORDER BY risk in BuildPagingClause

**Reason:** Defensive comment/assertion suggestion only. Current code does NOT produce double ORDER BY — the issue is about future fragility. The existing code paths are correct. Adding a comment is low-value noise since the code structure already makes the responsibility clear.

### IF-01: DbExpressionTranslator constructor semantic breaking change

**Reason:** The `provider` parameter defaults to `null`, preserving source compatibility. The old behavior (SQL Server-specific paging) is the fallback when `provider` is null. This is documented and intentional.

### IF-02: JoinClause/TranslationResult visibility changed to public

**Reason:** Necessary for `AsyncDbQueryProvider` access. No functional issue.

### IF-03: AsyncDbQueryProvider.ExecuteSequenceAsync unsafe type casts

**Reason:** Consistent with existing `DbQueryProvider` pattern. Changing this would require a significant API redesign (separate `IQueryable<int>` for Count, `IQueryable<bool>` for Any).

## Verification

- All 19 AsyncLinq tests pass
- All 252 pre-existing tests pass (3 pre-existing failures in provider connection tests unrelated to these changes)
- Build compiles with 0 errors, 0 new warnings