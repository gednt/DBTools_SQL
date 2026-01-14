# DBTools_SQL Quick Start Guide

Get up and running with DBTools_SQL in under 5 minutes!

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

## 5-Minute Tutorial

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

### Using Objects (Recommended)

Instead of manually defining fields and values, use objects with QueryBuilder:

```csharp
// Define a model
public class User
{
    public string Username { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
}

// Create an instance
var user = new User
{
    Username = "john_doe",
    Email = "john@example.com",
    Age = 30
};

// Use QueryBuilder to convert to database format
var queryData = utils.QueryBuilder(user);

// Insert easily
utils.Insert(
    queryData[0].columns,
    "Users",
    queryData[0].values
);
```

### Multiple Conditions

```csharp
// Multiple WHERE conditions
DataView results = utils.Select(
    "*",
    "Users",
    "age > @param0 AND city = @param1 AND status = @param2",
    new object[] { 18, "New York", "active" }
);
```

### Joins

```csharp
// Query with JOIN
string query = @"
    u.username,
    o.order_date,
    o.total
    FROM Users u
    INNER JOIN Orders o ON u.id = o.user_id
    WHERE u.id = @param0";

DataView results = utils.Select(query, new object[] { userId });
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
