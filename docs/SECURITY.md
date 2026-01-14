# DBTools_SQL Security Guide

Comprehensive security documentation for DBTools_SQL library.

## Table of Contents

1. [Security Overview](#security-overview)
2. [SQL Injection Prevention](#sql-injection-prevention)
3. [Configuration Security](#configuration-security)
4. [Input Validation](#input-validation)
5. [Best Practices](#best-practices)
6. [Common Vulnerabilities](#common-vulnerabilities)
7. [Security Checklist](#security-checklist)

---

## Security Overview

DBTools_SQL implements multiple layers of security to protect your application from common database attacks:

### Defense Layers

1. **Parameterized Queries** - Primary defense against SQL injection
2. **Identifier Validation** - Prevents malicious table/column names
3. **Keyword Blocking** - Blocks dangerous SQL keywords
4. **Mandatory Conditions** - Requires WHERE clauses for UPDATE/DELETE
5. **Input Sanitization** - Escapes special characters when needed

---

## SQL Injection Prevention

### What is SQL Injection?

SQL injection is a code injection technique that exploits security vulnerabilities in an application's database layer by inserting malicious SQL statements.

**Example of Vulnerable Code:**
```csharp
// ? VULNERABLE - Never do this!
string username = GetUserInput();
string query = "SELECT * FROM Users WHERE username = '" + username + "'";
// If username = "admin' OR '1'='1", this becomes:
// SELECT * FROM Users WHERE username = 'admin' OR '1'='1'
// This returns all users!
```

### How DBTools_SQL Protects You

#### 1. Parameterized Queries

All CRUD operations use parameterized queries that separate SQL code from data.

**? SAFE - DBTools_SQL Approach:**
```csharp
string username = GetUserInput(); // Could be anything, even malicious input
DataView users = utils.Select(
    "*",
    "Users",
    "username = @param0",
    new object[] { username }
);
// The parameter is treated as DATA, not CODE
// Even if username = "admin' OR '1'='1", it will look for that exact username
```

**How it Works:**
```
Query sent to SQL Server: SELECT * FROM Users WHERE username = @param0
Parameter @param0 value: "admin' OR '1'='1"

SQL Server treats @param0 as a literal string value, not SQL code.
Result: Looks for username exactly matching "admin' OR '1'='1" (probably no results)
```

#### 2. Identifier Validation

Table and column names cannot be parameterized, so DBTools_SQL validates them:

```csharp
// ? VALID identifiers
"Users"                    // Simple table name
"user_name"               // With underscore
"[User Table]"            // With brackets
"dbo.Users"               // Schema.table
"id, name, email"         // Field list
"*"                       // All columns

// ? INVALID identifiers - Will throw ArgumentException
"Users; DROP TABLE--"     // SQL injection attempt
"Users--"                 // SQL comment
"table/*comment*/"        // SQL comment
"Users' OR '1'='1"       // SQL injection attempt
```

**Implementation:**
```csharp
private static bool IsValidIdentifier(string identifier)
{
    // Check for null/empty
    if (string.IsNullOrWhiteSpace(identifier))
        return false;

    // Block dangerous keywords
    string upperIdentifier = identifier.ToUpper();
    if (upperIdentifier.Contains("DROP") || upperIdentifier.Contains("DELETE"))
        return false;

    // Block SQL comment patterns
    if (identifier.Contains("--") || identifier.Contains(";--") ||
        identifier.Contains("/*") || identifier.Contains("*/"))
        return false;

    // Only allow safe characters
    return Regex.IsMatch(identifier, @"^[\w\.\[\]\,\s\*]+$");
}
```

---

### Attack Examples and Prevention

#### Attack 1: Basic SQL Injection

**Attack:**
```csharp
string maliciousInput = "admin' OR '1'='1' --";
// Attacker tries to bypass authentication
```

**Without Protection:**
```sql
SELECT * FROM Users WHERE username = 'admin' OR '1'='1' --' AND password = 'xxx'
-- Everything after -- is commented out
-- '1'='1' is always true
-- Returns all users!
```

**With DBTools_SQL:**
```csharp
DataView users = utils.Select(
    "*",
    "Users",
    "username = @param0 AND password = @param1",
    new object[] { maliciousInput, password }
);
```

**Result:**
```sql
SELECT * FROM Users WHERE username = @param0 AND password = @param1
-- @param0 = "admin' OR '1'='1' --" (treated as literal string)
-- @param1 = password hash
-- No users found with that exact username
```

---

#### Attack 2: Union-Based Injection

**Attack:**
```csharp
string maliciousInput = "1 UNION SELECT username, password FROM Admin --";
```

**Without Protection:**
```sql
SELECT * FROM Users WHERE id = 1 UNION SELECT username, password FROM Admin --
-- Returns both user data and admin credentials!
```

**With DBTools_SQL:**
```csharp
DataView users = utils.Select(
    "*",
    "Users",
    "id = @param0",
    new object[] { maliciousInput }
);
```

**Result:**
```sql
SELECT * FROM Users WHERE id = @param0
-- @param0 = "1 UNION SELECT username, password FROM Admin --" (literal string)
-- Tries to match id with that entire string (type mismatch error or no results)
```

---

#### Attack 3: Drop Table Attack

**Attack:**
```csharp
string maliciousTable = "Users; DROP TABLE Users; --";
```

**Without Protection:**
```sql
SELECT * FROM Users; DROP TABLE Users; --
-- Executes multiple statements, drops table!
```

**With DBTools_SQL:**
```csharp
try
{
    DataView users = utils.Select("*", maliciousTable, "", new object[] { });
}
catch (ArgumentException ex)
{
    // Throws: Invalid table name. Only alphanumeric characters...
    Console.WriteLine("Attack prevented: " + ex.Message);
}
```

**Result:** Request rejected before reaching database.

---

#### Attack 4: Second-Order Injection

**Scenario:** Attacker stores malicious data in database, which is later used in unsafe query.

**Attack:**
```csharp
// Step 1: Store malicious username
string maliciousUsername = "admin' OR '1'='1' --";
utils.Insert(
    new[] { "username", "email" },
    "Users",
    new object[] { maliciousUsername, "evil@example.com" }
);
// Safely stored as: username = "admin' OR '1'='1' --"

// Step 2: Later retrieve and use it
DataView user = utils.Select("*", "Users", "email = @param0", new object[] { "evil@example.com" });
string retrievedUsername = user[0]["username"].ToString();

// Step 3: Use in another query (still safe!)
DataView profile = utils.Select("*", "Profiles", "username = @param0", new object[] { retrievedUsername });
// Still parameterized, still safe!
```

**Why it's Safe:** Even if malicious data is in the database, DBTools_SQL's parameterized queries prevent it from being executed as SQL code.

---

## Configuration Security

### Protecting config.json

The `config.json` file contains sensitive database credentials:

```json
{
  "Host": "localhost",
  "Database": "MyDatabase",
  "Uid": "db_user",
  "Password": "SuperSecret123!",
  "Port": "1433"
}
```

### Security Measures

#### 1. File System Permissions

**Windows:**
```powershell
# Grant read access only to application user
icacls config.json /grant:r "IIS_IUSRS:(R)"
icacls config.json /inheritance:r
```

**Linux:**
```bash
# Set restrictive permissions
chmod 400 config.json
chown appuser:appuser config.json
```

#### 2. Never Commit to Version Control

**Add to .gitignore:**
```gitignore
# Database configuration
config.json
appsettings.json
*.config

# Keep template
!config.template.json
```

**Create config.template.json:**
```json
{
  "Host": "your-server-host",
  "Database": "your-database-name",
  "Uid": "your-username",
  "Password": "your-password",
  "Port": "1433"
}
```

#### 3. Use Environment Variables (Recommended for Production)

**Enhanced Utils constructor:**
```csharp
public Utils()
{
    // Try environment variables first
    this.Host = Environment.GetEnvironmentVariable("DB_HOST") 
                ?? ReadFromConfig("Host");
    this.Database = Environment.GetEnvironmentVariable("DB_NAME") 
                    ?? ReadFromConfig("Database");
    this.Uid = Environment.GetEnvironmentVariable("DB_USER") 
               ?? ReadFromConfig("Uid");
    this.Password = Environment.GetEnvironmentVariable("DB_PASSWORD") 
                    ?? ReadFromConfig("Password");
    this.Port = Environment.GetEnvironmentVariable("DB_PORT") 
                ?? ReadFromConfig("Port") 
                ?? "1433";
}
```

**Set environment variables:**
```powershell
# Windows
[Environment]::SetEnvironmentVariable("DB_HOST", "localhost", "User")
[Environment]::SetEnvironmentVariable("DB_PASSWORD", "SecurePass123", "User")

# Linux
export DB_HOST=localhost
export DB_PASSWORD=SecurePass123
```

#### 4. Use Azure Key Vault (Enterprise)

```csharp
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;

public Utils()
{
    var client = new SecretClient(
        new Uri("https://your-vault.vault.azure.net/"),
        new DefaultAzureCredential()
    );

    this.Host = client.GetSecret("DBHost").Value.Value;
    this.Password = client.GetSecret("DBPassword").Value.Value;
    // ... other settings
}
```

#### 5. Encrypt config.json

```csharp
using System.Security.Cryptography;
using System.Text;

public class SecureConfig
{
    public static string DecryptConfig(string encryptedConfig, string key)
    {
        // Implementation of AES decryption
        // Store only encrypted config.json
    }
}
```

---

## Input Validation

### Validation Layers

#### Layer 1: Application Layer (Your Code)

Always validate input before passing to DBTools_SQL:

```csharp
public bool CreateUser(string username, string email, int age)
{
    // ? Validate at application layer
    if (string.IsNullOrWhiteSpace(username))
        throw new ArgumentException("Username cannot be empty");
    
    if (username.Length > 50)
        throw new ArgumentException("Username too long");
    
    if (!IsValidEmail(email))
        throw new ArgumentException("Invalid email format");
    
    if (age < 0 || age > 150)
        throw new ArgumentException("Invalid age");
    
    // Now safe to use with DBTools_SQL
    return utils.Insert(
        new[] { "username", "email", "age" },
        "Users",
        new object[] { username, email, age }
    );
}

private bool IsValidEmail(string email)
{
    return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
}
```

#### Layer 2: DBTools_SQL Layer

DBTools_SQL validates identifiers and uses parameterized queries:

```csharp
// Automatic validation
public DataView Select(string _fields, string _table, string whereClause, object[] parameters)
{
    // Validates _fields and _table
    if (!IsValidIdentifier(_fields))
        throw new ArgumentException("Invalid field names");
    
    if (!IsValidIdentifier(_table))
        throw new ArgumentException("Invalid table name");
    
    // Parameters are automatically parameterized (safe)
    // ...
}
```

#### Layer 3: Database Layer

Configure database with least privilege:

```sql
-- Create dedicated application user
CREATE LOGIN app_user WITH PASSWORD = 'SecurePassword123!';
CREATE USER app_user FOR LOGIN app_user;

-- Grant only necessary permissions
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.Users TO app_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.Orders TO app_user;

-- Don't grant:
-- DROP, CREATE, ALTER, EXECUTE (unless absolutely necessary)
```

---

## Best Practices

### 1. Always Use Parameterized Methods

```csharp
// ? CORRECT
DataView users = utils.Select(
    "*",
    "Users",
    "status = @param0",
    new object[] { userInput }
);

// ? WRONG - Never construct queries manually
string query = $"SELECT * FROM Users WHERE status = '{userInput}'";
utils.ExecuteQuery(query);
```

### 2. Validate All User Input

```csharp
public class UserValidator
{
    public static void ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username required");
        
        if (username.Length < 3 || username.Length > 50)
            throw new ArgumentException("Username must be 3-50 characters");
        
        if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
            throw new ArgumentException("Username can only contain letters, numbers, and underscores");
    }
}
```

### 3. Use Least Privilege Principle

```sql
-- ? Good: Specific permissions
GRANT SELECT ON dbo.Users TO app_user;
GRANT INSERT ON dbo.Orders TO app_user;

-- ? Bad: Too broad
GRANT db_owner TO app_user;
GRANT sysadmin TO app_user;
```

### 4. Always Require WHERE Clauses for UPDATE/DELETE

DBTools_SQL enforces this by design:

```csharp
// ? This works
utils.Update(fields, "Users", values, "id = @whereParam0", new object[] { userId });

// ? This throws ArgumentException
utils.Update(fields, "Users", values, "", new object[] { });
// Throws: "Condition is required for UPDATE operations for security reasons."
```

### 5. Log and Monitor Database Operations

```csharp
public class SecureUtils : Utils
{
    private ILogger logger;
    
    public new DataView Select(string fields, string table, string where, object[] params)
    {
        logger.LogInformation($"SELECT on {table} by {GetCurrentUser()}");
        
        try
        {
            return base.Select(fields, table, where, params);
        }
        catch (Exception ex)
        {
            logger.LogError($"SELECT failed: {ex.Message}");
            throw;
        }
    }
}
```

### 6. Handle Sensitive Data

```csharp
public void CreateUser(string username, string password)
{
    // ? Hash passwords before storing
    string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
    
    utils.Insert(
        new[] { "username", "password_hash" },
        "Users",
        new object[] { username, passwordHash }
    );
    
    // Clear sensitive data from memory
    password = null;
}
```

### 7. Use Transactions for Critical Operations

```csharp
using (var transaction = new TransactionScope())
{
    try
    {
        utils.Insert(orderFields, "Orders", orderValues);
        utils.Insert(paymentFields, "Payments", paymentValues);
        
        transaction.Complete();
    }
    catch
    {
        // Transaction automatically rolls back
        throw;
    }
}
```

### 8. Implement Rate Limiting

```csharp
public class RateLimitedUtils
{
    private Dictionary<string, DateTime> lastRequest = new();
    private TimeSpan minInterval = TimeSpan.FromMilliseconds(100);
    
    public DataView Select(string fields, string table, string where, object[] params)
    {
        string key = $"{table}:{GetCurrentUser()}";
        
        if (lastRequest.ContainsKey(key))
        {
            var elapsed = DateTime.Now - lastRequest[key];
            if (elapsed < minInterval)
            {
                throw new InvalidOperationException("Rate limit exceeded");
            }
        }
        
        lastRequest[key] = DateTime.Now;
        return utils.Select(fields, table, where, params);
    }
}
```

---

## Common Vulnerabilities

### Vulnerability 1: Trusting Client-Side Validation

**? WRONG:**
```csharp
// Only validating in JavaScript on web page
// Server-side code:
string username = Request.Form["username"];
utils.Insert(fields, "Users", new object[] { username }); // Dangerous!
```

**? CORRECT:**
```csharp
// Always validate server-side
string username = Request.Form["username"];
if (string.IsNullOrWhiteSpace(username) || username.Length > 50)
    throw new ArgumentException("Invalid username");

utils.Insert(fields, "Users", new object[] { username }); // Safe
```

### Vulnerability 2: Exposing Error Details

**? WRONG:**
```csharp
try
{
    utils.Insert(fields, "Users", values);
}
catch (Exception ex)
{
    // Sending detailed error to client
    Response.Write($"Error: {ex.Message}"); // Could expose database structure!
}
```

**? CORRECT:**
```csharp
try
{
    utils.Insert(fields, "Users", values);
}
catch (Exception ex)
{
    // Log detailed error server-side
    logger.LogError(ex.ToString());
    
    // Send generic error to client
    Response.Write("An error occurred. Please try again.");
}
```

### Vulnerability 3: Using Dynamic Table Names Without Validation

**? WRONG:**
```csharp
string tableName = Request.QueryString["table"];
// If table = "Users; DROP TABLE Users; --"
DataView data = utils.Select("*", tableName, "", new object[] { }); // Blocked by validation!
```

**? CORRECT:**
```csharp
string tableName = Request.QueryString["table"];

// Whitelist approach
List<string> allowedTables = new List<string> { "Users", "Orders", "Products" };
if (!allowedTables.Contains(tableName))
    throw new ArgumentException("Invalid table");

DataView data = utils.Select("*", tableName, "", new object[] { }); // Safe
```

### Vulnerability 4: Second-Order SQL Injection

**Protected by DBTools_SQL:**
```csharp
// Even if malicious data is in database, parameterized queries prevent execution
string storedValue = GetFromDatabase(); // Could be "'; DROP TABLE Users; --"
DataView results = utils.Select("*", "Logs", "message = @param0", new object[] { storedValue });
// Safe! storedValue is treated as data, not code
```

---

## Security Checklist

### Development Phase

- [ ] Use parameterized queries for all database operations
- [ ] Validate all user input at application layer
- [ ] Never construct SQL queries through string concatenation
- [ ] Use whitelist validation for table/column names when dynamic
- [ ] Hash passwords before storing
- [ ] Implement proper error handling without exposing details
- [ ] Use HTTPS for all network communications
- [ ] Implement authentication and authorization

### Deployment Phase

- [ ] Use environment variables or secure vaults for credentials
- [ ] Never commit config.json with real credentials
- [ ] Set restrictive file permissions on config.json
- [ ] Use least privilege database user
- [ ] Enable database audit logging
- [ ] Implement connection encryption (SSL/TLS)
- [ ] Regular security updates for all dependencies
- [ ] Configure firewall rules for database access

### Testing Phase

- [ ] Test with SQL injection payloads
- [ ] Perform penetration testing
- [ ] Code review by security team
- [ ] Static analysis security testing (SAST)
- [ ] Dynamic analysis security testing (DAST)
- [ ] Validate all error messages don't expose sensitive info
- [ ] Test rate limiting and DoS protection
- [ ] Verify logging captures security events

### Monitoring Phase

- [ ] Monitor failed login attempts
- [ ] Alert on suspicious SQL patterns
- [ ] Log all database operations
- [ ] Monitor for data exfiltration
- [ ] Regular security audits
- [ ] Keep security patches up to date
- [ ] Review access logs regularly
- [ ] Implement intrusion detection

---

## SQL Injection Testing

### Test Payloads

Test your application with these payloads to verify protection:

```csharp
string[] testPayloads = new[]
{
    "admin' OR '1'='1",
    "admin' OR '1'='1' --",
    "admin' OR '1'='1' /*",
    "' OR 1=1 --",
    "' UNION SELECT null, null, null --",
    "'; DROP TABLE Users; --",
    "1; DROP TABLE Users; --",
    "admin'/**/OR/**/1=1--",
    "admin' OR SLEEP(5) --",
    "admin'; EXEC xp_cmdshell('dir'); --"
};

foreach (var payload in testPayloads)
{
    try
    {
        var result = utils.Select("*", "Users", "username = @param0", new object[] { payload });
        Console.WriteLine($"Payload blocked or handled safely: {payload}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception (expected for invalid input): {ex.Message}");
    }
}
```

**Expected Results:**
- All payloads should be treated as literal strings
- No SQL errors should occur
- No tables should be dropped
- No unauthorized data should be returned

---

## Additional Resources

- [OWASP SQL Injection Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/SQL_Injection_Prevention_Cheat_Sheet.html)
- [CWE-89: SQL Injection](https://cwe.mitre.org/data/definitions/89.html)
- [Microsoft SQL Server Security Best Practices](https://docs.microsoft.com/en-us/sql/relational-databases/security/security-best-practices)

---

## Reporting Security Issues

If you discover a security vulnerability in DBTools_SQL, please report it privately:

1. **Do not** create a public GitHub issue
2. Email security concerns to: [security@example.com]
3. Include detailed steps to reproduce
4. Allow time for patch before public disclosure

---

**Last Updated**: 2024  
**Version**: 1.0
