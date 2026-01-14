# DBTools_SQL API Reference

Complete API documentation for the DBTools_SQL library.

## Table of Contents

1. [Utils Class](#utils-class)
2. [DBTools Class](#dbtools-class)
3. [GenericObject Class](#genericobject-class)
4. [DataExport Class](#dataexport-class)
5. [Controllers](#controllers)
6. [Interfaces](#interfaces)
7. [Models](#models)

---

## Utils Class

**Namespace**: `DBTools_Utilities`  
**Inherits**: `DbTools.DBTools`

Main utility class providing high-level database operations with built-in security features.

### Constructor

#### `Utils()`

Initializes a new instance of the Utils class and loads database configuration from `config.json`.

**Example:**
```csharp
var utils = new Utils();
```

**Throws:**
- `FileNotFoundException` - When config.json is not found in the application directory
- `InvalidOperationException` - When configuration is invalid or required keys are missing

**Required Configuration Keys:**
- Host
- Database
- Uid
- Password
- Port

---

### Properties

| Property | Type | Description | Access |
|----------|------|-------------|--------|
| `Host` | string | SQL Server hostname or IP address | Get/Set |
| `Database` | string | Database name | Get/Set |
| `Uid` | string | Database username | Get/Set |
| `Password` | string | Database password | Get/Set |
| `Port` | string | SQL Server port number | Get/Set |
| `Error` | string | Last error message | Get/Set |

---

### Methods

#### Query Execution Methods

##### `ExecuteQuery(string query)`

Executes a SQL command that doesn't return results (INSERT, UPDATE, DELETE, etc.).

**Parameters:**
- `query` (string) - SQL query to execute

**Returns:** void

**Example:**
```csharp
utils.ExecuteQuery("CREATE TABLE Users (id INT PRIMARY KEY, name VARCHAR(100))");
```

**Note:** For CRUD operations, use the specific methods (Insert, Update, Delete) which provide parameterized queries.

---

##### `getInBd(string query)`

Executes a query and returns the first column of all rows as a string array.

**Parameters:**
- `query` (string) - SQL SELECT query

**Returns:** `string[]` - Array of values from the first column

**Example:**
```csharp
string[] usernames = utils.getInBd("SELECT username FROM Users");
```

---

##### `getInBdDv(string query)`

Executes a query and returns results as a DataView.

**Parameters:**
- `query` (string) - SQL SELECT query

**Returns:** `DataView` - Query results, or null if an error occurs

**Example:**
```csharp
DataView users = utils.getInBdDv("SELECT * FROM Users");
if (users != null)
{
    foreach (DataRowView row in users)
    {
        Console.WriteLine(row["username"]);
    }
}
```

---

#### SELECT Methods

##### `Select(string _fields, string _table, string whereClause, object[] parameters)`

Retrieves data using parameterized WHERE clause for SQL injection prevention.

**Parameters:**
- `_fields` (string) - Comma-separated field names or "*" for all fields
- `_table` (string) - Table name
- `whereClause` (string) - WHERE condition with parameter placeholders (@param0, @param1, etc.)
- `parameters` (object[]) - Array of parameter values

**Returns:** `DataView` - Query results

**Throws:**
- `ArgumentException` - Invalid identifiers (table/field names)
- `ArgumentNullException` - Null parameters array

**Example:**
```csharp
// Single condition
DataView users = utils.Select(
    "id, username, email",
    "Users",
    "status = @param0",
    new object[] { "active" }
);

// Multiple conditions
DataView filtered = utils.Select(
    "*",
    "Users",
    "age > @param0 AND city = @param1",
    new object[] { 18, "New York" }
);

// No WHERE clause
DataView all = utils.Select("*", "Users", "", new object[] { });
```

**Security Notes:**
- Uses parameterized queries to prevent SQL injection
- Validates table and field names
- Blocks dangerous SQL keywords and patterns

---

##### `Select(string query_without_select, object[] parameters)`

Executes a custom query without the SELECT keyword, allowing for complex queries with joins.

**Parameters:**
- `query_without_select` (string) - Query starting from the field list (without "SELECT")
- `parameters` (object[]) - Array of parameter values

**Returns:** `DataView` - Query results

**Throws:**
- `ArgumentNullException` - Null parameters array

**Example:**
```csharp
string query = @"
    u.username,
    o.order_date,
    o.total
    FROM Users u
    INNER JOIN Orders o ON u.id = o.user_id
    WHERE u.id = @param0
    ORDER BY o.order_date DESC";

DataView results = utils.Select(query, new object[] { userId });
```

---

#### INSERT Methods

##### `Insert(string[] _fields, string[] _table, object[] _values, string primary_key_name = null, bool auto_increment = true)`

Inserts a new record into the database using parameterized queries.

**Parameters:**
- `_fields` (string[]) - Array of field names
- `_table` (string) - Table name
- `_values` (object[]) - Array of values corresponding to fields
- `primary_key_name` (string, optional) - Name of the primary key field
- `auto_increment` (bool, optional) - If true, excludes primary key from insert (default: true)

**Returns:** `bool` - True if successful, false otherwise

**Example:**
```csharp
// Basic insert
string[] fields = { "username", "email", "age" };
object[] values = { "john_doe", "john@example.com", 30 };
bool success = utils.Insert(fields, "Users", values);

// With auto-increment primary key
string[] fields = { "id", "username", "email" };
object[] values = { 1, "john_doe", "john@example.com" };
bool success = utils.Insert(fields, "Users", values, "id", true);
// The id field will be automatically excluded

// Without auto-increment
string[] fields = { "id", "username", "email" };
object[] values = { 1, "john_doe", "john@example.com" };
bool success = utils.Insert(fields, "Users", values, "id", false);
// All fields including id will be inserted
```

**Notes:**
- Automatically handles NULL values with DBNull.Value
- Supports DateTime conversion to SQL format
- Uses parameterized queries for security

---

#### UPDATE Methods

##### `Update(string[] _fields, string _table, string[] _values, string condition = "")`

Updates records using a string-based WHERE condition.

**Parameters:**
- `_fields` (string[]) - Array of field names to update
- `_table` (string) - Table name
- `_values` (string[]) - Array of new values
- `condition` (string) - WHERE clause condition (required for security)

**Returns:** `bool` - True if successful, false otherwise

**Throws:**
- `ArgumentException` - Invalid identifiers, empty arrays, or missing condition
- `ArgumentNullException` - Null arrays

**Example:**
```csharp
string[] fields = { "email", "updated_date" };
string[] values = { "newemail@example.com", "2024-01-15 10:30:00" };
string condition = "id = 5";

bool success = utils.Update(fields, "Users", values, condition);
```

**?? Warning:** This method is less secure than the parameterized version. Use the overload with whereParameters when possible.

---

##### `Update(string[] _fields, string _table, string[] _values, string whereClause, object[] whereParameters)` ? Recommended

Updates records using parameterized WHERE clause for better security.

**Parameters:**
- `_fields` (string[]) - Array of field names to update
- `_table` (string) - Table name
- `_values` (string[]) - Array of new values
- `whereClause` (string) - WHERE clause with parameter placeholders (@whereParam0, @whereParam1, etc.)
- `whereParameters` (object[]) - Array of parameter values for WHERE clause

**Returns:** `bool` - True if successful, false otherwise

**Throws:**
- `ArgumentException` - Invalid inputs
- `ArgumentNullException` - Null arrays

**Example:**
```csharp
string[] fields = { "username", "email", "updated_date" };
string[] values = { "new_username", "new@example.com", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
string whereClause = "id = @whereParam0 AND status = @whereParam1";
object[] whereParams = new object[] { 5, "active" };

bool success = utils.Update(fields, "Users", values, whereClause, whereParams);
```

**Security Benefits:**
- Prevents SQL injection through parameterization
- Validates all identifiers
- Requires explicit WHERE clause

---

#### DELETE Methods

##### `Delete(string _table, string whereClause, object[] parameters)`

Deletes records using parameterized WHERE clause.

**Parameters:**
- `_table` (string) - Table name
- `whereClause` (string) - WHERE clause with parameter placeholders (@param0, @param1, etc.)
- `parameters` (object[]) - Array of parameter values

**Returns:** `bool` - True if successful, false otherwise

**Throws:**
- `ArgumentException` - Invalid table name or empty WHERE clause
- `ArgumentNullException` - Null parameters array

**Example:**
```csharp
// Delete single record
bool success = utils.Delete(
    "Users",
    "id = @param0",
    new object[] { 5 }
);

// Delete multiple records
bool success = utils.Delete(
    "Users",
    "status = @param0 AND last_login < @param1",
    new object[] { "inactive", DateTime.Now.AddMonths(-6) }
);
```

**Security Features:**
- Requires WHERE clause (prevents accidental bulk deletes)
- Uses parameterized queries
- Validates table names

---

#### Helper Methods

##### `QueryBuilder(object obj, string primaryKeyName = "", bool autoIncrement = true)`

Converts any object into a format suitable for database operations.

**Parameters:**
- `obj` (object) - Object to convert (must have public properties)
- `primaryKeyName` (string, optional) - Name of primary key property to exclude
- `autoIncrement` (bool, optional) - If true, excludes primary key (default: true)

**Returns:** `List<GenericObject>` - List containing one GenericObject with columns, values, and types

**Example:**
```csharp
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public DateTime CreatedDate { get; set; }
}

var user = new User
{
    Id = 1,
    Username = "john_doe",
    Email = "john@example.com",
    CreatedDate = DateTime.Now
};

List<GenericObject> queryData = utils.QueryBuilder(user, "Id", true);

// Access the data
string[] columns = queryData[0].columns;     // ["Username", "Email", "CreatedDate"]
object[] values = queryData[0].values;       // ["john_doe", "john@example.com", "2024-01-15 10:30:00"]
string[] types = queryData[0].types;         // ["String", "String", "DateTime"]

// Use with Insert
bool success = utils.Insert(columns, "Users", values);
```

**Features:**
- Automatic DateTime formatting (yyyy-MM-dd HH:mm:ss)
- Handles all primitive types
- Supports auto-increment primary key exclusion
- Preserves type information

**Throws:**
- `Exception` - With detailed property information if conversion fails

---

##### `connectDB()`

Initializes database connection parameters. Called automatically by constructor.

**Returns:** void

**Example:**
```csharp
utils.connectDB();
```

---

### Static Query Builder Methods

These methods generate SQL query strings. **Note:** For better security, use the instance methods with parameterized queries instead.

#### `Select_Query(string _fields, string _table, string _conditions)`

Generates a SELECT query string.

**Parameters:**
- `_fields` (string) - Comma-separated field names
- `_table` (string) - Table name
- `_conditions` (string) - WHERE clause (without "WHERE" keyword)

**Returns:** `string` - SQL SELECT query

**Throws:**
- `ArgumentException` - Invalid identifiers

**Example:**
```csharp
string query = Utils.Select_Query("id, username", "Users", "age > 18");
// Returns: "SELECT id, username FROM Users WHERE age > 18"
```

---

#### `Insert_Query(string[] _fields, string _table, object[] _values, string primary_key_name = "", bool auto_increment = true)`

Generates an INSERT query string with parameter placeholders.

**Parameters:**
- `_fields` (string[]) - Array of field names
- `_table` (string) - Table name
- `_values` (object[]) - Array of values (used for validation)
- `primary_key_name` (string, optional) - Primary key field name
- `auto_increment` (bool, optional) - Exclude primary key if true

**Returns:** `string` - SQL INSERT query with @param placeholders

**Throws:**
- `ArgumentException` - Invalid inputs or mismatched array lengths

**Example:**
```csharp
string[] fields = { "username", "email" };
object[] values = { "john", "john@example.com" };
string query = Utils.Insert_Query(fields, "Users", values);
// Returns: "INSERT INTO Users(username,email) VALUES(@param0,@param1)"
```

---

#### `Update_Query(string[] _fields, string _table, string[] _values, string condition = "")`

Generates an UPDATE query string with escaped values.

**Parameters:**
- `_fields` (string[]) - Array of field names
- `_table` (string) - Table name
- `_values` (string[]) - Array of new values
- `condition` (string) - WHERE clause (required)

**Returns:** `string` - SQL UPDATE query

**Throws:**
- `ArgumentException` - Invalid inputs or missing condition

**Example:**
```csharp
string[] fields = { "username", "email" };
string[] values = { "new_name", "new@email.com" };
string query = Utils.Update_Query(fields, "Users", values, "id = 5");
// Returns: "UPDATE Users SET username='new_name',email='new@email.com' WHERE id = 5"
```

**?? Security Note:** This method uses string escaping. The parameterized instance method is more secure.

---

#### `Delete_Query(string _table, string condition)`

Generates a DELETE query string.

**Parameters:**
- `_table` (string) - Table name
- `condition` (string) - WHERE clause (required)

**Returns:** `string` - SQL DELETE query

**Throws:**
- `ArgumentException` - Invalid table name or missing condition

**Example:**
```csharp
string query = Utils.Delete_Query("Users", "id = 5");
// Returns: "DELETE FROM Users WHERE id = 5"
```

---

#### `GenerateSqlParameters(object[] values)`

Generates a list of SQL parameters from an array of values.

**Parameters:**
- `values` (object[]) - Array of values

**Returns:** `List<SqlParameter>` - List of SqlParameter objects

**Example:**
```csharp
object[] values = { "john", 30, DateTime.Now };
List<SqlParameter> params = Utils.GenerateSqlParameters(values);
// Creates @param0, @param1, @param2 with respective values
```

---

### Validation Methods

#### `IsValidIdentifier(string identifier)` (Private)

Validates identifiers (table names, column names) to prevent SQL injection.

**Validation Rules:**
- ? Alphanumeric characters (a-z, A-Z, 0-9)
- ? Underscores (_)
- ? Dots (.) for schema.table notation
- ? Brackets ([]) for escaped identifiers
- ? Commas and spaces for field lists
- ? Asterisk (*) for SELECT *
- ? SQL keywords: DROP, DELETE
- ? SQL comments: --, ;--, /*, */

**Example Valid Identifiers:**
```
Users
user_name
[User Table]
dbo.Users
id, name, email
*
schema.table.column
```

**Example Invalid Identifiers:**
```
Users; DROP TABLE--
Users--comment
table/*comment*/
DROP_users (contains DROP keyword)
```

---

## DBTools Class

**Namespace**: `DbTools`  
**Implements**: `IDBTools`

Core database connection and operation class.

### Properties

| Property | Type | Description | Access |
|----------|------|-------------|--------|
| `Host` | string | Database server host | Get/Set |
| `Uid` | string | Database username | Get/Set |
| `Password` | string | Database password | Get/Set |
| `Database` | string | Database name | Get/Set |
| `Query` | string | Current SQL query | Get/Set |
| `Error` | string | Last error message | Get/Set |
| `Table` | string | Current table name | Get/Set |
| `Port` | string | Database port (default: 1433) | Get/Set |
| `SqlParameters` | List<SqlParameter> | SQL parameters for queries | Get/Set |
| `count` | int | Record count from last operation | Public field |

### Methods

#### `RetrieveDataSql()`

Executes the current query and returns results as DataView.

**Returns:** `DataView` - Query results

**Example:**
```csharp
var dbTools = new DBTools();
dbTools.Query = "SELECT * FROM Users";
DataView results = dbTools.RetrieveDataSql();
```

---

#### `RetrieveDataSql(string query)`

Executes the specified query and returns results as DataView.

**Parameters:**
- `query` (string) - SQL query to execute

**Returns:** `DataView` - Query results

**Example:**
```csharp
DataView results = dbTools.RetrieveDataSql("SELECT * FROM Users WHERE age > 18");
```

---

#### `RetrieveObjectSql()`

Executes the current query and returns results as List of GenericObject.

**Returns:** `List<GenericObject>` - Query results

**Example:**
```csharp
dbTools.Query = "SELECT id, username, email FROM Users";
List<GenericObject> results = dbTools.RetrieveObjectSql();

foreach (var obj in results)
{
    Console.WriteLine($"Columns: {string.Join(", ", obj.columns)}");
}
```

---

#### `SqlExecuteQuery()`

Executes a non-query SQL command (INSERT, UPDATE, DELETE, etc.).

**Returns:** void

**Example:**
```csharp
dbTools.Query = "UPDATE Users SET status = 'active' WHERE id = 5";
dbTools.SqlExecuteQuery();
```

---

#### Setter Methods

```csharp
void setHost(string host)
void setUid(string uid)
void setPassword(string password)
void setDataBase(string database)
void setQuery(string query)
void setTable(string table)
void setPort(string port)
```

**Example:**
```csharp
dbTools.setHost("localhost");
dbTools.setDataBase("MyDatabase");
dbTools.setUid("sa");
dbTools.setPassword("password");
dbTools.setPort("1433");
```

---

## GenericObject Class

**Namespace**: `DBTools.Model`

Container for database operation data with built-in CRUD methods.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `columns` | string[] | Column names |
| `values` | object[] | Column values |
| `valuesString` | string[] | String representation of values |
| `types` | string[] | Data types of columns |
| `table` | string | Table name for operations |

### Constructors

#### `GenericObject()`

Creates a new instance without database operations capability.

#### `GenericObject(Utils dbTools)`

Creates a new instance with database operations capability.

**Parameters:**
- `dbTools` (Utils) - Utils instance for database operations

### Methods

#### `Insert()`

Inserts the object into the database.

**Returns:** `bool` - True if successful, false otherwise

**Example:**
```csharp
var utils = new Utils();
var obj = new GenericObject(utils)
{
    columns = new[] { "username", "email" },
    valuesString = new[] { "john", "john@example.com" },
    table = "Users"
};

bool success = obj.Insert();
```

---

#### `Update(string conditions)`

Updates the object in the database based on conditions.

**Parameters:**
- `conditions` (string) - WHERE clause conditions

**Returns:** `bool` - True if successful, false otherwise

**Example:**
```csharp
obj.valuesString = new[] { "john_updated", "john_new@example.com" };
bool success = obj.Update("id = 5");
```

---

## GenericObject_Simple Class

**Namespace**: `DbTools.Model`

Simplified version of GenericObject for single-value operations.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `column` | string | Column name |
| `value` | string | Column value |
| `type` | string | Data type |

---

## DataExport Class

**Namespace**: `DBTools_Utilities`

Handles data export operations.

### Methods

#### `ToCsv(List<GenericObject> genericObject, char separator, bool showColums = true, bool showTypes = true)`

Exports data to CSV format.

**Parameters:**
- `genericObject` (List<GenericObject>) - Data to export
- `separator` (char) - CSV separator character (, ; | etc.)
- `showColums` (bool, optional) - Include column headers (default: true)
- `showTypes` (bool, optional) - Include type information row (default: true)

**Returns:** `string` - CSV formatted data

**Example:**
```csharp
var export = new DataExport();
var queryData = utils.QueryBuilder(myObject);

string csv = export.ToCsv(queryData, ',', true, true);
File.WriteAllText("output.csv", csv);
```

**Output Format:**
```csv
String,Int32,DateTime
username,age,created_date
john_doe,30,2024-01-15 10:30:00
jane_smith,25,2024-01-16 14:20:00
```

**Features:**
- Handles DBNull values gracefully
- Removes line breaks from data
- Converts backslashes to forward slashes
- Configurable column headers and type information

---

## Controllers

### DBToolsController

**Namespace**: `DbTools.Controller`  
**Implements**: `IDBTools`

Controller wrapper for DBTools operations.

#### Constructor

```csharp
public DBToolsController(DBTools dbTools)
```

#### Methods

```csharp
DataView RetrieveDataSQL()
List<GenericObject> RetrieveObjectSQL()
void SqlExecuteQuery(string query = "")
```

---

### UtilsController

**Namespace**: `DBTools_Utilities.Controller`

Controller wrapper for Utils operations.

---

### DataExportController

**Namespace**: `DBTools_Utilities.Controller`

Controller wrapper for DataExport operations.

---

## Interfaces

### IDBTools

**Namespace**: `DbTools.Interfaces`

Interface defining core database operations.

#### Methods

```csharp
List<GenericObject> RetrieveObjectSQL();
DataView RetrieveDataSQL();
void SqlExecuteQuery(string query = "");
```

---

## Common Patterns and Best Practices

### Pattern 1: Safe Parameter Usage

```csharp
// ? CORRECT - Parameterized query
DataView users = utils.Select(
    "*",
    "Users",
    "username = @param0 AND status = @param1",
    new object[] { userInput, "active" }
);

// ? WRONG - String concatenation (SQL injection risk)
// Don't do this:
// string query = "SELECT * FROM Users WHERE username = '" + userInput + "'";
```

### Pattern 2: Error Handling

```csharp
try
{
    bool success = utils.Insert(fields, "Users", values);
    
    if (success)
    {
        Console.WriteLine("Operation successful");
    }
    else
    {
        Console.WriteLine($"Operation failed: {utils.Error}");
        // Log error for debugging
        Logger.Error(utils.Error);
    }
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid input: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
    // Log for debugging
}
```

### Pattern 3: Using QueryBuilder with Models

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// Create and insert
var product = new Product { Name = "Laptop", Price = 999.99m };
var queryData = utils.QueryBuilder(product, "Id", true);
utils.Insert(queryData[0].columns, "Products", queryData[0].values);
```

### Pattern 4: Transaction-Like Operations

```csharp
bool allSuccess = true;

try
{
    // Insert order
    bool orderSuccess = utils.Insert(orderFields, "Orders", orderValues);
    
    if (!orderSuccess)
    {
        throw new Exception("Order insert failed");
    }
    
    // Insert order items
    foreach (var item in items)
    {
        var itemData = utils.QueryBuilder(item);
        bool itemSuccess = utils.Insert(itemData[0].columns, "OrderItems", itemData[0].values);
        
        if (!itemSuccess)
        {
            // In a real scenario, implement rollback logic
            throw new Exception("Order item insert failed");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Transaction failed: {ex.Message}");
    // Implement rollback logic
    allSuccess = false;
}
```

---

## Version History

- **Version 1.0** - Initial release with core CRUD operations
- **Current** - Added parameterized queries, enhanced security, comprehensive validation

---

## See Also

- [README.md](../README.md) - General documentation and quick start
- [Security Guide](SECURITY.md) - Security best practices
- [Examples](EXAMPLES.md) - Comprehensive code examples

---

**Last Updated**: 2024
