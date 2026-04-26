# DBTools_SQL API Reference

Complete API documentation for the DBTools_SQL library.

## Table of Contents

1. [SqlClient Class](#sqlclient-class)
2. [DbConfiguration Class](#dbconfiguration-class)
3. [SqlQueryBuilder Class](#sqlquerybuilder-class)
4. [SqlValidator Class](#sqlvalidator-class)
5. [LinqHelper Class](#linqhelper-class)
6. [Linq Class](#linq-class)
7. [DBTools Class (Base)](#dbtools-class-base)
8. [DBToolsController Class](#dbtoolscontroller-class)
9. [GenericObject Class](#genericobject-class)
10. [DataExport Class](#dataexport-class)
11. [LINQ Infrastructure](#linq-infrastructure)
12. [Abstractions (Interfaces)](#abstractions-interfaces)

---

## SqlClient Class

**Namespace**: `DBTools.Core`
**Inherits**: `DBTools.Core.DBTools`
**Implements**: `DBTools.Abstractions.ISqlClient`

Main utility class providing high-level database operations with built-in security features.

### Constructors

#### `SqlClient()`

Initializes a new instance with configuration loaded from `config.json`.

```csharp
var db = new SqlClient();
```

**Throws:**
- `FileNotFoundException` - When config.json is not found
- `InvalidOperationException` - When configuration is invalid or required keys are missing

---

#### `SqlClient(IDbConfiguration configuration, ISqlValidator validator, ISqlQueryBuilder queryBuilder)`

Initializes with dependency injection for testability and custom configuration.

```csharp
var config = new DbConfiguration(myIConfiguration);
var validator = new SqlValidator();
var queryBuilder = new SqlQueryBuilder(validator);
var db = new SqlClient(config, validator, queryBuilder);
```

**Parameters:**
- `configuration` (IDbConfiguration) - Database configuration
- `validator` (ISqlValidator) - SQL identifier validator
- `queryBuilder` (ISqlQueryBuilder) - SQL query builder

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
| `ConnectionString` | string | Built connection string | Get/Set |
| `SqlParameters` | List\<SqlParameter\> | SQL parameters for queries | Get/Set |
| `Count` | int | Result count from last query | Get/Set |
| `Configuration` | IDbConfiguration | Loaded configuration | Get |
| `QueryBuilderInstance` | ISqlQueryBuilder | Query builder instance | Get |

---

### SELECT Methods

##### `Select(string _fields, string _table, string whereClause, object[] parameters)`

Retrieves data using parameterized WHERE clause.

**Parameters:**
- `_fields` (string) - Comma-separated field names or "*" for all
- `_table` (string) - Table name
- `whereClause` (string) - WHERE condition with @param0, @param1, etc.
- `parameters` (object[]) - Parameter values

**Returns:** `DataView`

**Throws:**
- `ArgumentException` - Invalid identifiers
- `ArgumentNullException` - Null parameters array

**Example:**
```csharp
DataView users = db.Select("id, username, email", "Users", "status = @param0", new object[] { "active" });
DataView all = db.Select("*", "Users", "", new object[] { });
```

---

##### `Select(string query_without_select, object[] parameters)`

Executes a custom query without the SELECT keyword (for JOINs, aggregates, etc.).

**Parameters:**
- `query_without_select` (string) - Query starting from field list
- `parameters` (object[]) - Parameter values

**Returns:** `DataView`

**Example:**
```csharp
string query = @"
    u.username, o.order_date, o.total
    FROM Users u
    INNER JOIN Orders o ON u.id = o.user_id
    WHERE u.id = @param0";
DataView results = db.Select(query, new object[] { userId });
```

---

### INSERT Method

##### `Insert(string[] _fields, string _table, object[] _values, string primary_key_name = null, bool auto_increment = true)`

Inserts a new record using parameterized queries.

**Parameters:**
- `_fields` (string[]) - Field names
- `_table` (string) - Table name
- `_values` (object[]) - Values corresponding to fields
- `primary_key_name` (string, optional) - Primary key field name
- `auto_increment` (bool, optional) - Exclude primary key if true (default: true)

**Returns:** `bool` - True if successful

**Example:**
```csharp
string[] fields = { "username", "email", "age" };
object[] values = { "john_doe", "john@example.com", 30 };
bool success = db.Insert(fields, "Users", values);
```

---

### UPDATE Methods

##### `Update(string[] _fields, string _table, string[] _values, string condition = "")`

Updates records using a string-based WHERE condition (legacy).

**Parameters:**
- `_fields` (string[]) - Field names to update
- `_table` (string) - Table name
- `_values` (string[]) - New values
- `condition` (string) - WHERE clause condition (required)

**Returns:** `bool` - True if successful

**Warning:** This method is less secure than the parameterized version. Use the overload with `whereParameters` when possible.

---

##### `Update(string[] _fields, string _table, string[] _values, string whereClause, object[] whereParameters)` **Recommended**

Updates records using parameterized WHERE clause.

**Parameters:**
- `_fields` (string[]) - Field names to update
- `_table` (string) - Table name
- `_values` (string[]) - New values
- `whereClause` (string) - WHERE clause with @whereParam0, @whereParam1, etc.
- `whereParameters` (object[]) - Parameter values for WHERE clause

**Returns:** `bool` - True if successful

**Example:**
```csharp
string[] fields = { "email", "updated_date" };
string[] values = { "new@example.com", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
bool success = db.Update(fields, "Users", values, "id = @whereParam0", new object[] { 5 });
```

---

### DELETE Method

##### `Delete(string _table, string whereClause, object[] parameters)`

Deletes records using parameterized WHERE clause.

**Parameters:**
- `_table` (string) - Table name
- `whereClause` (string) - WHERE clause with @param0, @param1, etc.
- `parameters` (object[]) - Parameter values

**Returns:** `bool` - True if successful

**Example:**
```csharp
bool success = db.Delete("Users", "id = @param0", new object[] { 5 });
```

---

### Helper Methods

##### `QueryBuilder(object obj, string primaryKeyName = "", bool autoIncrement = true)`

Converts any object into database-ready format.

**Returns:** `List<GenericObject>` with columns, values, and types

**Example:**
```csharp
var user = new User { Name = "John", Email = "john@example.com", Age = 30 };
List<GenericObject> queryData = db.QueryBuilder(user, "Id", true);
string[] columns = queryData[0].columns;   // ["Name", "Email", "Age"]
object[] values = queryData[0].values;      // ["John", "john@example.com", 30]
```

---

##### `GetInBd(string query)`

Returns first column of all rows as a string array.

**Returns:** `string[]`

---

##### `GetInBdDv(string query)`

Returns query results as a DataView.

**Returns:** `DataView` - null if error occurs

---

##### `ExecuteQuery(string query)`

Executes a non-query SQL command.

**Returns:** void

---

### Static Query Builder Methods

These methods generate SQL query strings. For better security, use the instance methods with parameterized queries instead.

| Method | Returns | Description |
|--------|---------|-------------|
| `Select_Query(string fields, string table, string conditions)` | `string` | SELECT query string |
| `Insert_Query(string[] fields, string table, object[] values, string primaryKeyName, bool autoIncrement)` | `string` | INSERT query string |
| `Update_Query(string[] fields, string table, string[] values, string condition)` | `string` | UPDATE query string |
| `Delete_Query(string table, string condition)` | `string` | DELETE query string |
| `GenerateSqlParameters(object[] values)` | `List<SqlParameter>` | SqlParameter list |

---

## DbConfiguration Class

**Namespace**: `DBTools.Core`
**Implements**: `DBTools.Abstractions.IDbConfiguration`

Loads and validates database configuration from `config.json` or `IConfiguration`.

### Constructors

#### `DbConfiguration()`

Loads configuration from `config.json` in the current directory.

```csharp
var config = new DbConfiguration();
```

**Throws:**
- `FileNotFoundException` - config.json not found
- `InvalidOperationException` - Missing or invalid configuration keys

---

#### `DbConfiguration(IConfiguration configuration)`

Loads configuration from an existing `IConfiguration` instance (for DI scenarios).

```csharp
IConfiguration myConfig = ...;
var config = new DbConfiguration(myConfig);
```

---

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Host` | string | SQL Server hostname (read-only) |
| `Database` | string | Database name (read-only) |
| `Uid` | string | Database username (read-only) |
| `Password` | string | Database password (read-only) |
| `Port` | string | SQL Server port (read-only) |
| `ConnectionString` | string | Built connection string (read-only) |

---

## SqlQueryBuilder Class

**Namespace**: `DBTools.Core`
**Implements**: `DBTools.Abstractions.ISqlQueryBuilder`

Generates SQL query strings with identifier validation.

### Constructor

```csharp
var builder = new SqlQueryBuilder(new SqlValidator());
```

### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `SelectQuery(string fields, string table, string conditions)` | `string` | Generate SELECT query |
| `InsertQuery(string[] fields, string table, object[] values, string primaryKeyName, bool autoIncrement)` | `string` | Generate INSERT query |
| `UpdateQuery(string[] fields, string table, string[] values, string condition)` | `string` | Generate UPDATE query |
| `DeleteQuery(string table, string condition)` | `string` | Generate DELETE query |
| `GenerateSqlParameters(object[] values)` | `List<SqlParameter>` | Generate SqlParameter list |

---

## SqlValidator Class

**Namespace**: `DBTools.Core`
**Implements**: `DBTools.Abstractions.ISqlValidator`

Validates SQL identifiers to prevent injection attacks.

### Method

##### `IsValidIdentifier(string identifier)`

Returns `bool` - true if the identifier is valid.

**Validation Rules:**
- Allows: alphanumeric, underscores, dots, brackets, commas, spaces, asterisks, parentheses
- Blocks: `DROP`, `DELETE` keywords (case-insensitive)
- Blocks: SQL comment patterns (`--`, `;--`, `/*`, `*/`)
- Regex: `^[\w\.\[\]\,\s\*\(\)]+$`

**Valid Examples:** `Users`, `user_name`, `[User Table]`, `dbo.Users`, `id, name, email`, `*`
**Invalid Examples:** `Users; DROP TABLE--`, `Users--`, `table/*comment*/`

---

## LinqHelper Class

**Namespace**: `DBTools.Controllers`
**Generic Type**: `LinqHelper<TModel>` where TModel : class, new()

Generic helper for LINQ-style database manipulation. Provides CRUD and query operations similar to Entity Framework.

### Constructors

#### `LinqHelper(string tableName, string primaryKeyName = "", bool autoIncrement = true)`

Creates a new instance with automatic configuration loading.

```csharp
var controller = new LinqHelper<User>("Users", "Id", true);
```

---

#### `LinqHelper(SqlClient utils, string tableName, string primaryKeyName = "", bool autoIncrement = true)`

Creates a new instance with an existing SqlClient.

```csharp
var db = new SqlClient();
var controller = new LinqHelper<User>(db, "Users", "Id", true);
```

---

### String-Based Query Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `Select(string conditions, IEnumerable<object> parameters)` | `IEnumerable<TModel>` | Select with conditions |
| `Where(string conditions, object[] parameters)` | `IEnumerable<TModel>` | Filter with conditions |
| `FirstOrDefault(string conditions = "", object[] parameters = null)` | `TModel` | First match or null |
| `Single(string conditions, object[] parameters)` | `TModel` | Exactly one match (throws if 0 or >1) |
| `SingleOrDefault(string conditions = "", object[] parameters = null)` | `TModel` | Single or null (throws if >1) |
| `All()` | `IEnumerable<TModel>` | All records |
| `Count(string conditions = "", object[] parameters = null)` | `int` | Count matching records |
| `Any(string conditions = "", object[] parameters = null)` | `bool` | Check if any exist |
| `Find(object primaryKeyValue)` | `TModel` | Find by primary key |

---

### LINQ Expression Methods (MySQLDBTools-compatible)

| Method | Returns | Description |
|--------|---------|-------------|
| `GetAll()` | `List<TModel>` | All records as list |
| `Where(Expression<Func<TModel, bool>> predicate)` | `List<TModel>` | Lambda filter |
| `FirstOrDefault(Expression<Func<TModel, bool>> predicate)` | `TModel` | First lambda match |
| `SingleOrDefault(Expression<Func<TModel, bool>> predicate)` | `TModel` | Single lambda match |
| `Any(Expression<Func<TModel, bool>> predicate)` | `bool` | Lambda existence check |
| `Count(Expression<Func<TModel, bool>> predicate)` | `int` | Lambda count |
| `Add(TModel entity)` | `bool` | Insert (alias for Insert) |
| `Insert(TModel model)` | `bool` | Insert from model |
| `InsertRange(IEnumerable<TModel> models)` | `bool` | Bulk insert |
| `Update(TModel model, Expression<Func<TModel, bool>> predicate)` | `bool` | Update by lambda |
| `Update(TModel model, string conditions)` | `bool` | Update by string conditions |
| `Update(TModel model, string whereClause, object[] whereParameters)` | `bool` | Update by parameterized WHERE |
| `SaveChanges(TModel entity)` | `bool` | Update by primary key |
| `Remove(Expression<Func<TModel, bool>> predicate)` | `bool` | Delete by lambda |
| `Delete(string conditions, object[] parameters)` | `bool` | Delete by conditions |

**Example:**
```csharp
var controller = new LinqHelper<User>("Users", "Id");

var adults = controller.Where(u => u.Age > 18);
var user = controller.FirstOrDefault(u => u.Id == 1);
int count = controller.Count(u => u.Status == "active");
bool added = controller.Add(new User { Name = "Alice", Age = 28 });
bool deleted = controller.Remove(u => u.Id == 1);
bool saved = controller.SaveChanges(existingUser);
```

#### Supported Expression Operators

| Operator | Example | SQL |
|----------|---------|-----|
| `==` | `u => u.Name == "John"` | `Name = @param0` |
| `!=` | `u => u.Status != "deleted"` | `Status <> @param0` |
| `>` | `u => u.Age > 18` | `Age > @param0` |
| `>=` | `u => u.Age >= 18` | `Age >= @param0` |
| `<` | `u => u.Age < 65` | `Age < @param0` |
| `<=` | `u => u.Age <= 65` | `Age <= @param0` |
| `&&` | `u => u.Age > 18 && u.Active == true` | `(Age > @param0) AND (Active = @param1)` |
| `\|\|` | `u => u.Name == "A" \|\| u.Name == "B"` | `(Name = @param0) OR (Name = @param1)` |
| `!` | `u => !(u.Age > 65)` | `NOT (Age > @param0)` |

---

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Utils` | `SqlClient` | Underlying SqlClient instance |
| `TableName` | string | Table name (protected) |
| `PrimaryKeyName` | string | Primary key name (protected) |
| `Error` | string | Last error message |

---

### AsQueryable

```csharp
// LinqHelper base: loads all data into memory first
var query = controller.AsQueryable()
    .Where(u => u.Age > 18)
    .OrderBy(u => u.Name)
    .Take(10)
    .ToList();
```

**Note:** For SQL-translated deferred execution, use `Linq<TModel>` instead.

---

## Linq Class

**Namespace**: `DBTools.Controllers`
**Generic Type**: `Linq<TModel>` where TModel : class, new()
**Inherits**: `LinqHelper<TModel>`

Extends `LinqHelper` with property-selector operations, JOIN support, and deferred IQueryable execution.

### Constructors

Same as `LinqHelper<TModel>`:

```csharp
var controller = new Linq<User>("Users", "Id", true);
var controller = new Linq<User>(db, "Users", "Id", true);
```

---

### Comparison Query Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `WhereEquals<TProperty>(Expression, TProperty value)` | `IEnumerable<TModel>` | Property equals value |
| `WhereNotEquals<TProperty>(Expression, TProperty value)` | `IEnumerable<TModel>` | Property not equals |
| `WhereGreaterThan<TProperty>(Expression, TProperty value)` | `IEnumerable<TModel>` | Property > value |
| `WhereGreaterThanOrEquals<TProperty>(Expression, TProperty value)` | `IEnumerable<TModel>` | Property >= value |
| `WhereLessThan<TProperty>(Expression, TProperty value)` | `IEnumerable<TModel>` | Property < value |
| `WhereLessThanOrEquals<TProperty>(Expression, TProperty value)` | `IEnumerable<TModel>` | Property <= value |
| `WhereBetween<TProperty>(Expression, TProperty min, TProperty max)` | `IEnumerable<TModel>` | Property between min and max |

---

### String Query Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `WhereContains(Expression, string substring)` | `IEnumerable<TModel>` | LIKE %substring% |
| `WhereStartsWith(Expression, string prefix)` | `IEnumerable<TModel>` | LIKE prefix% |
| `WhereEndsWith(Expression, string suffix)` | `IEnumerable<TModel>` | LIKE %suffix |

---

### Collection Query Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `WhereIn<TProperty>(Expression, IEnumerable<TProperty>)` | `IEnumerable<TModel>` | IN clause |
| `WhereNotIn<TProperty>(Expression, IEnumerable<TProperty>)` | `IEnumerable<TModel>` | NOT IN clause |

---

### Null Check Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `WhereIsNull<TProperty>(Expression)` | `IEnumerable<TModel>` | IS NULL |
| `WhereIsNotNull<TProperty>(Expression)` | `IEnumerable<TModel>` | IS NOT NULL |

---

### Example-Based Filtering

##### `WhereByExample(TModel filterModel)`

Uses a model instance as a filter template. Only non-null and non-default properties are included.

```csharp
var filter = new User { Status = "active", Age = 30 };
var results = controller.WhereByExample(filter);
// WHERE Status = 'active' AND Age = 30
```

---

### Retrieve Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `FirstOrDefaultByProperty<TProperty>(Expression, TProperty)` | `TModel` | First by property or null |
| `SingleByProperty<TProperty>(Expression, TProperty)` | `TModel` | Single by property (throws if 0 or >1) |
| `SingleOrDefaultByProperty<TProperty>(Expression, TProperty)` | `TModel` | Single or null by property |
| `CountByProperty<TProperty>(Expression, TProperty)` | `int` | Count by property |
| `AnyByProperty<TProperty>(Expression, TProperty)` | `bool` | Exists by property |
| `Exists<TProperty>(Expression, TProperty)` | `bool` | Alias for AnyByProperty |

---

### Insert Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `InsertAndFind<TProperty>(TModel, Expression)` | `TModel` | Insert and retrieve |
| `InsertOrUpdate<TProperty>(TModel, Expression)` | `bool` | Upsert by property |
| `GetOrCreate<TProperty>(TModel, Expression)` | `TModel` | Return existing or insert |

---

### Update Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `UpdateByProperty<TProperty>(TModel, Expression, TProperty)` | `bool` | Update by property value |
| `UpdateWhere<T1,T2>(TModel, Expr, T1, Expr, T2)` | `bool` | Update by 2 properties |
| `UpdateWhere<T1,T2,T3>(TModel, Expr, T1, Expr, T2, Expr, T3)` | `bool` | Update by 3 properties |
| `UpdateWhereIn<TProperty>(TModel, Expression, IEnumerable<TProperty>)` | `bool` | Update where IN |
| `UpdateWhereIsNull<TProperty>(TModel, Expression)` | `bool` | Update where IS NULL |

---

### Delete Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `DeleteByProperty<TProperty>(Expression, TProperty)` | `bool` | Delete by property |
| `DeleteWhere<T1,T2>(Expr, T1, Expr, T2)` | `bool` | Delete by 2 properties |
| `DeleteWhere<T1,T2,T3>(Expr, T1, Expr, T2, Expr, T3)` | `bool` | Delete by 3 properties |
| `DeleteWhereIn<TProperty>(Expression, IEnumerable<TProperty>)` | `bool` | Delete where IN |
| `DeleteWhereIsNull<TProperty>(Expression)` | `bool` | Delete where IS NULL |

---

### JOIN Methods

##### `InnerJoin<TRight>(string rightTable, Expression<Func<TModel, object>> leftKey, Expression<Func<TRight, object>> rightKey)`

Creates an INNER JOIN query.

**Returns:** `JoinQuery<TModel, TRight>` - supports LINQ chaining (Where, OrderBy, Skip, Take)

```csharp
var query = userController.InnerJoin<Order>(
    "Orders",
    u => u.Id,
    o => o.UserId
);

var results = query
    .Where(j => j.Left.Age > 18)
    .OrderBy(j => j.Left.Username)
    .ToList();

foreach (var row in results)
{
    Console.WriteLine($"{row.Left.Username}: {row.Right.Total}");
}
```

---

##### `LeftJoin<TRight>(string rightTable, Expression<Func<TModel, object>> leftKey, Expression<Func<TRight, object>> rightKey)`

Creates a LEFT JOIN query. `Right` property will be null for non-matching rows.

**Returns:** `JoinQuery<TModel, TRight>`

```csharp
var query = userController.LeftJoin<Order>(
    "Orders",
    u => u.Id,
    o => o.UserId
);

foreach (var row in query.ToList())
{
    Console.WriteLine($"{row.Left.Username}: {(row.Right != null ? row.Right.Total.ToString() : "No orders")}");
}
```

---

### Deferred IQueryable

##### `AsQueryable()` (override)

Returns `DbQuery<TModel>` with SQL-translated deferred execution. LINQ methods are translated to SQL and executed only on enumeration.

```csharp
var query = userController.AsQueryable()
    .Where(u => u.Age > 18)
    .OrderBy(u => u.Name)
    .Skip(10)
    .Take(5);

// SQL is executed only here:
var results = query.ToList();

// View generated SQL:
Console.WriteLine(query.ToString());
```

**Difference from LinqHelper.AsQueryable():** `LinqHelper` loads all data into memory first. `Linq` translates LINQ to SQL for efficient deferred execution.

---

## DBTools Class (Base)

**Namespace**: `DBTools.Core`

Base class providing low-level database connection and query execution.

### Key Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `RetrieveObjectSql()` | `List<GenericObject>` | Execute query, return as GenericObject list |
| `RetrieveDataSql(string query = "")` | `DataView` | Execute query, return as DataView |
| `SqlExecuteQuery(string query = "")` | `void` | Execute non-query command |

**Note:** Prefer `SqlClient` for all operations. The base `DBTools` class is used internally.

### Legacy Methods (Obsolete)

| Method | Status |
|--------|--------|
| `sqlExecuteQuery()` | Obsolete - use `SqlExecuteQuery` |
| `retrieveDataSql()` | Obsolete - use `RetrieveObjectSql` or `RetrieveDataSql` |

---

## DBToolsController Class

**Namespace**: `DBTools.Controllers`
**Implements**: `DBTools.Abstractions.IDBTools`

Wrapper controller for the base `DBTools` class.

### Constructor

```csharp
var controller = new DBToolsController(new DBTools.Core.DBTools());
```

### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `RetrieveDataSQL()` | `DataView` | Retrieve data as DataView |
| `RetrieveObjectSQL()` | `List<GenericObject>` | Retrieve data as GenericObject list |
| `SqlExecuteQuery(string query = "")` | `void` | Execute non-query |

---

## GenericObject Class

**Namespace**: `DBTools.Models`

Container for database operation data.

### Constructors

```csharp
new GenericObject()                          // Without database operations
new GenericObject(SqlClient dbTools)         // With database operations
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `columns` | string[] | Column names |
| `values` | object[] | Column values |
| `valuesString` | string[] | String representation of values |
| `types` | string[] | Data types |
| `table` | string | Table name |

### Methods

```csharp
public bool Insert()                    // Insert using columns/valuesString/table
public bool Update(string conditions)    // Update using columns/valuesString/table
```

---

### GenericObject_Simple Class

**Namespace**: `DBTools.Models`

Simple container used internally by `QueryBuilder`.

| Property | Type | Description |
|----------|------|-------------|
| `column` | string | Column name |
| `value` | object | Column value |
| `type` | string | Data type name |

---

## DataExport Class

**Namespace**: `DBTools.Export`

### ToCsv

```csharp
public string ToCsv(List<GenericObject> genericObject, char separator, bool showColums = true, bool showTypes = true)
```

Exports data to CSV format.

**Parameters:**
- `genericObject` (List\<GenericObject\>) - Data to export
- `separator` (char) - CSV separator character
- `showColums` (bool) - Include column headers
- `showTypes` (bool) - Include type information

**Returns:** `string` - CSV formatted data

---

### ToDataTable

```csharp
public DataTable ToDataTable(string csv, char separator, bool specifyColumnTypes = false)
```

Converts CSV content to a DataTable.

**Parameters:**
- `csv` (string) - CSV content
- `separator` (char) - CSV separator character
- `specifyColumnTypes` (bool) - Whether first line specifies column types

**Returns:** `DataTable`

---

## LINQ Infrastructure

### DbQuery\<T\>

**Namespace**: `DBTools.Linq`

IQueryable implementation for deferred SQL queries. No SQL is executed until the query is enumerated.

Supports: Where, OrderBy, ThenBy, Skip, Take, First, Count, Any, etc.

```csharp
var query = new DbQuery<User>(provider);
var results = query.Where(u => u.Age > 18).OrderBy(u => u.Name).Take(10).ToList();
```

---

### DbQueryProvider

**Namespace**: `DBTools.Linq`

IQueryProvider that translates LINQ expressions into SQL queries and executes them through `SqlClient`.

---

### DbExpressionTranslator

**Namespace**: `DBTools.Linq`

Translates LINQ expression trees into SQL WHERE clauses and full queries. Used internally by `DbQueryProvider` and `JoinQueryProvider`.

---

### JoinQuery\<TLeft, TRight\>

**Namespace**: `DBTools.Linq`

IQueryable implementation for JOIN queries. Supports Where, OrderBy, Skip, Take, and other LINQ operators.

Created by `Linq<TModel>.InnerJoin<TRight>()` or `Linq<TModel>.LeftJoin<TRight>()`.

---

### JoinResult\<TLeft, TRight\>

**Namespace**: `DBTools.Linq`

Represents a single row from a JOIN query.

| Property | Type | Description |
|----------|------|-------------|
| `Left` | TLeft | Model from the primary table |
| `Right` | TRight | Model from the joined table (null for LEFT JOIN non-matches) |

---

## Abstractions (Interfaces)

### ISqlClient

**Namespace**: `DBTools.Abstractions`

```csharp
public interface ISqlClient
{
    DataView Select(string fields, string table, string whereClause, object[] parameters);
    DataView Select(string queryWithoutSelect, object[] parameters);
    bool Insert(string[] fields, string table, object[] values, string primaryKeyName = null, bool autoIncrement = true);
    bool Update(string[] fields, string table, string[] values, string condition = "");
    bool Update(string[] fields, string table, string[] values, string whereClause, object[] whereParameters);
    bool Delete(string table, string whereClause, object[] parameters);
    List<GenericObject> QueryBuilder(object obj, string primaryKeyName = "", bool autoIncrement = true);
    string[] GetInBd(string query);
    DataView GetInBdDv(string query);
    void ExecuteQuery(string query);
    ISqlQueryBuilder QueryBuilderInstance { get; }
    IDbConfiguration Configuration { get; }
}
```

---

### IDbConfiguration

**Namespace**: `DBTools.Abstractions`

```csharp
public interface IDbConfiguration
{
    string Host { get; }
    string Database { get; }
    string Uid { get; }
    string Password { get; }
    string Port { get; }
    string ConnectionString { get; }
}
```

---

### ISqlQueryBuilder

**Namespace**: `DBTools.Abstractions`

```csharp
public interface ISqlQueryBuilder
{
    string SelectQuery(string fields, string table, string conditions);
    string InsertQuery(string[] fields, string table, object[] values, string primaryKeyName = "", bool autoIncrement = true);
    string UpdateQuery(string[] fields, string table, string[] values, string condition = "");
    string DeleteQuery(string table, string condition);
    List<SqlParameter> GenerateSqlParameters(object[] values);
}
```

---

### ISqlValidator

**Namespace**: `DBTools.Abstractions`

```csharp
public interface ISqlValidator
{
    bool IsValidIdentifier(string identifier);
}
```

---

### IDBTools

**Namespace**: `DBTools.Abstractions`

```csharp
public interface IDBTools
{
    List<GenericObject> RetrieveObjectSQL();
    DataView RetrieveDataSQL();
    void SqlExecuteQuery(string query = "");
}
```
