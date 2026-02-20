# DBTools_SQL API Reference

Complete API documentation for the DBTools_SQL library.

## Table of Contents

1. [Utils Class](#utils-class)
2. [UtilsController Class](#utilscontroller-class)
3. [PropertyBasedUtilsController Class](#propertybasedutilscontroller-class)
4. [DBTools Class](#dbtools-class)
5. [GenericObject Class](#genericobject-class)
6. [DataExport Class](#dataexport-class)
7. [Controllers](#controllers)
8. [Interfaces](#interfaces)

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

## PropertyBasedUtilsController Class

**Namespace**: `DBTools_Utilities.Controller`  
**Generic Type**: `PropertyBasedUtilsController<TModel>` where TModel : class, new()  
**Inherits**: `UtilsController<TModel>`

Modern, type-safe database operations using LINQ-style lambda expressions. Similar to Entity Framework's LINQ queries but for raw SQL databases.

### Constructor

#### `PropertyBasedUtilsController(string tableName, string primaryKeyName = "", bool autoIncrement = true)`

Creates a new instance with automatic configuration loading from config.json.

**Parameters:**
- `tableName` (string) - Database table name
- `primaryKeyName` (string, optional) - Primary key column name
- `autoIncrement` (bool, optional) - Whether primary key auto-increments (default: true)

**Example:**
```csharp
var userController = new PropertyBasedUtilsController<User>("Users", "Id", true);
```

---

#### `PropertyBasedUtilsController(Utils utils, string tableName, string primaryKeyName = "", bool autoIncrement = true)`

Creates a new instance with an existing Utils instance.

**Parameters:**
- `utils` (Utils) - Existing Utils instance with configured connection
- `tableName` (string) - Database table name
- `primaryKeyName` (string, optional) - Primary key column name
- `autoIncrement` (bool, optional) - Whether primary key auto-increments (default: true)

**Example:**
```csharp
var utils = new Utils();
var userController = new PropertyBasedUtilsController<User>(utils, "Users", "Id");
```

---

### Query Methods

#### Comparison Queries

##### `WhereEquals<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)`

Filters records where the specified property equals the given value.

**Parameters:**
- `propertySelector` (Expression) - Lambda expression selecting the property (e.g., `u => u.Name`)
- `value` (TProperty) - Value to match

**Returns:** `IEnumerable<TModel>` - Matching records

**Example:**
```csharp
var activeUsers = userController.WhereEquals(u => u.Status, "active");
var user30 = userController.WhereEquals(u => u.Age, 30);
```

---

##### `WhereNotEquals<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)`

Filters records where the specified property does not equal the given value.

**Example:**
```csharp
var notDeleted = userController.WhereNotEquals(u => u.Status, "deleted");
```

---

##### `WhereGreaterThan<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)`

Filters records where the property is greater than the value.

**Type Constraint:** TProperty must implement IComparable

**Example:**
```csharp
var adults = userController.WhereGreaterThan(u => u.Age, 18);
var recentOrders = orderController.WhereGreaterThan(o => o.OrderDate, DateTime.Now.AddDays(-7));
```

---

##### `WhereGreaterThanOrEquals<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)`

Filters records where the property is greater than or equal to the value.

**Example:**
```csharp
var seniors = userController.WhereGreaterThanOrEquals(u => u.Age, 65);
```

---

##### `WhereLessThan<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)`

Filters records where the property is less than the value.

**Example:**
```csharp
var young = userController.WhereLessThan(u => u.Age, 25);
```

---

##### `WhereLessThanOrEquals<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)`

Filters records where the property is less than or equal to the value.

**Example:**
```csharp
var eligible = userController.WhereLessThanOrEquals(u => u.Age, 35);
```

---

##### `WhereBetween<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty minValue, TProperty maxValue)`

Filters records where the property is between two values (inclusive).

**Parameters:**
- `propertySelector` (Expression) - Property selector
- `minValue` (TProperty) - Minimum value (inclusive)
- `maxValue` (TProperty) - Maximum value (inclusive)

**Example:**
```csharp
var middleAged = userController.WhereBetween(u => u.Age, 30, 50);
var thisWeek = orderController.WhereBetween(
    o => o.OrderDate, 
    DateTime.Now.AddDays(-7), 
    DateTime.Now
);
```

---

#### String Queries

##### `WhereContains(Expression<Func<TModel, string>> propertySelector, string substring)`

Filters records where the string property contains the substring.

**Uses:** SQL LIKE with wildcards (%substring%)

**Example:**
```csharp
var gmailUsers = userController.WhereContains(u => u.Email, "@gmail.com");
var johnUsers = userController.WhereContains(u => u.Username, "john");
```

---

##### `WhereStartsWith(Expression<Func<TModel, string>> propertySelector, string prefix)`

Filters records where the string property starts with the prefix.

**Uses:** SQL LIKE with wildcard (prefix%)

**Example:**
```csharp
var adminUsers = userController.WhereStartsWith(u => u.Username, "admin_");
var usOrders = orderController.WhereStartsWith(o => o.Country, "US");
```

---

##### `WhereEndsWith(Expression<Func<TModel, string>> propertySelector, string suffix)`

Filters records where the string property ends with the suffix.

**Uses:** SQL LIKE with wildcard (%suffix)

**Example:**
```csharp
var orgEmails = userController.WhereEndsWith(u => u.Email, ".org");
```

---

#### Collection Queries

##### `WhereIn<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, IEnumerable<TProperty> values)`

Filters records where the property value is in the given list.

**Uses:** SQL IN clause with parameterized values

**Parameters:**
- `propertySelector` (Expression) - Property selector
- `values` (IEnumerable<TProperty>) - List of values to match

**Returns:** `IEnumerable<TModel>` - Empty if values list is empty

**Example:**
```csharp
var specificUsers = userController.WhereIn(u => u.Id, new[] { 1, 2, 3, 5, 8 });
var activeOrPending = userController.WhereIn(u => u.Status, new[] { "active", "pending", "verified" });
```

---

##### `WhereNotIn<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, IEnumerable<TProperty> values)`

Filters records where the property value is not in the given list.

**Uses:** SQL NOT IN clause

**Example:**
```csharp
var excludedUsers = userController.WhereNotIn(u => u.Id, new[] { 99, 100, 101 });
```

---

#### Null Checks

##### `WhereIsNull<TProperty>(Expression<Func<TModel, TProperty>> propertySelector)`

Filters records where the property is null.

**Example:**
```csharp
var usersWithoutEmail = userController.WhereIsNull(u => u.Email);
var neverLoggedIn = userController.WhereIsNull(u => u.LastLogin);
```

---

##### `WhereIsNotNull<TProperty>(Expression<Func<TModel, TProperty>> propertySelector)`

Filters records where the property is not null.

**Example:**
```csharp
var usersWithEmail = userController.WhereIsNotNull(u => u.Email);
var loggedInUsers = userController.WhereIsNotNull(u => u.LastLogin);
```

---

#### Example-Based Filtering

##### `WhereByExample(TModel filterModel)`

Filters records using a model instance as a filter template. Only non-null and non-default properties are included in the WHERE clause.

**Parameters:**
- `filterModel` (TModel) - Model with properties set to filter values

**Returns:** `IEnumerable<TModel>` - All records if no properties are set

**Example:**
```csharp
var filter = new User
{
    Status = "active",
    Age = 30,
    // Email is null, so it won't be included in the filter
};

var matchingUsers = userController.WhereByExample(filter);
// Equivalent to: WHERE Status = 'active' AND Age = 30
```

---

### CRUD Operations

#### Retrieve Methods

##### `FirstOrDefaultByProperty<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)`

Gets the first record matching the property value, or null if not found.

**Returns:** `TModel` - First matching record or null

**Example:**
```csharp
var user = userController.FirstOrDefaultByProperty(u => u.Username, "john_doe");
if (user != null)
{
    Console.WriteLine($"Found user: {user.Email}");
}
```

---

##### `SingleByProperty<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)`

Gets the single record matching the property value. Throws exception if zero or more than one record found.

**Returns:** `TModel` - Single matching record

**Throws:**
- `InvalidOperationException` - If zero or more than one record matches

**Example:**
```csharp
try
{
    var user = userController.SingleByProperty(u => u.Email, "unique@example.com");
    Console.WriteLine($"User: {user.Username}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine("Expected exactly one user with that email");
}
```

---

##### `SingleOrDefaultByProperty<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)`

Gets the single record matching the property value, or null if not found. Throws if more than one record found.

**Returns:** `TModel` - Single matching record or null

**Throws:**
- `InvalidOperationException` - If more than one record matches

**Example:**
```csharp
var user = userController.SingleOrDefaultByProperty(u => u.Email, "might-exist@example.com");
```

---

##### `CountByProperty<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)`

Counts records matching the property value.

**Returns:** `int` - Number of matching records

**Example:**
```csharp
int activeCount = userController.CountByProperty(u => u.Status, "active");
Console.WriteLine($"Active users: {activeCount}");
```

---

##### `AnyByProperty<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)`

Checks if any records exist matching the property value.

**Returns:** `bool` - True if any matching records exist

**Example:**
```csharp
if (userController.AnyByProperty(u => u.Username, "admin"))
{
    Console.WriteLine("Admin user exists");
}
```

---

##### `Exists<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)`

Alias for `AnyByProperty`. Checks if a record exists with the specified property value.

**Returns:** `bool` - True if record exists

**Example:**
```csharp
bool usernameExists = userController.Exists(u => u.Username, "john_doe");
```

---

#### Insert Methods

##### `InsertAndFind<TProperty>(TModel model, Expression<Func<TModel, TProperty>> propertySelector)`

Inserts a record and retrieves it after insertion (useful for getting auto-generated IDs).

**Parameters:**
- `model` (TModel) - Model to insert
- `propertySelector` (Expression) - Property used to find the inserted record

**Returns:** `TModel` - Inserted model with database-generated values, or null if failed

**Example:**
```csharp
var newUser = new User { Username = "jane_doe", Email = "jane@example.com" };
var insertedUser = userController.InsertAndFind(newUser, u => u.Username);

if (insertedUser != null)
{
    Console.WriteLine($"New user ID: {insertedUser.Id}");
}
```

---

##### `InsertOrUpdate<TProperty>(TModel model, Expression<Func<TModel, TProperty>> propertySelector)`

Inserts the model if it doesn't exist, otherwise updates it (upsert operation).

**Parameters:**
- `model` (TModel) - Model to insert or update
- `propertySelector` (Expression) - Property used to check existence

**Returns:** `bool` - True if successful

**Example:**
```csharp
var user = new User { Username = "john_doe", Email = "newemail@example.com", Age = 31 };
bool success = userController.InsertOrUpdate(user, u => u.Username);
// If john_doe exists, updates; otherwise inserts
```

---

##### `GetOrCreate<TProperty>(TModel model, Expression<Func<TModel, TProperty>> propertySelector)`

Returns existing record if found, otherwise inserts and returns the new record.

**Parameters:**
- `model` (TModel) - Model to insert if not found
- `propertySelector` (Expression) - Property used to check existence

**Returns:** `TModel` - Existing or newly created record

**Example:**
```csharp
var user = new User { Username = "john_doe", Email = "john@example.com" };
var existingOrNew = userController.GetOrCreate(user, u => u.Username);
Console.WriteLine($"User ID: {existingOrNew.Id}");
```

---

#### Update Methods

##### `UpdateByProperty<TProperty>(TModel model, Expression<Func<TModel, TProperty>> propertySelector, TProperty value)`

Updates records where the specified property equals the given value.

**Parameters:**
- `model` (TModel) - Model with updated values
- `propertySelector` (Expression) - Property for WHERE clause
- `value` (TProperty) - Value to match

**Returns:** `bool` - True if successful

**Example:**
```csharp
var user = userController.GetById(5);
user.Email = "newemail@example.com";
user.Status = "verified";

bool success = userController.UpdateByProperty(user, u => u.Id, 5);
```

---

##### `UpdateWhere<TProperty1, TProperty2>(TModel model, Expression<...> property1Selector, TProperty1 value1, Expression<...> property2Selector, TProperty2 value2)`

Updates records matching multiple property conditions.

**Parameters:**
- `model` (TModel) - Model with updated values
- `property1Selector` (Expression) - First property selector
- `value1` (TProperty1) - First value to match
- `property2Selector` (Expression) - Second property selector
- `value2` (TProperty2) - Second value to match

**Returns:** `bool` - True if successful

**Example:**
```csharp
var user = new User { Status = "active", LastLogin = DateTime.Now };

bool success = userController.UpdateWhere(user,
    u => u.Username, "john_doe",
    u => u.Status, "pending");
// Updates users where Username='john_doe' AND Status='pending'
```

**Note:** There's also a three-property overload: `UpdateWhere<TProperty1, TProperty2, TProperty3>(...)`

---

##### `UpdateWhereIn<TProperty>(TModel model, Expression<Func<TModel, TProperty>> propertySelector, IEnumerable<TProperty> values)`

Updates records where a property value is in the specified list.

**Parameters:**
- `model` (TModel) - Model with updated values
- `propertySelector` (Expression) - Property selector
- `values` (IEnumerable<TProperty>) - List of values to match

**Returns:** `bool` - True if successful

**Example:**
```csharp
var updateModel = new User { Status = "verified" };
bool success = userController.UpdateWhereIn(updateModel, u => u.Id, new[] { 1, 2, 3, 5 });
// Updates users with IDs 1, 2, 3, or 5
```

---

##### `UpdateWhereIsNull<TProperty>(TModel model, Expression<Func<TModel, TProperty>> propertySelector)`

Updates all records where the specified property is null.

**Parameters:**
- `model` (TModel) - Model with updated values
- `propertySelector` (Expression) - Property to check for null

**Returns:** `bool` - True if successful

**Example:**
```csharp
var defaultEmail = new User { Email = "noemail@example.com" };
bool success = userController.UpdateWhereIsNull(defaultEmail, u => u.Email);
// Sets email for all users where Email IS NULL
```

---

#### Delete Methods

##### `DeleteByProperty<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, TProperty value)`

Deletes records where the specified property equals the given value.

**Parameters:**
- `propertySelector` (Expression) - Property selector
- `value` (TProperty) - Value to match for deletion

**Returns:** `bool` - True if successful

**Example:**
```csharp
bool success = userController.DeleteByProperty(u => u.Id, 5);
bool deleted = userController.DeleteByProperty(u => u.Status, "deleted");
```

---

##### `DeleteWhere<TProperty1, TProperty2>(Expression<...> property1Selector, TProperty1 value1, Expression<...> property2Selector, TProperty2 value2)`

Deletes records matching multiple property conditions.

**Parameters:**
- `property1Selector` (Expression) - First property selector
- `value1` (TProperty1) - First value to match
- `property2Selector` (Expression) - Second property selector
- `value2` (TProperty2) - Second value to match

**Returns:** `bool` - True if successful

**Example:**
```csharp
bool success = userController.DeleteWhere(
    u => u.Status, "inactive",
    u => u.LastLogin, DateTime.Now.AddYears(-1));
// Deletes users where Status='inactive' AND LastLogin < 1 year ago
```

**Note:** There's also a three-property overload: `DeleteWhere<TProperty1, TProperty2, TProperty3>(...)`

---

##### `DeleteWhereIn<TProperty>(Expression<Func<TModel, TProperty>> propertySelector, IEnumerable<TProperty> values)`

Deletes records where a property value is in the specified list.

**Parameters:**
- `propertySelector` (Expression) - Property selector
- `values` (IEnumerable<TProperty>) - List of values to match

**Returns:** `bool` - True if successful

**Example:**
```csharp
bool success = userController.DeleteWhereIn(u => u.Id, new[] { 10, 11, 12, 15 });
// Deletes users with IDs 10, 11, 12, or 15
```

---

##### `DeleteWhereIsNull<TProperty>(Expression<Func<TModel, TProperty>> propertySelector)`

Deletes records where the specified property is null.

**Parameters:**
- `propertySelector` (Expression) - Property to check for null

**Returns:** `bool` - True if successful

**Example:**
```csharp
bool success = userController.DeleteWhereIsNull(u => u.Email);
// Deletes all users where Email IS NULL
```

---

### LINQ Integration

All query methods return `IEnumerable<TModel>`, allowing LINQ chaining:

```csharp
// Database query + LINQ processing
var topAdultUsers = userController
    .WhereGreaterThan(u => u.Age, 18)        // Database filter
    .OrderByDescending(u => u.Age)           // In-memory sort
    .Take(10)                                 // In-memory limit
    .ToList();

// Complex filtering
var results = userController
    .WhereEquals(u => u.Status, "active")    // Database filter
    .Where(u => u.Email.Contains("@"))       // In-memory filter
    .Select(u => new { u.Username, u.Email }) // Projection
    .ToList();

// Grouping
var ageGroups = userController
    .All()                                    // Get all
    .GroupBy(u => u.Age)                     // Group
    .Select(g => new { 
        Age = g.Key, 
        Count = g.Count() 
    })
    .ToList();
```

**Performance Note:** LINQ operations after the initial database query execute in memory. For best performance, use the database query methods first, then apply LINQ operations.

---

### Thread Safety

`PropertyBasedUtilsController` is not thread-safe. Create separate instances for each thread or implement proper synchronization.

---

### Inheritance Hierarchy

```
Object
  ?? UtilsController<TModel>
       ?? PropertyBasedUtilsController<TModel>
```

All methods from `UtilsController<TModel>` are available, including:
- `GetAll()` - Get all records
- `Add(TModel entity)` - Insert record (LINQ standard name)
- `Where(predicate)` - Filter with lambda expression (LINQ standard)
- `FirstOrDefault(predicate)` - Get first match (LINQ standard)
- `SingleOrDefault(predicate)` - Get single match (LINQ standard)
- `Any(predicate)` - Check existence (LINQ standard)
- `Count(predicate)` - Count matches (LINQ standard)
- `Update(model, predicate)` - Update records by lambda (LINQ standard)
- `Remove(predicate)` - Delete records by lambda (LINQ standard)
- `SaveChanges(entity)` - Save entity changes by primary key (LINQ standard)
- `AsQueryable()` - Get IQueryable for LINQ chaining
- `Insert(TModel model)` - Insert record
- `Update(TModel model, string whereClause, object[] parameters)` - Update records
- `Delete(string whereClause, object[] parameters)` - Delete records
- `Count()` - Count all records
- `Select(string whereClause, object[] parameters)` - Select with custom WHERE clause

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

**Namespace**: `DbTools.Controller`  
**Generic Type**: `UtilsController<TModel>` where TModel : class, new()

Generic controller for LINQ-style database manipulation compatible with any model type. Provides Entity Framework-like CRUD operations using both string-based conditions and LINQ lambda expressions.

#### Constructors

##### `UtilsController(string tableName, string primaryKeyName = "", bool autoIncrement = true)`

Initializes a new instance loading connection settings from `config.json`.

**Parameters:**
- `tableName` (string) - The name of the database table
- `primaryKeyName` (string, optional) - The name of the primary key column
- `autoIncrement` (bool, optional) - Whether the primary key is auto-incremented (default: true)

##### `UtilsController(Utils utils, string tableName, string primaryKeyName = "", bool autoIncrement = true)`

Initializes a new instance with an existing Utils instance.

**Parameters:**
- `utils` (Utils) - An existing Utils instance with database connection configured
- `tableName` (string) - The name of the database table
- `primaryKeyName` (string, optional) - The name of the primary key column
- `autoIncrement` (bool, optional) - Whether the primary key is auto-incremented (default: true)

#### LINQ Expression Methods

These methods follow the same LINQ standards as MySQLDBTools, accepting lambda expressions for type-safe queries.

##### `GetAll()`

Returns all records from the table as a `List<TModel>`.

**Returns:** `List<TModel>` - All records

**Example:**
```csharp
var allUsers = userController.GetAll();
```

---

##### `AsQueryable()`

Returns all records as `IQueryable<TModel>` for advanced LINQ chaining (OrderBy, Skip, Take, etc.).

**Returns:** `IQueryable<TModel>`

**Example:**
```csharp
var sorted = userController.AsQueryable()
    .Where(u => u.Age > 18)
    .OrderBy(u => u.Name)
    .ToList();
```

---

##### `Add(TModel entity)`

Inserts a new record into the database. Alias for `Insert(TModel)` following LINQ naming conventions.

**Parameters:**
- `entity` (TModel) - The model instance to insert

**Returns:** `bool` - True if insertion was successful

**Example:**
```csharp
var user = new User { Name = "John Doe", Email = "john@example.com", Age = 30 };
bool success = userController.Add(user);
```

---

##### `Where(Expression<Func<TModel, bool>> predicate)`

Filters records using a LINQ lambda expression. Supports `==`, `!=`, `>`, `>=`, `<`, `<=`, `&&` (AND), `||` (OR), and `!` (NOT).

**Parameters:**
- `predicate` - A lambda expression representing the WHERE condition

**Returns:** `List<TModel>` - Matching records

**Example:**
```csharp
// Single condition
var adults = userController.Where(u => u.Age > 18);

// Multiple conditions (AND)
var filtered = userController.Where(u => u.Age > 18 && u.Age < 65);

// OR condition
var specific = userController.Where(u => u.Name == "John" || u.Name == "Jane");
```

---

##### `FirstOrDefault(Expression<Func<TModel, bool>> predicate)`

Gets the first record matching the predicate, or `null` if not found.

**Parameters:**
- `predicate` - A lambda expression representing the WHERE condition

**Returns:** `TModel` - First matching record, or `null`

**Example:**
```csharp
var user = userController.FirstOrDefault(u => u.Id == 1);
var john = userController.FirstOrDefault(u => u.Name == "John Doe");
```

---

##### `SingleOrDefault(Expression<Func<TModel, bool>> predicate)`

Gets a single record matching the predicate, or `null` if no records match. Throws `InvalidOperationException` if more than one record matches.

**Parameters:**
- `predicate` - A lambda expression representing the WHERE condition

**Returns:** `TModel` - Single matching record, or `null`

**Throws:** `InvalidOperationException` - When more than one record matches

**Example:**
```csharp
var user = userController.SingleOrDefault(u => u.Id == 1);
```

---

##### `Any(Expression<Func<TModel, bool>> predicate)`

Checks if any records exist that match the specified predicate.

**Parameters:**
- `predicate` - A lambda expression representing the WHERE condition

**Returns:** `bool` - True if any matching records exist

**Example:**
```csharp
bool hasAdults = userController.Any(u => u.Age >= 18);
```

---

##### `Count(Expression<Func<TModel, bool>> predicate)`

Counts records matching the specified predicate.

**Parameters:**
- `predicate` - A lambda expression representing the WHERE condition

**Returns:** `int` - Count of matching records

**Example:**
```csharp
int adultCount = userController.Count(u => u.Age >= 18);
int allCount = userController.Count(); // uses string-based Count("", null) with no conditions
```

---

##### `Update(TModel model, Expression<Func<TModel, bool>> predicate)`

Updates records identified by a LINQ lambda expression.

**Parameters:**
- `model` (TModel) - The model instance with updated values
- `predicate` - A lambda expression identifying which records to update

**Returns:** `bool` - True if update was successful

**Throws:** `ArgumentException` - When the predicate produces an empty WHERE clause

**Example:**
```csharp
var updatedUser = new User { Name = "Jane Doe", Email = "jane@example.com", Age = 31 };
bool success = userController.Update(updatedUser, u => u.Id == 1);
```

---

##### `Remove(Expression<Func<TModel, bool>> predicate)`

Deletes records matching the specified LINQ lambda expression.

**Parameters:**
- `predicate` - A lambda expression identifying which records to delete

**Returns:** `bool` - True if deletion was successful

**Throws:** `ArgumentException` - When the predicate produces an empty WHERE clause

**Example:**
```csharp
bool deleted = userController.Remove(u => u.Id == 1);
bool deletedOld = userController.Remove(u => u.CreatedDate < DateTime.Now.AddYears(-1));
```

---

##### `SaveChanges(TModel entity)`

Saves changes to an entity by updating the record identified by its primary key. Requires the primary key name to be specified in the constructor.

**Parameters:**
- `entity` (TModel) - The model instance with updated values; its primary key property identifies the record

**Returns:** `bool` - True if update was successful

**Throws:**
- `InvalidOperationException` - When primary key name is not specified in the constructor
- `InvalidOperationException` - When the primary key property is not found on the model type
- `InvalidOperationException` - When the primary key value is null

**Example:**
```csharp
var user = userController.FirstOrDefault(u => u.Id == 1);
if (user != null)
{
    user.Name = "Updated Name";
    bool saved = userController.SaveChanges(user);
}
```

#### String-Based Methods (legacy)

These methods use string conditions and are still available for complex or custom WHERE clauses.

```csharp
IEnumerable<TModel> Select(string conditionsParametrized, IEnumerable<object> parameters)
IEnumerable<TModel> Where(string conditions, object[] parameters)
TModel FirstOrDefault(string conditions = "", object[] parameters = null)
TModel SingleOrDefault(string conditions = "", object[] parameters = null)
bool Any(string conditions = "", object[] parameters = null)
int Count(string conditions = "", object[] parameters = null)
bool Insert(TModel model)
bool InsertRange(IEnumerable<TModel> models)
bool Update(TModel model, string conditions)
bool Update(TModel model, string whereClause, object[] whereParameters)
bool Delete(string conditions, object[] parameters)
TModel Find(object primaryKeyValue)
IEnumerable<TModel> All()
TModel Single(string conditions, object[] parameters)
```

#### Supported Expression Types

| Expression | SQL Equivalent | Example |
|-----------|----------------|---------|
| `==` | `=` | `u => u.Name == "John"` |
| `!=` | `!=` | `u => u.Status != "deleted"` |
| `>` | `>` | `u => u.Age > 18` |
| `>=` | `>=` | `u => u.Age >= 18` |
| `<` | `<` | `u => u.Age < 65` |
| `<=` | `<=` | `u => u.Age <= 65` |
| `&&` | `AND` | `u => u.Age > 18 && u.Active == true` |
| `||` | `OR` | `u => u.Name == "A" || u.Name == "B"` |
| `!` | `NOT` | `u => !(u.Age > 65)` |



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
