# DBTools_SQL Examples

Comprehensive code examples for DBTools_SQL library.

## Table of Contents

1. [Basic Operations](#basic-operations)
2. [Advanced Queries](#advanced-queries)
3. [Real-World Applications](#real-world-applications)
4. [Data Export](#data-export)
5. [Error Handling](#error-handling)
6. [Performance Optimization](#performance-optimization)

---

## Basic Operations

### Example 1: Simple SELECT

```csharp
using DBTools_Utilities;
using System;
using System.Data;

class Program
{
    static void Main()
    {
        var utils = new Utils();
        
        // Get all active users
        DataView users = utils.Select(
            "id, username, email, created_date",
            "Users",
            "status = @param0",
            new object[] { "active" }
        );
        
        // Display results
        foreach (DataRowView row in users)
        {
            Console.WriteLine($"User: {row["username"]}, Email: {row["email"]}");
        }
    }
}
```

### Example 2: INSERT with QueryBuilder

```csharp
using DBTools_Utilities;
using System;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public DateTime CreatedDate { get; set; }
}

class Program
{
    static void Main()
    {
        var utils = new Utils();
        
        // Create a new user
        var newUser = new User
        {
            Username = "john_doe",
            Email = "john@example.com",
            PasswordHash = HashPassword("SecurePass123!"),
            CreatedDate = DateTime.Now
        };
        
        // Use QueryBuilder to convert object to database format
        var queryData = utils.QueryBuilder(newUser, "Id", autoIncrement: true);
        
        // Insert into database
        bool success = utils.Insert(
            queryData[0].columns,
            "Users",
            queryData[0].values,
            "Id",
            true
        );
        
        if (success)
        {
            Console.WriteLine("User created successfully!");
        }
        else
        {
            Console.WriteLine($"Error: {utils.Error}");
        }
    }
    
    static string HashPassword(string password)
    {
        // Use BCrypt or similar in production
        return Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(password)
        );
    }
}
```

### Example 3: UPDATE with Parameterized WHERE

```csharp
using DBTools_Utilities;
using System;

class Program
{
    static void Main()
    {
        var utils = new Utils();
        
        int userId = 5;
        string newEmail = "newemail@example.com";
        
        // Update user email
        string[] fields = { "email", "updated_date" };
        string[] values = { 
            newEmail, 
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") 
        };
        
        bool success = utils.Update(
            fields,
            "Users",
            values,
            "id = @whereParam0",
            new object[] { userId }
        );
        
        if (success)
        {
            Console.WriteLine("Email updated successfully!");
        }
    }
}
```

### Example 4: DELETE with Multiple Conditions

```csharp
using DBTools_Utilities;
using System;

class Program
{
    static void Main()
    {
        var utils = new Utils();
        
        // Delete inactive users older than 1 year
        DateTime cutoffDate = DateTime.Now.AddYears(-1);
        
        bool success = utils.Delete(
            "Users",
            "status = @param0 AND last_login < @param1",
            new object[] { "inactive", cutoffDate }
        );
        
        if (success)
        {
            Console.WriteLine("Inactive users deleted successfully!");
        }
    }
}
```

---

## Advanced Queries

### Example 5: JOIN Query

```csharp
using DBTools_Utilities;
using System;
using System.Data;

class Program
{
    static void Main()
    {
        var utils = new Utils();
        
        // Query with JOIN
        string query = @"
            u.id AS UserId,
            u.username AS Username,
            o.id AS OrderId,
            o.order_date AS OrderDate,
            o.total AS Total
            FROM Users u
            INNER JOIN Orders o ON u.id = o.user_id
            WHERE u.id = @param0
            ORDER BY o.order_date DESC";
        
        int userId = 10;
        
        DataView results = utils.Select(query, new object[] { userId });
        
        // Display results
        Console.WriteLine($"Orders for User ID {userId}:");
        foreach (DataRowView row in results)
        {
            Console.WriteLine($"Order #{row["OrderId"]} - " +
                            $"Date: {row["OrderDate"]} - " +
                            $"Total: ${row["Total"]}");
        }
    }
}
```

### Example 6: Aggregate Functions

```csharp
using DBTools_Utilities;
using System;
using System.Data;

class Program
{
    static void Main()
    {
        var utils = new Utils();
        
        // Get user statistics
        string query = @"
            status,
            COUNT(*) AS UserCount,
            AVG(CAST(age AS FLOAT)) AS AverageAge,
            MIN(created_date) AS FirstRegistration,
            MAX(created_date) AS LastRegistration
            FROM Users
            GROUP BY status";
        
        DataView stats = utils.Select(query, new object[] { });
        
        // Display statistics
        foreach (DataRowView row in stats)
        {
            Console.WriteLine($"\nStatus: {row["status"]}");
            Console.WriteLine($"  Users: {row["UserCount"]}");
            Console.WriteLine($"  Avg Age: {row["AverageAge"]:F1}");
            Console.WriteLine($"  First: {row["FirstRegistration"]:d}");
            Console.WriteLine($"  Last: {row["LastRegistration"]:d}");
        }
    }
}
```

### Example 7: Subquery

```csharp
using DBTools_Utilities;
using System;
using System.Data;

class Program
{
    static void Main()
    {
        var utils = new Utils();
        
        // Find users with above-average order totals
        string query = @"
            u.username,
            u.email,
            SUM(o.total) AS TotalSpent
            FROM Users u
            INNER JOIN Orders o ON u.id = o.user_id
            GROUP BY u.username, u.email
            HAVING SUM(o.total) > (
                SELECT AVG(total) FROM Orders
            )
            ORDER BY TotalSpent DESC";
        
        DataView results = utils.Select(query, new object[] { });
        
        Console.WriteLine("High-Value Customers:");
        foreach (DataRowView row in results)
        {
            Console.WriteLine($"{row["username"]}: ${row["TotalSpent"]:F2}");
        }
    }
}
```

---

## Real-World Applications

### Example 8: User Authentication System

```csharp
using DBTools_Utilities;
using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;

public class AuthenticationManager
{
    private Utils utils;
    
    public AuthenticationManager()
    {
        utils = new Utils();
    }
    
    public bool RegisterUser(string username, string email, string password)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
                throw new ArgumentException("Username must be at least 3 characters");
            
            if (!IsValidEmail(email))
                throw new ArgumentException("Invalid email address");
            
            if (password.Length < 8)
                throw new ArgumentException("Password must be at least 8 characters");
            
            // Check if username exists
            var existing = utils.Select(
                "id",
                "Users",
                "username = @param0",
                new object[] { username }
            );
            
            if (existing.Count > 0)
                throw new InvalidOperationException("Username already exists");
            
            // Hash password
            string passwordHash = HashPassword(password);
            
            // Insert user
            string[] fields = { "username", "email", "password_hash", "created_date", "status" };
            object[] values = { username, email, passwordHash, DateTime.Now, "active" };
            
            bool success = utils.Insert(fields, "Users", values, "id", true);
            
            if (success)
            {
                Console.WriteLine($"User '{username}' registered successfully!");
                return true;
            }
            else
            {
                Console.WriteLine($"Registration failed: {utils.Error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }
    
    public bool Login(string username, string password)
    {
        try
        {
            // Get user from database
            var user = utils.Select(
                "id, username, password_hash, status",
                "Users",
                "username = @param0",
                new object[] { username }
            );
            
            if (user.Count == 0)
            {
                Console.WriteLine("Invalid username or password");
                return false;
            }
            
            string storedHash = user[0]["password_hash"].ToString();
            string status = user[0]["status"].ToString();
            
            // Check if account is active
            if (status != "active")
            {
                Console.WriteLine("Account is not active");
                return false;
            }
            
            // Verify password
            if (VerifyPassword(password, storedHash))
            {
                // Update last login
                int userId = Convert.ToInt32(user[0]["id"]);
                UpdateLastLogin(userId);
                
                Console.WriteLine($"Welcome, {username}!");
                return true;
            }
            else
            {
                Console.WriteLine("Invalid username or password");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
            return false;
        }
    }
    
    public bool ChangePassword(int userId, string oldPassword, string newPassword)
    {
        try
        {
            // Get current password hash
            var user = utils.Select(
                "password_hash",
                "Users",
                "id = @param0",
                new object[] { userId }
            );
            
            if (user.Count == 0)
                throw new InvalidOperationException("User not found");
            
            string currentHash = user[0]["password_hash"].ToString();
            
            // Verify old password
            if (!VerifyPassword(oldPassword, currentHash))
            {
                Console.WriteLine("Current password is incorrect");
                return false;
            }
            
            // Validate new password
            if (newPassword.Length < 8)
                throw new ArgumentException("New password must be at least 8 characters");
            
            // Update password
            string newHash = HashPassword(newPassword);
            string[] fields = { "password_hash", "updated_date" };
            string[] values = { newHash, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
            
            bool success = utils.Update(
                fields,
                "Users",
                values,
                "id = @whereParam0",
                new object[] { userId }
            );
            
            if (success)
            {
                Console.WriteLine("Password changed successfully!");
                return true;
            }
            else
            {
                Console.WriteLine($"Password change failed: {utils.Error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }
    
    private void UpdateLastLogin(int userId)
    {
        string[] fields = { "last_login" };
        string[] values = { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
        
        utils.Update(
            fields,
            "Users",
            values,
            "id = @whereParam0",
            new object[] { userId }
        );
    }
    
    private string HashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
    
    private bool VerifyPassword(string password, string hash)
    {
        string computedHash = HashPassword(password);
        return computedHash == hash;
    }
    
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

// Usage
class Program
{
    static void Main()
    {
        var auth = new AuthenticationManager();
        
        // Register
        auth.RegisterUser("john_doe", "john@example.com", "SecurePass123!");
        
        // Login
        bool loggedIn = auth.Login("john_doe", "SecurePass123!");
        
        if (loggedIn)
        {
            // Change password
            auth.ChangePassword(1, "SecurePass123!", "NewSecurePass456!");
        }
    }
}
```

### Example 9: E-Commerce Order Management

```csharp
using DBTools_Utilities;
using System;
using System.Collections.Generic;
using System.Data;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; }
    public List<OrderItem> Items { get; set; }
}

public class OrderItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class OrderManager
{
    private Utils utils;
    
    public OrderManager()
    {
        utils = new Utils();
    }
    
    public int CreateOrder(int userId, List<OrderItem> items)
    {
        try
        {
            // Calculate total
            decimal total = 0;
            foreach (var item in items)
            {
                total += item.Price * item.Quantity;
            }
            
            // Insert order
            string[] orderFields = { "user_id", "order_date", "total", "status" };
            object[] orderValues = { userId, DateTime.Now, total, "pending" };
            
            bool orderSuccess = utils.Insert(orderFields, "Orders", orderValues, "id", true);
            
            if (!orderSuccess)
            {
                throw new Exception("Failed to create order");
            }
            
            // Get the order ID (assuming IDENTITY column)
            var lastOrder = utils.Select(
                "TOP 1 id",
                "Orders",
                "user_id = @param0",
                new object[] { userId }
            );
            
            if (lastOrder.Count == 0)
            {
                throw new Exception("Failed to retrieve order ID");
            }
            
            int orderId = Convert.ToInt32(lastOrder[0]["id"]);
            
            // Insert order items
            foreach (var item in items)
            {
                string[] itemFields = { "order_id", "product_id", "quantity", "price" };
                object[] itemValues = { orderId, item.ProductId, item.Quantity, item.Price };
                
                bool itemSuccess = utils.Insert(itemFields, "OrderItems", itemValues, "id", true);
                
                if (!itemSuccess)
                {
                    // In production, implement transaction rollback
                    Console.WriteLine($"Warning: Failed to add item {item.ProductName}");
                }
            }
            
            Console.WriteLine($"Order #{orderId} created successfully! Total: ${total:F2}");
            return orderId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating order: {ex.Message}");
            return -1;
        }
    }
    
    public Order GetOrderDetails(int orderId)
    {
        try
        {
            // Get order information
            var orderData = utils.Select(
                "id, user_id, order_date, total, status",
                "Orders",
                "id = @param0",
                new object[] { orderId }
            );
            
            if (orderData.Count == 0)
            {
                Console.WriteLine("Order not found");
                return null;
            }
            
            var order = new Order
            {
                Id = Convert.ToInt32(orderData[0]["id"]),
                UserId = Convert.ToInt32(orderData[0]["user_id"]),
                OrderDate = Convert.ToDateTime(orderData[0]["order_date"]),
                Total = Convert.ToDecimal(orderData[0]["total"]),
                Status = orderData[0]["status"].ToString(),
                Items = new List<OrderItem>()
            };
            
            // Get order items
            string itemsQuery = @"
                oi.product_id,
                p.name AS product_name,
                oi.quantity,
                oi.price
                FROM OrderItems oi
                INNER JOIN Products p ON oi.product_id = p.id
                WHERE oi.order_id = @param0";
            
            var itemsData = utils.Select(itemsQuery, new object[] { orderId });
            
            foreach (DataRowView row in itemsData)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = Convert.ToInt32(row["product_id"]),
                    ProductName = row["product_name"].ToString(),
                    Quantity = Convert.ToInt32(row["quantity"]),
                    Price = Convert.ToDecimal(row["price"])
                });
            }
            
            return order;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving order: {ex.Message}");
            return null;
        }
    }
    
    public bool UpdateOrderStatus(int orderId, string newStatus)
    {
        try
        {
            string[] fields = { "status", "updated_date" };
            string[] values = { newStatus, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
            
            bool success = utils.Update(
                fields,
                "Orders",
                values,
                "id = @whereParam0",
                new object[] { orderId }
            );
            
            if (success)
            {
                Console.WriteLine($"Order #{orderId} status updated to '{newStatus}'");
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to update order status: {utils.Error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }
    
    public List<Order> GetUserOrders(int userId)
    {
        try
        {
            var orders = new List<Order>();
            
            var orderData = utils.Select(
                "id, user_id, order_date, total, status",
                "Orders",
                "user_id = @param0",
                new object[] { userId }
            );
            
            foreach (DataRowView row in orderData)
            {
                orders.Add(new Order
                {
                    Id = Convert.ToInt32(row["id"]),
                    UserId = Convert.ToInt32(row["user_id"]),
                    OrderDate = Convert.ToDateTime(row["order_date"]),
                    Total = Convert.ToDecimal(row["total"]),
                    Status = row["status"].ToString()
                });
            }
            
            return orders;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving user orders: {ex.Message}");
            return new List<Order>();
        }
    }
}

// Usage
class Program
{
    static void Main()
    {
        var orderManager = new OrderManager();
        
        // Create an order
        var items = new List<OrderItem>
        {
            new OrderItem { ProductId = 1, ProductName = "Laptop", Quantity = 1, Price = 999.99m },
            new OrderItem { ProductId = 2, ProductName = "Mouse", Quantity = 2, Price = 29.99m }
        };
        
        int orderId = orderManager.CreateOrder(userId: 5, items: items);
        
        if (orderId > 0)
        {
            // Get order details
            Order order = orderManager.GetOrderDetails(orderId);
            
            if (order != null)
            {
                Console.WriteLine($"\nOrder #{order.Id}");
                Console.WriteLine($"Date: {order.OrderDate}");
                Console.WriteLine($"Total: ${order.Total:F2}");
                Console.WriteLine($"Status: {order.Status}");
                Console.WriteLine("\nItems:");
                foreach (var item in order.Items)
                {
                    Console.WriteLine($"  {item.ProductName} x{item.Quantity} @ ${item.Price:F2}");
                }
            }
            
            // Update status
            orderManager.UpdateOrderStatus(orderId, "shipped");
        }
    }
}
```

### Example 10: Inventory Management System

```csharp
using DBTools_Utilities;
using System;
using System.Data;

public class InventoryManager
{
    private Utils utils;
    
    public InventoryManager()
    {
        utils = new Utils();
    }
    
    public bool AddProduct(string name, string sku, decimal price, int quantity, string category)
    {
        try
        {
            // Check if SKU already exists
            var existing = utils.Select(
                "id",
                "Products",
                "sku = @param0",
                new object[] { sku }
            );
            
            if (existing.Count > 0)
            {
                Console.WriteLine($"Product with SKU '{sku}' already exists");
                return false;
            }
            
            // Insert product
            string[] fields = { "name", "sku", "price", "quantity", "category", "created_date" };
            object[] values = { name, sku, price, quantity, category, DateTime.Now };
            
            bool success = utils.Insert(fields, "Products", values, "id", true);
            
            if (success)
            {
                Console.WriteLine($"Product '{name}' added successfully!");
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to add product: {utils.Error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }
    
    public bool UpdateStock(string sku, int quantityChange)
    {
        try
        {
            // Get current quantity
            var product = utils.Select(
                "id, quantity, name",
                "Products",
                "sku = @param0",
                new object[] { sku }
            );
            
            if (product.Count == 0)
            {
                Console.WriteLine($"Product with SKU '{sku}' not found");
                return false;
            }
            
            int productId = Convert.ToInt32(product[0]["id"]);
            int currentQuantity = Convert.ToInt32(product[0]["quantity"]);
            string productName = product[0]["name"].ToString();
            
            int newQuantity = currentQuantity + quantityChange;
            
            if (newQuantity < 0)
            {
                Console.WriteLine($"Insufficient stock. Current: {currentQuantity}, Requested: {Math.Abs(quantityChange)}");
                return false;
            }
            
            // Update quantity
            string[] fields = { "quantity", "updated_date" };
            string[] values = { newQuantity.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
            
            bool success = utils.Update(
                fields,
                "Products",
                values,
                "id = @whereParam0",
                new object[] { productId }
            );
            
            if (success)
            {
                Console.WriteLine($"Stock updated for '{productName}': {currentQuantity} ? {newQuantity}");
                
                // Check for low stock
                if (newQuantity < 10)
                {
                    Console.WriteLine($"?? WARNING: Low stock for '{productName}' ({newQuantity} units)");
                }
                
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to update stock: {utils.Error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }
    
    public DataView GetLowStockProducts(int threshold = 10)
    {
        try
        {
            return utils.Select(
                "id, name, sku, quantity, price, category",
                "Products",
                "quantity < @param0",
                new object[] { threshold }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }
    
    public DataView GetInventoryReport()
    {
        try
        {
            string query = @"
                category,
                COUNT(*) AS ProductCount,
                SUM(quantity) AS TotalQuantity,
                SUM(quantity * price) AS TotalValue,
                AVG(price) AS AvgPrice
                FROM Products
                GROUP BY category
                ORDER BY TotalValue DESC";
            
            return utils.Select(query, new object[] { });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }
}

// Usage
class Program
{
    static void Main()
    {
        var inventory = new InventoryManager();
        
        // Add products
        inventory.AddProduct("Laptop Pro", "LP-001", 1299.99m, 50, "Electronics");
        inventory.AddProduct("Wireless Mouse", "WM-001", 29.99m, 200, "Accessories");
        inventory.AddProduct("USB-C Cable", "UC-001", 12.99m, 500, "Accessories");
        
        // Update stock (simulate sales)
        inventory.UpdateStock("LP-001", -5);  // Sold 5 laptops
        inventory.UpdateStock("WM-001", -20); // Sold 20 mice
        
        // Check low stock
        Console.WriteLine("\n=== Low Stock Products ===");
        var lowStock = inventory.GetLowStockProducts(10);
        if (lowStock != null && lowStock.Count > 0)
        {
            foreach (DataRowView row in lowStock)
            {
                Console.WriteLine($"{row["name"]} (SKU: {row["sku"]}): {row["quantity"]} units");
            }
        }
        else
        {
            Console.WriteLine("All products have sufficient stock");
        }
        
        // Generate inventory report
        Console.WriteLine("\n=== Inventory Report by Category ===");
        var report = inventory.GetInventoryReport();
        if (report != null)
        {
            foreach (DataRowView row in report)
            {
                Console.WriteLine($"\nCategory: {row["category"]}");
                Console.WriteLine($"  Products: {row["ProductCount"]}");
                Console.WriteLine($"  Total Units: {row["TotalQuantity"]}");
                Console.WriteLine($"  Total Value: ${row["TotalValue"]:F2}");
                Console.WriteLine($"  Avg Price: ${row["AvgPrice"]:F2}");
            }
        }
    }
}
```

---

## Data Export

### Example 11: Export to CSV

```csharp
using DBTools_Utilities;
using System;
using System.Data;
using System.IO;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var utils = new Utils();
        var exporter = new DataExport();
        
        // Get data to export
        DataView users = utils.Select("*", "Users", "status = @param0", new object[] { "active" });
        
        // Convert DataView to GenericObject list
        var genericObjects = ConvertDataViewToGenericObject(users);
        
        // Export to CSV with all options
        string csv = exporter.ToCsv(
            genericObject: genericObjects,
            separator: ',',
            showColums: true,
            showTypes: true
        );
        
        // Save to file
        string filename = $"users_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        File.WriteAllText(filename, csv);
        
        Console.WriteLine($"Data exported to {filename}");
    }
    
    static List<DBTools.Model.GenericObject> ConvertDataViewToGenericObject(DataView dv)
    {
        var list = new List<DBTools.Model.GenericObject>();
        
        if (dv == null || dv.Count == 0)
            return list;
        
        foreach (DataRowView row in dv)
        {
            var columns = new List<string>();
            var values = new List<object>();
            var types = new List<string>();
            var valuesString = new List<string>();
            
            foreach (DataColumn column in dv.Table.Columns)
            {
                columns.Add(column.ColumnName);
                values.Add(row[column.ColumnName]);
                types.Add(column.DataType.Name);
                valuesString.Add(row[column.ColumnName]?.ToString() ?? "");
            }
            
            list.Add(new DBTools.Model.GenericObject
            {
                columns = columns.ToArray(),
                values = values.ToArray(),
                types = types.ToArray(),
                valuesString = valuesString.ToArray()
            });
        }
        
        return list;
    }
}
```

---

## Error Handling

### Example 12: Comprehensive Error Handling

```csharp
using DBTools_Utilities;
using System;
using System.Data;
using System.IO;

public class SafeDatabaseOperations
{
    private Utils utils;
    private string logFile = "database_errors.log";
    
    public SafeDatabaseOperations()
    {
        try
        {
            utils = new Utils();
        }
        catch (FileNotFoundException ex)
        {
            LogError("Configuration file not found", ex);
            Console.WriteLine("ERROR: config.json not found. Please create the configuration file.");
            throw;
        }
        catch (InvalidOperationException ex)
        {
            LogError("Invalid configuration", ex);
            Console.WriteLine("ERROR: Invalid database configuration. Check config.json.");
            throw;
        }
    }
    
    public DataView SafeSelect(string fields, string table, string where, object[] parameters)
    {
        try
        {
            return utils.Select(fields, table, where, parameters);
        }
        catch (ArgumentException ex)
        {
            LogError($"Invalid input for SELECT on table '{table}'", ex);
            Console.WriteLine($"ERROR: {ex.Message}");
            return null;
        }
        catch (System.Data.SqlClient.SqlException ex)
        {
            LogError($"Database error during SELECT on table '{table}'", ex);
            Console.WriteLine("ERROR: Database connection or query error. Please try again later.");
            return null;
        }
        catch (Exception ex)
        {
            LogError($"Unexpected error during SELECT on table '{table}'", ex);
            Console.WriteLine("ERROR: An unexpected error occurred.");
            return null;
        }
    }
    
    public bool SafeInsert(string[] fields, string table, object[] values)
    {
        try
        {
            bool success = utils.Insert(fields, table, values);
            
            if (!success)
            {
                LogError($"Insert failed on table '{table}'", new Exception(utils.Error));
                Console.WriteLine($"ERROR: Failed to insert data: {utils.Error}");
            }
            
            return success;
        }
        catch (ArgumentException ex)
        {
            LogError($"Invalid input for INSERT on table '{table}'", ex);
            Console.WriteLine($"ERROR: {ex.Message}");
            return false;
        }
        catch (System.Data.SqlClient.SqlException ex)
        {
            LogError($"Database error during INSERT on table '{table}'", ex);
            
            if (ex.Number == 2627) // Duplicate key
            {
                Console.WriteLine("ERROR: A record with this key already exists.");
            }
            else if (ex.Number == 547) // Foreign key violation
            {
                Console.WriteLine("ERROR: Invalid reference to related data.");
            }
            else
            {
                Console.WriteLine("ERROR: Database error occurred.");
            }
            
            return false;
        }
        catch (Exception ex)
        {
            LogError($"Unexpected error during INSERT on table '{table}'", ex);
            Console.WriteLine("ERROR: An unexpected error occurred.");
            return false;
        }
    }
    
    public bool SafeUpdate(string[] fields, string table, string[] values, string where, object[] whereParams)
    {
        try
        {
            // Validate that WHERE clause is provided
            if (string.IsNullOrWhiteSpace(where))
            {
                throw new ArgumentException("UPDATE requires a WHERE clause for safety");
            }
            
            bool success = utils.Update(fields, table, values, where, whereParams);
            
            if (!success)
            {
                LogError($"Update failed on table '{table}'", new Exception(utils.Error));
                Console.WriteLine($"ERROR: Failed to update data: {utils.Error}");
            }
            
            return success;
        }
        catch (ArgumentException ex)
        {
            LogError($"Invalid input for UPDATE on table '{table}'", ex);
            Console.WriteLine($"ERROR: {ex.Message}");
            return false;
        }
        catch (System.Data.SqlClient.SqlException ex)
        {
            LogError($"Database error during UPDATE on table '{table}'", ex);
            Console.WriteLine("ERROR: Database error occurred during update.");
            return false;
        }
        catch (Exception ex)
        {
            LogError($"Unexpected error during UPDATE on table '{table}'", ex);
            Console.WriteLine("ERROR: An unexpected error occurred.");
            return false;
        }
    }
    
    public bool SafeDelete(string table, string where, object[] parameters)
    {
        try
        {
            // Confirm deletion (in real app, this would be more sophisticated)
            Console.WriteLine($"WARNING: About to delete from '{table}' WHERE {where}");
            Console.Write("Are you sure? (yes/no): ");
            string confirmation = Console.ReadLine();
            
            if (confirmation?.ToLower() != "yes")
            {
                Console.WriteLine("Delete operation cancelled.");
                return false;
            }
            
            bool success = utils.Delete(table, where, parameters);
            
            if (!success)
            {
                LogError($"Delete failed on table '{table}'", new Exception(utils.Error));
                Console.WriteLine($"ERROR: Failed to delete data: {utils.Error}");
            }
            
            return success;
        }
        catch (ArgumentException ex)
        {
            LogError($"Invalid input for DELETE on table '{table}'", ex);
            Console.WriteLine($"ERROR: {ex.Message}");
            return false;
        }
        catch (System.Data.SqlClient.SqlException ex)
        {
            LogError($"Database error during DELETE on table '{table}'", ex);
            
            if (ex.Number == 547) // Foreign key violation
            {
                Console.WriteLine("ERROR: Cannot delete - this record is referenced by other data.");
            }
            else
            {
                Console.WriteLine("ERROR: Database error occurred during delete.");
            }
            
            return false;
        }
        catch (Exception ex)
        {
            LogError($"Unexpected error during DELETE on table '{table}'", ex);
            Console.WriteLine("ERROR: An unexpected error occurred.");
            return false;
        }
    }
    
    private void LogError(string message, Exception ex)
    {
        try
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n" +
                            $"Exception: {ex.GetType().Name}\n" +
                            $"Message: {ex.Message}\n" +
                            $"Stack Trace: {ex.StackTrace}\n" +
                            new string('-', 80) + "\n";
            
            File.AppendAllText(logFile, logEntry);
        }
        catch
        {
            // If logging fails, at least try to write to console
            Console.WriteLine($"Failed to write to log file: {message}");
        }
    }
}

// Usage
class Program
{
    static void Main()
    {
        var safeDb = new SafeDatabaseOperations();
        
        // Safe SELECT
        var users = safeDb.SafeSelect("*", "Users", "id = @param0", new object[] { 1 });
        if (users != null)
        {
            Console.WriteLine($"Found {users.Count} user(s)");
        }
        
        // Safe INSERT
        string[] fields = { "username", "email" };
        object[] values = { "test_user", "test@example.com" };
        bool inserted = safeDb.SafeInsert(fields, "Users", values);
        
        // Safe UPDATE
        if (inserted)
        {
            string[] updateFields = { "email" };
            string[] updateValues = { "newemail@example.com" };
            safeDb.SafeUpdate(updateFields, "Users", updateValues, "username = @whereParam0", new object[] { "test_user" });
        }
        
        // Safe DELETE
        safeDb.SafeDelete("Users", "username = @param0", new object[] { "test_user" });
    }
}
```

---

## Performance Optimization

### Example 13: Batch Operations

```csharp
using DBTools_Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class BatchOperations
{
    private Utils utils;
    
    public BatchOperations()
    {
        utils = new Utils();
    }
    
    public void BulkInsertUsers(List<User> users)
    {
        var stopwatch = Stopwatch.StartNew();
        int successCount = 0;
        int failCount = 0;
        
        Console.WriteLine($"Starting bulk insert of {users.Count} users...");
        
        foreach (var user in users)
        {
            try
            {
                var queryData = utils.QueryBuilder(user, "Id", true);
                bool success = utils.Insert(
                    queryData[0].columns,
                    "Users",
                    queryData[0].values,
                    "Id",
                    true
                );
                
                if (success)
                    successCount++;
                else
                    failCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to insert user {user.Username}: {ex.Message}");
                failCount++;
            }
        }
        
        stopwatch.Stop();
        
        Console.WriteLine($"\nBulk insert completed in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Success: {successCount}, Failed: {failCount}");
        Console.WriteLine($"Average: {stopwatch.ElapsedMilliseconds / users.Count}ms per record");
    }
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public DateTime CreatedDate { get; set; }
}

// Usage
class Program
{
    static void Main()
    {
        var batch = new BatchOperations();
        
        // Generate test data
        var users = new List<User>();
        for (int i = 0; i < 100; i++)
        {
            users.Add(new User
            {
                Username = $"user_{i}",
                Email = $"user{i}@example.com",
                CreatedDate = DateTime.Now
            });
        }
        
        // Bulk insert
        batch.BulkInsertUsers(users);
    }
}
```

---

**Last Updated**: 2024
