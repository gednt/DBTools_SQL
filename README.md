# DBTools_SQL

A robust .NET library for multi-provider database operations with built-in security features, parameterized queries, LINQ expression support, and comprehensive data manipulation utilities. Supports **SQL Server**, **PostgreSQL**, **MySQL**, and **SQLite**.

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![C#](https://img.shields.io/badge/C%23-latest-green.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/license-MIT-orange.svg)](LICENSE)

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Configuration](#configuration)
- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Core Functionality](#core-functionality)
  - [Database Connection](#database-connection)
  - [CRUD Operations](#crud-operations)
  - [Query Builder](#query-builder)
  - [Data Export](#data-export)
- [Multi-Provider Support](#multi-provider-support)
- [LinqHelper - LINQ Expression Queries](#linqhelper---linq-expression-queries)
- [Linq - Property-Based Queries & JOINs](#linq---property-based-queries--joins)
  - [Query Methods](#query-methods)
  - [CRUD Operations](#linq-crud-operations)
  - [JOIN Support](#join-support)
  - [Deferred IQueryable Execution](#deferred-iqueryable-execution)
- [Connection Pooling](#connection-pooling)
- [Stored Procedures](#stored-procedures)
- [Query Result Caching](#query-result-caching)
- [Migration Tools](#migration-tools)
- [Security](#security)
- [API Reference](#api-reference)
- [Examples](#examples)
- [Contributing](#contributing)
- [License](#license)

## Features

- **Multi-Provider Support**: SQL Server, PostgreSQL, MySQL, and SQLite with provider-specific SQL dialects
- **Provider-Agnostic Core**: Uses `System.Data.Common` abstractions (`DbConnection`, `DbCommand`, `DbParameter`) internally
- **Parameterized Queries**: Built-in protection against SQL injection attacks
- **CRUD Operations**: Complete Create, Read, Update, Delete functionality
- **LINQ Expression Queries**: Lambda predicates like `Where(u => u.Age > 18)` with `LinqHelper<TModel>`
- **Property-Based Queries**: Type-safe `WhereEquals`, `WhereContains`, `WhereBetween`, etc. with `Linq<TModel>`
- **JOIN Support**: `InnerJoin` and `LeftJoin` with `JoinResult<TLeft, TRight>` and LINQ chaining
- **Deferred IQueryable**: SQL-translated `AsQueryable()` with `DbQuery<T>` for deferred execution
- **Provider-Aware Upsert**: `InsertOrUpdate` generates provider-appropriate SQL (MERGE, ON CONFLICT, ON DUPLICATE KEY)
- **Query Builder**: Helper utilities for dynamic query construction
- **Data Export**: Export data to CSV and convert CSV to DataTable
- **Configuration-Based**: JSON configuration file with provider selection
- **Type-Safe**: Generic object models for type-safe data handling
- **Input Validation**: Comprehensive identifier validation via `SqlValidator`
- **Dependency Injection**: Full DI support with `AddDbTools()` and provider auto-resolution
- **Error Handling**: Robust error handling and validation throughout
- **Connection Pooling Configuration**: Configurable pool size, connection lifetime, and idle timeout per provider
- **Stored Procedures**: Execute stored procedures with input, output, input/output parameters, and return values (sync and async)
- **Query Result Caching**: In-memory cache with configurable expiration, LRU eviction, table-based auto-invalidation, and statistics
- **Migration Tools**: Schema migration system with apply/rollback, transaction-per-migration, checksum verification, and fluent builder

## Installation

### Via Source
1. Clone the repository:
```bash
git clone https://github.com/gednt/DBTools_SQL.git
```

2. Add reference to your project:
   - In Visual Studio, right-click on your project → Add → Reference
   - Browse to the compiled `DBTools.dll`

3. Add the configuration file to your project root (see [Configuration](#configuration))

## Configuration

Create a `config.json` file in your application's root directory:

```json
{
  "Provider": "SqlServer",
  "Host": "localhost\\SQLEXPRESS",
  "Database": "YourDatabaseName",
  "Uid": "YourUsername",
  "Password": "YourPassword",
  "Port": "1433"
}
```

### Supported Providers

| Provider Value | Database | Connection String Format |
|---------------|----------|--------------------------|
| `SqlServer` | SQL Server | `Data Source=tcp:Host,Port;Initial Catalog=Database;User ID=Uid;Password=Password;TrustServerCertificate=True;` |
| `PostgreSQL` | PostgreSQL | `Host=Host;Port=Port;Database=Database;Username=Uid;Password=Password;` |
| `MySQL` | MySQL / MariaDB | `Server=Host;Port=Port;Database=Database;Uid=Uid;Pwd=Password;` |
| `SQLite` | SQLite | `Data Source=Database;` |

The `Provider` key determines both the SQL dialect and the connection string format. If omitted, it defaults to `SqlServer`.

### Configuration Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| Provider | Database provider (`SqlServer`, `PostgreSQL`, `MySQL`, `SQLite`) | No | `SqlServer` |
| Host | Server hostname or IP address | Yes* | - |
| Database | Database name (or file path for SQLite) | Yes | - |
| Uid | Database username | Yes* | - |
| Password | Database password | Yes* | - |
| Port | Server port | Yes* | 1433 |

*For SQLite, only `Database` is required (as the file path).

### Provider Configuration Examples

**SQL Server:**
```json
{
  "Provider": "SqlServer",
  "Host": "localhost\\SQLEXPRESS",
  "Database": "MyAppDb",
  "Uid": "sa",
  "Password": "YourPassword",
  "Port": "1433"
}
```

**PostgreSQL:**
```json
{
  "Provider": "PostgreSQL",
  "Host": "localhost",
  "Database": "myappdb",
  "Uid": "postgres",
  "Password": "YourPassword",
  "Port": "5432"
}
```

**MySQL:**
```json
{
  "Provider": "MySQL",
  "Host": "localhost",
  "Database": "myappdb",
  "Uid": "root",
  "Password": "YourPassword",
  "Port": "3306"
}
```

**SQLite:**
```json
{
  "Provider": "SQLite",
  "Host": "localhost",
  "Database": "myapp.db",
  "Uid": "unused",
  "Password": "unused",
  "Port": "0"
}
```

**Note**: Ensure `config.json` is copied to the output directory. Set **Copy to Output Directory** to **Copy always** or **Copy if newer** in Visual Studio.

## Quick Start

```csharp
using DBTools.Core;
using System.Data;

// Initialize SqlClient (reads config.json including Provider setting)
var db = new SqlClient();

// Perform a simple SELECT query
DataView results = db.Select(
    _fields: "*",
    _table: "Users",
    whereClause: "Age > @param0",
    parameters: new object[] { 18 }
);

// Iterate through results
foreach (DataRowView row in results)
{
    Console.WriteLine($"Name: {row["Name"]}, Age: {row["Age"]}");
}
```

### Using a Specific Provider Programmatically

```csharp
using DBTools.Core;
using DBTools.Abstractions;
using DBTools.Providers;

// Create a provider explicitly
IDbProvider provider = DbProviderFactory.Create("PostgreSQL");
// Or: DbProviderFactory.Create(DatabaseProvider.PostgreSQL);

var config = new DbConfiguration(); // Reads config.json
var validator = new SqlValidator();
var queryBuilder = new SqlQueryBuilder(validator);

// Pass the provider to SqlClient
var db = new SqlClient(config, validator, queryBuilder, provider);
```

### Dependency Injection Setup

```csharp
using DBTools.Configuration;

// In your Startup.cs or Program.cs
services.AddDbTools(options =>
{
    options.Provider = DatabaseProvider.PostgreSQL;
    options.Host = "localhost";
    options.Port = "5432";
    options.Database = "myappdb";
    options.Username = "postgres";
    options.Password = "secret";
});

// Or with a raw connection string (defaults to SQL Server)
services.AddDbTools("Data Source=tcp:localhost,1433;Initial Catalog=MyDb;User ID=sa;Password=secret;");
```

Then inject `SqlClient`, `IAsyncSqlClient`, or `AsyncSqlClient` as needed:

```csharp
public class UserService
{
    private readonly IAsyncSqlClient _db;

    public UserService(IAsyncSqlClient db)
    {
        _db = db;
    }
}
```

#### Full-Featured DI Setup

```csharp
// Full-featured DI setup with all options
services.AddDbTools(options =>
{
    options.Provider = DatabaseProvider.PostgreSQL;
    options.Host = "localhost";
    options.Port = "5432";
    options.Database = "myappdb";
    options.Username = "postgres";
    options.Password = "secret";

    // Connection pooling
    options.ConfigurePooling(pool =>
    {
        pool.MinPoolSize = 5;
        pool.MaxPoolSize = 50;
    });

    // Query caching
    options.ConfigureCaching(cache =>
    {
        cache.Enabled = true;
        cache.DefaultExpiration = TimeSpan.FromMinutes(10);
    });

    // Migrations
    options.ConfigureMigrations(m =>
    {
        m.Migrations.Add(new CreateUsersTable());
    });
});
```

In addition to `SqlClient`, `IAsyncSqlClient`, and `AsyncSqlClient`, the following services are also injectable: `IStoredProcedureClient`, `IAsyncStoredProcedureClient`, `IQueryCache`, and `IMigrationRunner`.

## Architecture

DBTools_SQL is organized into the following namespaces:

| Namespace | Description |
|-----------|-------------|
| `DBTools.Core` | Core classes: `SqlClient`, `AsyncSqlClient`, `DBTools`, `DbConfiguration`, `SqlQueryBuilder`, `SqlValidator` |
| `DBTools.Abstractions` | Interfaces: `ISqlClient`, `IAsyncSqlClient`, `IDbProvider`, `IDbConfiguration`, `ISqlQueryBuilder`, `ISqlValidator`, `IDBTools` |
| `DBTools.Providers` | Database providers: `SqlServerProvider`, `PostgresProvider`, `MySqlProvider`, `SqliteProvider`, `DbProviderFactory` |
| `DBTools.Configuration` | DI support: `ServiceCollectionExtensions`, `DbToolsOptions`, `DatabaseProvider` enum |
| `DBTools.Controllers` | Controller classes: `LinqHelper<TModel>`, `Linq<TModel>`, `DBToolsController`, `DataExportController` |
| `DBTools.Models` | Data models: `GenericObject`, `GenericObject_Simple` |
| `DBTools.Linq` | LINQ infrastructure: `DbQuery<T>`, `DbQueryProvider`, `DbExpressionTranslator`, `JoinQuery<TLeft,TRight>`, `JoinResult<TLeft,TRight>` |
| `DBTools.Export` | Export utilities: `DataExport` |
| `DBTools.Pooling` | Connection pool configuration: `ConnectionPoolOptions` |
| `DBTools.StoredProcedures` | Stored procedure execution: `StoredProcedureClient`, `StoredProcedureParameter`, `StoredProcedureResult` |
| `DBTools.Caching` | Query result caching: `MemoryQueryCache`, `CachingInterceptor`, `CacheKeyGenerator`, `QueryCacheOptions`, `CacheEntry`, `CacheStatistics` |
| `DBTools.Migrations` | Database migrations: `MigrationRunner`, `MigrationBuilder`, `MigrationBase`, `MigrationOptions`, `MigrationRecord`, `SqlMigration` |

### Project Structure

```
DBTools/
├── Abstractions/
│   ├── IDBTools.cs             # Base DBTools interface
│   ├── IDbConfiguration.cs     # Configuration interface (includes Provider property)
│   ├── IDbProvider.cs          # Provider abstraction for multi-database support
│   ├── ISqlClient.cs           # SqlClient interface
│   ├── ISqlQueryBuilder.cs     # Query builder interface
│   ├── ISqlValidator.cs        # Validator interface
│   ├── IAsyncSqlClient.cs      # Async SQL client interface
│   ├── IDbTransaction.cs       # Transaction interface
│   ├── IQueryInterceptor.cs    # Query interceptor interface
│   ├── IStoredProcedureClient.cs # Stored procedure interfaces (sync + async)
│   ├── IMigration.cs            # Migration interface
│   └── IMigrationRunner.cs      # Migration runner interface
├── Providers/
│   ├── DbProviderFactory.cs    # Factory for creating IDbProvider instances
│   ├── SqlServerProvider.cs    # SQL Server dialect (MERGE, SCOPE_IDENTITY, [brackets])
│   ├── PostgresProvider.cs     # PostgreSQL dialect (ON CONFLICT, RETURNING, "quotes")
│   ├── MySqlProvider.cs        # MySQL dialect (ON DUPLICATE KEY, LAST_INSERT_ID, `backticks`)
│   └── SqliteProvider.cs       # SQLite dialect (ON CONFLICT, last_insert_rowid, "quotes")
├── Configuration/
│   ├── DbToolsOptions.cs       # Options class with DatabaseProvider enum
│   └── ServiceCollectionExtensions.cs # DI registration (AddDbTools)
├── Core/
│   ├── DBTools.cs              # Base database connection class (provider-agnostic)
│   ├── DbConfiguration.cs      # Configuration loading from config.json
│   ├── SqlClient.cs            # Main utility class (CRUD, QueryBuilder)
│   ├── AsyncSqlClient.cs       # Async operations with IDbProvider
│   ├── SqlQueryBuilder.cs      # SQL query generation with validation
│   ├── SqlValidator.cs         # SQL injection prevention validator
│   └── DbTransaction.cs        # Transaction management
├── Controllers/
│   ├── DBToolsController.cs    # Legacy DBTools controller
│   ├── DataExportController.cs # Data export controller
│   ├── LinqHelper.cs           # LINQ expression-based queries
│   └── Linq.cs                 # Property-based queries, JOINs, deferred IQueryable
├── Linq/
│   ├── DbQuery.cs              # IQueryable implementation (deferred execution)
│   ├── DbQueryProvider.cs      # IQueryProvider (translates LINQ to SQL)
│   ├── DbExpressionTranslator.cs # Expression tree to SQL translator
│   ├── JoinQuery.cs            # IQueryable for JOIN queries
│   └── JoinResult.cs           # JOIN result row (Left + Right models)
├── Models/
│   ├── GenericObject.cs        # Generic data container with Insert/Update
│   └── GenericObject_Simple.cs # Simple key-value-column container
├── Bulk/
│   └── BulkOperations.cs       # Bulk insert (SQL Server SqlBulkCopy)
├── Context/
│   ├── DbContext.cs            # EF-like context pattern
│   ├── DbSet.cs                # Entity set
│   └── ChangeTracker.cs        # Change tracking
├── Interceptors/
│   ├── LoggingInterceptor.cs   # Query logging
│   ├── AuditInterceptor.cs     # Audit trail
│   └── SoftDeleteInterceptor.cs # Soft delete support
├── Pooling/
│   └── ConnectionPoolOptions.cs    # Pool size, lifetime, idle timeout settings
├── StoredProcedures/
│   ├── StoredProcedureClient.cs    # Sync and async stored procedure execution
│   ├── StoredProcedureParameter.cs # Parameter with direction (Input/Output/InputOutput/ReturnValue)
│   └── StoredProcedureResult.cs    # Result with return value, output params, rows affected
├── Caching/
│   ├── IQueryCache.cs              # Cache interface (Get/Set/Invalidate/Clear/Statistics)
│   ├── MemoryQueryCache.cs         # ConcurrentDictionary-based LRU cache
│   ├── CachingInterceptor.cs       # Transparent cache via interceptor pipeline
│   ├── CacheKeyGenerator.cs        # SHA256-based deterministic cache keys
│   ├── CacheEntry.cs               # Cache entry with metadata and hit count
│   ├── CacheStatistics.cs          # Hit rate, miss count, eviction count
│   └── QueryCacheOptions.cs        # Expiration, max size, auto-invalidation settings
├── Migrations/
│   ├── IMigrationRunner.cs         # Runner interface (Apply/Rollback/GetPending)
│   ├── MigrationRunner.cs          # Provider-aware runner with per-migration transactions
│   ├── MigrationBuilder.cs         # Fluent API for defining migrations
│   ├── MigrationBase.cs            # Abstract base with SHA256 checksum
│   ├── MigrationOptions.cs         # Table name, auto-migrate settings
│   ├── MigrationRecord.cs          # Applied migration tracking record
│   └── SqlMigration.cs             # Concrete migration with Up/Down SQL
├── Export/
│   └── DataExport.cs           # CSV export and DataTable conversion
├── DBTools.csproj
└── config.json
```

## Core Functionality

### Database Connection

The `SqlClient` class automatically establishes a connection using the configuration file:

```csharp
using DBTools.Core;

// Reads config.json (including Provider key) and connects to the configured database
var db = new SqlClient();
```

**With Explicit Provider:**

```csharp
using DBTools.Core;
using DBTools.Abstractions;
using DBTools.Providers;

// Create a specific provider
IDbProvider provider = DbProviderFactory.Create(DatabaseProvider.PostgreSQL);

var config = new DbConfiguration();
var validator = new SqlValidator();
var queryBuilder = new SqlQueryBuilder(validator);

var db = new SqlClient(config, validator, queryBuilder, provider);
```

**Dependency Injection:**

```csharp
using DBTools.Configuration;

// Register in DI container with provider selection
services.AddDbTools(options =>
{
    options.Provider = DatabaseProvider.MySQL;
    options.Host = "localhost";
    options.Port = "3306";
    options.Database = "myapp";
    options.Username = "root";
    options.Password = "secret";
});
```

### CRUD Operations

#### SELECT - Retrieve Data

**With Parameterized WHERE Clause:**
```csharp
DataView users = db.Select(
    "id, name, email",
    "Users",
    "status = @param0 AND age > @param1",
    new object[] { "active", 18 }
);
```

**Without WHERE Clause:**
```csharp
DataView allUsers = db.Select(
    "*",
    "Users",
    "",
    new object[] { }
);
```

**Using Query Without SELECT Keyword:**
```csharp
DataView customQuery = db.Select(
    "* FROM Users INNER JOIN Orders ON Users.id = Orders.user_id WHERE Orders.total > @param0",
    new object[] { 100.00 }
);
```

#### INSERT - Add New Records

**Manual Approach:**
```csharp
string[] fields = { "name", "email", "age" };
object[] values = { "John Doe", "john@example.com", 30 };

bool success = db.Insert(fields, "Users", values);
```

**With Auto-Increment Primary Key:**
```csharp
string[] fields = { "id", "name", "email", "age" };
object[] values = { 1, "John Doe", "john@example.com", 30 };

// The 'id' field will be automatically excluded
bool success = db.Insert(fields, "Users", values, "id", true);
```

**Using QueryBuilder:**
```csharp
public class User
{
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
}

var newUser = new User
{
    Name = "Jane Doe",
    Email = "jane@example.com",
    Age = 25
};

var queryData = db.QueryBuilder(newUser);

bool success = db.Insert(
    queryData[0].columns,
    "Users",
    queryData[0].values
);
```

#### UPDATE - Modify Records

**Parameterized Update (Recommended):**
```csharp
string[] fields = { "name", "email", "age" };
string[] values = { "John Smith", "johnsmith@example.com", "31" };
string whereClause = "id = @whereParam0";
object[] whereParams = new object[] { 5 };

bool success = db.Update(fields, "Users", values, whereClause, whereParams);
```

**Legacy String Condition Update:**
```csharp
string[] fields = { "name", "email" };
string[] values = { "John Smith", "johnsmith@example.com" };
string condition = "id = 5";

bool success = db.Update(fields, "Users", values, condition);
```

#### DELETE - Remove Records

```csharp
bool success = db.Delete(
    "Users",
    "id = @param0",
    new object[] { 5 }
);
```

**Delete Multiple Records:**
```csharp
bool success = db.Delete(
    "Users",
    "status = @param0 AND created_date < @param1",
    new object[] { "inactive", DateTime.Now.AddYears(-1) }
);
```

### Query Builder

The `QueryBuilder` method converts any object into a format suitable for database operations:

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedDate { get; set; }
}

var product = new Product
{
    Id = 1,
    Name = "Laptop",
    Price = 999.99m,
    CreatedDate = DateTime.Now
};

List<GenericObject> queryData = db.QueryBuilder(product, "Id", true);

string[] columns = queryData[0].columns;     // ["Name", "Price", "CreatedDate"]
object[] values = queryData[0].values;        // ["Laptop", 999.99, "2024-01-15 10:30:00"]
string[] types = queryData[0].types;          // ["String", "Decimal", "DateTime"]
```

### Data Export

Export query results to CSV format or convert CSV to DataTable:

```csharp
using DBTools.Export;

var dataExport = new DataExport();

// Export to CSV
var queryData = db.QueryBuilder(myObject);
string csvContent = dataExport.ToCsv(
    genericObject: queryData,
    separator: ',',
    showColums: true,
    showTypes: true
);
File.WriteAllText("export.csv", csvContent);

// Convert CSV to DataTable
DataTable table = dataExport.ToCsv(csvContent, ',', specifyColumnTypes: true);
```

**CSV Output Example:**
```csv
String,Decimal,DateTime
Name,Price,CreatedDate
Laptop,999.99,2024-01-15 10:30:00
Mouse,29.99,2024-01-16 14:20:00
```

---

## Multi-Provider Support

DBTools_SQL supports four database providers with dialect-specific SQL generation. The core library uses `System.Data.Common` abstractions (`DbConnection`, `DbCommand`, `DbParameter`) so all CRUD operations, LINQ queries, and query building work transparently across providers.

### IDbProvider Interface

Each provider implements `IDbProvider`, which handles:

| Capability | SQL Server | PostgreSQL | MySQL | SQLite |
|------------|-----------|------------|-------|--------|
| Identifier quoting | `[brackets]` | `"double quotes"` | `` `backticks` `` | `"double quotes"` |
| Paging | `OFFSET/FETCH` | `LIMIT/OFFSET` | `LIMIT/OFFSET` | `LIMIT/OFFSET` |
| Last insert ID | `SCOPE_IDENTITY()` | `RETURNING id` | `LAST_INSERT_ID()` | `last_insert_rowid()` |
| Upsert | `MERGE` | `ON CONFLICT DO UPDATE` | `ON DUPLICATE KEY UPDATE` | `ON CONFLICT DO UPDATE` |

### DbProviderFactory

Use `DbProviderFactory` to create providers from a string or enum:

```csharp
using DBTools.Providers;
using DBTools.Configuration;

// From enum
IDbProvider provider = DbProviderFactory.Create(DatabaseProvider.PostgreSQL);

// From string (case-insensitive)
IDbProvider provider = DbProviderFactory.Create("mysql");
IDbProvider provider = DbProviderFactory.Create("postgres"); // alias for PostgreSQL
```

Supported string values: `"SqlServer"`, `"PostgreSQL"`, `"Postgres"`, `"MySQL"`, `"SQLite"`.

### Provider-Specific Upsert

The `Linq<TModel>.InsertOrUpdate()` method generates provider-appropriate upsert SQL automatically:

```csharp
var userController = new Linq<User>("Users", "Id", true);

// Generates MERGE (SQL Server), ON CONFLICT (Postgres/SQLite), or ON DUPLICATE KEY (MySQL)
userController.InsertOrUpdate(user, u => u.Email);
```

### Switching Providers

To switch from SQL Server to another provider:

1. Install the required NuGet package (see [Requirements](#requirements))
2. Update `config.json` with the new `Provider` value and connection details
3. No code changes needed - the same API works across all providers

---

## LinqHelper - LINQ Expression Queries

The `LinqHelper<TModel>` supports lambda expression predicates similar to Entity Framework's LINQ queries.

### Why Use LinqHelper?

- **LINQ Lambda Expressions**: Write `Where(u => u.Age > 18)` instead of `"Age > @param0"`
- **Type Safety**: Compile-time checking for conditions
- **MySQLDBTools Compatibility**: Same LINQ API across all supported database providers
- **Familiar API**: Methods like `Add()`, `Remove()`, `SaveChanges()`, `GetAll()`, `AsQueryable()`
- **Expression Support**: `==`, `!=`, `>`, `>=`, `<`, `<=`, `&&`, `||`, `!`

### Setup

```csharp
using DBTools.Controllers;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
    public string Status { get; set; }
}

var userController = new LinqHelper<User>(
    tableName: "Users",
    primaryKeyName: "Id",
    autoIncrement: true
);
```

**With existing SqlClient:**
```csharp
using DBTools.Core;
using DBTools.Controllers;

var db = new SqlClient();
var userController = new LinqHelper<User>(db, "Users", "Id", true);
```

### LINQ Methods

```csharp
// Get all records
var allUsers = userController.GetAll();

// Filter with lambda
var adults = userController.Where(u => u.Age > 18);
var activeAdults = userController.Where(u => u.Age > 18 && u.Status == "active");

// Get first match
var user = userController.FirstOrDefault(u => u.Id == 1);

// Get single match (throws if multiple)
var unique = userController.SingleOrDefault(u => u.Id == 1);

// Check existence
bool exists = userController.Any(u => u.Name == "John");

// Count matches
int count = userController.Count(u => u.Age > 18);

// Find by primary key
var found = userController.Find(5);

// Add (insert)
bool added = userController.Add(new User { Name = "Alice", Email = "alice@example.com", Age = 28 });

// Bulk insert
bool bulkAdded = userController.InsertRange(new List<User>
{
    new User { Name = "Bob", Email = "bob@example.com", Age = 30 },
    new User { Name = "Carol", Email = "carol@example.com", Age = 25 }
});

// Update with lambda predicate
bool updated = userController.Update(updatedUser, u => u.Id == 1);

// Save changes by primary key
var existing = userController.FirstOrDefault(u => u.Id == 1);
existing.Status = "active";
bool saved = userController.SaveChanges(existing);

// Remove (delete) with lambda predicate
bool deleted = userController.Remove(u => u.Id == 1);
```

### String-Based Query Methods

LinqHelper also supports string-based conditions for complex queries:

```csharp
// String-based Where
var results = userController.Where("Age > @param0 AND Status = @param1", new object[] { 18, "active" });

// String-based FirstOrDefault
var user = userController.FirstOrDefault("Id = @param0", new object[] { 5 });

// String-based Count
int count = userController.Count("Status = @param0", new object[] { "active" });

// String-based Any
bool any = userController.Any("Age > @param0", new object[] { 65 });

// String-based Single
var single = userController.Single("Id = @param0", new object[] { 1 });

// Get all
var all = userController.All();
```

---

## Linq - Property-Based Queries & JOINs

The `Linq<TModel>` extends `LinqHelper<TModel>` with property-selector operations and JOIN support.

### Why Use Linq?

- **Extended Operations**: `WhereContains`, `WhereIn`, `WhereBetween`, `WhereIsNull`, etc.
- **Type Safety**: Compile-time checking for property names
- **IntelliSense Support**: IDE autocomplete for properties
- **JOIN Support**: `InnerJoin` and `LeftJoin` with LINQ chaining
- **Deferred IQueryable**: SQL-translated `AsQueryable()` with `DbQuery<T>`

### Setup

```csharp
using DBTools.Controllers;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
    public string Status { get; set; }
    public DateTime? LastLogin { get; set; }
}

var userController = new Linq<User>(
    tableName: "Users",
    primaryKeyName: "Id",
    autoIncrement: true
);
```

### Query Methods

#### Comparison Queries

```csharp
var user = userController.WhereEquals(u => u.Username, "john_doe");
var notActive = userController.WhereNotEquals(u => u.Status, "active");
var adults = userController.WhereGreaterThan(u => u.Age, 18);
var seniors = userController.WhereGreaterThanOrEquals(u => u.Age, 65);
var young = userController.WhereLessThan(u => u.Age, 25);
var ageRange = userController.WhereBetween(u => u.Age, 25, 40);
```

#### String Queries

```csharp
var gmailUsers = userController.WhereContains(u => u.Email, "@gmail.com");
var johnUsers = userController.WhereStartsWith(u => u.Username, "john");
var adminUsers = userController.WhereEndsWith(u => u.Email, "@admin.com");
```

#### Collection Queries

```csharp
var specificUsers = userController.WhereIn(u => u.Id, new[] { 1, 2, 3, 5, 8 });
var excludedUsers = userController.WhereNotIn(u => u.Id, new[] { 99, 100 });
```

#### Null Checks

```csharp
var usersWithoutEmail = userController.WhereIsNull(u => u.Email);
var usersWithEmail = userController.WhereIsNotNull(u => u.Email);
```

#### Example-Based Filtering

```csharp
var filter = new User
{
    Status = "active",
    Age = 30
    // Only set properties you want to filter by
};

var matchingUsers = userController.WhereByExample(filter);
// Finds all users with Status='active' AND Age=30
```

### Linq CRUD Operations

#### Retrieve Records

```csharp
var allUsers = userController.All();
var user = userController.Find(5);
var firstJohn = userController.FirstOrDefaultByProperty(u => u.Username, "john");
var singleUser = userController.SingleByProperty(u => u.Email, "unique@example.com");
bool exists = userController.Exists(u => u.Username, "john_doe");
int activeCount = userController.CountByProperty(u => u.Status, "active");
```

#### Insert Records

```csharp
var newUser = new User { Username = "jane_doe", Email = "jane@example.com", Age = 28 };
bool success = userController.Insert(newUser);

// Insert and retrieve (gets auto-generated ID)
var insertedUser = userController.InsertAndFind(newUser, u => u.Username);

// Get or create (insert if not exists)
var user = userController.GetOrCreate(newUser, u => u.Username);

// Insert or update (upsert)
userController.InsertOrUpdate(newUser, u => u.Username);
```

#### Update Records

```csharp
userController.UpdateByProperty(user, u => u.Id, 5);
userController.UpdateWhere(user, u => u.Username, "john_doe", u => u.Status, "active");
userController.UpdateWhereIn(user, u => u.Id, new[] { 1, 2, 3 });
userController.UpdateWhereIsNull(user, u => u.Email);
```

#### Delete Records

```csharp
userController.DeleteByProperty(u => u.Id, 5);
userController.DeleteWhere(u => u.Status, "inactive", u => u.Age, 100);
userController.DeleteWhereIn(u => u.Id, new[] { 10, 11, 12 });
userController.DeleteWhereIsNull(u => u.Email);
```

### JOIN Support

The `Linq<TModel>` provides type-safe JOIN operations with LINQ chaining:

```csharp
using DBTools.Controllers;
using DBTools.Linq;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; }
}

var userController = new Linq<User>("Users", "Id");

// INNER JOIN
var innerJoinQuery = userController.InnerJoin<Order>(
    rightTable: "Orders",
    leftKey: u => u.Id,
    rightKey: o => o.UserId
);

var results = innerJoinQuery
    .Where(j => j.Left.Age > 18)
    .ToList();

foreach (var row in results)
{
    Console.WriteLine($"User: {row.Left.Username}, Order Total: {row.Right.Total}");
}

// LEFT JOIN
var leftJoinQuery = userController.LeftJoin<Order>(
    rightTable: "Orders",
    leftKey: u => u.Id,
    rightKey: o => o.UserId
);

var leftResults = leftJoinQuery.ToList();
foreach (var row in leftResults)
{
    // row.Right may be null for LEFT JOIN non-matches
    Console.WriteLine($"User: {row.Left.Username}, Order: {(row.Right != null ? row.Right.Total.ToString() : "No orders")}");
}
```

**JoinResult<TLeft, TRight>** properties:
- `Left` - The model from the primary table
- `Right` - The model from the joined table (null for LEFT JOIN non-matches)

### Deferred IQueryable Execution

The `Linq<TModel>.AsQueryable()` returns a `DbQuery<TModel>` that translates LINQ operations to SQL. No query is executed until the result is enumerated:

```csharp
// SQL-translated deferred execution (Linq<TModel> override)
var query = userController.AsQueryable()
    .Where(u => u.Age > 18)
    .OrderBy(u => u.Name)
    .Skip(10)
    .Take(5);

// SQL is only executed when enumerated
var results = query.ToList();

// Debug: see the generated SQL
Console.WriteLine(query.ToString());
```

**Note:** `LinqHelper<TModel>.AsQueryable()` loads all data into memory first. Use `Linq<TModel>` for SQL-translated deferred execution.

### Chaining with LINQ

The controller returns `IEnumerable<TModel>`, so you can chain LINQ operations:

```csharp
var topUsers = userController
    .WhereGreaterThan(u => u.Age, 18)
    .OrderByDescending(u => u.Age)
    .Take(10)
    .ToList();

var filteredUsers = userController
    .WhereEquals(u => u.Status, "active")
    .Where(u => u.Email.Contains("@gmail.com"))
    .Select(u => new { u.Username, u.Email })
    .ToList();
```

### Performance Considerations

- **Database-Side Filtering**: All `Where*` methods execute on the database, not in memory
- **Parameterized Queries**: All queries use parameterized SQL for security and performance
- **Deferred Execution**: `DbQuery<T>` translates LINQ to SQL and executes only on enumeration
- **Connection Management**: Connections are automatically managed

### Migration from Traditional SqlClient

**Before (Traditional):**
```csharp
var db = new SqlClient();
DataView results = db.Select("*", "Users", "age > @param0", new object[] { 18 });

foreach (DataRowView row in results)
{
    Console.WriteLine($"User: {row["username"]}");
}
```

**After (LINQ-Style):**
```csharp
var userController = new Linq<User>("Users", "Id");
var results = userController.WhereGreaterThan(u => u.Age, 18);

foreach (var user in results)
{
    Console.WriteLine($"User: {user.Username}");
}
```

## Connection Pooling

DBTools_SQL supports configurable connection pooling through provider-specific connection string parameters. Pool settings are translated to the appropriate format for each provider.

### Configuration

#### Via Dependency Injection

```csharp
services.AddDbTools(options =>
{
    options.Provider = DatabaseProvider.PostgreSQL;
    options.Host = "localhost";
    options.Database = "myappdb";
    options.Username = "postgres";
    options.Password = "secret";
    options.Port = "5432";

    // Configure connection pooling
    options.ConfigurePooling(pool =>
    {
        pool.Pooling = true;
        pool.MinPoolSize = 5;
        pool.MaxPoolSize = 50;
        pool.ConnectionLifetimeSeconds = 300;
        pool.ConnectionIdleTimeoutSeconds = 120;
    });
});
```

#### Direct Configuration

```csharp
var options = new DbToolsOptions
{
    Provider = DatabaseProvider.SqlServer,
    Host = "localhost",
    Database = "MyDb",
    Username = "sa",
    Password = "secret"
};

options.PoolOptions = new ConnectionPoolOptions
{
    Pooling = true,
    MinPoolSize = 0,
    MaxPoolSize = 100,
    ConnectionLifetimeSeconds = 0,     // Unlimited
    ConnectionIdleTimeoutSeconds = 300  // 5 minutes
};
```

### Pool Options

| Property | Default | Description |
|----------|---------|-------------|
| `Pooling` | `true` | Enable/disable connection pooling |
| `MinPoolSize` | `0` | Minimum connections maintained in the pool |
| `MaxPoolSize` | `100` | Maximum connections allowed in the pool |
| `ConnectionLifetimeSeconds` | `0` | Max lifetime before a connection is destroyed (0 = unlimited) |
| `ConnectionIdleTimeoutSeconds` | `300` | Idle time before removal from pool (5 minutes) |

### Provider-Specific Behavior

Pool options are appended to the connection string using provider-appropriate parameter names:

| Provider | Min Pool | Max Pool | Lifetime | Idle Timeout |
|----------|----------|----------|----------|--------------|
| SQL Server | `Min Pool Size` | `Max Pool Size` | `Connection Lifetime` | N/A |
| PostgreSQL | `Minimum Pool Size` | `Maximum Pool Size` | N/A | `Connection Idle Lifetime` |
| MySQL | `MinimumPoolSize` | `MaximumPoolSize` | `ConnectionLifeTime` | N/A |
| SQLite | N/A | N/A | N/A | N/A (pooling on/off only) |

### Disabling Pooling

```csharp
options.ConfigurePooling(pool =>
{
    pool.Pooling = false;
});
// Appends "Pooling=False;" to the connection string
```

---

## Stored Procedures

DBTools_SQL provides full stored procedure support with input, output, input/output parameters, and return values. Both synchronous and asynchronous interfaces are available.

### Setup

#### Via Dependency Injection

```csharp
// StoredProcedureClient is automatically registered when using AddDbTools()
services.AddDbTools(options => { /* ... */ });

// Inject via interface
public class OrderService
{
    private readonly IAsyncStoredProcedureClient _spClient;

    public OrderService(IAsyncStoredProcedureClient spClient)
    {
        _spClient = spClient;
    }
}
```

#### Manual Instantiation

```csharp
using DBTools.StoredProcedures;
using DBTools.Providers;

var provider = DbProviderFactory.Create(DatabaseProvider.SqlServer);
var spClient = new StoredProcedureClient(provider, connectionString);
```

### Executing Stored Procedures

#### Basic Execution (Non-Query)

```csharp
var parameters = new[]
{
    StoredProcedureParameter.Input("UserId", 42),
    StoredProcedureParameter.Input("NewStatus", "active")
};

StoredProcedureResult result = spClient.ExecuteStoredProcedure("sp_UpdateUserStatus", parameters);
Console.WriteLine($"Rows affected: {result.RowsAffected}");
```

#### With Output Parameters

```csharp
var parameters = new[]
{
    StoredProcedureParameter.Input("OrderId", 1001),
    StoredProcedureParameter.Output("TotalAmount", DbType.Decimal, size: 18),
    StoredProcedureParameter.Output("OrderStatus", DbType.String, size: 50)
};

StoredProcedureResult result = spClient.ExecuteStoredProcedure("sp_GetOrderDetails", parameters);

decimal total = (decimal)result.OutputParameters["TotalAmount"];
string status = (string)result.OutputParameters["OrderStatus"];
```

#### With Return Value

```csharp
var parameters = new[]
{
    StoredProcedureParameter.Input("Username", "john_doe"),
    StoredProcedureParameter.Input("Password", "hashed_password"),
    StoredProcedureParameter.ReturnValue()
};

StoredProcedureResult result = spClient.ExecuteStoredProcedure("sp_AuthenticateUser", parameters);
int statusCode = (int)result.ReturnValue; // 0 = success, 1 = invalid credentials, etc.
```

#### With Input/Output Parameters

```csharp
var parameters = new[]
{
    StoredProcedureParameter.InputOutput("Counter", 10, DbType.Int32)
};

StoredProcedureResult result = spClient.ExecuteStoredProcedure("sp_IncrementCounter", parameters);
int newValue = (int)result.OutputParameters["Counter"];
```

#### Returning Result Sets

```csharp
var parameters = new[]
{
    StoredProcedureParameter.Input("DepartmentId", 5),
    StoredProcedureParameter.Input("MinSalary", 50000)
};

DataTable employees = spClient.ExecuteStoredProcedureReader("sp_GetEmployees", parameters);

foreach (DataRow row in employees.Rows)
{
    Console.WriteLine($"{row["Name"]} - {row["Salary"]}");
}
```

### Async Execution

```csharp
var parameters = new[]
{
    StoredProcedureParameter.Input("CategoryId", 3)
};

// Non-query
StoredProcedureResult result = await spClient.ExecuteStoredProcedureAsync(
    "sp_ArchiveCategory", parameters, cancellationToken);

// With result set
DataTable products = await spClient.ExecuteStoredProcedureReaderAsync(
    "sp_GetProductsByCategory", parameters, cancellationToken);
```

### Parameter Factory Methods

| Method | Direction | Description |
|--------|-----------|-------------|
| `StoredProcedureParameter.Input(name, value)` | Input | Standard input parameter |
| `StoredProcedureParameter.Output(name, dbType, size?)` | Output | Output-only parameter |
| `StoredProcedureParameter.InputOutput(name, value, dbType, size?)` | InputOutput | Bidirectional parameter |
| `StoredProcedureParameter.ReturnValue()` | ReturnValue | Captures the procedure's return value |

---

## Query Result Caching

DBTools_SQL includes a transparent query result caching layer that integrates with the interceptor pipeline. SELECT results are cached automatically, and write operations (INSERT/UPDATE/DELETE) invalidate related cache entries.

### Configuration

```csharp
services.AddDbTools(options =>
{
    options.Provider = DatabaseProvider.SqlServer;
    // ... connection settings ...

    options.ConfigureCaching(cache =>
    {
        cache.Enabled = true;
        cache.DefaultExpiration = TimeSpan.FromMinutes(10);
        cache.MaxCacheSize = 500;
        cache.EnableAutoInvalidation = true;
    });
});
```

### Cache Options

| Property | Default | Description |
|----------|---------|-------------|
| `Enabled` | `false` | Enable/disable query caching |
| `DefaultExpiration` | 5 minutes | Default TTL for cache entries |
| `MaxCacheSize` | `1000` | Maximum entries before LRU eviction |
| `EnableAutoInvalidation` | `true` | Invalidate table cache on write operations |

### How It Works

1. **SELECT queries**: The `CachingInterceptor` generates a SHA256-based cache key from the SQL + parameters. On cache hit, the query is suppressed and the cached result is returned.
2. **Write operations**: When `EnableAutoInvalidation` is true, any INSERT/UPDATE/DELETE automatically invalidates all cached entries associated with the affected table.
3. **LRU eviction**: When the cache reaches `MaxCacheSize`, the least recently accessed entry is evicted.

### Manual Cache Control

Inject `IQueryCache` for direct cache manipulation:

```csharp
public class ProductService
{
    private readonly IQueryCache _cache;

    public ProductService(IQueryCache cache)
    {
        _cache = cache;
    }

    public void InvalidateProductCache()
    {
        _cache.InvalidateByTable("Products");
    }

    public void ClearAllCache()
    {
        _cache.Clear();
    }

    public void PrintCacheStats()
    {
        var stats = _cache.GetStatistics();
        Console.WriteLine($"Entries: {stats.TotalEntries}");
        Console.WriteLine($"Hit rate: {stats.HitRate:P1}");
        Console.WriteLine($"Hits: {stats.HitCount}, Misses: {stats.MissCount}");
        Console.WriteLine($"Evictions: {stats.EvictionCount}");
    }
}
```

### Cache Behavior Notes

- Cache keys are deterministic (same SQL + parameters = same key) using SHA256
- Caching only applies to queries routed through the interceptor pipeline (AsyncSqlClient)
- Table association requires the interceptor context to include the table name
- Thread-safe implementation using `ConcurrentDictionary` and `Interlocked` operations

---

## Migration Tools

DBTools_SQL includes a database schema migration system that tracks applied migrations in a database table, supports transactional apply/rollback, and detects script tampering via SHA256 checksums.

### Configuration

```csharp
services.AddDbTools(options =>
{
    options.Provider = DatabaseProvider.PostgreSQL;
    // ... connection settings ...

    options.ConfigureMigrations(migrations =>
    {
        migrations.MigrationTableName = "__DBToolsMigrations"; // default
        migrations.AutoMigrateOnStartup = false;              // default

        // Register migrations using the builder
        var builder = new MigrationBuilder();
        builder
            .AddMigration(
                id: "20240101120000_CreateUsersTable",
                description: "Create the Users table",
                upSql: @"CREATE TABLE Users (
                    Id SERIAL PRIMARY KEY,
                    Username VARCHAR(100) NOT NULL,
                    Email VARCHAR(255) NOT NULL,
                    CreatedAt TIMESTAMP DEFAULT NOW()
                )",
                downSql: "DROP TABLE Users")
            .AddMigration(
                id: "20240102120000_AddAgeColumn",
                description: "Add Age column to Users",
                upSql: "ALTER TABLE Users ADD COLUMN Age INT DEFAULT 0",
                downSql: "ALTER TABLE Users DROP COLUMN Age");

        migrations.Migrations = new List<IMigration>(builder.Build());
    });
});
```

### Migration Options

| Property | Default | Description |
|----------|---------|-------------|
| `MigrationTableName` | `"__DBToolsMigrations"` | Table name for tracking applied migrations |
| `AutoMigrateOnStartup` | `false` | Whether to auto-apply pending migrations on startup |
| `Migrations` | `[]` | List of registered migration definitions |

### Using the Migration Runner

Inject `IMigrationRunner` to manage migrations programmatically:

```csharp
public class MigrationService
{
    private readonly IMigrationRunner _runner;

    public MigrationService(IMigrationRunner runner)
    {
        _runner = runner;
    }

    public async Task MigrateAsync(CancellationToken ct = default)
    {
        // Apply all pending migrations
        await _runner.ApplyAsync(ct);
    }

    public async Task RollbackLastAsync(CancellationToken ct = default)
    {
        // Roll back only the most recent migration
        await _runner.RollbackAsync(targetMigrationId: null, ct);
    }

    public async Task RollbackToAsync(string migrationId, CancellationToken ct = default)
    {
        // Roll back all migrations at or after the specified ID
        await _runner.RollbackAsync(migrationId, ct);
    }

    public async Task PrintStatusAsync(CancellationToken ct = default)
    {
        var applied = await _runner.GetAppliedMigrationsAsync(ct);
        var pending = await _runner.GetPendingMigrationsAsync(ct);

        Console.WriteLine($"Applied: {applied.Count}, Pending: {pending.Count}");

        foreach (var m in applied)
            Console.WriteLine($"  [APPLIED] {m.MigrationId} - {m.Description} (at {m.AppliedAt:u})");

        foreach (var m in pending)
            Console.WriteLine($"  [PENDING] {m.MigrationId} - {m.Description}");
    }
}
```

### Creating Custom Migrations

You can also create migrations by implementing `MigrationBase`:

```csharp
public class CreateOrdersTable : MigrationBase
{
    public override string MigrationId => "20240201120000_CreateOrdersTable";
    public override string Description => "Create the Orders table";

    public override string UpSql => @"
        CREATE TABLE Orders (
            Id SERIAL PRIMARY KEY,
            UserId INT NOT NULL REFERENCES Users(Id),
            Total DECIMAL(10,2) NOT NULL,
            Status VARCHAR(50) DEFAULT 'pending',
            CreatedAt TIMESTAMP DEFAULT NOW()
        )";

    public override string DownSql => "DROP TABLE Orders";
}
```

Then register it:
```csharp
options.ConfigureMigrations(m =>
{
    m.Migrations.Add(new CreateOrdersTable());
});
```

### Migration Tracking Table

The migration runner automatically creates a tracking table (default: `__DBToolsMigrations`) with provider-appropriate DDL:

| Column | Type | Description |
|--------|------|-------------|
| `MigrationId` | VARCHAR(255) PK | Unique migration identifier |
| `Description` | VARCHAR(500) | Human-readable description |
| `AppliedAt` | DATETIME/TIMESTAMP | UTC time when applied |
| `Checksum` | VARCHAR(64) | SHA256 hash of the UpSql script |

### Safety Features

- **Transaction-per-migration**: Each migration runs in its own transaction; on failure, only that migration is rolled back
- **Checksum verification**: SHA256 hashes detect if a migration script was modified after being applied
- **SQL injection protection**: The `MigrationTableName` is validated with a regex pattern `[a-zA-Z_][a-zA-Z0-9_]*`
- **Provider-aware DDL**: CREATE TABLE and DML use the correct SQL dialect for each provider

---

## Security

### SQL Injection Prevention

DBTools_SQL implements multiple layers of security:

#### 1. Parameterized Queries

All CRUD methods use parameterized queries:

```csharp
// SAFE - Uses parameters
db.Select(
    "*",
    "Users",
    "username = @param0",
    new object[] { userInput }
);

// UNSAFE - String concatenation (NOT supported by parameterized methods)
// Don't do: "WHERE username = '" + userInput + "'"
```

#### 2. Identifier Validation (`SqlValidator`)

All table and column names are validated:

```csharp
// VALID identifiers
"Users"
"user_name"
"[User Table]"
"schema.table"
"id, name, email"

// INVALID identifiers (will throw ArgumentException)
"Users; DROP TABLE--"
"Users--"
"Users/*comment*/"
```

#### 3. Forbidden Keywords

The `SqlValidator` blocks dangerous SQL keywords:
- DROP
- DELETE (in identifiers, not in WHERE clauses)
- SQL comment patterns (`--`, `;--`, `/*`, `*/`)

### Best Practices

1. **Always use parameterized methods** for user input
2. **Validate input** at the application layer
3. **Use WHERE clauses** - UPDATE and DELETE require conditions
4. **Limit permissions** - Use database user with minimal required privileges
5. **Secure config.json** - Store database credentials securely (use environment variables or secret managers in production)
6. **Use abstractions** - Depend on `ISqlClient`, `ISqlValidator`, etc. for testability

## API Reference

See [API Reference](docs/API_REFERENCE.md) for complete method documentation.

### SqlClient Class (Main Utility)

**Namespace**: `DBTools.Core`
**Inherits**: `DBTools.Core.DBTools`

| Method | Returns | Description |
|--------|---------|-------------|
| `Select(string fields, string table, string whereClause, object[] parameters)` | `DataView` | Parameterized SELECT |
| `Select(string queryWithoutSelect, object[] parameters)` | `DataView` | Custom query without SELECT keyword |
| `Insert(string[] fields, string table, object[] values, string primaryKeyName, bool autoIncrement)` | `bool` | Parameterized INSERT |
| `Update(string[] fields, string table, string[] values, string condition)` | `bool` | UPDATE with string condition |
| `Update(string[] fields, string table, string[] values, string whereClause, object[] whereParameters)` | `bool` | Parameterized UPDATE |
| `Delete(string table, string whereClause, object[] parameters)` | `bool` | Parameterized DELETE |
| `QueryBuilder(object obj, string primaryKeyName, bool autoIncrement)` | `List<GenericObject>` | Object to database format |
| `ExecuteQuery(string query)` | `void` | Execute non-query SQL |
| `GetInBd(string query)` | `string[]` | First column as string array |
| `GetInBdDv(string query)` | `DataView` | Query results as DataView |

**Constructors:**
- `SqlClient()` - Reads config.json, uses the configured Provider
- `SqlClient(IDbConfiguration, ISqlValidator, ISqlQueryBuilder)` - DI with default provider
- `SqlClient(IDbConfiguration, ISqlValidator, ISqlQueryBuilder, IDbProvider)` - DI with explicit provider

### Static Query Helpers

```csharp
public static string Select_Query(string fields, string table, string conditions)
public static string Insert_Query(string[] fields, string table, object[] values, string primaryKeyName, bool autoIncrement)
public static string Update_Query(string[] fields, string table, string[] values, string condition)
public static string Delete_Query(string table, string condition)
public static List<DbParameter> GenerateSqlParameters(object[] values)
```

### GenericObject Class

**Namespace**: `DBTools.Models`

| Property | Type | Description |
|----------|------|-------------|
| `columns` | `string[]` | Column names |
| `values` | `object[]` | Column values |
| `valuesString` | `string[]` | String representation of values |
| `types` | `string[]` | Data types |
| `table` | `string` | Table name |

| Method | Returns | Description |
|--------|---------|-------------|
| `Insert()` | `bool` | Insert using columns/values |
| `Update(string conditions)` | `bool` | Update using columns/values |

### DataExport Class

**Namespace**: `DBTools.Export`

| Method | Returns | Description |
|--------|---------|-------------|
| `ToCsv(List<GenericObject>, char, bool, bool)` | `string` | Export to CSV |
| `ToDataTable(string csv, char, bool)` | `DataTable` | Convert CSV to DataTable |

## Examples

See [Examples](docs/EXAMPLES.md) for comprehensive code examples.

## Error Handling

All methods include comprehensive error handling:

```csharp
try
{
    var db = new SqlClient();
    bool success = db.Insert(fields, "Users", values);

    if (!success)
    {
        Console.WriteLine($"Insert failed: {db.Error}");
    }
}
catch (FileNotFoundException ex)
{
    Console.WriteLine("Configuration file not found: " + ex.Message);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine("Configuration error: " + ex.Message);
}
catch (ArgumentException ex)
{
    Console.WriteLine("Invalid input: " + ex.Message);
}
catch (Exception ex)
{
    Console.WriteLine("Unexpected error: " + ex.Message);
}
```

## Requirements

- **.NET 8.0** or higher
- **C#** latest version
- **Database**: SQL Server, PostgreSQL, MySQL, or SQLite

### NuGet Dependencies

The core library includes:

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.Data.SqlClient | 5.2.2 | SQL Server connectivity (included) |
| Microsoft.Extensions.Configuration | 10.0.1 | Configuration framework |
| Microsoft.Extensions.Configuration.Json | 10.0.1 | JSON configuration provider |
| Microsoft.Extensions.DependencyInjection | 10.0.1 | DI framework |
| NPOI | 2.7.3 | Excel export support |
| System.Configuration.ConfigurationManager | 10.0.1 | Legacy configuration support |

### Additional Provider Packages

For databases other than SQL Server, add the corresponding NuGet package to your project:

| Provider | Package to Install | Version |
|----------|-------------------|---------|
| PostgreSQL | [Npgsql](https://www.nuget.org/packages/Npgsql) | 8.0+ |
| MySQL | [MySqlConnector](https://www.nuget.org/packages/MySqlConnector) | 2.3+ |
| SQLite | [Microsoft.Data.Sqlite](https://www.nuget.org/packages/Microsoft.Data.Sqlite) | 8.0+ |

```bash
# Example: adding PostgreSQL support
dotnet add package Npgsql

# Example: adding MySQL support
dotnet add package MySqlConnector

# Example: adding SQLite support
dotnet add package Microsoft.Data.Sqlite
```

The library uses reflection to load provider-specific types at runtime, so you only need to install the package for the provider you actually use.

## Contributing

Contributions are welcome! Please follow these guidelines:

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/my-new-feature`
3. **Make your changes** and add tests if applicable
4. **Commit your changes**: `git commit -am 'Add some feature'`
5. **Push to the branch**: `git push origin feature/my-new-feature`
6. **Submit a pull request**

### Coding Standards

- Follow C# naming conventions
- Add XML documentation comments for public methods
- Include unit tests for new features
- Ensure all tests pass before submitting PR
- Use parameterized queries for all database operations
- Depend on abstractions (`ISqlClient`, `ISqlValidator`, etc.) for testability

## Testing

The project includes a comprehensive unit test project (`DBToolsUnitTest`). To run tests:

```bash
dotnet test
```

### Test Structure

```
DBToolsUnitTest/
├── Controllers/
│   ├── DBToolsControllerTests.cs
│   ├── LinqHelperTests.cs
│   ├── LinqHelperLinqTests.cs
│   └── LinqTests.cs
├── Core/
│   ├── DBToolsTests.cs
│   ├── ObsoleteMethodTests.cs
│   ├── QueryBuilderTests.cs
│   ├── SqlClientConnectionTests.cs
│   ├── SqlClientParameterizedQueryTests.cs
│   ├── SqlClientQueryBuilderTests.cs
│   └── SqlClientValidationTests.cs
├── Providers/
│   ├── ProviderTests.cs         # SQL dialect tests (quoting, paging, upsert, identity)
│   └── DbProviderFactoryTests.cs # Factory resolution tests
├── Models/
│   ├── GenericObjectTests.cs
│   └── GenericObjectSimpleTests.cs
├── Linq/
│   └── DbQueryLinqTests.cs
├── Export/
│   └── DataExportTests.cs
├── Pooling/
│   └── ConnectionPoolOptionsTests.cs
├── StoredProcedures/
│   └── StoredProcedureClientTests.cs
│   └── StoredProcedureParameterTests.cs
├── Caching/
│   ├── MemoryQueryCacheTests.cs
│   ├── CachingInterceptorTests.cs
│   ├── CacheKeyGeneratorTests.cs
│   └── QueryCacheOptionsTests.cs
├── Migrations/
│   ├── MigrationRunnerTests.cs
│   ├── MigrationBuilderTests.cs
│   └── MigrationBaseTests.cs
├── Integration/
│   ├── EdgeCaseTests.cs
│   └── WorkflowTests.cs
└── TestBase.cs
```

## Troubleshooting

### Configuration File Not Found

**Error**: `FileNotFoundException: The configuration file 'config.json' was not found`

**Solution**: Ensure `config.json` is in your application's root directory and set to copy to output directory.

### Provider Package Not Found

**Error**: `InvalidOperationException: Npgsql package is not available` (or similar for MySQL/SQLite)

**Solution**: Install the appropriate NuGet package for your configured provider:
- PostgreSQL: `dotnet add package Npgsql`
- MySQL: `dotnet add package MySqlConnector`
- SQLite: `dotnet add package Microsoft.Data.Sqlite`

### Unknown Provider

**Error**: `ArgumentException: Unknown database provider: 'Oracle'`

**Solution**: Use one of the supported provider values: `SqlServer`, `PostgreSQL`, `MySQL`, `SQLite`.

### Connection Failed

**Error**: Connection timeouts or authentication failures

**Solutions**:
- Verify the database server is running
- Check firewall settings
- Verify credentials in `config.json`
- Ensure the correct `Provider` is set in `config.json`
- Check that the `Port` matches your server configuration
- For SQL Server: ensure SQL Server authentication is enabled
- For PostgreSQL: check `pg_hba.conf` for allowed connections
- For MySQL: verify the user has remote access permissions

### Invalid Identifier Exception

**Error**: `ArgumentException: Invalid table name` or `Invalid field names`

**Solution**: Ensure table and column names only contain alphanumeric characters, underscores, dots, brackets, and spaces. Avoid SQL keywords and special characters.

## Performance Tips

1. **Use appropriate indexes** on frequently queried columns
2. **Limit SELECT fields** - Avoid `SELECT *` when possible
3. **Use parameterized queries** - They support query plan caching
4. **Use deferred IQueryable** - `Linq<TModel>.AsQueryable()` translates LINQ to SQL for efficient queries
5. **Batch operations** - Use `InsertRange` for bulk inserts

## Roadmap

Completed enhancements:

- [x] Additional database providers (MySQL, PostgreSQL, SQLite)
- [x] Provider-agnostic core using System.Data.Common abstractions
- [x] Dependency Injection support with `AddDbTools()`
- [x] Async/await support (`AsyncSqlClient`)
- [x] Transaction support (`DbTransaction`)
- [x] Query interceptors (logging, audit, soft delete)
- [x] Connection pooling configuration
- [x] Support for stored procedures
- [x] Query result caching
- [x] Migration tools

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For issues, questions, or contributions:

- **GitHub Issues**: [https://github.com/gednt/DBTools_SQL/issues](https://github.com/gednt/DBTools_SQL/issues)
- **GitHub Repository**: [https://github.com/gednt/DBTools_SQL](https://github.com/gednt/DBTools_SQL)

---

**Note**: This library supports SQL Server, PostgreSQL, MySQL, and SQLite. Set the `Provider` key in `config.json` or `DbToolsOptions` to select your database.

**Security Notice**: Always store database credentials securely. Never commit `config.json` with real credentials to version control. Consider using environment variables or Azure Key Vault for production deployments.
