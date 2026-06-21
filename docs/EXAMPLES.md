# DBTools_SQL Code Examples

Comprehensive code examples for all DBTools_SQL features.

## Table of Contents

1. [Basic Operations with SqlClient](#basic-operations-with-sqlclient)
2. [LINQ Expression Queries with LinqHelper](#linq-expression-queries-with-linqhelper)
3. [Async LINQ with AsyncLinqHelper](#async-linq-with-asynclinqhelper)
4. [Property-Based Queries with Linq](#property-based-queries-with-linq)
5. [JOIN Queries](#join-queries)
6. [Deferred IQueryable Execution](#deferred-iqueryable-execution)
7. [Advanced Queries](#advanced-queries)
8. [Real-World Applications](#real-world-applications)
9. [Data Export](#data-export)
10. [Error Handling](#error-handling)
11. [Dependency Injection](#dependency-injection)
12. [Performance Optimization](#performance-optimization)

---

## Basic Operations with SqlClient

### Setup

```csharp
using DBTools.Core;
using System.Data;

var db = new SqlClient();
```

### SELECT Examples

```csharp
// Simple SELECT with WHERE
DataView activeUsers = db.Select(
    "id, username, email",
    "Users",
    "status = @param0",
    new object[] { "active" }
);

// SELECT all records
DataView allUsers = db.Select(
    "*",
    "Users",
    "",
    new object[] { }
);

// SELECT with multiple parameters
DataView filtered = db.Select(
    "*",
    "Users",
    "age > @param0 AND status = @param1 AND created_date > @param2",
    new object[] { 18, "active", new DateTime(2024, 1, 1) }
);

// Custom query (without SELECT keyword)
DataView custom = db.Select(
    "u.*, COUNT(o.id) AS order_count FROM Users u LEFT JOIN Orders o ON u.id = o.user_id GROUP BY u.id, u.username HAVING COUNT(o.id) > @param0",
    new object[] { 5 }
);
```

### INSERT Examples

```csharp
// Basic INSERT
string[] fields = { "username", "email", "age" };
object[] values = { "john_doe", "john@example.com", 30 };
bool success = db.Insert(fields, "Users", values);

// INSERT with auto-increment primary key
string[] fields = { "id", "username", "email" };
object[] values = { 0, "jane_doe", "jane@example.com" };
bool success = db.Insert(fields, "Users", values, "id", true);
// The 'id' field is automatically excluded from the INSERT

// INSERT using QueryBuilder
public class User
{
    public string Username { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
}

var user = new User { Username = "bob", Email = "bob@example.com", Age = 25 };
var queryData = db.QueryBuilder(user);
bool success = db.Insert(queryData[0].columns, "Users", queryData[0].values);
```

### UPDATE Examples

```csharp
// Parameterized UPDATE (recommended)
string[] fields = { "email", "status" };
string[] values = { "newemail@example.com", "active" };
bool success = db.Update(
    fields,
    "Users",
    values,
    "id = @whereParam0",
    new object[] { 5 }
);

// UPDATE with multiple WHERE parameters
string[] fields = { "status" };
string[] values = { "inactive" };
bool success = db.Update(
    fields,
    "Users",
    values,
    "last_login < @whereParam0 AND status = @whereParam1",
    new object[] { DateTime.Now.AddYears(-1), "active" }
);
```

### DELETE Examples

```csharp
// DELETE by ID
bool success = db.Delete("Users", "id = @param0", new object[] { 5 });

// DELETE with multiple conditions
bool success = db.Delete(
    "Users",
    "status = @param0 AND last_login < @param1",
    new object[] { "inactive", DateTime.Now.AddYears(-2) }
);
```

---

## LINQ Expression Queries with LinqHelper

### Setup

```csharp
using DBTools.Controllers;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
    public string Status { get; set; }
    public DateTime? LastLogin { get; set; }
}

var userController = new LinqHelper<User>("Users", "Id");
```

### Query Examples

```csharp
// Get all records
List<User> allUsers = userController.GetAll();

// Simple WHERE
List<User> activeUsers = userController.Where(u => u.Status == "active");

// Multiple conditions (AND)
List<User> activeAdults = userController.Where(u => u.Age > 18 && u.Status == "active");

// Multiple conditions (OR)
List<User> specificUsers = userController.Where(u => u.Username == "john" || u.Username == "jane");

// Nested conditions
List<User> complex = userController.Where(u =>
    (u.Age > 18 && u.Status == "active") ||
    u.Email.Contains("admin")
);

// First or default
User user = userController.FirstOrDefault(u => u.Id == 1);

// Single or default (throws if more than one)
User single = userController.SingleOrDefault(u => u.Email == "unique@example.com");

// Find by primary key
User found = userController.Find(5);

// Check existence
bool exists = userController.Any(u => u.Username == "john_doe");

// Count records
int count = userController.Count(u => u.Age > 18);
```

### CRUD Examples

```csharp
// INSERT (Add)
var newUser = new User
{
    Username = "alice",
    Email = "alice@example.com",
    Age = 28,
    Status = "active"
};
bool added = userController.Add(newUser);

// BULK INSERT (InsertRange)
bool bulkAdded = userController.InsertRange(new List<User>
{
    new User { Username = "bob", Email = "bob@example.com", Age = 30, Status = "active" },
    new User { Username = "carol", Email = "carol@example.com", Age = 25, Status = "pending" },
    new User { Username = "dave", Email = "dave@example.com", Age = 35, Status = "active" }
});

// UPDATE by lambda
var updatedUser = new User
{
    Username = "john_doe",
    Email = "newemail@example.com",
    Age = 31,
    Status = "active"
};
bool updated = userController.Update(updatedUser, u => u.Id == 5);

// SAVE CHANGES by primary key
var existing = userController.FirstOrDefault(u => u.Id == 5);
if (existing != null)
{
    existing.Email = "updated@example.com";
    existing.LastLogin = DateTime.Now;
    bool saved = userController.SaveChanges(existing);
}

// DELETE (Remove) by lambda
bool deleted = userController.Remove(u => u.Id == 5);
```

### String-Based Query Examples

```csharp
// String-based WHERE
var results = userController.Where("Age > @param0 AND Status = @param1", new object[] { 18, "active" });

// String-based FirstOrDefault
var user = userController.FirstOrDefault("Id = @param0", new object[] { 5 });

// String-based Count
int count = userController.Count("Status = @param0", new object[] { "active" });

// String-based Any
bool any = userController.Any("Age > @param0", new object[] { 65 });

// Get all records
var all = userController.All();
```

---

## Async LINQ with AsyncLinqHelper

`AsyncLinqHelper<TModel>` is the async counterpart to `LinqHelper<TModel>`. It uses `AsyncSqlClient`, honors query interceptors, and resolves column/table names from mapping attributes (`[Table]`, `[Column]`, `[Key]`, `[NotMapped]`).

### Setup with attribute mapping

```csharp
using DBTools.Controllers;
using DBTools.Core;
using DBTools.Mapping;

[Table("Users")]
public class User
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
}

// Direct construction
var client = new AsyncSqlClient();
var users = new AsyncLinqHelper<User>(client);

// Or with explicit table/PK (same as LinqHelper-style setup)
var usersExplicit = new AsyncLinqHelper<User>(client, "Users", "Id", autoIncrement: true);
```

### Setup with dependency injection

```csharp
using DBTools.Configuration;
using DBTools.Interceptors;
using Microsoft.Extensions.DependencyInjection;

services.AddDbTools(options =>
{
    options.Provider = DatabaseProvider.SqlServer;
    options.ConnectionString = "Server=localhost;Database=MyApp;Trusted_Connection=True;";
    options.AddInterceptor(new LoggingInterceptor());
});

// In a service
public class UserService
{
    private readonly AsyncLinqHelper<User> _users;

    public UserService(IAsyncSqlClient asyncClient)
    {
        _users = new AsyncLinqHelper<User>((AsyncSqlClient)asyncClient);
    }

    public async Task<List<User>> GetActiveAdultsAsync(CancellationToken ct = default)
    {
        return await _users.WhereAsync(u => u.Age >= 18, ct);
    }
}
```

### Async query examples

```csharp
// All records
var all = await users.AllAsync();

// Lambda WHERE
var adults = await users.WhereAsync(u => u.Age > 18);

// Combined conditions
var filtered = await users.WhereAsync(u => u.Age > 18 && u.Age < 65);

// First match
var first = await users.FirstOrDefaultAsync(u => u.Name == "Alice");

// String-based conditions (same parameter style as SqlClient)
var byStatus = await users.SelectAsync("Age > @param0", new object[] { 21 });

// Count and Any
int count = await users.CountAsync(u => u.Age > 18);
bool any = await users.AnyAsync(u => u.Email == "bob@example.com");

// Primary key lookup
var user = await users.FindAsync(42);
```

### Async mutation examples

```csharp
// Insert (PK omitted when autoIncrement is true)
bool inserted = await users.InsertAsync(new User
{
    Name = "Alice",
    Email = "alice@example.com",
    Age = 28
});

// Insert and reload by a natural key
var created = await users.InsertAndFindAsync(
    new User { Name = "Bob", Email = "bob@example.com", Age = 30 },
    u => u.Email
);

// Update by predicate
var updated = new User { Id = 1, Name = "Alice Updated", Email = "alice@example.com", Age = 29 };
await users.UpdateAsync(updated, u => u.Id == 1);

// Update by primary key on the entity
await users.SaveChangesAsync(updated);

// Delete by predicate
await users.RemoveAsync(u => u.Id == 5);

// Transactional bulk insert
await users.InsertRangeAsync(new[]
{
    new User { Name = "Carol", Email = "carol@example.com", Age = 25 },
    new User { Name = "Dave", Email = "dave@example.com", Age = 31 }
});
```

### When to use AsyncLinqHelper vs LinqHelper vs Linq

| Helper | Client | Async | JOINs / IQueryable | Interceptors | Attribute mapping |
|--------|--------|-------|--------------------|--------------|-------------------|
| `LinqHelper<T>` | `SqlClient` | No | No | No | Manual table/PK args |
| `AsyncLinqHelper<T>` | `AsyncSqlClient` | Yes | No | Yes | `[Table]` / `[Key]` / fluent |
| `Linq<T>` | `SqlClient` | No | Yes | No | Manual table/PK args |

For ASP.NET Core apps using `AddDbTools()`, prefer `AsyncLinqHelper<T>` with the injected `IAsyncSqlClient` so logging, audit, and soft-delete interceptors run on every query.

---

## Property-Based Queries with Linq

### Setup

```csharp
using DBTools.Controllers;

var userController = new Linq<User>("Users", "Id");
```

### Comparison Queries

```csharp
// Equals
var johns = userController.WhereEquals(u => u.Username, "john_doe");

// Not equals
var notInactive = userController.WhereNotEquals(u => u.Status, "inactive");

// Greater than
var adults = userController.WhereGreaterThan(u => u.Age, 18);

// Greater than or equals
var seniors = userController.WhereGreaterThanOrEquals(u => u.Age, 65);

// Less than
var minors = userController.WhereLessThan(u => u.Age, 18);

// Less than or equals
var youngAdults = userController.WhereLessThanOrEquals(u => u.Age, 25);

// Between (inclusive)
var midAge = userController.WhereBetween(u => u.Age, 25, 40);
```

### String Queries

```csharp
// Contains (LIKE %value%)
var gmailUsers = userController.WhereContains(u => u.Email, "@gmail.com");

// Starts with (LIKE value%)
var adminUsers = userController.WhereStartsWith(u => u.Username, "admin_");

// Ends with (LIKE %value)
var corporateUsers = userController.WhereEndsWith(u => u.Email, "@company.com");
```

### Collection Queries

```csharp
// IN clause
var specificUsers = userController.WhereIn(u => u.Id, new[] { 1, 2, 3, 5, 8 });

// NOT IN clause
var excludedUsers = userController.WhereNotIn(u => u.Status, new[] { "banned", "deleted" });
```

### Null Checks

```csharp
// IS NULL
var neverLoggedIn = userController.WhereIsNull(u => u.LastLogin);

// IS NOT NULL
var hasLoggedIn = userController.WhereIsNotNull(u => u.LastLogin);
```

### Example-Based Filtering

```csharp
// Create a filter template
var filter = new User
{
    Status = "active",
    Age = 30
    // Only set properties you want to filter by
};

// Find all users matching the template
var matchingUsers = userController.WhereByExample(filter);
// Generates: WHERE Status = 'active' AND Age = 30
```

### CRUD with Property Selectors

```csharp
// Insert and find (gets auto-generated values)
var newUser = new User { Username = "alice", Email = "alice@example.com", Age = 28 };
var inserted = userController.InsertAndFind(newUser, u => u.Username);

// Insert or update (upsert)
userController.InsertOrUpdate(newUser, u => u.Username);

// Get or create
var user = userController.GetOrCreate(newUser, u => u.Username);

// Update by property
userController.UpdateByProperty(user, u => u.Id, 5);

// Update by multiple properties
userController.UpdateWhere(user, u => u.Username, "john_doe", u => u.Status, "active");

// Update where IN
userController.UpdateWhereIn(user, u => u.Id, new[] { 1, 2, 3 });

// Delete by property
userController.DeleteByProperty(u => u.Id, 5);

// Delete by multiple properties
userController.DeleteWhere(u => u.Status, "inactive", u => u.Age, 100);

// Delete where IN
userController.DeleteWhereIn(u => u.Id, new[] { 10, 11, 12 });

// Check existence
bool exists = userController.Exists(u => u.Username, "john_doe");
```

---

## JOIN Queries

### Basic INNER JOIN

```csharp
using DBTools.Controllers;
using DBTools.Linq;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; }
    public DateTime OrderDate { get; set; }
}

var userController = new Linq<User>("Users", "Id");

// INNER JOIN Users with Orders
var joinResults = userController.InnerJoin<Order>(
    "Orders",
    u => u.Id,
    o => o.UserId
).ToList();

foreach (var row in joinResults)
{
    Console.WriteLine($"User: {row.Left.Username}, Order: ${row.Right.Total}");
}
```

### LEFT JOIN

```csharp
// LEFT JOIN - Right may be null for non-matching rows
var leftJoinResults = userController.LeftJoin<Order>(
    "Orders",
    u => u.Id,
    o => o.UserId
).ToList();

foreach (var row in leftJoinResults)
{
    string orderInfo = row.Right != null
        ? $"Order: ${row.Right.Total}"
        : "No orders";
    Console.WriteLine($"User: {row.Left.Username}, {orderInfo}");
}
```

### JOIN with LINQ Chaining

```csharp
// JOIN with WHERE filter
var filteredJoin = userController.InnerJoin<Order>("Orders", u => u.Id, o => o.UserId)
    .Where(j => j.Left.Age > 18 && j.Right.Status == "completed")
    .ToList();

// JOIN with ORDER BY
var orderedJoin = userController.InnerJoin<Order>("Orders", u => u.Id, o => o.UserId)
    .OrderBy(j => j.Right.Total)
    .ToList();

// JOIN with SKIP and TAKE (pagination)
var pagedJoin = userController.InnerJoin<Order>("Orders", u => u.Id, o => o.UserId)
    .Skip(10)
    .Take(5)
    .ToList();
```

---

## Deferred IQueryable Execution

The `Linq<TModel>.AsQueryable()` returns a `DbQuery<TModel>` that translates LINQ to SQL with deferred execution:

```csharp
using DBTools.Controllers;

var userController = new Linq<User>("Users", "Id");

// Build query (no SQL executed yet)
var query = userController.AsQueryable()
    .Where(u => u.Age > 18)
    .OrderBy(u => u.Username)
    .Skip(10)
    .Take(5);

// View generated SQL (without executing)
Console.WriteLine(query.ToString());
// Output: SELECT * FROM Users WHERE Age > @param0 ORDER BY Username OFFSET 10 ROWS FETCH NEXT 5 ROWS ONLY

// Execute query (SQL is sent to database)
var results = query.ToList();

foreach (var user in results)
{
    Console.WriteLine($"{user.Username} (Age: {user.Age})");
}
```

**Difference from LinqHelper.AsQueryable():**
- `LinqHelper<TModel>.AsQueryable()` - Loads ALL data into memory, then applies LINQ
- `Linq<TModel>.AsQueryable()` - Translates LINQ to SQL, executes only needed query

---

## Advanced Queries

### Aggregates

```csharp
// Count with conditions
int activeCount = userController.Count(u => u.Status == "active");

// Count all
int total = userController.Count();

// Check existence
bool hasAdmins = userController.Any(u => u.Username.Contains("admin"));
```

### Complex WHERE Clauses

```csharp
// Nested conditions
var results = userController.Where(u =>
    (u.Age > 18 && u.Status == "active") ||
    (u.Email.Contains("@admin") && u.Status != "banned")
);

// NOT operator
var notBanned = userController.Where(u => !(u.Status == "banned"));
```

### Using SqlClient for Complex Queries

```csharp
using DBTools.Core;

var db = new SqlClient();

// Aggregate functions
DataView stats = db.Select(
    "COUNT(*) AS total, AVG(age) AS avg_age, MAX(age) AS max_age FROM Users WHERE status = @param0",
    new object[] { "active" }
);

// Subqueries
DataView subquery = db.Select(
    "* FROM Users WHERE id IN (SELECT user_id FROM Orders WHERE total > @param0)",
    new object[] { 1000 }
);

// GROUP BY
DataView grouped = db.Select(
    "status, COUNT(*) AS count FROM Users GROUP BY status HAVING COUNT(*) > @param0",
    new object[] { 5 }
);
```

---

## Real-World Applications

### User Authentication System

```csharp
using DBTools.Controllers;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLogin { get; set; }
}

var userController = new LinqHelper<User>("Users", "Id");

// Register new user
public bool Register(string username, string email, string passwordHash)
{
    // Check if username exists
    if (userController.Any(u => u.Username == username))
        return false;

    return userController.Add(new User
    {
        Username = username,
        Email = email,
        PasswordHash = passwordHash,
        Role = "user",
        IsActive = true
    });
}

// Authenticate user
public User Authenticate(string username, string passwordHash)
{
    var user = userController.FirstOrDefault(u =>
        u.Username == username && u.PasswordHash == passwordHash && u.IsActive);

    if (user != null)
    {
        user.LastLogin = DateTime.Now;
        userController.SaveChanges(user);
    }

    return user;
}

// Deactivate user
public bool Deactivate(int userId)
{
    var user = userController.Find(userId);
    if (user == null) return false;

    user.IsActive = false;
    return userController.SaveChanges(user);
}
```

### E-Commerce Order Management

```csharp
using DBTools.Controllers;
using DBTools.Linq;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Category { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; }
    public DateTime OrderDate { get; set; }
}

var productController = new Linq<Product>("Products", "Id");
var orderController = new LinqHelper<Order>("Orders", "Id");

// Search products
var electronics = productController.WhereContains(p => p.Category, "Electronics");
var affordable = productController.WhereBetween(p => p.Price, 10m, 100m);
var inStock = productController.WhereGreaterThan(p => p.Stock, 0);

// Get products by category and price range
var premiumElectronics = productController
    .WhereEquals(p => p.Category, "Electronics")
    .Where(p => p.Price > 500m)
    .ToList();

// Create order
var newOrder = new Order
{
    UserId = 5,
    Total = 149.99m,
    Status = "pending",
    OrderDate = DateTime.Now
};
bool orderCreated = orderController.Add(newOrder);

// Update order status
var order = orderController.Find(1);
order.Status = "shipped";
orderController.SaveChanges(order);

// Get orders with user info using JOIN
var userController = new Linq<User>("Users", "Id");
var ordersWithUsers = userController.InnerJoin<Order>(
    "Orders",
    u => u.Id,
    o => o.UserId
).Where(j => j.Right.Status == "pending").ToList();

// Low stock alert
var lowStock = productController.WhereLessThan(p => p.Stock, 10);
```

### Inventory Management System

```csharp
using DBTools.Controllers;
using DBTools.Export;

public class InventoryItem
{
    public int Id { get; set; }
    public string Sku { get; set; }
    public string Name { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Warehouse { get; set; }
    public DateTime? LastRestocked { get; set; }
}

var inventory = new Linq<InventoryItem>("Inventory", "Id");

// Find items by SKU
var item = inventory.FirstOrDefaultByProperty(i => i.Sku, "SKU-12345");

// Low stock items
var lowStock = inventory.WhereLessThan(i => i.Quantity, 10);

// Items by warehouse
var warehouseA = inventory.WhereEquals(i => i.Warehouse, "Warehouse-A");

// Price range search
var midRange = inventory.WhereBetween(i => i.UnitPrice, 50m, 200m);

// Out of stock (null restocked date and zero quantity)
var outOfStock = inventory.WhereIsNull(i => i.LastRestocked);

// Restock item
var restockItem = inventory.Find(5);
if (restockItem != null)
{
    restockItem.Quantity += 100;
    restockItem.LastRestocked = DateTime.Now;
    inventory.SaveChanges(restockItem);
}

// Bulk update warehouse
var itemsToMove = new List<InventoryItem> { /* ... */ };
foreach (var item in itemsToMove)
{
    item.Warehouse = "Warehouse-B";
    inventory.UpdateByProperty(item, i => i.Id, item.Id);
}

// Export inventory to CSV
var allItems = inventory.GetAll();
var queryData = new SqlClient().QueryBuilder(allItems.First());
var exporter = new DataExport();
string csv = exporter.ToCsv(queryData, ',');
File.WriteAllText("inventory_report.csv", csv);
```

---

## Data Export

### Export to CSV

```csharp
using DBTools.Core;
using DBTools.Export;

var db = new SqlClient();

// Query data
DataView results = db.Select("*", "Users", "status = @param0", new object[] { "active" });

// Convert to GenericObject list
var genericObjects = new List<GenericObject>();
// ... populate from DataView or use QueryBuilder

// Export
var exporter = new DataExport();
string csv = exporter.ToCsv(
    genericObjects,
    separator: ',',
    showColums: true,
    showTypes: false
);

File.WriteAllText("export.csv", csv);
```

### CSV to DataTable

```csharp
var exporter = new DataExport();

string csvContent = File.ReadAllText("data.csv");
DataTable table = exporter.ToDataTable(csvContent, ',', specifyColumnTypes: true);

// Use the DataTable
foreach (DataRow row in table.Rows)
{
    Console.WriteLine(row[0]);
}
```

---

## Error Handling

### Basic Error Handling

```csharp
using DBTools.Core;

try
{
    var db = new SqlClient();

    bool success = db.Insert(fields, "Users", values);
    if (!success)
    {
        Console.WriteLine($"Database error: {db.Error}");
    }
}
catch (FileNotFoundException ex)
{
    Console.WriteLine("Configuration file not found: " + ex.Message);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine("Configuration error: " + ex.Message);
}
catch (ArgumentException ex)
{
    Console.WriteLine("Invalid input: " + ex.Message);
}
```

### LINQ Controller Error Handling

```csharp
using DBTools.Controllers;

var userController = new LinqHelper<User>("Users", "Id");

try
{
    var user = userController.FirstOrDefault(u => u.Id == 1);
    if (user == null)
    {
        Console.WriteLine("User not found");
        return;
    }

    user.Email = "new@example.com";
    bool saved = userController.SaveChanges(user);
    if (!saved)
    {
        Console.WriteLine($"Save failed: {userController.Error}");
    }
}
catch (InvalidOperationException ex)
{
    Console.WriteLine("Operation error: " + ex.Message);
}
```

### Validation Error Handling

```csharp
try
{
    // This will throw ArgumentException for invalid identifiers
    DataView results = db.Select("*", "Users; DROP TABLE Users--", "", new object[] { });
}
catch (ArgumentException ex)
{
    Console.WriteLine("Invalid identifier: " + ex.Message);
    // Output: "Invalid table name. Only alphanumeric characters, underscores, dots, and brackets are allowed."
}
```

---

## Dependency Injection

### Custom Configuration

```csharp
using DBTools.Core;
using DBTools.Abstractions;
using Microsoft.Extensions.Configuration;

// Build configuration from environment
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

var config = new DbConfiguration(configuration);
var validator = new SqlValidator();
var queryBuilder = new SqlQueryBuilder(validator);

var db = new SqlClient(config, validator, queryBuilder);
```

### Using Interfaces for Testability

```csharp
public class UserService
{
    private readonly ISqlClient _db;

    public UserService(ISqlClient db)
    {
        _db = db;
    }

    public DataView GetActiveUsers()
    {
        return _db.Select("*", "Users", "status = @param0", new object[] { "active" });
    }
}

// In production
var db = new SqlClient();
var service = new UserService(db);

// In tests (mock ISqlClient)
var mockDb = new Mock<ISqlClient>();
var testService = new UserService(mockDb.Object);
```

---

## Performance Optimization

### Use Specific Fields

```csharp
// BAD: Select all columns
db.Select("*", "Users", "id = @param0", new object[] { 1 });

// GOOD: Select only needed columns
db.Select("id, username, email", "Users", "id = @param0", new object[] { 1 });
```

### Use Deferred IQueryable for Large Datasets

```csharp
using DBTools.Controllers;

var userController = new Linq<User>("Users", "Id");

// GOOD: SQL-translated deferred execution (Linq<TModel>)
var pagedResults = userController.AsQueryable()
    .Where(u => u.Status == "active")
    .OrderBy(u => u.Username)
    .Skip(100)
    .Take(25)
    .ToList();

// AVOID: Loading all data into memory (LinqHelper<TModel>)
var allData = userController.AsQueryable().ToList(); // Loads ALL rows
```

### Use Bulk Insert

```csharp
// GOOD: Bulk insert
userController.InsertRange(userList);

// AVOID: Individual inserts in a loop
foreach (var user in userList)
{
    userController.Add(user); // N database roundtrips
}
```

### Use Database-Side Filtering

```csharp
// GOOD: Filter at database level
var activeAdults = userController.Where(u => u.Age > 18 && u.Status == "active");

// AVOID: Load all then filter in memory
var allUsers = userController.GetAll();
var filtered = allUsers.Where(u => u.Age > 18 && u.Status == "active"); // In-memory
```
