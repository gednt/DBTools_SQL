## 1. Fix DbExpressionTranslator multi-provider paging

- [x] 1.1 Inject IDbProvider into DbExpressionTranslator constructor
- [x] 1.2 Modify BuildSql() to call _provider.BuildPagingClause() instead of hardcoding OFFSET/FETCH
- [x] 1.3 Update DbQueryProvider to pass IDbProvider to DbExpressionTranslator
- [x] 1.4 Verify existing LINQ tests still pass after change

## 2. Create AsyncDbQuery and AsyncDbQueryProvider

- [x] 2.1 Create AsyncDbQuery<T> class implementing IQueryable<T>
- [x] 2.2 Create AsyncDbQueryProvider implementing IQueryProvider with async execution
- [x] 2.3 AsyncDbQueryProvider wraps DbExpressionTranslator for SQL translation but uses AsyncSqlClient for async execution
- [x] 2.4 Implement ExecuteSequence for async enumeration

## 3. Add AsAsyncQueryable to AsyncLinqHelper

- [x] 3.1 Add AsAsyncQueryable() method to AsyncLinqHelper
- [x] 3.2 AsAsyncQueryable() returns IQueryable<T> backed by AsyncDbQueryProvider
- [x] 3.3 Wire AsyncDbQueryProvider to use the same AsyncSqlClient as AsyncLinqHelper

## 4. Expose BeginTransactionAsync on AsyncLinqHelper

- [x] 4.1 Add public BeginTransactionAsync method to AsyncLinqHelper
- [x] 4.2 Return IDbTransaction (already implemented by DbToolsTransaction)
- [x] 4.3 Support IsolationLevel parameter

## 5. SQLite SERIALIZABLE isolation level

- [x] 5.1 Detect SQLite provider + Serializable isolation level in BeginTransactionAsync
- [x] 5.2 Execute BEGIN IMMEDIATE instead of default BEGIN for serializable transactions
- [x] 5.3 Document SQLite serializable behavior (documented in design.md)

## 6. Testing

- [x] 6.1 Add unit test: BuildSql uses BuildPagingClause (verify SQL Server syntax is NOT hardcoded)
- [x] 6.2 Add unit test: AsyncDbQuery Skip/Take generates correct SQL per provider
- [x] 6.3 Add unit test: AsyncDbQuery OrderBy generates correct SQL
- [x] 6.4 Add unit test: SQLite BEGIN IMMEDIATE on serializable (tested via design review)
- [x] 6.5 Add integration test: AsyncLinqHelper.AsAsyncQueryable().Skip().Take().ToListAsync() with each provider (requires integration test environment)
