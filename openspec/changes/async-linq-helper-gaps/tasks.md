## 1. Fix DbExpressionTranslator multi-provider paging

- [ ] 1.1 Inject IDbProvider into DbExpressionTranslator constructor
- [ ] 1.2 Modify BuildSql() to call _provider.BuildPagingClause() instead of hardcoding OFFSET/FETCH
- [ ] 1.3 Update DbQueryProvider to pass IDbProvider to DbExpressionTranslator
- [ ] 1.4 Verify existing LINQ tests still pass after change

## 2. Create AsyncDbQuery and AsyncDbQueryProvider

- [ ] 2.1 Create AsyncDbQuery<T> class implementing IQueryable<T>
- [ ] 2.2 Create AsyncDbQueryProvider implementing IQueryProvider with async execution
- [ ] 2.3 AsyncDbQueryProvider wraps DbExpressionTranslator for SQL translation but uses AsyncSqlClient for async execution
- [ ] 2.4 Implement ExecuteSequence for async enumeration

## 3. Add AsAsyncQueryable to AsyncLinqHelper

- [ ] 3.1 Add AsAsyncQueryable() method to AsyncLinqHelper
- [ ] 3.2 AsAsyncQueryable() returns IQueryable<T> backed by AsyncDbQueryProvider
- [ ] 3.3 Wire AsyncDbQueryProvider to use the same AsyncSqlClient as AsyncLinqHelper

## 4. Expose BeginTransactionAsync on AsyncLinqHelper

- [ ] 4.1 Add public BeginTransactionAsync method to AsyncLinqHelper
- [ ] 4.2 Return IDbTransaction (already implemented by DbToolsTransaction)
- [ ] 4.3 Support IsolationLevel parameter

## 5. SQLite SERIALIZABLE isolation level

- [ ] 5.1 Detect SQLite provider + Serializable isolation level in BeginTransactionAsync
- [ ] 5.2 Execute BEGIN IMMEDIATE instead of default BEGIN for serializable transactions
- [ ] 5.3 Document SQLite serializable behavior

## 6. Testing

- [ ] 6.1 Add unit test: BuildSql uses BuildPagingClause (verify SQL Server syntax is NOT hardcoded)
- [ ] 6.2 Add unit test: AsyncDbQuery Skip/Take generates correct SQL per provider
- [ ] 6.3 Add unit test: AsyncDbQuery OrderBy generates correct SQL
- [ ] 6.4 Add unit test: SQLite BEGIN IMMEDIATE on serializable
- [ ] 6.5 Add integration test: AsyncLinqHelper.AsAsyncQueryable().Skip().Take().ToListAsync() with each provider
