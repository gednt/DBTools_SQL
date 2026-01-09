using System;
using System.Data;
using DBTools_Utilities;

namespace DBTools.Examples
{
    /// <summary>
    /// Examples demonstrating the use of parameterized queries in DBTools
    /// </summary>
    public class ParameterizedQueryExamples
    {
        private Utils _utils;

        public ParameterizedQueryExamples(string host, string database, string uid, string password, string port)
        {
            _utils = new Utils(host, database, uid, password, port);
        }

        /// <summary>
        /// Example: Select with single parameter
        /// </summary>
        public void SelectSingleParameter()
        {
            // Secure: User input is passed as parameter
            string userEmail = "user@example.com";
            DataView result = _utils.Select(
                "*", 
                "Users", 
                "email = @param0",
                new object[] { userEmail }
            );

            Console.WriteLine($"Found {result.Count} user(s) with email: {userEmail}");
        }

        /// <summary>
        /// Example: Select with multiple parameters
        /// </summary>
        public void SelectMultipleParameters()
        {
            // Secure: Multiple conditions with parameters
            string status = "active";
            DateTime dateThreshold = DateTime.Now.AddDays(-30);

            DataView result = _utils.Select(
                "id, username, email, created_date",
                "Users",
                "status = @param0 AND created_date > @param1",
                new object[] { status, dateThreshold }
            );

            Console.WriteLine($"Found {result.Count} active users created in the last 30 days");
        }

        /// <summary>
        /// Example: Select all records (no WHERE clause)
        /// </summary>
        public void SelectAllRecords()
        {
            // When no filtering needed, use empty string and empty array
            DataView result = _utils.Select(
                "*",
                "Products",
                "",
                new object[] { }
            );

            Console.WriteLine($"Total products: {result.Count}");
        }

        /// <summary>
        /// Example: Update with parameterized WHERE clause
        /// </summary>
        public void UpdateWithParameters()
        {
            // Secure: Both SET values and WHERE clause use parameters
            string newStatus = "completed";
            DateTime processedDate = DateTime.Now;
            int orderId = 12345;
            
            bool success = _utils.Update(
                new string[] { "status", "processed_date" },
                "Orders",
                new string[] { newStatus, processedDate.ToString("yyyy-MM-dd HH:mm:ss") },
                "order_id = @whereParam0",
                new object[] { orderId }
            );

            Console.WriteLine($"Update {(success ? "successful" : "failed")}");
        }

        /// <summary>
        /// Example: Update with multiple WHERE conditions
        /// </summary>
        public void UpdateMultipleConditions()
        {
            string newQuantity = "100";
            int productId = 567;
            int warehouseId = 3;

            bool success = _utils.Update(
                new string[] { "quantity" },
                "Inventory",
                new string[] { newQuantity },
                "product_id = @whereParam0 AND warehouse_id = @whereParam1",
                new object[] { productId, warehouseId }
            );

            Console.WriteLine($"Inventory update {(success ? "successful" : "failed")}");
        }

        /// <summary>
        /// Example: Delete with single parameter
        /// </summary>
        public void DeleteSingleParameter()
        {
            // Secure: Parameter prevents SQL injection
            int sessionId = 789;

            bool success = _utils.Delete(
                "Sessions",
                "session_id = @param0",
                new object[] { sessionId }
            );

            Console.WriteLine($"Delete {(success ? "successful" : "failed")}");
        }

        /// <summary>
        /// Example: Delete with multiple parameters
        /// </summary>
        public void DeleteMultipleParameters()
        {
            // Delete expired sessions for a specific user
            int userId = 101;
            DateTime expirationDate = DateTime.Now;

            bool success = _utils.Delete(
                "Sessions",
                "user_id = @param0 AND expires_at < @param1",
                new object[] { userId, expirationDate }
            );

            Console.WriteLine($"Cleanup {(success ? "successful" : "failed")}");
        }

        /// <summary>
        /// Example: Custom query with parameters
        /// </summary>
        public void CustomQueryWithParameters()
        {
            // Using the Select overload that accepts partial queries
            string searchTerm = "%electronics%";
            decimal minPrice = 100.00m;

            DataView result = _utils.Select(
                "p.id, p.name, p.price, c.category_name " +
                "FROM Products p " +
                "JOIN Categories c ON p.category_id = c.id " +
                "WHERE p.name LIKE @param0 AND p.price > @param1",
                new object[] { searchTerm, minPrice }
            );

            Console.WriteLine($"Found {result.Count} matching products");
        }

        /// <summary>
        /// Example: Handling NULL values
        /// </summary>
        public void HandlingNullValues()
        {
            // NULL values are handled safely by the parameterized query
            string userId = "U123";
            string optionalNote = null; // This will be converted to DBNull.Value

            bool success = _utils.Update(
                new string[] { "last_login", "login_note" },
                "Users",
                new string[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), optionalNote },
                "user_id = @whereParam0",
                new object[] { userId }
            );

            Console.WriteLine($"Update with NULL handling {(success ? "successful" : "failed")}");
        }

        /// <summary>
        /// Example: Complex query with multiple data types
        /// </summary>
        public void ComplexQueryMultipleTypes()
        {
            // Demonstrate using various data types in parameters
            string status = "pending";
            int priorityLevel = 5;
            DateTime startDate = new DateTime(2026, 1, 1);
            DateTime endDate = DateTime.Now;
            bool isActive = true;

            DataView result = _utils.Select(
                "task_id, title, priority, created_date",
                "Tasks",
                "status = @param0 AND priority >= @param1 AND created_date BETWEEN @param2 AND @param3 AND is_active = @param4",
                new object[] { status, priorityLevel, startDate, endDate, isActive }
            );

            Console.WriteLine($"Found {result.Count} tasks matching complex criteria");
        }

        /// <summary>
        /// Example: Transaction-like batch operations
        /// </summary>
        public void BatchOperations()
        {
            try
            {
                _utils.connectDB();

                // Insert order
                bool orderInserted = _utils.Insert(
                    new string[] { "customer_id", "order_date", "total" },
                    "Orders",
                    new string[] { "C123", DateTime.Now.ToString("yyyy-MM-dd"), "150.00" }
                );

                if (!orderInserted)
                {
                    Console.WriteLine("Order insertion failed");
                    return;
                }

                // Update inventory
                bool inventoryUpdated = _utils.Update(
                    new string[] { "quantity" },
                    "Inventory",
                    new string[] { "95" },
                    "product_id = @whereParam0",
                    new object[] { "P456" }
                );

                if (!inventoryUpdated)
                {
                    Console.WriteLine("Inventory update failed");
                    return;
                }

                // Log the transaction
                bool logInserted = _utils.Insert(
                    new string[] { "action", "timestamp", "details" },
                    "AuditLog",
                    new string[] { "ORDER_PLACED", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "Order for customer C123" }
                );

                Console.WriteLine("Batch operation completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during batch operation: {ex.Message}");
            }
        }
    }
}
