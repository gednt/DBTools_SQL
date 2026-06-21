<!-- generated-by: gsd-doc-writer -->

# Database Providers

How DBTools_SQL abstracts SQL dialect differences across SQL Server, PostgreSQL, MySQL, and SQLite through the `IDbProvider` interface and provider factory.

## Table of Contents

1. [Overview](#overview)
2. [IDbProvider Interface](#idbprovider-interface)
3. [Provider Factory](#provider-factory)
4. [Provider Comparison](#provider-comparison)
5. [Dialect Differences](#dialect-differences)
6. [NuGet Dependencies](#nuget-dependencies)
7. [Integration Points](#integration-points)
8. [Usage Examples](#usage-examples)
9. [Testing](#testing)

---

## Overview

DBTools_SQL targets four relational databases through a single provider-agnostic API built on `System.Data.Common`. Each database engine has its own SQL dialect for paging, identifier quoting, identity retrieval, and upsert semantics. The provider layer (`DBTools/Providers/`) encapsulates those differences so higher-level components—`SqlClient`, `AsyncSqlClient`, `BulkOperations`, and `Linq<T>`—can share the same code paths.

The central abstraction is `IDbProvider` in `DBTools/Abstractions/IDbProvider.cs`. Concrete implementations live in:

| File | Class | Database |
|------|-------|----------|
| `DBTools/Providers/SqlServerProvider.cs` | `SqlServerProvider` | SQL Server |
| `DBTools/Providers/PostgresProvider.cs` | `PostgresProvider` | PostgreSQL |
| `DBTools/Providers/MySqlProvider.cs` | `MySqlProvider` | MySQL |
| `DBTools/Providers/SqliteProvider.cs` | `SqliteProvider` | SQLite |

Provider selection is driven by the `DatabaseProvider` enum in `DBTools/Configuration/DbToolsOptions.cs` or by a provider name string resolved through `DbProviderFactory`.

---

## IDbProvider Interface

`IDbProvider` defines the contract every provider must implement:

| Member | Purpose |
|--------|---------|
| `CreateConnection(string)` | Returns a `DbConnection` for the provider's ADO.NET driver |
| `CreateCommand()` | Returns a provider-specific `DbCommand` |
| `CreateParameter(string, object)` | Returns a provider-specific `DbParameter` with null mapped to `DBNull.Value` |
| `ParameterPrefix` | Parameter placeholder prefix (all four providers use `@`) |
| `ProviderName` | Human-readable provider identifier (`"SqlServer"`, `"PostgreSQL"`, etc.) |
| `QuoteIdentifier(string)` | Escapes table/column names using the provider's quoting rules |
| `BuildPagingClause(int? skip, int? take, string orderByClause)` | Appends dialect-correct `SKIP`/`TAKE` or `LIMIT`/`OFFSET` SQL |
| `GetLastInsertedIdSql()` | Returns the SQL fragment to retrieve the last auto-generated identity value |
| `SupportsMerge` | Indicates whether the provider can perform upsert operations natively |
| `BuildUpsertSql(...)` | Generates a provider-specific upsert statement |

All providers report `SupportsMerge => true`, but each uses a different underlying mechanism (MERGE, ON CONFLICT, or ON DUPLICATE KEY UPDATE).

---

## Provider Factory

`DbProviderFactory` in `DBTools/Providers/DbProviderFactory.cs` is the single entry point for creating provider instances from configuration.

### From enum

```csharp
using DBTools.Configuration;
using DBTools.Providers;

IDbProvider provider = DbProviderFactory.Create(DatabaseProvider.PostgreSQL);
```

Supported enum values: `SqlServer`, `PostgreSQL`, `MySQL`, `SQLite`.

### From string (case-insensitive)

```csharp
IDbProvider provider = DbProviderFactory.Create("postgres"); // alias for PostgreSQL
```

Accepted string names:

| Input | Provider |
|-------|----------|
| `"SqlServer"` | `SqlServerProvider` |
| `"PostgreSQL"` or `"postgres"` | `PostgresProvider` |
| `"MySQL"` | `MySqlProvider` |
| `"SQLite"` | `SqliteProvider` |

Null, empty, or whitespace strings throw `ArgumentException`. Unknown names (for example, `"Oracle"`) also throw `ArgumentException` with a message listing supported providers.

### Dependency injection

When using `AddDbTools`, the factory is called internally to register `IDbProvider` as a singleton based on `DbToolsOptions.Provider`:

```csharp
services.AddDbTools(options =>
{
    options.Provider = DatabaseProvider.MySQL;
    options.ConnectionString = "...";
});
```

See [CONFIGURATION.md](CONFIGURATION.md) for connection string formats per provider.

---

## Provider Comparison

| Feature | SQL Server | PostgreSQL | MySQL | SQLite |
|---------|-----------|------------|-------|--------|
| **Provider class** | `SqlServerProvider` | `PostgresProvider` | `MySqlProvider` | `SqliteProvider` |
| **ADO.NET driver** | `Microsoft.Data.SqlClient` (direct reference) | `Npgsql` (reflection) | `MySqlConnector` or `MySql.Data` (reflection) | `Microsoft.Data.Sqlite` (reflection) |
| **Identifier quoting** | `[name]` | `"name"` | `` `name` `` | `"name"` |
| **Pagination** | `ORDER BY … OFFSET n ROWS FETCH NEXT m ROWS ONLY` | `ORDER BY … LIMIT m OFFSET n` | `ORDER BY … LIMIT m OFFSET n` | `ORDER BY … LIMIT m OFFSET n` |
| **Last inserted ID** | `SELECT SCOPE_IDENTITY()` | `RETURNING id` (appended to INSERT) | `SELECT LAST_INSERT_ID()` | `SELECT last_insert_rowid()` |
| **Upsert strategy** | `MERGE INTO …` | `INSERT … ON CONFLICT … DO UPDATE SET` | `INSERT … ON DUPLICATE KEY UPDATE` | `INSERT … ON CONFLICT … DO UPDATE SET` |
| **Parameter prefix** | `@` | `@` | `@` | `@` |
| **SupportsMerge** | `true` | `true` | `true` | `true` |

---

## Dialect Differences

### Identifier quoting

Each provider escapes reserved words and special characters in table and column names using its native quoting syntax. All implementations skip re-quoting identifiers that are already quoted, and return null or empty input unchanged.

| Provider | Input | Output |
|----------|-------|--------|
| SQL Server | `ColumnName` | `[ColumnName]` |
| SQL Server | `[Already]` | `[Already]` (unchanged) |
| PostgreSQL | `ColumnName` | `"ColumnName"` |
| MySQL | `ColumnName` | `` `ColumnName` `` |
| SQLite | `ColumnName` | `"ColumnName"` |

Embedded quote characters are escaped by doubling: `]` → `]]` in SQL Server, `"` → `""` in PostgreSQL/SQLite, and `` ` `` → ``` `` `` in MySQL.

### Pagination

`BuildPagingClause` accepts optional `skip` and `take` values plus an `orderByClause` string (column names or expressions, without the `ORDER BY` keyword).

**SQL Server** uses `OFFSET`/`FETCH` and requires an `ORDER BY` clause. When no order is supplied, it defaults to `ORDER BY (SELECT NULL)`. If both `skip` and `take` are null, the method returns an empty string.

```
 ORDER BY Id OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY
```

**PostgreSQL and SQLite** use `LIMIT`/`OFFSET`. An `ORDER BY` clause is appended only when `orderByClause` is non-empty. `OFFSET` is omitted when `skip` is zero or null.

```
 ORDER BY Id LIMIT 20 OFFSET 10
```

**MySQL** also uses `LIMIT`/`OFFSET`, but MySQL requires a `LIMIT` whenever `OFFSET` is used. When only `skip` is provided (no `take`), the provider emits a very large limit to satisfy this constraint:

```
 ORDER BY Id LIMIT 18446744073709551615 OFFSET 5
```

### Last inserted identity

Providers return different SQL fragments depending on how each engine exposes auto-generated keys:

| Provider | SQL fragment | Usage note |
|----------|-------------|------------|
| SQL Server | `SELECT SCOPE_IDENTITY()` | Executed as a separate query after INSERT |
| PostgreSQL | `RETURNING id` | Intended to be appended to the INSERT statement |
| MySQL | `SELECT LAST_INSERT_ID()` | Executed as a separate query after INSERT |
| SQLite | `SELECT last_insert_rowid()` | Executed as a separate query after INSERT |

PostgreSQL's `RETURNING id` assumes the primary key column is named `id`. Callers that need a different column name should handle identity retrieval explicitly.

### Upsert (merge) SQL generation

`BuildUpsertSql` generates dialect-specific upsert statements from a table name, column list, match column, and parameter prefix. Parameters are named `{prefix}p0`, `{prefix}p1`, … for column values, plus `{prefix}match` for the SQL Server MERGE source clause.

**SQL Server — MERGE**

```sql
MERGE INTO Users AS target
USING (SELECT @match AS Id) AS source ON target.Id = source.Id
WHEN MATCHED THEN UPDATE SET target.Name = @p0, target.Email = @p1
WHEN NOT MATCHED THEN INSERT (Id, Name, Email) VALUES (@p0, @p1);
```

**PostgreSQL — ON CONFLICT**

```sql
INSERT INTO Users (Id, Name, Email) VALUES (@p0, @p1, @p2)
ON CONFLICT (Id) DO UPDATE SET Name = EXCLUDED.Name, Email = EXCLUDED.Email;
```

**MySQL — ON DUPLICATE KEY UPDATE**

```sql
INSERT INTO Users (Id, Name, Email) VALUES (@p0, @p1, @p2)
ON DUPLICATE KEY UPDATE Name = VALUES(Name), Email = VALUES(Email);
```

MySQL matches on any unique key or primary key violation, not necessarily the column named in `matchColumn`. Ensure the match column corresponds to a unique index.

**SQLite — ON CONFLICT**

```sql
INSERT INTO Users (Id, Name, Email) VALUES (@p0, @p1, @p2)
ON CONFLICT(Id) DO UPDATE SET Name = excluded.Name, Email = excluded.Email;
```

Note the lowercase `excluded` alias in SQLite versus PostgreSQL's uppercase `EXCLUDED`.

---

## NuGet Dependencies

The core `DBTools` package ships with `Microsoft.Data.SqlClient` as a direct dependency, so SQL Server works out of the box. The other three providers load their ADO.NET drivers at runtime via reflection to avoid forcing consumers to install packages they do not need.

| Provider | Required NuGet package | Loaded type |
|----------|----------------------|-------------|
| SQL Server | `Microsoft.Data.SqlClient` (included in DBTools) | `Microsoft.Data.SqlClient.SqlConnection` |
| PostgreSQL | `Npgsql` | `Npgsql.NpgsqlConnection` |
| MySQL | `MySqlConnector` (preferred) or `MySql.Data` | `MySqlConnector.MySqlConnection` or `MySql.Data.MySqlClient.MySqlConnection` |
| SQLite | `Microsoft.Data.Sqlite` | `Microsoft.Data.Sqlite.SqliteConnection` |

If the required package is not present, calling `CreateConnection`, `CreateCommand`, or `CreateParameter` on PostgreSQL, MySQL, or SQLite providers throws `InvalidOperationException` with an installation hint.

---

## Integration Points

Provider instances flow through the library at several layers:

| Component | How the provider is used |
|-----------|-------------------------|
| **`AddDbTools` DI** | Registers `IDbProvider` singleton via `DbProviderFactory.Create(options.Provider)` |
| **`SqlClient` / `AsyncSqlClient`** | Creates connections and parameters through `_provider.CreateConnection()` and `_provider.CreateParameter()` |
| **`SqlQueryBuilder.GenerateSqlParameters(values, provider)`** | Provider-aware parameter creation (preferred over the SqlParameter-only overload for non-SQL Server) |
| **`BulkOperations`** | Uses provider connections and parameters for batch insert/update/delete |
| **`Linq<T>.InsertOrUpdate`** | Calls `provider.BuildUpsertSql()` and binds parameters with `provider.CreateParameter()` |
| **`DbContext`** | Resolves the provider from `DbToolsOptions.Provider` when constructed with explicit options |

`BuildPagingClause` and `GetLastInsertedIdSql` are defined on the interface and fully tested, but are not yet consumed by the core query builder or LINQ expression translator. Use them directly when building custom paginated queries or identity-retrieval logic.

---

## Usage Examples

### Select a provider manually

```csharp
using DBTools.Providers;

IDbProvider provider = DbProviderFactory.Create("SQLite");

using var connection = provider.CreateConnection("Data Source=mydb.sqlite");
connection.Open();

using var command = provider.CreateCommand();
command.Connection = connection;
command.CommandText = $"SELECT {provider.QuoteIdentifier("Name")} FROM {provider.QuoteIdentifier("Users")}";
// ...
```

### Build a paginated query

```csharp
var provider = DbProviderFactory.Create(DatabaseProvider.SqlServer);

string baseQuery = "SELECT Id, Name FROM Users WHERE Active = 1";
string paging = provider.BuildPagingClause(skip: 20, take: 10, orderByClause: "Name");
string fullQuery = baseQuery + paging;
// SQL Server result:
// SELECT Id, Name FROM Users WHERE Active = 1 ORDER BY Name OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY
```

### Generate an upsert statement

```csharp
var provider = DbProviderFactory.Create(DatabaseProvider.PostgreSQL);
var columns = new[] { "Id", "Name", "Email" };

string sql = provider.BuildUpsertSql("Users", columns, matchColumn: "Id", parameterPrefix: "@");
// Bind @p0, @p1, @p2 with column values before executing
```

### Configure via dependency injection

```csharp
services.AddDbTools(options =>
{
    options.Provider = DatabaseProvider.PostgreSQL;
    options.Host = "localhost";
    options.Port = "5432";
    options.Database = "myapp";
    options.Username = "appuser";
    options.Password = "secret";
});
```

---

## Testing

Provider behavior is covered by unit tests in `DBToolsUnitTest/Providers/`:

| Test class | Coverage |
|------------|----------|
| `ProviderDialectTests` | Identifier quoting, pagination, identity SQL, upsert SQL, parameter prefix, provider names, merge support, connection creation |
| `DbProviderFactoryTests` | Enum-based and string-based factory resolution, case insensitivity, PostgreSQL alias (`postgres`), invalid provider handling |

Run provider tests with the full unit test suite:

```bash
dotnet test DBToolsUnitTest/DBToolsUnitTest.csproj --filter "FullyQualifiedName~DBToolsUnitTest.Providers"
```

SQL Server connection tests run without additional packages. PostgreSQL, MySQL, and SQLite connection tests expect `InvalidOperationException` when their respective NuGet drivers are not loaded in the test project.
