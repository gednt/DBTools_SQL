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

---

## 7. Custom Mapper Extensibility (extension — value objects + factory-only construction)

Consumers (e.g. Allied) need two extensibility points to map entities whose construction is not compatible with `new TModel()`:

- `PropertyMapping.ValueConverter` — converts a raw column value to the property's CLR type (handles `string` → sealed value object, enums, etc.)
- `EntityMapping.ModelFactory` — creates the entity from a `IDataRecord`, bypassing `new TModel()` entirely

### 7.1 ValueConverter on PropertyMapping

- [x] 7.1.1 Add `Func<object, object> ValueConverter { get; set; }` to `PropertyMapping` in `DBTools/Mapping/EntityMapping.cs`
- [x] 7.1.2 In `EntityTypeConfiguration<TModel>.Apply`, copy `PropertyBuilder.ValueConverter` (the read converter) onto the matching `PropertyMapping`
- [x] 7.1.3 In `EntityMappingResolver.BuildMapping`, after attribute scan + fluent config, retain any `ValueConverter` already set on `PropertyMapping` (no special handling needed)

### 7.2 ModelFactory on EntityMapping

- [x] 7.2.1 Add `Func<IDataRecord, object> ModelFactory { get; set; }` to `EntityMapping` in `DBTools/Mapping/EntityMapping.cs`
- [x] 7.2.2 Add `EntityBuilder<TModel>.HasModelFactory(Func<IDataRecord, TModel>)` and a non-generic overload that returns `EntityBuilder<TModel>`
- [x] 7.2.3 In `EntityTypeConfiguration<TModel>.Apply`, copy the `ModelFactory` from the builder onto the `EntityMapping`

### 7.3 AsyncDbQueryProvider honors ModelFactory + ValueConverter

- [x] 7.3.1 In `AsyncDbQueryProvider<TModel>.MapDataViewToModels`, if `_mapping.ModelFactory != null`, use it: wrap each `DataRow` as `IDataRecord` and call `_mapping.ModelFactory(row)` for each row
- [x] 7.3.2 Otherwise, when setting a property value, look up the matching `PropertyMapping` and use `ValueConverter` if set; otherwise fall back to existing `Convert.ChangeType` logic
- [x] 7.3.3 Drop the `new()` constraint from `AsyncDbQueryProvider<TModel>` (replace with `where TModel : class`); reflection-based construction paths now need to support `ModelFactory`-created instances

### 7.4 AsyncLinqHelper honors ModelFactory + ValueConverter

- [x] 7.4.1 In `AsyncLinqHelper<TModel>.MapDataTableToModels`, mirror the same logic: `ModelFactory` first, then `ValueConverter`, then existing `Convert.ChangeType` fallback
- [x] 7.4.2 In `AsyncLinqHelper<TModel>.SelectAsync` / `WhereAsync` / `FirstOrDefaultAsync` paths, ensure the same mapping path is used (delegates to `MapDataTableToModels`)

### 7.5 DataRowRecordAdapter

- [x] 7.5.1 Create `DBTools/Linq/DataRowRecordAdapter.cs` — `internal sealed class` implementing `System.Data.IDataRecord` by delegating to a wrapped `DataRow`. Required members: `GetName(int)`, `GetOrdinal(string)`, `this[int]`, `this[string]`, `FieldCount`, `GetValue(int)`, `IsDBNull(int)`, and explicit `GetXxx(int)` accessors used by callers (e.g. `GetString`, `GetGuid`, `GetDateTime`, `GetInt32`, `GetDecimal`, `GetBoolean`).

### 7.6 Tests — ValueConverter

- [x] 7.6.1 Add `DBToolsUnitTest/Mapping/PropertyMappingValueConverterTests.cs`: unit test that `PropertyMapping.ValueConverter` is null by default, can be set, and is invoked by the hydration code
- [x] 7.6.2 Add test fixture entity with a sealed `Currency` value object (private constructor, `Create(string)` factory) and verify `HasConversion` on `PropertyBuilder` flows into `PropertyMapping.ValueConverter`
- [x] 7.6.3 Add a hydration test verifying primitive type conversions still work when no converter is set

### 7.7 Tests — ModelFactory

- [x] 7.7.1 Add `DBToolsUnitTest/Mapping/EntityMappingModelFactoryTests.cs`: unit test that `EntityMapping.ModelFactory` is null by default, can be set, and is invoked by `AsyncDbQueryProvider` / `AsyncLinqHelper`
- [x] 7.7.2 Add a test fixture entity with a `private` parameterless constructor and a `HasModelFactory(record => Entity.Hydrate(record))` registration; verify hydration works end-to-end through `AsAsyncQueryable().ToList()` against SQLite
- [x] 7.7.3 Add unit test for `DataRowRecordAdapter` — verify `GetName(i)`, `GetOrdinal(name)`, `this[i]`, `IsDBNull(i)` match the wrapped `DataRow`

### 7.8 ExtractFieldsAndValues — value object serialization

- [x] 7.8.1 Audit `AsyncLinqHelper.ExtractFieldsAndValues` — value object properties (e.g. `Currency`) return the object itself from `PropertyInfo.GetValue`. Add `PropertyMapping.WriteConverter` so `HasConversion(write, read)` flows the write side onto the mapping. Apply it in `ExtractFieldsAndValues` so value-object writes (Currency → string code) are stored correctly.
