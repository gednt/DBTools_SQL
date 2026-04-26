# DBTools_SQL Security Guide

Security features, best practices, and vulnerability prevention for DBTools_SQL.

## Table of Contents

1. [SQL Injection Prevention](#sql-injection-prevention)
2. [Identifier Validation](#identifier-validation)
3. [Configuration Security](#configuration-security)
4. [Input Validation](#input-validation)
5. [Common Vulnerabilities](#common-vulnerabilities)
6. [Abstractions for Testability](#abstractions-for-testability)
7. [Security Checklist](#security-checklist)

---

## SQL Injection Prevention

SQL injection is the most critical security risk for database applications. DBTools_SQL provides multiple layers of defense.

### How Parameterized Queries Work

All CRUD methods in `SqlClient` use parameterized queries by default:

```csharp
using DBTools.Core;

var db = new SqlClient();

// SAFE: Parameterized query
DataView results = db.Select(
    "*",
    "Users",
    "username = @param0 AND status = @param1",
    new object[] { userInput, "active" }
);
```

**What happens internally:**
1. The WHERE clause uses `@param0`, `@param1` placeholders
2. Values are passed as `SqlParameter` objects
3. SQL Server treats parameters as data, not executable code
4. Even if `userInput` contains SQL, it cannot alter the query

### Attack Scenario: Without Parameterization

```csharp
// VULNERABLE: String concatenation (NOT how DBTools_SQL works)
string query = "SELECT * FROM Users WHERE username = '" + userInput + "'";
// If userInput = "admin' OR '1'='1"
// Result: SELECT * FROM Users WHERE username = 'admin' OR '1'='1'
// Returns ALL users!
```

### How DBTools_SQL Prevents This

```csharp
// SAFE: DBTools_SQL parameterized approach
DataView results = db.Select(
    "*",
    "Users",
    "username = @param0",
    new object[] { "admin' OR '1'='1" }
);
// The input is treated as a literal string value
// Searches for a user literally named "admin' OR '1'='1"
// Returns no results (expected behavior)
```

### Parameterized Methods Summary

| Method | Parameter Style | Security Level |
|--------|----------------|----------------|
| `Select(fields, table, whereClause, parameters)` | `@param0`, `@param1`, ... | **Secure** |
| `Select(queryWithoutSelect, parameters)` | `@param0`, `@param1`, ... | **Secure** |
| `Insert(fields, table, values, ...)` | `@param0`, `@param1`, ... | **Secure** |
| `Update(fields, table, values, whereClause, whereParameters)` | `@whereParam0`, `@whereParam1`, ... | **Secure** |
| `Delete(table, whereClause, parameters)` | `@param0`, `@param1`, ... | **Secure** |

### LINQ Expression Security

`LinqHelper<TModel>` and `Linq<TModel>` automatically generate parameterized queries from lambda expressions:

```csharp
using DBTools.Controllers;

var userController = new LinqHelper<User>("Users", "Id");

// Lambda expression is automatically converted to parameterized SQL
var results = userController.Where(u => u.Username == userInput);
// Generated SQL: SELECT * FROM Users WHERE Username = @param0
// Parameter: @param0 = userInput (safe)
```

**Supported expression operators** are all safely parameterized:
- `==`, `!=` → `=`, `<>`
- `>`, `>=`, `<`, `<=` → `>`, `>=`, `<`, `<=`
- `&&`, `||` → `AND`, `OR`
- `!` → `NOT`

---

## Identifier Validation

The `SqlValidator` class validates all table and column names to prevent injection through identifiers.

### How It Works

```csharp
using DBTools.Core;

var validator = new SqlValidator();

// Valid identifiers
validator.IsValidIdentifier("Users");           // true
validator.IsValidIdentifier("user_name");        // true
validator.IsValidIdentifier("[User Table]");     // true
validator.IsValidIdentifier("dbo.Users");        // true
validator.IsValidIdentifier("id, name, email");  // true
validator.IsValidIdentifier("*");                // true

// Invalid identifiers (blocked)
validator.IsValidIdentifier("Users; DROP TABLE Users--");  // false (contains ; and --)
validator.IsValidIdentifier("Users--");                     // false (contains --)
validator.IsValidIdentifier("Users/*comment*/");            // false (contains /* */)
validator.IsValidIdentifier("DROP TABLE Users");            // false (contains DROP)
validator.IsValidIdentifier("DELETE FROM Users");           // false (contains DELETE)
```

### Validation Rules

1. **Regex Pattern**: `^[\w\.\[\]\,\s\*\(\)]+$`
   - Allows: alphanumeric, underscores, dots, brackets, commas, spaces, asterisks, parentheses
   - Blocks: semicolons, quotes, dashes, special characters

2. **Keyword Blocking** (case-insensitive):
   - `DROP` - Prevents DROP TABLE attacks
   - `DELETE` - Prevents DELETE injection through identifiers

3. **Comment Blocking**:
   - `--` - Single-line SQL comments
   - `;--` - Statement termination + comment
   - `/*` and `*/` - Multi-line SQL comments

### Automatic Validation

All `SqlClient` CRUD methods automatically validate identifiers:

```csharp
var db = new SqlClient();

// This throws ArgumentException
db.Select("*", "Users; DROP TABLE Users--", "", new object[] { });
// ArgumentException: Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed.
```

### Custom Validation

You can implement `ISqlValidator` for custom validation rules:

```csharp
using DBTools.Abstractions;

public class CustomValidator : ISqlValidator
{
    public bool IsValidIdentifier(string identifier)
    {
        // Custom validation logic
        return !string.IsNullOrWhiteSpace(identifier)
            && identifier.Length <= 128
            && !identifier.Contains(";")
            && !identifier.Contains("'");
    }
}

// Use with SqlClient
var db = new SqlClient(config, new CustomValidator(), queryBuilder);
```

---

## Configuration Security

### Protecting config.json

The `config.json` file contains database credentials and must be protected:

```json
{
  "Host": "localhost\\SQLEXPRESS",
  "Database": "production_db",
  "Uid": "app_user",
  "Password": "sensitive_password",
  "Port": "1433"
}
```

### Best Practices

1. **Never commit credentials to version control**
   ```gitignore
   # .gitignore
   config.json
   appsettings.json
   ```

2. **Use environment variables in production**
   ```csharp
   using Microsoft.Extensions.Configuration;

   var configuration = new ConfigurationBuilder()
       .AddJsonFile("config.json", optional: true)
       .AddEnvironmentVariables("DBTOOLS_")  // DBTOOLS_Host, DBTOOLS_Password, etc.
       .Build();

   var config = new DbConfiguration(configuration);
   ```

3. **Use minimal privilege database users**
   - Create a dedicated SQL login for your application
   - Grant only SELECT, INSERT, UPDATE, DELETE on required tables
   - Deny DDL operations (CREATE, ALTER, DROP)
   - Deny access to system tables

4. **Encrypt connections**
   ```json
   {
     "Host": "prod-server.database.windows.net",
     "Database": "mydb",
     "Uid": "app_user",
     "Password": "***",
     "Port": "1433"
   }
   ```
   For Azure SQL, connections are encrypted by default. For on-premises SQL Server, configure SSL/TLS.

5. **Rotate credentials regularly**
   - Change database passwords periodically
   - Use different credentials for different environments

---

## Input Validation

### Application-Layer Validation

Always validate user input before passing it to database operations:

```csharp
public bool CreateUser(string username, string email, int age)
{
    // Validate input
    if (string.IsNullOrWhiteSpace(username))
        throw new ArgumentException("Username is required");

    if (username.Length > 50)
        throw new ArgumentException("Username too long");

    if (!email.Contains("@"))
        throw new ArgumentException("Invalid email format");

    if (age < 0 || age > 150)
        throw new ArgumentException("Invalid age");

    // Safe to use with DBTools_SQL
    var userController = new LinqHelper<User>("Users", "Id");
    return userController.Add(new User
    {
        Username = username,
        Email = email,
        Age = age
    });
}
```

### WHERE Clause Requirements

DBTools_SQL enforces WHERE clauses on UPDATE and DELETE operations:

```csharp
var db = new SqlClient();

// This throws ArgumentException - condition is required
db.Update(fields, "Users", values, "");

// This throws ArgumentException - WHERE clause is required
db.Delete("Users", "", new object[] { });
```

**Why?** Without a WHERE clause, UPDATE and DELETE affect ALL rows in the table. This is a safety measure to prevent accidental data loss.

---

## Common Vulnerabilities

### 1. SQL Injection via User Input

**Vulnerability**: Concatenating user input into SQL strings

```csharp
// DANGEROUS: Never do this
string query = "SELECT * FROM Users WHERE name = '" + userName + "'";
db.ExecuteQuery(query);
```

**Prevention**: Always use parameterized methods

```csharp
// SAFE: Use parameterized queries
DataView results = db.Select("*", "Users", "name = @param0", new object[] { userName });
```

### 2. Mass Assignment

**Vulnerability**: Unrestricted model binding allowing users to modify fields they shouldn't

```csharp
// RISK: User could set Role = "admin" if all properties are bound
var user = new User { Username = "hacker", Role = "admin" };
userController.Add(user);
```

**Prevention**: Validate and filter properties before database operations

```csharp
// SAFE: Explicitly set only allowed fields
var user = new User { Username = "newuser", Role = "user" }; // Force default role
userController.Add(user);
```

### 3. Information Disclosure

**Vulnerability**: Exposing database error messages to users

```csharp
// DANGEROUS: Exposing raw errors
catch (Exception ex)
{
    return ex.ToString(); // May contain connection strings, table names, etc.
}
```

**Prevention**: Log errors internally, return generic messages

```csharp
catch (Exception ex)
{
    logger.LogError(ex, "Database operation failed");
    return "An error occurred. Please try again later.";
}
```

### 4. Credential Exposure

**Vulnerability**: Hardcoded credentials in source code

```csharp
// DANGEROUS: Never hardcode credentials
var db = new SqlClient();
db.Host = "prod-server";
db.Password = "P@ssw0rd123"; // Exposed in source code
```

**Prevention**: Use configuration files and environment variables

```csharp
// SAFE: Configuration from environment
var config = new DbConfiguration(configuration); // From environment/IConfiguration
var db = new SqlClient(config, validator, queryBuilder);
```

---

## Abstractions for Testability

DBTools_SQL provides interfaces for all major components, enabling dependency injection and testability:

### Available Interfaces

| Interface | Implementation | Purpose |
|-----------|---------------|---------|
| `ISqlClient` | `SqlClient` | Database operations |
| `IDbConfiguration` | `DbConfiguration` | Configuration access |
| `ISqlQueryBuilder` | `SqlQueryBuilder` | Query generation |
| `ISqlValidator` | `SqlValidator` | Identifier validation |
| `IDBTools` | `DBToolsController` | Basic DB operations |

### Using Interfaces for Security Testing

```csharp
using DBTools.Abstractions;

public class UserRepository
{
    private readonly ISqlClient _db;

    public UserRepository(ISqlClient db)
    {
        _db = db;
    }

    public DataView GetActiveUsers()
    {
        return _db.Select("*", "Users", "status = @param0", new object[] { "active" });
    }
}

// Production: Real SqlClient
var db = new SqlClient();
var repo = new UserRepository(db);

// Testing: Mock ISqlClient to verify parameterized queries are used
var mock = new Mock<ISqlClient>();
mock.Setup(x => x.Select("*", "Users", "status = @param0", It.IsAny<object[]>()))
    .Returns(new DataView());
var testRepo = new UserRepository(mock.Object);
```

---

## Security Checklist

### Development

- [ ] All database queries use parameterized methods
- [ ] No string concatenation for SQL queries
- [ ] Input validation on all user-provided data
- [ ] WHERE clauses on all UPDATE and DELETE operations
- [ ] Error messages don't expose database details
- [ ] Sensitive data is not logged

### Deployment

- [ ] `config.json` is not in version control
- [ ] Database user has minimal required privileges
- [ ] Connection strings use encrypted connections where possible
- [ ] Credentials are rotated regularly
- [ ] Environment-specific configuration (dev/staging/prod)

### Testing

- [ ] SQL injection tests for all user inputs
- [ ] Identifier validation tests for table/column names
- [ ] Authorization tests for data access
- [ ] Error handling tests (no information disclosure)
- [ ] Configuration security tests

### Monitoring

- [ ] Database access logging enabled
- [ ] Failed authentication attempts monitored
- [ ] Unusual query patterns detected
- [ ] Regular security audits performed
- [ ] Dependency vulnerability scanning

---

## Security Architecture

```
User Input
    │
    ▼
Application Validation ────── Input sanitization, type checking
    │
    ▼
SqlValidator ─────────────── Identifier validation (regex + keyword blocking)
    │
    ▼
Parameterized Queries ─────── SQL parameters (@param0, @whereParam0)
    │
    ▼
SqlClient ─────────────────── CRUD with built-in validation
    │
    ▼
SQL Server ────────────────── Parameterized execution
```

**Defense in Depth**: Multiple layers ensure that even if one layer is bypassed, other layers still provide protection.

---

## Reporting Security Issues

If you discover a security vulnerability in DBTools_SQL:

1. **Do not** publicly disclose the vulnerability
2. Report via GitHub Security Advisories
3. Provide detailed description and reproduction steps
4. Allow reasonable time for a fix before public disclosure

---

**Remember**: Security is everyone's responsibility. Always use parameterized queries, validate input, and follow the principle of least privilege.
