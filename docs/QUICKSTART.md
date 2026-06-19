# DBTools_SQL Quick Start Guide

Get up and running with DBTools_SQL in under 5 minutes!

## Table of Contents

1. [Installation](#installation)
2. [Traditional Approach - 5-Minute Tutorial](#traditional-approach---5-minute-tutorial)
3. [LINQ-Style Approach - Modern Queries](#linq-style-approach---modern-queries)
4. [JOIN Queries](#join-queries)
5. [Common Patterns](#common-patterns)
6. [Best Practices](#best-practices)
7. [Troubleshooting](#troubleshooting)

## Installation

### Step 1: Add DBTools_SQL to Your Project

1. Clone or download the DBTools_SQL repository
2. Add a reference to `DBTools.dll` in your project
3. Add the following using statements to your code:

```csharp
using DBTools.Core;       // SqlClient, DbConfiguration, SqlValidator
using DBTools.Models;     // GenericObject
using System.Data;
```

### Step 2: Create Configuration File

Copy `config.json.example` to `config.json` and update the values:

```bash
cp config.json.example config.json
```

Example `config.json`:

```json
{
  "Provider": "SqlServer",
  "Host": "localhost\\SQLEXPRESS",
  "Database": "YourDatabaseName",
  "Uid": "your_username",
  "Password": "your_password",
  "Port": "1433"
}
```

**Important**: Set the file properties in Visual Studio:
- Right-click `config.json` → Properties
- Set **Copy to Output Directory** to **Copy always**

### Step 3: Initialize SqlClient

```csharp
using DBTools.Core;

// Initialize (automatically reads config.json)
var db = new SqlClient();

// You're ready to go!
```

---

## Traditional Approach - 5-Minute Tutorial

### 1. SELECT - Read Data

```csharp
// Get all active users
DataView users = db.Select(
    "*",                          // Fields to select
    "Users",                      // Table name
    "status = @param0",          // WHERE clause with parameter
    new object[] { "active" }    // Parameter value
);

// Display results
foreach (DataRowView row in users)
{
    Console.WriteLine($"User: {row["username"]}");
}
```

### 2. INSERT - Add New Data

```csharp
// Define fields and values
string[] fields = { "username", "email", "age" };
object[] values = { "john_doe", "john@example.com", 30 };

// Insert
bool success = db.Insert(fields, "Users", values);

if (success)
    Console.WriteLine("User added!");
else
    Console.WriteLine($"Error: {db.Error}");
```

### 3. UPDATE - Modify Data

```csharp
// Update user's email
string[] fields = { "email" };
string[] values = { "newemail@example.com" };

bool success = db.Update(
    fields,
    "Users",
    values,
    "username = @whereParam0",        // WHERE clause
    new object[] { "john_doe" }       // Parameter value
);

if (success)
    Console.WriteLine("Email updated!");
```

### 4. DELETE - Remove Data

```csharp
// Delete user by ID
bool success = db.Delete(
    "Users",                           // Table
    "id = @param0",                   // WHERE clause
    new object[] { 5 }                // Parameter value
);

if (success)
    Console.WriteLine("User deleted!");
```

---

## LINQ-Style Approach - Modern Queries

For a more modern, type-safe approach similar to Entity Framework, use `LinqHelper<TModel>` with LINQ lambda expressions:

### Setup

```csharp
using DBTools.Controllers;

// Define your model
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
    public string Status { get; set; }
}

// Create controller (reads config.json automatically)
var userController = new LinqHelper<User>("Users", "Id");
```

### Query Operations

```csharp
// GET ALL: Return all records
var allUsers = userController.GetAll();

// WHERE: Filter with lambda expression
var activeUsers = userController.Where(u => u.Status == "active");

// WHERE: Multiple conditions (AND)
var adults = userController.Where(u => u.Age > 18 && u.Status == "active");

// WHERE: OR conditions
var specific = userController.Where(u => u.Username == "john" || u.Username == "jane");

// FIRST: Get first match
var user = userController.FirstOrDefault(u => u.Id == 1);

// FIND: Get by primary key
var found = userController.Find(5);

// EXISTS: Check if any match
bool exists = userController.Any(u => u.Username == "john_doe");

// COUNT: Count matching records
int count = userController.Count(u => u.Age > 18);
```

### CRUD Operations

```csharp
// INSERT: Add (MySQLDBTools-compatible name)
var newUser = new User
{
    Username = "john_doe",
    Email = "john@example.com",
    Age = 30,
    Status = "active"
};
bool added = userController.Add(newUser);

// BULK INSERT: InsertRange
bool bulkAdded = userController.InsertRange(new List<User>
{
    new User { Username = "bob", Email = "bob@example.com", Age = 30 },
    new User { Username = "carol", Email = "carol@example.com", Age = 25 }
});

// UPDATE: Update by lambda expression
var updated = new User { Username = "john_doe", Email = "newemail@example.com", Age = 31, Status = "active" };
bool updateSuccess = userController.Update(updated, u => u.Id == 5);

// SAVE CHANGES: Update using primary key
var existingUser = userController.FirstOrDefault(u => u.Id == 5);
if (existingUser != null)
{
    existingUser.Email = "updated@example.com";
    bool saved = userController.SaveChanges(existingUser);
}

// DELETE: Remove by lambda expression
bool deleted = userController.Remove(u => u.Id == 5);
bool deletedOld = userController.Remove(u => u.Status == "inactive");
```

### Property-Based Queries (Linq<TModel>)

For even more type-safe queries with IntelliSense support:

```csharp
using DBTools.Controllers;

var userController = new Linq<User>("Users", "Id");

// Comparison queries
var adults = userController.WhereGreaterThan(u => u.Age, 18);
var ageRange = userController.WhereBetween(u => u.Age, 25, 40);

// String queries
var gmailUsers = userController.WhereContains(u => u.Email, "@gmail.com");
var adminUsers = userController.WhereStartsWith(u => u.Username, "admin_");

// Collection queries
var specificUsers = userController.WhereIn(u => u.Id, new[] { 1, 2, 3 });

// Null checks
var neverLoggedIn = userController.WhereIsNull(u => u.LastLogin);

// Upsert
userController.InsertOrUpdate(newUser, u => u.Username);

// Get or create
var user = userController.GetOrCreate(newUser, u => u.Username);
```

### Benefits of LINQ-Style Approach

- **Type Safety**: Compile-time checking
- **IntelliSense**: IDE autocomplete
- **Refactoring**: Safe property renames
- **Cleaner Code**: More readable
- **Less Errors**: No string-based column names

---

## JOIN Queries

The `Linq<TModel>` supports type-safe JOIN operations:

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
var innerResults = userController.InnerJoin<Order>(
    "Orders",
    u => u.Id,
    o => o.UserId
).ToList();

foreach (var row in innerResults)
{
    Console.WriteLine($"{row.Left.Username}: ${row.Right.Total}");
}

// LEFT JOIN (Right may be null)
var leftResults = userController.LeftJoin<Order>(
    "Orders",
    u => u.Id,
    o => o.UserId
).ToList();

foreach (var row in leftResults)
{
    var orderTotal = row.Right != null ? row.Right.Total.ToString() : "No orders";
    Console.WriteLine($"{row.Left.Username}: {orderTotal}");
}

// JOIN with LINQ chaining
var filteredJoin = userController.InnerJoin<Order>("Orders", u => u.Id, o => o.UserId)
    .Where(j => j.Left.Age > 18)
    .OrderBy(j => j.Left.Username)
    .Take(10)
    .ToList();
```

---

## Common Patterns

### Pattern 1: Traditional SqlClient Class

```csharp
using DBTools.Core;

var db = new SqlClient();

// Direct SQL-style queries
DataView results = db.Select(
    "*",
    "Users",
    "age > @param0",
    new object[] { 18 }
);
```

### Pattern 2: LINQ-Style Controller (Recommended)

```csharp
using DBTools.Controllers;

var userController = new LinqHelper<User>("Users", "Id");

// Type-safe queries with lambda expressions
var adults = userController.Where(u => u.Age > 18);
var user = userController.FirstOrDefault(u => u.Id == 1);
bool added = userController.Add(new User { Name = "John", Age = 30 });
bool deleted = userController.Remove(u => u.Id == 1);
```

### Pattern 3: Dependency Injection

```csharp
using DBTools.Core;
using DBTools.Abstractions;
using Microsoft.Extensions.Configuration;

// Custom configuration
var config = new DbConfiguration(myIConfiguration);
var validator = new SqlValidator();
var queryBuilder = new SqlQueryBuilder(validator);

var db = new SqlClient(config, validator, queryBuilder);
var userController = new LinqHelper<User>(db, "Users", "Id");
```

---

## Best Practices

**DO:**
- Always use parameterized queries (the library does this automatically)
- Include WHERE clauses for UPDATE and DELETE operations
- Validate input at the application layer
- Handle errors appropriately
- Use environment variables for production credentials
- Depend on abstractions (`ISqlClient`, `ISqlValidator`) for testability

**DON'T:**
- Don't concatenate user input into SQL strings
- Don't update/delete without WHERE clauses (library prevents this)
- Don't expose detailed error messages to end users
- Don't commit `config.json` with real credentials to version control

---

## Next Steps

### Learn More:
1. Read the [Full README](../README.md) for comprehensive documentation
2. Check [API Reference](API_REFERENCE.md) for detailed method descriptions
3. Review [Security Guide](SECURITY.md) for security best practices
4. Explore [Examples](EXAMPLES.md) for real-world use cases

### Create Your First Application:
1. **User Management System** - Implement user CRUD operations
2. **Inventory System** - Track products and stock levels
3. **Order Management** - Handle orders with JOINs
4. **Reporting Dashboard** - Generate data reports with exports

---

## Troubleshooting

### Problem: "config.json not found"

**Solution**: Ensure `config.json` is in the output directory:
1. Right-click `config.json` in Visual Studio
2. Properties → Copy to Output Directory → "Copy always"

### Problem: "Cannot connect to database"

**Solutions**:
- Verify SQL Server is running
- Check credentials in `config.json`
- Ensure firewall allows connections
- Verify SQL Server authentication is enabled

### Problem: "Invalid identifier"

**Solution**: Table/column names can only contain:
- Letters, numbers, underscores
- Dots (for schema.table)
- Brackets ([Table Name])
- No special characters or SQL keywords

---

## Need Help?

- [Full Documentation](../README.md)
- [Security Guide](SECURITY.md)
- [Code Examples](EXAMPLES.md)
- [Report Issues](https://github.com/gednt/DBTools_SQL/issues)

---

**Happy Coding!**
