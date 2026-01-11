# UtilsController Usage Guide

## Overview

The `UtilsController<TModel>` class provides LINQ-style database manipulation capabilities compatible with any model type. It offers a familiar interface similar to Entity Framework for Insert, Update, Delete, and Select operations.

## Features

- **Generic Type Support**: Works with any model class
- **LINQ-Style Operations**: Familiar syntax with methods like `Select()`, `Where()`, `FirstOrDefault()`, `Any()`, `Count()`, etc.
- **Type Safety**: Compile-time type checking for your models
- **Automatic Mapping**: Properties are automatically mapped to database columns
- **Deferred Execution**: IEnumerable support for efficient querying
- **Security**: Uses parameterized queries internally to prevent SQL injection

## Prerequisites

Your model class must:
1. Be a reference type (`class`)
2. Have a parameterless constructor
3. Have public properties with getters and setters that match database column names

## Basic Usage

### 1. Define Your Model

```csharp
public class User
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
```

### 2. Create a UtilsController Instance

```csharp
// Method 1: Direct initialization with connection parameters
var userController = new UtilsController<User>(
    host: "localhost",
    database: "MyDatabase",
    uid: "sa",
    password: "YourPassword",
    tableName: "Users",
    port: "1433",
    primaryKeyName: "UserId",
    autoIncrement: true
);

// Method 2: Using an existing Utils instance
var utils = new DBTools_Utilities.Utils("localhost", "MyDatabase", "sa", "YourPassword", "1433");
var userController = new UtilsController<User>(utils, "Users", "UserId", true);
```

### 3. Select Operations

```csharp
// Get all users
IEnumerable<User> allUsers = userController.Select();

// Get users with conditions (SQL WHERE clause)
IEnumerable<User> activeUsers = userController.Select("IsActive = 1");

// Use LINQ Where for in-memory filtering
var adminUsers = userController.Where(u => u.Username.StartsWith("admin"));

// Get as a list
List<User> userList = userController.ToList();

// Get with conditions as a list
List<User> activeUserList = userController.ToList("IsActive = 1");
```

### 4. FirstOrDefault Operations

```csharp
// Get first user with SQL conditions
User firstUser = userController.FirstOrDefault("UserId = 1");

// Get first user with LINQ predicate
User adminUser = userController.FirstOrDefault(u => u.Username == "admin");

// Get first user (no conditions)
User anyUser = userController.FirstOrDefault();
```

### 5. Insert Operations

```csharp
var newUser = new User
{
    // UserId is auto-incremented, so we don't set it
    Username = "johndoe",
    Email = "john@example.com",
    CreatedAt = DateTime.Now,
    IsActive = true
};

bool success = userController.Insert(newUser);
if (success)
{
    Console.WriteLine("User inserted successfully!");
}
else
{
    Console.WriteLine($"Error: {userController.Error}");
}
```

### 6. Update Operations

```csharp
var user = userController.FirstOrDefault("UserId = 1");
if (user != null)
{
    user.Email = "newemail@example.com";
    user.IsActive = false;
    
    bool success = userController.Update(user, "UserId = 1");
    if (success)
    {
        Console.WriteLine("User updated successfully!");
    }
    else
    {
        Console.WriteLine($"Error: {userController.Error}");
    }
}
```

### 7. Delete Operations

```csharp
// Delete specific user
bool success = userController.Delete("UserId = 1");

// Delete multiple users matching conditions
success = userController.Delete("IsActive = 0 AND CreatedAt < '2020-01-01'");

if (success)
{
    Console.WriteLine("User(s) deleted successfully!");
}
else
{
    Console.WriteLine($"Error: {userController.Error}");
}
```

### 8. Query Operations

```csharp
// Count all users
int totalUsers = userController.Count();

// Count with conditions
int activeUserCount = userController.Count("IsActive = 1");

// Check if any users exist
bool hasUsers = userController.Any();

// Check if specific users exist
bool hasActiveUsers = userController.Any("IsActive = 1");

// Check with LINQ predicate
bool hasAdmins = userController.Any(u => u.Username.StartsWith("admin"));
```

## Advanced Usage

### Working with Complex Queries

```csharp
// Chaining LINQ operations
var recentActiveUsers = userController
    .Select("IsActive = 1")
    .Where(u => u.CreatedAt > DateTime.Now.AddMonths(-1))
    .OrderBy(u => u.Username)
    .ToList();

// Using LINQ aggregations
var usersByDomain = userController
    .Select()
    .GroupBy(u => u.Email.Split('@')[1])
    .ToDictionary(g => g.Key, g => g.Count());
```

