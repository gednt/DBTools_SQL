# DBTools_SQL Quick Start Guide

Get up and running with DBTools_SQL in under 5 minutes!

## Table of Contents

1. [Installation](#installation)
2. [Traditional Approach - 5-Minute Tutorial](#traditional-approach---5-minute-tutorial)
3. [LINQ-Style Approach - Modern Queries](#linq-style-approach---modern-queries)
4. [Common Patterns](#common-patterns)
5. [Best Practices](#best-practices)
6. [Troubleshooting](#troubleshooting)

## Installation

### Step 1: Add DBTools_SQL to Your Project

1. Clone or download the DBTools_SQL repository
2. Add a reference to `DBTools.dll` in your project
3. Add the following using statements to your code:

```csharp
using DBTools_Utilities;
using System.Data;
```

### Step 2: Create Configuration File

Create a `config.json` file in your application's root directory:

```json
{
  "Host": "localhost",
  "Database": "YourDatabaseName",
  "Uid": "your_username",
  "Password": "your_password",
  "Port": "1433"
}
```

**Important**: Set the file properties in Visual Studio:
- Right-click `config.json` ? Properties
- Set **Copy to Output Directory** to **Copy always**

### Step 3: Initialize DBTools

```csharp
using DBTools_Utilities;

// Initialize (automatically reads config.json)
var utils = new Utils();

// You're ready to go!
```

---

## Traditional Approach - 5-Minute Tutorial

### 1. SELECT - Read Data

```csharp
// Get all active users
DataView users = utils.Select(
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
bool success = utils.Insert(fields, "Users", values);

if (success)
    Console.WriteLine("User added!");
else
    Console.WriteLine($"Error: {utils.Error}");
```

### 3. UPDATE - Modify Data

```csharp
// Update user's email
string[] fields = { "email" };
string[] values = { "newemail@example.com" };

bool success = utils.Update(
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
bool success = utils.Delete(
    "Users",                           // Table
    "id = @param0",                   // WHERE clause
    new object[] { 5 }                // Parameter value
);

if (success)
    Console.WriteLine("User deleted!");
```

---

## LINQ-Style Approach - Modern Queries

For a more modern, type-safe approach similar to Entity Framework, use the `UtilsController` with LINQ lambda expressions:

### Setup

```csharp
using DbTools.Controller;

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
var userController = new UtilsController<User>("Users", "Id");
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

// SINGLE: Get exactly one match
var unique = userController.SingleOrDefault(u => u.Id == 1);

// EXISTS: Check if any match
bool exists = userController.Any(u => u.Username == "john_doe");

// COUNT: Count matching records
int count = userController.Count(u => u.Age > 18);

// QUERYABLE: LINQ chaining
var topUsers = userController.AsQueryable()
    .Where(u => u.Age > 18)
    .OrderByDescending(u => u.Age)
    .Take(10)
    .ToList();
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

// DELETE: Remove by lambda expression (MySQLDBTools-compatible name)
bool deleted = userController.Remove(u => u.Id == 5);
bool deletedOld = userController.Remove(u => u.Status == "inactive");
```

### Benefits of LINQ-Style Approach

? **Type Safety**: Compile-time checking  
? **IntelliSense**: IDE autocomplete  
? **Refactoring**: Safe property renames  
? **Cleaner Code**: More readable  
? **Less Errors**: No string-based column names  

---

## Complete Example

Here's a complete console application example:

```csharp
using System;
using System.Data;
using DBTools_Utilities;

namespace DBToolsQuickStart
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Initialize
                var utils = new Utils();
                Console.WriteLine("Connected to database!");
                
                // INSERT: Add a new user
                Console.WriteLine("\n1. Adding new user...");
                string[] fields = { "username", "email", "age", "created_date" };
                object[] values = { "jane_doe", "jane@example.com", 28, DateTime.Now };
                
                bool inserted = utils.Insert(fields, "Users", values, "id", true);
                
                if (inserted)
                {
                    Console.WriteLine("? User added successfully!");
                }
                
                // SELECT: Retrieve users
                Console.WriteLine("\n2. Retrieving users...");
                DataView users = utils.Select(
                    "id, username, email, age",
                    "Users",
                    "age >= @param0",
                    new object[] { 18 }
                );
                
                Console.WriteLine($"? Found {users.Count} users:");
                foreach (DataRowView row in users)
                {
                    Console.WriteLine($"  - {row["username"]} ({row["email"]}), Age: {row["age"]}");
                }
                
                // UPDATE: Modify a user
                Console.WriteLine("\n3. Updating user...");
                string[] updateFields = { "email" };
                string[] updateValues = { "jane.doe@example.com" };
                
                bool updated = utils.Update(
                    updateFields,
                    "Users",
                    updateValues,
                    "username = @whereParam0",
                    new object[] { "jane_doe" }
                );
                
                if (updated)
                {
                    Console.WriteLine("? User updated successfully!");
                }
                
                // SELECT: Verify update
                Console.WriteLine("\n4. Verifying update...");
                DataView updatedUser = utils.Select(
                    "username, email",
                    "Users",
                    "username = @param0",
                    new object[] { "jane_doe" }
                );
                
                if (updatedUser.Count > 0)
                {
                    Console.WriteLine($"? New email: {updatedUser[0]["email"]}");
                }
                
                Console.WriteLine("\n? All operations completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error: {ex.Message}");
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
```

---

## Common Patterns

### Pattern 1: Traditional Utils Class

```csharp
var utils = new Utils();

// Direct SQL-style queries
DataView results = utils.Select(
    "*",
    "Users",
    "age > @param0",
    new object[] { 18 }
);
```

### Pattern 2: LINQ-Style Controller (Recommended)

```csharp
var userController = new UtilsController<User>("Users", "Id");

// Type-safe queries with lambda expressions (MySQLDBTools-compatible LINQ standard)
var adults = userController.Where(u => u.Age > 18);
var user = userController.FirstOrDefault(u => u.Id == 1);
bool added = userController.Add(new User { Name = "John", Age = 30 });
bool deleted = userController.Remove(u => u.Id == 1);
```

---

## Best Practices

? **DO:**
- Always use parameterized queries (the library does this automatically)
- Include WHERE clauses for UPDATE and DELETE operations
- Validate input at the application layer
- Handle errors appropriately
- Use environment variables for production credentials

? **DON'T:**
- Don't concatenate user input into SQL strings
- Don't update/delete without WHERE clauses (library prevents this)
- Don't expose detailed error messages to end users
- Don't commit `config.json` with real credentials to version control

---

## Next Steps

### Learn More:
1. Read the [Full README](../README.md) for comprehensive documentation
2. Check [API Reference](docs/API_REFERENCE.md) for detailed method descriptions
3. Review [Security Guide](docs/SECURITY.md) for security best practices
4. Explore [Examples](docs/EXAMPLES.md) for real-world use cases

### Create Your First Application:
1. **User Management System** - Implement user CRUD operations
2. **Inventory System** - Track products and stock levels
3. **Order Management** - Handle orders and order items
4. **Reporting Dashboard** - Generate data reports with exports

---

## Troubleshooting

### Problem: "config.json not found"

**Solution**: Ensure `config.json` is in the output directory:
1. Right-click `config.json` in Visual Studio
2. Properties ? Copy to Output Directory ? "Copy always"

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

- ?? [Full Documentation](../README.md)
- ?? [Security Guide](docs/SECURITY.md)
- ?? [Code Examples](docs/EXAMPLES.md)
- ?? [Report Issues](https://github.com/gednt/DBTools_SQL/issues)

---

**Happy Coding!** ??
