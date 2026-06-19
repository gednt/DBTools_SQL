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

## Installation

### Via Source
1. Clone the repository:
```bash
git clone https://github.com/gednt/DBTools_SQL.git
```

2. Add reference to your project:
   - In Visual Studio, right-click on your project → Add → Reference
   - Browse to the compiled `DBTools.dll`

3. Copy `DBTools/config.json.example` to `config.json` in your project root and update the values (see [Configuration](#configuration))

## Configuration

Copy the example configuration and customize it for your environment:

```bash
cp config.json.example config.json
```

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

**Note**: Do not commit `config.json` with real credentials. Use `config.json.example` as a template. Ensure `config.json` is copied to the output directory. Set **Copy to Output Directory** to **Copy always** or **Copy if newer** in Visual Studio.

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
│   └── IQueryInterceptor.cs    # Query interceptor interface
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

### Local Setup

Before running tests locally, create configuration files from the examples:

```bash
cp DBTools/config.json.example DBTools/config.json
cp DBToolsUnitTest/config.json.example DBToolsUnitTest/config.json
```

CI uses the same steps automatically (see `.github/workflows/ci.yml`).

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

Future enhancements being considered:

- [ ] Connection pooling configuration
- [ ] Support for stored procedures
- [ ] Query result caching
- [ ] Migration tools

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For issues, questions, or contributions:

- **GitHub Issues**: [https://github.com/gednt/DBTools_SQL/issues](https://github.com/gednt/DBTools_SQL/issues)
- **GitHub Repository**: [https://github.com/gednt/DBTools_SQL](https://github.com/gednt/DBTools_SQL)

---

**Note**: This library supports SQL Server, PostgreSQL, MySQL, and SQLite. Set the `Provider` key in `config.json` or `DbToolsOptions` to select your database.

**Security Notice**: Always store database credentials securely. Never commit `config.json` with real credentials to version control. Consider using environment variables or Azure Key Vault for production deployments.