### Access to Underlying Utils

```csharp
// Access the underlying Utils instance for advanced operations
var rawDataView = userController.Utils.Select("*", "Users", "IsActive = 1");

// Execute custom queries
userController.Utils.ExecuteQuery("UPDATE Users SET LastLogin = GETDATE() WHERE UserId = 1");

// Get error details
string errorMessage = userController.Error;
```

### Working with Different Models

```csharp
// Define another model
public class Product
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

// Create controller for Products table
var productController = new UtilsController<Product>(
    host: "localhost",
    database: "MyDatabase",
    uid: "sa",
    password: "YourPassword",
    tableName: "Products",
    primaryKeyName: "ProductId",
    autoIncrement: true
);

// Use the same operations
var products = productController.Select("Stock > 0").ToList();
```

## Important Notes

### Security Considerations

1. **Always use conditions**: The `Update` and `Delete` methods require conditions to prevent accidental mass operations
2. **Parameterized queries**: The underlying implementation uses parameterized queries for Insert and Update operations
3. **Input validation**: The Utils class validates identifiers to prevent SQL injection

### Performance Considerations

1. **Deferred execution**: `Select()` returns `IEnumerable<TModel>` which supports deferred execution
2. **In-memory filtering**: Methods like `Where(predicate)` load all data first, then filter. Use SQL conditions in `Select(conditions)` for better performance with large datasets
3. **Connection management**: The underlying Utils class manages database connections efficiently

### Type Conversion

The UtilsController automatically handles type conversions between database types and .NET types, including:
- Nullable types
- DateTime conversions
- Numeric types
- String values

### Limitations

1. **Property names must match column names**: The automatic mapping is case-insensitive but requires property names to match database column names
2. **Simple primary key handling**: Currently supports single-column primary keys
3. **No navigation properties**: Unlike Entity Framework, there's no support for related entities or lazy loading

## Examples

### Complete Example

```csharp
using DBTools.Controller;
using System;
using System.Linq;

public class Program
{
    public static void Main()
    {
        // Initialize controller
        var userController = new UtilsController<User>(
            "localhost", "TestDB", "sa", "password", "Users", 
            primaryKeyName: "UserId", autoIncrement: true
        );

        // Insert a new user
        var newUser = new User
        {
            Username = "alice",
            Email = "alice@example.com",
            CreatedAt = DateTime.Now,
            IsActive = true
        };
        
        if (userController.Insert(newUser))
        {
            Console.WriteLine("User created successfully!");
        }

        // Query users
        var activeUsers = userController
            .Select("IsActive = 1")
            .OrderBy(u => u.Username)
            .ToList();

        Console.WriteLine($"Found {activeUsers.Count} active users:");
        foreach (var user in activeUsers)
        {
            Console.WriteLine($"- {user.Username} ({user.Email})");
        }

        // Update a user
        var userToUpdate = userController.FirstOrDefault("Username = 'alice'");
        if (userToUpdate != null)
        {
            userToUpdate.Email = "alice.new@example.com";
            userController.Update(userToUpdate, $"UserId = {userToUpdate.UserId}");
        }

        // Delete inactive users
        userController.Delete("IsActive = 0");

        Console.WriteLine("Operations completed!");
    }
}
```

## Comparison with Entity Framework

| Feature | UtilsController | Entity Framework |
|---------|----------------|------------------|
| LINQ Support | Yes (basic) | Yes (comprehensive) |
| Generic Types | Yes | Yes |
| Automatic Mapping | Yes | Yes |
| Change Tracking | No | Yes |
| Navigation Properties | No | Yes |
| Migrations | No | Yes |
| SQL Generation | Manual conditions | Automatic |
| Connection String | Constructor parameter | Configuration |
| Learning Curve | Low | Medium-High |

## Troubleshooting

### Common Issues

1. **"Invalid identifier" exceptions**: Ensure table and column names contain only alphanumeric characters, underscores, dots, and brackets
2. **Type conversion errors**: Verify that model property types match database column types
3. **Update/Delete requires conditions**: Both operations require a WHERE clause for security
4. **Property not mapped**: Check that property names match column names (case-insensitive)

### Getting Error Details

```csharp
if (!userController.Insert(newUser))
{
    Console.WriteLine($"Error: {userController.Error}");
}
```

## Conclusion

The `UtilsController<TModel>` provides a lightweight, LINQ-style interface for database operations without the overhead of a full ORM. It's ideal for projects that need simple CRUD operations with type safety and a familiar syntax.
