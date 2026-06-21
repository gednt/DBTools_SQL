<!-- generated-by: gsd-doc-writer -->
# DBTools

A multi-provider .NET 8 database access library for SQL Server, PostgreSQL, MySQL, and SQLite — with parameterized CRUD, LINQ-style queries, JOINs, upsert, and built-in SQL injection protection.

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Version](https://img.shields.io/badge/version-1.4.0-green.svg)](https://github.com/gednt/DBTools_SQL/releases)
[![License](https://img.shields.io/badge/license-MIT-orange.svg)](https://github.com/gednt/DBTools_SQL/blob/main/LICENSE)
[![CI](https://github.com/gednt/DBTools_SQL/actions/workflows/ci.yml/badge.svg)](https://github.com/gednt/DBTools_SQL/actions/workflows/ci.yml)

## Installation

### From GitHub Packages (recommended)

Releases are published to [GitHub Packages](https://github.com/gednt/DBTools_SQL/packages) on every `v*` tag.

1. Authenticate with a GitHub PAT that has `read:packages`:

```bash
dotnet nuget add source "https://nuget.pkg.github.com/gednt/index.json" \
  --name github \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_GITHUB_PAT \
  --store-password-in-clear-text
```

2. Install the package:

```bash
dotnet add package DBTools --version 1.4.0 --source github
```

Alternatively, copy `nuget.config.github-packages.example` to your solution as `nuget.config` and add credentials as described in that file.

### Optional provider packages

SQL Server support is bundled via `Microsoft.Data.SqlClient`. For other providers, add the matching package to your application:

| Provider   | Package                  |
|------------|--------------------------|
| PostgreSQL | `Npgsql`                 |
| MySQL      | `MySqlConnector`         |
| SQLite     | `Microsoft.Data.Sqlite`  |

```bash
dotnet add package DBTools --version 1.4.0 --source github
dotnet add package Npgsql   # only if using PostgreSQL
```

### From source

```bash
git clone https://github.com/gednt/DBTools_SQL.git
cd DBTools_SQL
dotnet build DBTools.sln
```

Reference `DBTools/DBTools.csproj` from your project, or pack locally:

```bash
dotnet pack DBTools/DBTools.csproj -c Release -o ./artifacts
dotnet add package DBTools --source ./artifacts
```

## Quick start

1. Install `DBTools` (see [Installation](#installation)).
2. Copy the example config and set your connection details:

```bash
cp DBTools/config.json.example config.json
```

3. Ensure `config.json` is copied to your app's output directory (Visual Studio: **Copy to Output Directory → Copy always**).
4. Query the database:

```csharp
using DBTools.Core;
using System.Data;

var db = new SqlClient();

DataView users = db.Select(
    "*",
    "Users",
    "Age > @param0",
    new object[] { 18 }
);

foreach (DataRowView row in users)
{
    Console.WriteLine($"{row["Name"]}, age {row["Age"]}");
}
```

Example `config.json`:

```json
{
  "Provider": "SqlServer",
  "Host": "localhost\\SQLEXPRESS",
  "Database": "YourDatabaseName",
  "Uid": "YourUsername",
  "Password": "YourPassword",
  "Port": "1433"
}
```

Supported `Provider` values: `SqlServer` (default), `PostgreSQL`, `MySQL`, `SQLite`.

## Usage examples

### Parameterized CRUD with `SqlClient`

```csharp
using DBTools.Core;

var db = new SqlClient();

// Insert
db.Insert(
    new[] { "Name", "Email", "Age" },
    "Users",
    new object[] { "Jane Doe", "jane@example.com", 30 }
);

// Update
db.Update(
    new[] { "Email" },
    "Users",
    new[] { "new@example.com" },
    "Id = @whereParam0",
    new object[] { 1 }
);

// Delete
db.Delete("Users", "Id = @param0", new object[] { 1 });
```

### LINQ-style queries with `LinqHelper<T>`

```csharp
using DBTools.Controllers;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}

var users = new LinqHelper<User>("Users", "Id", autoIncrement: true);

var adults = users.Where(u => u.Age > 18);
users.Add(new User { Name = "Alice", Age = 28 });
users.Remove(u => u.Id == 5);
```

For property-based filters (`WhereContains`, `WhereBetween`), JOINs, and deferred `IQueryable` execution, use `Linq<T>` instead. See [docs/EXAMPLES.md](docs/EXAMPLES.md).

### Async LINQ with `AsyncLinqHelper<T>`

For async CRUD with lambda predicates, attribute-based table mapping, and query interceptors, use `AsyncLinqHelper<T>` on top of `AsyncSqlClient`:

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
    public int Age { get; set; }
}

var client = new AsyncSqlClient();
var users = new AsyncLinqHelper<User>(client);

var adults = await users.WhereAsync(u => u.Age > 18);
await users.InsertAsync(new User { Name = "Alice", Age = 28 });
await users.RemoveAsync(u => u.Id == 5);
```

With DI, inject `IAsyncSqlClient` and pass it to the constructor. See [docs/API_REFERENCE.md](docs/API_REFERENCE.md#asynclinqhelper-class) and [docs/EXAMPLES.md](docs/EXAMPLES.md#async-linq-with-asynclinqhelper).

### Dependency injection

```csharp
using DBTools.Configuration;

services.AddDbTools(options =>
{
    options.Provider = DatabaseProvider.PostgreSQL;
    options.Host = "localhost";
    options.Port = "5432";
    options.Database = "myappdb";
    options.Username = "postgres";
    options.Password = "secret";
});

// Inject IAsyncSqlClient or SqlClient in your services
```

## Documentation

- [Quick Start Guide](docs/QUICKSTART.md) — step-by-step tutorial
- [Examples](docs/EXAMPLES.md) — comprehensive code samples
- [API Reference](docs/API_REFERENCE.md) — method-level documentation
- [Security](docs/SECURITY.md) — SQL injection prevention and best practices

## License

This project is licensed under the MIT License (see `PackageLicenseExpression` in `DBTools/DBTools.csproj`).
