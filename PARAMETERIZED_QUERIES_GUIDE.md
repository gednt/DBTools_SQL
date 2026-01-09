# Parameterized Queries Guide

This document explains how to use the new parameterized query methods in DBTools to prevent SQL injection vulnerabilities.

## Overview

The DBTools library now provides secure, parameterized versions of all query methods:
- `Select()` - with parameterized WHERE clauses
- `Update()` - with parameterized WHERE clauses  
- `Delete()` - with parameterized WHERE clauses

## Why Use Parameterized Queries?

Parameterized queries prevent SQL injection attacks by separating SQL code from data. Instead of concatenating user input directly into SQL strings, parameters are passed separately and handled safely by the database driver.

## Usage Examples

### Select with Parameters

**Old method (deprecated):**
```csharp
// VULNERABLE TO SQL INJECTION
string userId = userInput; // Could contain malicious SQL
var result = utils.Select("*", "Users", $"id = {userId}");
```

**New method (secure):**
```csharp
// SAFE - Uses parameterized query
string userId = userInput;
var result = utils.Select("*", "Users", "id = @param0", new object[] { userId });
```

**Multiple conditions:**
```csharp
var result = utils.Select(
    "id, name, email", 
    "Users", 
    "status = @param0 AND created_date > @param1",
    new object[] { "active", DateTime.Now.AddDays(-30) }
);
```

**No WHERE clause:**
```csharp
// When no filtering is needed, pass empty string and empty array
var result = utils.Select("*", "Users", "", new object[] { });
```

### Update with Parameters

**Old method (deprecated):**
```csharp
// VULNERABLE TO SQL INJECTION
utils.Update(
    new string[] { "name", "email" },
    "Users",
    new string[] { newName, newEmail },
    $"id = {userId}" // Condition not parameterized
);
```

**New method (secure):**
```csharp
// SAFE - Both SET values and WHERE clause are parameterized
utils.Update(
    new string[] { "name", "email" },
    "Users",
    new string[] { newName, newEmail },
    "id = @whereParam0",
    new object[] { userId }
);
```

**Multiple WHERE conditions:**
```csharp
utils.Update(
    new string[] { "status" },
    "Orders",
    new string[] { "shipped" },
    "order_id = @whereParam0 AND customer_id = @whereParam1",
    new object[] { orderId, customerId }
);
```

### Delete with Parameters

**Old method (deprecated):**
```csharp
// VULNERABLE TO SQL INJECTION
utils.Delete("Users", $"id = {userId}");
```

**New method (secure):**
```csharp
// SAFE - Uses parameterized WHERE clause
utils.Delete("Users", "id = @param0", new object[] { userId });
```

**Multiple conditions:**
```csharp
utils.Delete(
    "Sessions",
    "user_id = @param0 AND expires_at < @param1",
    new object[] { userId, DateTime.Now }
);
```

### Select with Custom Query

**Old method (deprecated):**
```csharp
// VULNERABLE TO SQL INJECTION
var result = utils.Select($"* FROM Users WHERE email = '{email}'");
```

**New method (secure):**
```csharp
// SAFE - Uses parameters
var result = utils.Select(
    "* FROM Users WHERE email = @param0",
    new object[] { email }
);
```

## Parameter Naming Convention

- **Select/Delete**: Parameters are named `@param0`, `@param1`, `@param2`, etc.
- **Update WHERE clause**: Parameters are named `@whereParam0`, `@whereParam1`, etc.
- **Update SET clause**: Parameters are named `@param0`, `@param1`, etc. (managed internally)

The library automatically creates these parameter names. You just need to:
1. Use the parameter placeholders in your SQL strings
2. Pass the values in the correct order in the parameters array

## Data Types

The parameterized methods accept `Object[]` arrays, so you can pass any type:
```csharp
new object[] { 
    42,                          // int
    "John Doe",                  // string
    DateTime.Now,                // DateTime
    true,                        // bool
    null                         // null values are handled safely
}
```

## Migration Guide

### Migrating Select Queries

**Before:**
```csharp
var data = utils.Select("*", "Products", $"category = '{category}' AND price > {minPrice}");
```

**After:**
```csharp
var data = utils.Select("*", "Products", "category = @param0 AND price > @param1", 
    new object[] { category, minPrice });
```

### Migrating Update Queries

**Before:**
```csharp
utils.Update(
    new string[] { "quantity" },
    "Inventory",
    new string[] { newQty.ToString() },
    $"product_id = {productId}"
);
```

**After:**
```csharp
utils.Update(
    new string[] { "quantity" },
    "Inventory",
    new string[] { newQty.ToString() },
    "product_id = @whereParam0",
    new object[] { productId }
);
```

### Migrating Delete Queries

**Before:**
```csharp
utils.Delete("TempData", $"created_at < '{cutoffDate}'");
```

**After:**
```csharp
utils.Delete("TempData", "created_at < @param0", new object[] { cutoffDate });
```

## Best Practices

1. **Always use parameterized queries** for user input or any external data
2. **Never concatenate user input** into SQL strings
3. **Validate input** at the application level (length, format, etc.)
4. **Use specific column names** instead of `*` when possible for better performance
5. **Test with malicious input** to ensure your queries are secure

## Backward Compatibility

The old methods are still available but marked as `[Obsolete]`. You'll see compiler warnings:
- `Select(String, String, String)` - Use the 4-parameter overload instead
- `Update(String[], String, String[], String)` - Use the 5-parameter overload instead  
- `Delete(String, String)` - Use the 3-parameter overload instead

Plan to migrate your code to the new methods to avoid security vulnerabilities.

## Security Benefits

✅ **SQL Injection Prevention**: User input cannot modify SQL structure  
✅ **Type Safety**: Database driver handles type conversion safely  
✅ **Better Performance**: Database can cache execution plans  
✅ **Cleaner Code**: Separation of SQL logic and data  

## Additional Resources

- [OWASP SQL Injection Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/SQL_Injection_Prevention_Cheat_Sheet.html)
- [Microsoft SQL Parameter Documentation](https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlparameter)
