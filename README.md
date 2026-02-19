# DBTools_SQL

A robust .NET library for SQL Server database operations with built-in security features, parameterized queries, and comprehensive data manipulation utilities.

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![C#](https://img.shields.io/badge/C%23-latest-green.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/license-MIT-orange.svg)](LICENSE)

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Configuration](#configuration)
- [Quick Start](#quick-start)
- [Core Functionality](#core-functionality)
  - [Database Connection](#database-connection)
  - [CRUD Operations](#crud-operations)
  - [Query Builder](#query-builder)
  - [Data Export](#data-export)
- [Security](#security)
- [API Reference](#api-reference)
- [Examples](#examples)
- [Contributing](#contributing)
- [License](#license)

## Features

? **Key Features:**

- **Parameterized Queries**: Built-in protection against SQL injection attacks
- **CRUD Operations**: Complete Create, Read, Update, Delete functionality
- **Query Builder**: Helper utilities for dynamic query construction
- **Data Export**: Export data to CSV format with flexible options
- **Configuration-Based**: JSON configuration file for database settings
- **Type-Safe**: Generic object models for type-safe data handling
- **Input Validation**: Comprehensive identifier validation to prevent SQL injection
- **Error Handling**: Robust error handling and validation throughout

## Installation

### Via Source
1. Clone the repository:
```bash
git clone https://github.com/gednt/DBTools_SQL.git
```

2. Add reference to your project:
   - In Visual Studio, right-click on your project ? Add ? Reference
   - Browse to the compiled `DBTools.dll`

3. Add the configuration file to your project root (see [Configuration](#configuration))

## Configuration

Create a `config.json` file in your application's root directory:

```json
{
  "Host": "localhost",
  "Database": "YourDatabaseName",
  "Uid": "YourUsername",
  "Password": "YourPassword",
  "Port": "1433"
}
```

### Configuration Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| Host | SQL Server hostname or IP address | Yes | - |
| Database | Database name | Yes | - |
| Uid | Database username | Yes | - |
| Password | Database password | Yes | - |
| Port | SQL Server port | Yes | 1433 |

**Note**: Ensure `config.json` is copied to the output directory. Set **Copy to Output Directory** to **Copy always** or **Copy if newer** in Visual Studio.

## Quick Start

```csharp
using DBTools_Utilities;
using System.Data;

// Initialize the Utils class (automatically reads config.json)
var dbUtils = new Utils();

// Perform a simple SELECT query
DataView results = dbUtils.Select(
    "_fields": "*",
    "_table": "Users",
    "whereClause": "Age > @param0",
    "parameters": new object[] { 18 }
);

// Iterate through results
foreach (DataRowView row in results)
{
    Console.WriteLine($"Name: {row["Name"]}, Age: {row["Age"]}");
}
```

## Core Functionality

### Database Connection

The `Utils` class automatically establishes a connection using the configuration file:

```csharp
var dbUtils = new Utils();
// Connection is automatically established
```

### CRUD Operations

#### SELECT - Retrieve Data

**With Parameterized WHERE Clause:**
```csharp
DataView users = dbUtils.Select(
    "_fields": "id, name, email",
    "_table": "Users",
    "whereClause": "status = @param0 AND age > @param1",
    "parameters": new object[] { "active", 18 }
);
```

**Without WHERE Clause:**
```csharp
DataView allUsers = dbUtils.Select(
    "_fields": "*",
    "_table": "Users",
    "whereClause": "",
    "parameters": new object[] { }
);
```

**Using Query Without SELECT Keyword:**
```csharp
DataView customQuery = dbUtils.Select(
    "query_without_select": "* FROM Users INNER JOIN Orders ON Users.id = Orders.user_id WHERE Orders.total > @param0",
    "parameters": new object[] { 100.00 }
);
```

#### INSERT - Add New Records

**Manual Approach:**
```csharp
string[] fields = { "name", "email", "age" };
object[] values = { "John Doe", "john@example.com", 30 };

bool success = dbUtils.Insert(
    "_fields": fields,
    "_table": "Users",
    "_values": values
);
```

**With Auto-Increment Primary Key:**
```csharp
string[] fields = { "id", "name", "email", "age" };
object[] values = { 1, "John Doe", "john@example.com", 30 };

// The 'id' field will be automatically excluded
bool success = dbUtils.Insert(
    "_fields": fields,
    "_table": "Users",
    "_values": values,
    "primary_key_name": "id",
    "auto_increment": true
);
```

**Using QueryBuilder:**
```csharp
// Define your model class
public class User
{
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
}

// Create an instance
var newUser = new User
{
    Name = "Jane Doe",
    Email = "jane@example.com",
    Age = 25
};

// Use QueryBuilder to generate fields and values
var queryData = dbUtils.QueryBuilder(newUser);

bool success = dbUtils.Insert(
    "_fields": queryData[0].columns,
    "_table": "Users",
    "_values": queryData[0].values
);
```

#### UPDATE - Modify Records

**Traditional Approach:**
```csharp
string[] fields = { "name", "email" };
string[] values = { "John Smith", "johnsmith@example.com" };
string condition = "id = 5";

bool success = dbUtils.Update(
    "_fields": fields,
    "_table": "Users",
    "_values": values,
    "condition": condition
);
```

**LINQ-Style Approach (Recommended - MySQLDBTools-compatible standard):**
```csharp
var userController = new UtilsController<User>("Users", "Id");

// Lambda expression queries - same standard as MySQLDBTools
var user = userController.FirstOrDefault(u => u.Id == 5);
user.Email = "johnsmith@example.com";

// Update by lambda predicate
bool success = userController.Update(user, u => u.Id == 5);

// Or save changes by primary key
user.Username = "john_smith";
userController.SaveChanges(user);

// Remove by lambda predicate
userController.Remove(u => u.Status == "inactive");
```

**Parameterized Update (Recommended):**
```csharp
string[] fields = { "name", "email", "age" };
string[] values = { "John Smith", "johnsmith@example.com", "31" };
string whereClause = "id = @whereParam0";
object[] whereParams = new object[] { 5 };

bool success = dbUtils.Update(
    "_fields": fields,
    "_table": "Users",
    "_values": values,
    "whereClause": whereClause,
    "whereParameters": whereParams
);
```

#### DELETE - Remove Records

```csharp
bool success = dbUtils.Delete(
    "_table": "Users",
    "whereClause": "id = @param0",
    "parameters": new object[] { 5 }
);
```

**Delete Multiple Records:**
```csharp
bool success = dbUtils.Delete(
    "_table": "Users",
    "whereClause": "status = @param0 AND created_date < @param1",
    "parameters": new object[] { "inactive", DateTime.Now.AddYears(-1) }
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

// Generate query data
List<GenericObject> queryData = dbUtils.QueryBuilder(
    obj: product,
    primaryKeyName: "Id",
    autoIncrement: true
);

// Access generated data
string[] columns = queryData[0].columns;     // ["Name", "Price", "CreatedDate"]
object[] values = queryData[0].values;        // ["Laptop", 999.99, "2024-01-15 10:30:00"]
string[] types = queryData[0].types;          // ["String", "Decimal", "DateTime"]
```

### Data Export

Export query results to CSV format:

```csharp
using DBTools_Utilities;

var dataExport = new DataExport();

// Get data using QueryBuilder or direct query
var queryData = dbUtils.QueryBuilder(myObject);

// Export to CSV
string csvContent = dataExport.ToCsv(
    genericObject: queryData,
    separator: ',',
    showColums: true,    // Include column headers
    showTypes: true      // Include data type row
);

// Save to file
File.WriteAllText("export.csv", csvContent);
```

**CSV Output Example:**
```csv
String,Decimal,DateTime
Name,Price,CreatedDate
Laptop,999.99,2024-01-15 10:30:00
Mouse,29.99,2024-01-16 14:20:00
```

---

## UtilsController - LINQ Expression Queries (MySQLDBTools-compatible)

The `UtilsController<TModel>` follows the same LINQ standards as MySQLDBTools, supporting full lambda expression predicates similar to Entity Framework's LINQ queries.

### Why Use UtilsController?

? **LINQ Lambda Expressions**: Write `Where(u => u.Age > 18)` instead of `"Age > @param0"`  
? **Type Safety**: Compile-time checking for conditions  
? **MySQLDBTools Compatibility**: Same LINQ API across SQL Server and MySQL  
? **Familiar API**: Methods like `Add()`, `Remove()`, `SaveChanges()`, `GetAll()`, `AsQueryable()`  
? **Expression Support**: `==`, `!=`, `>`, `>=`, `<`, `<=`, `&&`, `||`, `!`

### Setup

```csharp
using DbTools.Controller;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
    public string Status { get; set; }
}

var userController = new UtilsController<User>(
    tableName: "Users",
    primaryKeyName: "Id",
    autoIncrement: true
);
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

// LINQ chaining via AsQueryable
var sorted = userController.AsQueryable()
    .Where(u => u.Age > 18)
    .OrderBy(u => u.Name)
    .Take(10)
    .ToList();

// Add (insert)
bool added = userController.Add(new User { Name = "Alice", Email = "alice@example.com", Age = 28 });

// Update with lambda predicate
bool updated = userController.Update(updatedUser, u => u.Id == 1);

// Save changes by primary key
var existing = userController.FirstOrDefault(u => u.Id == 1);
existing.Status = "active";
bool saved = userController.SaveChanges(existing);

// Remove (delete) with lambda predicate
bool deleted = userController.Remove(u => u.Id == 1);
```

---

## PropertyBasedUtilsController - LINQ-Style Queries

The `PropertyBasedUtilsController<TModel>` provides additional property-selector operations extending `UtilsController<TModel>`.

### Why Use PropertyBasedUtilsController?

? **Extended Operations**: `WhereContains`, `WhereIn`, `WhereBetween`, `WhereIsNull`, etc.  
? **Type Safety**: Compile-time checking for property names  
? **IntelliSense Support**: IDE autocomplete for properties  
? **Cleaner Code**: More readable and maintainable queries  
? **Refactoring-Friendly**: Rename properties safely with IDE refactoring tools  

### Setup

```csharp
using DBTools_Utilities.Controller;

// Define your model
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
    public string Status { get; set; }
    public DateTime? LastLogin { get; set; }
}

// Create controller
var userController = new PropertyBasedUtilsController<User>(
    tableName: "Users",
    primaryKeyName: "Id",
    autoIncrement: true
);
```

### Query Methods

#### Comparison Queries

```csharp
// Equality
var user = userController.WhereEquals(u => u.Username, "john_doe");
var notActive = userController.WhereNotEquals(u => u.Status, "active");

// Numeric comparisons
var adults = userController.WhereGreaterThan(u => u.Age, 18);
var seniors = userController.WhereGreaterThanOrEquals(u => u.Age, 65);
var young = userController.WhereLessThan(u => u.Age, 25);
var ageRange = userController.WhereBetween(u => u.Age, 25, 40);
```

#### String Queries

```csharp
// Pattern matching
var gmailUsers = userController.WhereContains(u => u.Email, "@gmail.com");
var johnUsers = userController.WhereStartsWith(u => u.Username, "john");
var adminUsers = userController.WhereEndsWith(u => u.Email, "@admin.com");
```

#### Collection Queries

```csharp
// IN clause
var specificUsers = userController.WhereIn(u => u.Id, new[] { 1, 2, 3, 5, 8 });
var activeOrPending = userController.WhereIn(u => u.Status, new[] { "active", "pending" });

// NOT IN clause
var excludedUsers = userController.WhereNotIn(u => u.Id, new[] { 99, 100 });
```

#### Null Checks

```csharp
// Check for null
var usersWithoutEmail = userController.WhereIsNull(u => u.Email);
var neverLoggedIn = userController.WhereIsNull(u => u.LastLogin);

// Check for not null
var usersWithEmail = userController.WhereIsNotNull(u => u.Email);
```

#### Example-Based Filtering

```csharp
// Use a model as a filter template
var filter = new User
{
    Status = "active",
    Age = 30
    // Only set properties you want to filter by
};

var matchingUsers = userController.WhereByExample(filter);
// Finds all users with Status='active' AND Age=30
```

### CRUD Operations

#### Retrieve Records

```csharp
// Get all records
var allUsers = userController.All();

// Get by ID
var user = userController.GetById(5);

// Get first matching record
var firstJohn = userController.FirstOrDefaultByProperty(u => u.Username, "john");

// Get single record (throws if 0 or >1 found)
var singleUser = userController.SingleByProperty(u => u.Email, "unique@example.com");

// Check existence
bool exists = userController.Exists(u => u.Username, "john_doe");
bool anyAdults = userController.AnyByProperty(u => u.Age, 18);

// Count records
int userCount = userController.Count();
int activeCount = userController.CountByProperty(u => u.Status, "active");
```

#### Insert Records

```csharp
var newUser = new User
{
    Username = "jane_doe",
    Email = "jane@example.com",
    Age = 28,
    Status = "active"
};

// Simple insert
bool success = userController.Insert(newUser);

// Insert and retrieve (gets auto-generated ID)
var insertedUser = userController.InsertAndFind(newUser, u => u.Username);
Console.WriteLine($"New user ID: {insertedUser.Id}");

// Get or create (insert if not exists)
var user = userController.GetOrCreate(newUser, u => u.Username);

// Insert or update (upsert)
userController.InsertOrUpdate(newUser, u => u.Username);
```

#### Update Records

```csharp
// Update by single property
var user = userController.GetById(5);
user.Email = "newemail@example.com";
userController.UpdateByProperty(user, u => u.Id, 5);

// Update by multiple properties
userController.UpdateWhere(user,
    u => u.Username, "john_doe",
    u => u.Status, "active");

// Update where property is in list
userController.UpdateWhereIn(user, u => u.Id, new[] { 1, 2, 3 });

// Update where property is null
userController.UpdateWhereIsNull(user, u => u.Email);
```

#### Delete Records

```csharp
// Delete by single property
userController.DeleteByProperty(u => u.Id, 5);

// Delete by multiple properties
userController.DeleteWhere(
    u => u.Status, "inactive",
    u => u.Age, 100);

// Delete where property is in list
userController.DeleteWhereIn(u => u.Id, new[] { 10, 11, 12 });

// Delete where property is null
userController.DeleteWhereIsNull(u => u.Email);
```

### Chaining with LINQ

The controller returns `IEnumerable<TModel>`, so you can chain LINQ operations:

```csharp
// Get users and process with LINQ
var topUsers = userController
    .WhereGreaterThan(u => u.Age, 18)
    .OrderByDescending(u => u.Age)
    .Take(10)
    .ToList();

// Complex filtering
var filteredUsers = userController
    .WhereEquals(u => u.Status, "active")
    .Where(u => u.Email.Contains("@gmail.com"))
    .Select(u => new { u.Username, u.Email })
    .ToList();

// Grouping
var usersByAge = userController
    .All()
    .GroupBy(u => u.Age)
    .Select(g => new { Age = g.Key, Count = g.Count() })
    .ToList();
```

### Performance Considerations

- **Database-Side Filtering**: All `Where*` methods execute on the database, not in memory
- **Parameterized Queries**: All queries use parameterized SQL for security and performance
- **Efficient Queries**: Only specified columns are retrieved
- **Connection Management**: Connections are automatically managed

### Migration from Traditional Utils

**Before (Traditional):**
```csharp
var utils = new Utils();
DataView results = utils.Select("*", "Users", "age > @param0", new object[] { 18 });

foreach (DataRowView row in results)
{
    Console.WriteLine($"User: {row["username"]}");
}
```

**After (LINQ-Style):**
```csharp
var userController = new PropertyBasedUtilsController<User>("Users", "Id");
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
// ? SAFE - Uses parameters
dbUtils.Select(
    "_fields": "*",
    "_table": "Users",
    "whereClause": "username = @param0",
    "parameters": new object[] { userInput }
);

// ? UNSAFE - String concatenation (NOT supported by parameterized methods)
// Don't do: "WHERE username = '" + userInput + "'"
```

#### 2. Identifier Validation

All table and column names are validated:

```csharp
// ? VALID identifiers
"Users"
"user_name"
"[User Table]"
"schema.table"
"id, name, email"

// ? INVALID identifiers (will throw ArgumentException)
"Users; DROP TABLE--"
"Users--"
"Users/*comment*/"
"table WITH DROP"
```

#### 3. Forbidden Keywords

The library blocks dangerous SQL keywords:
- DROP
- DELETE (in identifiers, not in WHERE clauses)
- SQL comment patterns (--, ;--, /*, */)

### Best Practices

1. **Always use parameterized methods** for user input
2. **Validate input** at the application layer
3. **Use WHERE clauses** - UPDATE and DELETE require conditions
4. **Limit permissions** - Use database user with minimal required privileges
5. **Secure config.json** - Store database credentials securely (use environment variables or secret managers in production)

## API Reference

### Utils Class

Main utility class for database operations.

#### Constructor

```csharp
public Utils()
```
Initializes the Utils instance and loads configuration from `config.json`.

**Throws:**
- `FileNotFoundException` - If config.json is not found
- `InvalidOperationException` - If configuration is invalid or incomplete

#### Methods

##### Select

```csharp
public DataView Select(string _fields, string _table, string whereClause, object[] parameters)
```

Retrieves data with parameterized WHERE clause.

**Parameters:**
- `_fields` (string) - Comma-separated field names or "*"
- `_table` (string) - Table name
- `whereClause` (string) - WHERE clause with @param0, @param1, etc.
- `parameters` (object[]) - Array of parameter values

**Returns:** `DataView` with query results

**Throws:**
- `ArgumentException` - Invalid identifiers
- `ArgumentNullException` - Null parameters array

---

```csharp
public DataView Select(string query_without_select, object[] parameters)
```

Executes a custom query without the SELECT keyword.

**Parameters:**
- `query_without_select` (string) - Query starting from field list
- `parameters` (object[]) - Array of parameter values

**Returns:** `DataView` with query results

##### Insert

```csharp
public bool Insert(string[] _fields, string _table, object[] _values, string primary_key_name = null, bool auto_increment = true)
```

Inserts a new record into the database.

**Parameters:**
- `_fields` (string[]) - Array of field names
- `_table` (string) - Table name
- `_values` (object[]) - Array of values
- `primary_key_name` (string, optional) - Primary key field name
- `auto_increment` (bool, optional) - Whether primary key auto-increments

**Returns:** `bool` - True if successful, false otherwise

##### Update

```csharp
public bool Update(string[] _fields, string _table, string[] _values, string condition = "")
```

Updates records with string condition (legacy method).

```csharp
public bool Update(string[] _fields, string _table, string[] _values, string whereClause, object[] whereParameters)
```

Updates records with parameterized WHERE clause (recommended).

**Returns:** `bool` - True if successful, false otherwise

##### Delete

```csharp
public bool Delete(string _table, string whereClause, object[] parameters)
```

Deletes records using parameterized WHERE clause.

**Parameters:**
- `_table` (string) - Table name
- `whereClause` (string) - WHERE clause with parameters
- `parameters` (object[]) - Array of parameter values

**Returns:** `bool` - True if successful, false otherwise

##### QueryBuilder

```csharp
public List<GenericObject> QueryBuilder(object obj, string primaryKeyName = "", bool autoIncrement = true)
```

Converts an object to database-ready format.

**Parameters:**
- `obj` (object) - Object to convert
- `primaryKeyName` (string, optional) - Primary key property name
- `autoIncrement` (bool, optional) - Whether to exclude primary key

**Returns:** `List<GenericObject>` with columns, values, and types

##### ExecuteQuery

```csharp
public void ExecuteQuery(string query)
```

Executes a non-query SQL command.

**Parameters:**
- `query` (string) - SQL query to execute

### GenericObject Class

Container for database operation data.

**Properties:**
- `columns` (string[]) - Column names
- `values` (object[]) - Column values
- `valuesString` (string[]) - String representation of values
- `types` (string[]) - Data types
- `table` (string) - Table name

**Methods:**
```csharp
public bool Insert()
public bool Update(string conditions)
```

### DataExport Class

Handles data export operations.

#### ToCsv

```csharp
public string ToCsv(List<GenericObject> genericObject, char separator, bool showColums = true, bool showTypes = true)
```

Exports data to CSV format.

**Parameters:**
- `genericObject` (List<GenericObject>) - Data to export
- `separator` (char) - CSV separator character
- `showColums` (bool) - Include column headers
- `showTypes` (bool) - Include type information

**Returns:** `string` - CSV formatted data

### Static Query Helpers

```csharp
public static string Select_Query(string _fields, string _table, string _conditions)
public static string Insert_Query(string[] _fields, string _table, object[] _values, string primary_key_name = "", bool auto_increment = true)
public static string Update_Query(string[] _fields, string _table, string[] _values, string condition = "")
public static string Delete_Query(string _table, string condition)
public static List<SqlParameter> GenerateSqlParameters(object[] values)
```

Generate SQL query strings (use parameterized methods instead for better security).

## Examples

### Example 1: User Management System

```csharp
using DBTools_Utilities;
using System;
using System.Data;

public class UserManager
{
    private Utils dbUtils;

    public UserManager()
    {
        dbUtils = new Utils();
    }

    public void CreateUser(string username, string email, string password)
    {
        string[] fields = { "username", "email", "password_hash", "created_date" };
        object[] values = { username, email, HashPassword(password), DateTime.Now };

        bool success = dbUtils.Insert(fields, "Users", values);

        if (success)
            Console.WriteLine("User created successfully!");
        else
            Console.WriteLine($"Error: {dbUtils.Error}");
    }

    public DataView GetUserByEmail(string email)
    {
        return dbUtils.Select(
            "*",
            "Users",
            "email = @param0",
            new object[] { email }
        );
    }

    public void UpdateUserEmail(int userId, string newEmail)
    {
        string[] fields = { "email", "updated_date" };
        string[] values = { newEmail, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };

        bool success = dbUtils.Update(
            fields,
            "Users",
            values,
            "id = @whereParam0",
            new object[] { userId }
        );

        if (success)
            Console.WriteLine("Email updated successfully!");
    }

    public void DeleteUser(int userId)
    {
        bool success = dbUtils.Delete(
            "Users",
            "id = @param0",
            new object[] { userId }
        );

        if (success)
            Console.WriteLine("User deleted successfully!");
    }

    private string HashPassword(string password)
    {
        // Implement your password hashing logic
        return password; // Placeholder
    }
}
```

### Example 2: Product Catalog with Export

```csharp
using DBTools_Utilities;
using System;
using System.Collections.Generic;
using System.IO;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class ProductCatalog
{
    private Utils dbUtils;
    private DataExport dataExport;

    public ProductCatalog()
    {
        dbUtils = new Utils();
        dataExport = new DataExport();
    }

    public void AddProduct(Product product)
    {
        var queryData = dbUtils.QueryBuilder(product, "Id", true);

        bool success = dbUtils.Insert(
            queryData[0].columns,
            "Products",
            queryData[0].values,
            "Id",
            true
        );

        if (success)
            Console.WriteLine($"Product '{product.Name}' added successfully!");
    }

    public void ExportProductsToCSV(string category, string outputPath)
    {
        // Get products by category
        var results = dbUtils.Select(
            "*",
            "Products",
            "category = @param0",
            new object[] { category }
        );

        // Convert DataView to GenericObject list
        var productList = new List<DBTools.Model.GenericObject>();

        foreach (DataRowView row in results)
        {
            var columns = new List<string>();
            var values = new List<object>();
            var types = new List<string>();

            foreach (DataColumn column in results.Table.Columns)
            {
                columns.Add(column.ColumnName);
                values.Add(row[column.ColumnName]);
                types.Add(column.DataType.Name);
            }

            productList.Add(new DBTools.Model.GenericObject
            {
                columns = columns.ToArray(),
                values = values.ToArray(),
                types = types.ToArray()
            });
        }

        // Export to CSV
        string csvContent = dataExport.ToCsv(productList, ',', true, true);
        File.WriteAllText(outputPath, csvContent);

        Console.WriteLine($"Products exported to {outputPath}");
    }

    public void UpdateProductPrice(int productId, decimal newPrice)
    {
        string[] fields = { "price" };
        string[] values = { newPrice.ToString() };

        bool success = dbUtils.Update(
            fields,
            "Products",
            values,
            "id = @whereParam0",
            new object[] { productId }
        );

        if (success)
            Console.WriteLine("Price updated successfully!");
    }
}
```

### Example 3: Advanced Query with Joins

```csharp
public class OrderManager
{
    private Utils dbUtils;

    public OrderManager()
    {
        dbUtils = new Utils();
    }

    public DataView GetUserOrdersWithDetails(int userId)
    {
        string query = @"
            o.id AS OrderId,
            o.order_date AS OrderDate,
            o.total AS Total,
            u.username AS Username,
            p.name AS ProductName,
            oi.quantity AS Quantity
            FROM Orders o
            INNER JOIN Users u ON o.user_id = u.id
            INNER JOIN OrderItems oi ON o.id = oi.order_id
            INNER JOIN Products p ON oi.product_id = p.id
            WHERE o.user_id = @param0
            ORDER BY o.order_date DESC";

        return dbUtils.Select(query, new object[] { userId });
    }

    public decimal GetTotalOrderAmount(int orderId)
    {
        var result = dbUtils.Select(
            "SUM(total) AS TotalAmount",
            "Orders",
            "id = @param0",
            new object[] { orderId }
        );

        if (result.Count > 0)
        {
            return Convert.ToDecimal(result[0]["TotalAmount"]);
        }

        return 0;
    }
}
```

## Error Handling

All methods include comprehensive error handling:

```csharp
try
{
    var dbUtils = new Utils();

    bool success = dbUtils.Insert(fields, "Users", values);

    if (!success)
    {
        // Check the Error property for details
        Console.WriteLine($"Insert failed: {dbUtils.Error}");
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
- **SQL Server** (any version compatible with Microsoft.Data.SqlClient)
- **Microsoft.Extensions.Configuration** NuGet package
- **Microsoft.Extensions.Configuration.Json** NuGet package

## Project Structure

```
DBTools_SQL/
??? DBTools/
?   ??? Classes/
?   ?   ??? DBTools.cs          # Core database connection class
?   ?   ??? DataExport.cs       # Data export utilities
?   ??? Controller/
?   ?   ??? DBToolsController.cs
?   ?   ??? UtilsController.cs
?   ?   ??? DataExportController.cs
?   ??? Interfaces/
?   ?   ??? IDBTools.cs         # Database operations interface
?   ??? Model/
?   ?   ??? GenericObject.cs    # Generic data container
?   ?   ??? GenericObject_Simple.cs
?   ??? Utils.cs                # Main utility class
?   ??? DBTools.csproj
??? DBToolsUnitTest/
?   ??? DBToolsUnitTest.csproj  # Unit tests
??? config.json                 # Database configuration
??? README.md
```

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

## Testing

The project includes a unit test project (`DBToolsUnitTest`). To run tests:

1. Open the solution in Visual Studio
2. Open Test Explorer (Test ? Test Explorer)
3. Click "Run All"

Or via command line:
```bash
dotnet test
```

## Troubleshooting

### Configuration File Not Found

**Error**: `FileNotFoundException: The configuration file 'config.json' was not found`

**Solution**: Ensure `config.json` is in your application's root directory and set to copy to output directory.

### Connection Failed

**Error**: Connection timeouts or authentication failures

**Solutions**:
- Verify SQL Server is running
- Check firewall settings
- Verify credentials in `config.json`
- Ensure SQL Server authentication is enabled
- Check network connectivity to SQL Server

### Invalid Identifier Exception

**Error**: `ArgumentException: Invalid table name` or `Invalid field names`

**Solution**: Ensure table and column names only contain alphanumeric characters, underscores, dots, brackets, and spaces. Avoid SQL keywords and special characters.

## Performance Tips

1. **Use appropriate indexes** on frequently queried columns
2. **Limit SELECT fields** - Avoid `SELECT *` when possible
3. **Use parameterized queries** - They support query plan caching
4. **Close connections** - The library handles this automatically
5. **Batch operations** when inserting/updating multiple records

## Roadmap

Future enhancements being considered:

- [ ] Async/await support for asynchronous operations
- [ ] Transaction support
- [ ] Connection pooling configuration
- [ ] Support for stored procedures
- [ ] Bulk insert operations
- [ ] Additional database providers (MySQL, PostgreSQL)
- [ ] Query result caching
- [ ] Logging and diagnostics
- [ ] Migration tools

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For issues, questions, or contributions:

- **GitHub Issues**: [https://github.com/gednt/DBTools_SQL/issues](https://github.com/gednt/DBTools_SQL/issues)
- **GitHub Repository**: [https://github.com/gednt/DBTools_SQL](https://github.com/gednt/DBTools_SQL)

## Acknowledgments

- Built with ?? for the .NET community
- Inspired by the need for simple, secure database operations
- Thanks to all contributors

---

**Note**: This library is designed for SQL Server. For other database systems, modifications may be required.

**?? Security Notice**: Always store database credentials securely. Never commit `config.json` with real credentials to version control. Consider using environment variables or Azure Key Vault for production deployments.
