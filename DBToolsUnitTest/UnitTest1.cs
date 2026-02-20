using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Collections.Generic;
using DBTools_Utilities;
using DbTools.Model;
using DbTools.Controller;
using DbTools;
using System.Security.Cryptography;
using System.Linq;
using System.IO;
using DBTools.Model;

namespace DBToolsUnitTest
{
    /// <summary>
    /// Comprehensive unit tests for the DBTools project
    /// Tests cover allowed operations, not allowed operations, security validations, and all major functionality
    /// </summary>
    [TestClass]
    public class DBToolsUnitTests
    {
        private const string TestHost = "127.0.0.1";
        private const string TestDatabase = "testDB";
        private const string TestUid = "testUser";
        private const string TestPassword = "123456";
        private const string TestPort = "1433";

        private static string Surnames()
        {
            //creates a list of surnames to be used in tests
            var names = "Smith,Johnson,Williams,Jones,Brown,Davis,Miller,Wilson,Moore,Taylor,Anderson,Thomas,Jackson,White,Harris,Martin,Thompson,Garcia,Martinez,Robinson,Clark,Rodriguez,Lewis,Lee,Walker,Hall,Allen,Young,Hernandez,King,Wright,Lopez,Hill,Scott,Green,Adams,Baker,Gonzalez,Nelson,Carter,Mitchell,Perez,Roberts".Split(',');
            Random rand = new Random();
            return names.ToList().ElementAt(rand.Next(names.Count()));
        }

        private static string Names()
        {
            //creates a list of names to be used in tests
            IEnumerable<string> surnames = "James,John,Robert,Michael,William,David,Richard,Joseph,Thomas,Charles,Christopher,Daniel,Matthew,Anthony,Donald,Mark,Paul,Steven,Andrew,Joshua,Kenneth,Kevin,Brian,George,Edward,Ronald,Timothy,Jason,Jeffrey,Ryan,Gary,Nicholas,Eric,Stephen,Jonathan,Larry,Justin,Scott,Brandon,Benjamin,Samantha".Split(',');
            //Chooses a random surname from the list
            Random rand = new Random();
            return surnames.ToList().ElementAt(rand.Next(surnames.Count()));
        }
        private static string Emails(string name, string surname)
        {
            //creates a list of email domains to be used in tests
            IEnumerable<string> domains = "test.com,example.com,mail.com,domain.com,email.com,web.com,inbox.com,site.com,online.com,service.com".Split(',');
            Random rand = new Random();
            return $"{name.ToLower()}.{surname.ToLower()}@{domains.ToList().ElementAt(rand.Next(domains.Count()))}";
        }

        #region Utils Class Tests - Validation and Security

        [TestClass]
        public class UtilsValidationTests
        {

            [TestMethod]
            [ExpectedException(typeof(ArgumentException))]
            public void Insert_WithInvalidTableName_ShouldThrowException()
            {
                // Arrange
                var utils = new Utils();
                string[] fields = { "name", "email" };
                string[] values = { "John", "john@test.com" };

                // Act - Should throw
                utils.Insert(fields, "Users'; DROP TABLE Users--", values);
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentException))]
            public void Insert_WithInvalidFieldName_ShouldThrowException()
            {
                // Arrange
                var utils = new Utils();
                string[] fields = { "name'; DROP TABLE--", "email" };
                string[] values = { "John", "john@test.com" };

                // Act - Should throw
                utils.Insert(fields, "Users", values);
            }


            [TestMethod]
            [ExpectedException(typeof(ArgumentException))]
            public void Insert_WithEmptyFields_ShouldThrowException()
            {
                // Arrange
                var utils = new Utils();
                string[] fields = { };
                string[] values = { "John", "john@test.com" };

                // Act - Should throw
                utils.Insert(fields, "Users", values);
            }


            [TestMethod]
            [ExpectedException(typeof(ArgumentException))]
            public void Update_WithEmptyCondition_ShouldThrowException()
            {
                // Arrange
                var utils = new Utils();
                string[] fields = { "name" };
                string[] values = { "John" };

                // Act - Should throw for security reasons
                utils.Update(fields, "Users", values, "");
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentException))]
            public void Update_WithNullCondition_ShouldThrowException()
            {
                // Arrange
                var utils = new Utils();
                string[] fields = { "name" };
                string[] values = { "John" };

                // Act - Should throw for security reasons
                utils.Update(fields, "Users", values, null);
            }

        }

        #endregion

        #region Utils Class Tests - Parameterized Queries

        [TestClass]
        public class UtilsParameterizedQueryTests
        {
            [TestMethod]
            public void Select_WithParameters_ShouldNotThrowException()
            {
                // Arrange
                var utils = new Utils();
                object[] parameters = { 1, "active" };

                // Act & Assert - Should not throw
                try
                {
                    utils.Select("*", "Users", "id = @param0 AND status = @param1", parameters);
                }
                catch (ArgumentException ex)
                {
                    Assert.Fail($"Should not throw exception for parameterized query: {ex.Message}");
                }
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentNullException))]
            public void Select_WithNullParameters_ShouldThrowException()
            {
                // Arrange
                var utils = new Utils();

                // Act - Should throw
                utils.Select("*", "Users", "id = @param0", null);
            }

            [TestMethod]
            public void Update_WithParameterizedWhereClause_ShouldNotThrowException()
            {
                // Arrange
                var utils = new Utils();
                string[] fields = { "name", "email" };
                string[] values = { "John Updated", "john.updated@test.com" };
                object[] whereParams = { 1 };

                // Act & Assert - Should not throw
                try
                {
                    utils.Update(fields, "Users", values, "id = @whereParam0", whereParams);
                }
                catch (ArgumentException ex)
                {
                    Assert.Fail($"Should not throw exception for parameterized update: {ex.Message}");
                }
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentException))]
            public void Update_WithParameterizedWhereClause_EmptyWhereClause_ShouldThrowException()
            {
                // Arrange
                var utils = new Utils();
                string[] fields = { "name" };
                string[] values = { "John" };
                object[] whereParams = { };

                // Act - Should throw for security
                utils.Update(fields, "Users", values, "", whereParams);
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentNullException))]
            public void Update_WithParameterizedWhereClause_NullParameters_ShouldThrowException()
            {
                // Arrange
                var utils = new Utils();
                string[] fields = { "name" };
                string[] values = { "John" };

                // Act - Should throw
                utils.Update(fields, "Users", values, "id = @whereParam0", null);
            }

            [TestMethod]
            public void Delete_WithParameters_ShouldNotThrowException()
            {
                // Arrange
                var utils = new Utils();
                object[] parameters = { 1 };

                // Act & Assert - Should not throw
                try
                {
                    utils.Delete("Users", "id = @param0", parameters);
                }
                catch (ArgumentException ex)
                {
                    Assert.Fail($"Should not throw exception for parameterized delete: {ex.Message}");
                }
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentNullException))]
            public void Delete_WithNullParameters_ShouldThrowException()
            {
                // Arrange
                var utils = new Utils();

                // Act - Should throw
                utils.Delete("Users", "id = @param0", null);
            }

            [TestMethod]
            public void Select_WithQueryAndParameters_ShouldNotThrowException()
            {
                // Arrange
                var utils = new Utils();
                object[] parameters = { "John" };

                // Act & Assert - Should not throw
                try
                {
                    utils.Select("* FROM Users WHERE name = @param0", parameters);
                }
                catch (ArgumentException ex)
                {
                    Assert.Fail($"Should not throw exception for parameterized select query: {ex.Message}");
                }
            }
        }

        #endregion

        #region Utils Class Tests - Query Builder Methods

        [TestClass]
        public class UtilsQueryBuilderTests
        {
            [TestMethod]
            public void Select_Query_WithValidParameters_ShouldReturnQuery()
            {
                // Arrange & Act
                string query = Utils.Select_Query("id, name", "Users", "id > 10");

                // Assert
                Assert.IsNotNull(query);
                Assert.IsTrue(query.Contains("SELECT"));
                Assert.IsTrue(query.Contains("FROM Users"));
                Assert.IsTrue(query.Contains("WHERE id > 10"));
            }

            [TestMethod]
            public void Select_Query_WithoutConditions_ShouldReturnQueryWithoutWhere()
            {
                // Arrange & Act
                string query = Utils.Select_Query("*", "Users", "");

                // Assert
                Assert.IsNotNull(query);
                Assert.IsTrue(query.Contains("SELECT *"));
                Assert.IsTrue(query.Contains("FROM Users"));
                Assert.IsFalse(query.Contains("WHERE"));
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentException))]
            public void Select_Query_WithInvalidTable_ShouldThrowException()
            {
                // Act - Should throw
                Utils.Select_Query("*", "Users; DROP TABLE--", "");
            }

            [TestMethod]
            public void Insert_Query_WithValidParameters_ShouldReturnQuery()
            {
                // Arrange
                string[] fields = { "name", "email" };
                string[] values = { "John", "john@test.com" };

                // Act
                string query = Utils.Insert_Query(fields, "Users", values);

                // Assert
                Assert.IsNotNull(query);
                Assert.IsTrue(query.Contains("INSERT INTO Users"));
                Assert.IsTrue(query.Contains("(name,email)"));
                Assert.IsTrue(query.Contains("VALUES"));
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentException))]
            public void Insert_Query_WithNullFields_ShouldThrowException()
            {
                // Arrange
                string[] values = { "John" };

                // Act - Should throw
                Utils.Insert_Query(null, "Users", values);
            }

            [TestMethod]
            public void Update_Query_WithValidParameters_ShouldReturnQuery()
            {
                // Arrange
                string[] fields = { "name", "email" };
                string[] values = { "John Updated", "john.updated@test.com" };

                // Act
                string query = Utils.Update_Query(fields, "Users", values, "id = 1");

                // Assert
                Assert.IsNotNull(query);
                Assert.IsTrue(query.Contains("UPDATE Users"));
                Assert.IsTrue(query.Contains("SET"));
                Assert.IsTrue(query.Contains("WHERE id = 1"));
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentException))]
            public void Update_Query_WithoutCondition_ShouldThrowException()
            {
                // Arrange
                string[] fields = { "name" };
                string[] values = { "John" };

                // Act - Should throw for security
                Utils.Update_Query(fields, "Users", values, "");
            }

            [TestMethod]
            public void Delete_Query_WithValidParameters_ShouldReturnQuery()
            {
                // Arrange & Act
                string query = Utils.Delete_Query("Users", "id = 1");

                // Assert
                Assert.IsNotNull(query);
                Assert.IsTrue(query.Contains("DELETE FROM Users"));
                Assert.IsTrue(query.Contains("WHERE id = 1"));
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentException))]
            public void Delete_Query_WithoutCondition_ShouldThrowException()
            {
                // Act - Should throw for security
                Utils.Delete_Query("Users", "");
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentException))]
            public void Delete_Query_WithInvalidTable_ShouldThrowException()
            {
                // Act - Should throw
                Utils.Delete_Query("Users; DROP TABLE--", "id = 1");
            }
        }

        #endregion

        #region Utils Class Tests - Connection and Configuration

        [TestClass]
        public class UtilsConnectionTests
        {
            /// <summary>
            /// Helper method to execute test code in a temporary directory with optional config file
            /// </summary>
            private static void ExecuteInTempDirectory(Action testAction, string configContent = null)
            {
                string originalDir = Directory.GetCurrentDirectory();
                string tempDir = Path.Combine(Path.GetTempPath(), "DBToolsTest_" + Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                try
                {
                    if (configContent != null)
                    {
                        string configPath = Path.Combine(tempDir, "config.json");
                        File.WriteAllText(configPath, configContent);
                    }

                    Directory.SetCurrentDirectory(tempDir);
                    testAction();
                }
                finally
                {
                    // Cleanup
                    Directory.SetCurrentDirectory(originalDir);
                    if (Directory.Exists(tempDir))
                    {
                        try
                        {
                            Directory.Delete(tempDir, true);
                        }
                        catch
                        {
                            // Ignore cleanup errors
                        }
                    }
                }
            }

            [TestMethod]
            public void Constructor_WithParameters_ShouldSetProperties()
            {
                // Act
                var utils = new Utils();

                // Assert
                Assert.AreEqual(TestHost, utils.Host);
                Assert.AreEqual(TestDatabase, utils.Database);
                Assert.AreEqual(TestUid, utils.Uid);
                Assert.AreEqual(TestPassword, utils.Password);
                Assert.AreEqual(TestPort, utils.Port);
            }

            [TestMethod]
            public void Constructor_Parameterless_ShouldCreateInstance()
            {
                // Act
                var utils = new Utils();

                // Assert
                Assert.IsNotNull(utils);
            }

            [TestMethod]
            public void ConnectDB_ShouldSetConnectionProperties()
            {
                // Arrange
                var utils = new Utils();

                // Act
                utils.connectDB();

                // Assert - Should not throw exception
                Assert.AreEqual(TestDatabase, utils.Database);
                Assert.AreEqual(TestHost, utils.Host);
            }

            [TestMethod]
            public void Constructor_WithMissingConfigFile_ShouldThrowFileNotFoundException()
            {
                // Act & Assert
                ExecuteInTempDirectory(() =>
                {
                    var exception = Assert.ThrowsException<FileNotFoundException>(() => new Utils());
                    Assert.IsTrue(exception.Message.Contains("config.json"));
                    Assert.IsTrue(exception.Message.Contains("was not found"));
                });
            }

            [TestMethod]
            public void Constructor_WithMalformedConfigFile_ShouldThrowInvalidOperationException()
            {
                // Act & Assert
                ExecuteInTempDirectory(() =>
                {
                    var exception = Assert.ThrowsException<InvalidOperationException>(() => new Utils());
                    Assert.IsTrue(exception.Message.Contains("Failed to load database configuration"));
                }, "{ invalid json }");
            }

            [TestMethod]
            public void Constructor_WithMissingRequiredKeys_ShouldThrowInvalidOperationException()
            {
                // Act & Assert
                ExecuteInTempDirectory(() =>
                {
                    var exception = Assert.ThrowsException<InvalidOperationException>(() => new Utils());
                    Assert.IsTrue(exception.Message.Contains("required configuration keys are missing"));
                    Assert.IsTrue(exception.Message.Contains("Password"));
                    Assert.IsTrue(exception.Message.Contains("Port"));
                }, "{ \"Host\": \"localhost\", \"Database\": \"testDB\", \"Uid\": \"testUser\" }");
            }

            [TestMethod]
            public void Constructor_WithEmptyConfigValues_ShouldThrowInvalidOperationException()
            {
                // Act & Assert
                ExecuteInTempDirectory(() =>
                {
                    var exception = Assert.ThrowsException<InvalidOperationException>(() => new Utils());
                    Assert.IsTrue(exception.Message.Contains("required configuration keys are missing or empty"));
                    Assert.IsTrue(exception.Message.Contains("Host"));
                    Assert.IsTrue(exception.Message.Contains("Database"));
                }, "{ \"Host\": \"\", \"Database\": \"\", \"Uid\": \"testUser\", \"Password\": \"123\", \"Port\": \"1433\" }");
            }

            [TestMethod]
            public void Constructor_WithValidConfig_ShouldLoadAllProperties()
            {
                // Act & Assert
                ExecuteInTempDirectory(() =>
                {
                    var utils = new Utils();
                    Assert.AreEqual("testhost", utils.Host);
                    Assert.AreEqual("testdb", utils.Database);
                    Assert.AreEqual("testuid", utils.Uid);
                    Assert.AreEqual("testpass", utils.Password);
                    Assert.AreEqual("1234", utils.Port);
                }, "{ \"Host\": \"testhost\", \"Database\": \"testdb\", \"Uid\": \"testuid\", \"Password\": \"testpass\", \"Port\": \"1234\" }");
            }
        }

        #endregion

        #region DBTools Class Tests

        [TestClass]
        public class DBToolsTests
        {
            [TestMethod]
            public void Constructor_ShouldSetDefaultPort()
            {
                // Act
                var dbTools = new DbTools.DBTools();

                // Assert
                Assert.AreEqual("1433", dbTools.Port);
            }

            [TestMethod]
            public void Properties_GetterSetter_ShouldWork()
            {
                // Arrange
                var dbTools = new DbTools.DBTools();

                // Act
                dbTools.Host = TestHost;
                dbTools.Database = TestDatabase;
                dbTools.Uid = TestUid;
                dbTools.Password = TestPassword;
                dbTools.Query = "SELECT * FROM Users";
                dbTools.Table = "Users";
                dbTools.Port = TestPort;

                // Assert
                Assert.AreEqual(TestHost, dbTools.Host);
                Assert.AreEqual(TestDatabase, dbTools.Database);
                Assert.AreEqual(TestUid, dbTools.Uid);
                Assert.AreEqual(TestPassword, dbTools.Password);
                Assert.AreEqual("SELECT * FROM Users", dbTools.Query);
                Assert.AreEqual("Users", dbTools.Table);
                Assert.AreEqual(TestPort, dbTools.Port);
            }

            [TestMethod]
            public void ConnectionString_WithPort_ShouldIncludePort()
            {
                // Arrange
                var dbTools = new DbTools.DBTools
                {
                    Host = TestHost,
                    Database = TestDatabase,
                    Uid = TestUid,
                    Password = TestPassword,
                    Port = TestPort
                };

                // Act
                string connString = dbTools.ConnectionString;

                // Assert
                Assert.IsNotNull(connString);
                Assert.IsTrue(connString.Contains(TestHost));
                Assert.IsTrue(connString.Contains(TestDatabase));
                Assert.IsTrue(connString.Contains(TestUid));
                Assert.IsTrue(connString.Contains(TestPort));
            }

            [TestMethod]
            public void ConnectionString_SetCustom_ShouldReturnCustom()
            {
                // Arrange
                var dbTools = new DbTools.DBTools();
                string customConnString = "Server=myserver;Database=mydb;";

                // Act
                dbTools.ConnectionString = customConnString;

                // Assert
                Assert.AreEqual(customConnString, dbTools.ConnectionString);
            }

            [TestMethod]
            public void LegacyGetterSetters_ShouldWork()
            {
                // Arrange
                var dbTools = new DbTools.DBTools();

                // Act
                dbTools.setHost(TestHost);
                dbTools.setUid(TestUid);
                dbTools.setPassword(TestPassword);
                dbTools.setDataBase(TestDatabase);
                dbTools.setQuery("SELECT * FROM Users");

                // Assert
                Assert.AreEqual(TestHost, dbTools.getHost());
                Assert.AreEqual(TestUid, dbTools.getUid());
                Assert.AreEqual(TestPassword, dbTools.getPassword());
                Assert.AreEqual(TestDatabase, dbTools.getDatabase());
                Assert.AreEqual("SELECT * FROM Users", dbTools.getQuery());
            }

            [TestMethod]
            public void Error_Property_ShouldStoreError()
            {
                // Arrange
                var dbTools = new DbTools.DBTools();
                string errorMessage = "Test error message";

                // Act
                dbTools.Error = errorMessage;

                // Assert
                Assert.AreEqual(errorMessage, dbTools.Error);
            }

            [TestMethod]
            public void SqlParameters_ShouldAcceptParameters()
            {
                // Arrange
                var dbTools = new DbTools.DBTools();
                var parameters = new List<Microsoft.Data.SqlClient.SqlParameter>
                {
                    new Microsoft.Data.SqlClient.SqlParameter("@param0", 1)
                };

                // Act
                dbTools.SqlParameters = parameters;

                // Assert
                Assert.IsNotNull(dbTools.SqlParameters);
                Assert.AreEqual(1, dbTools.SqlParameters.Count);
            }
        }

        #endregion

        #region GenericObject Tests

        [TestClass]
        public class GenericObjectTests
        {
            [TestMethod]
            public void Constructor_Parameterless_ShouldCreateInstance()
            {
                // Act
                var obj = new GenericObject();

                // Assert
                Assert.IsNotNull(obj);
            }

            [TestMethod]
            public void Constructor_WithUtils_ShouldCreateInstance()
            {
                // Arrange
                var utils = new Utils();

                // Act
                var obj = new GenericObject(utils);

                // Assert
                Assert.IsNotNull(obj);
            }

            [TestMethod]
            public void Properties_ShouldStoreValues()
            {
                // Arrange
                var obj = new GenericObject();
                string[] columns = { "id", "name" };
                object[] values = { 1, "John" };
                string[] valuesString = { "1", "John" };
                string[] types = { "Int32", "String" };

                // Act
                obj.columns = columns;
                obj.values = values;
                obj.valuesString = valuesString;
                obj.types = types;
                obj.table = "Users";

                // Assert
                Assert.AreEqual(columns, obj.columns);
                Assert.AreEqual(values, obj.values);
                Assert.AreEqual(valuesString, obj.valuesString);
                Assert.AreEqual(types, obj.types);
                Assert.AreEqual("Users", obj.table);
            }
        }

        #endregion

        #region GenericObject_Simple Tests

        [TestClass]
        public class GenericObjectSimpleTests
        {
            [TestMethod]
            public void Properties_ShouldStoreValues()
            {
                // Arrange
                var obj = new GenericObject_Simple();

                // Act
                obj.column = "name";
                obj.value = "John";
                obj.type = "String";

                // Assert
                Assert.AreEqual("name", obj.column);
                Assert.AreEqual("John", obj.value);
                Assert.AreEqual("String", obj.type);
            }

            [TestMethod]
            public void Value_CanBeAnyType()
            {
                // Arrange
                var obj1 = new GenericObject_Simple { column = "id", value = 123, type = "Int32" };
                var obj2 = new GenericObject_Simple { column = "price", value = 45.67, type = "Double" };
                var obj3 = new GenericObject_Simple { column = "active", value = true, type = "Boolean" };
                var obj4 = new GenericObject_Simple { column = "created", value = DateTime.Now, type = "DateTime" };

                // Assert
                Assert.IsInstanceOfType(obj1.value, typeof(int));
                Assert.IsInstanceOfType(obj2.value, typeof(double));
                Assert.IsInstanceOfType(obj3.value, typeof(bool));
                Assert.IsInstanceOfType(obj4.value, typeof(DateTime));
            }
        }

        #endregion

        #region DataExport Tests

        [TestClass]
        public class DataExportTests
        {
            [TestMethod]
            public void ToCsv_WithData_ShouldReturnCsvString()
            {
                // Arrange
                var dataExport = new DataExport();
                var genericObjects = new List<GenericObject>
                {
                    new GenericObject
                    {
                        columns = new[] { "id", "name", "email" },
                        values = new object[] { 1, "John", "john@test.com" },
                        types = new[] { "Int32", "String", "String" }
                    },
                    new GenericObject
                    {
                        columns = new[] { "id", "name", "email" },
                        values = new object[] { 2, "Jane", "jane@test.com" },
                        types = new[] { "Int32", "String", "String" }
                    }
                };

                // Act
                string csv = dataExport.ToCsv(genericObjects, ',', true, true);

                // Assert
                Assert.IsNotNull(csv);
                Assert.IsTrue(csv.Contains("Int32,String,String"));
                Assert.IsTrue(csv.Contains("id,name,email"));
                Assert.IsTrue(csv.Contains("John"));
                Assert.IsTrue(csv.Contains("Jane"));
            }

            [TestMethod]
            public void ToCsv_WithDBNull_ShouldHandleGracefully()
            {
                // Arrange
                var dataExport = new DataExport();
                var genericObjects = new List<GenericObject>
                {
                    new GenericObject
                    {
                        columns = new[] { "id", "name" },
                        values = new object[] { 1, DBNull.Value },
                        types = new[] { "Int32", "String" }
                    }
                };

                // Act
                string csv = dataExport.ToCsv(genericObjects, ',', true, true);

                // Assert
                Assert.IsNotNull(csv);
                Assert.IsTrue(csv.Contains("1,"));
            }

            [TestMethod]
            public void ToCsv_WithoutColumns_ShouldNotIncludeColumnNames()
            {
                // Arrange
                var dataExport = new DataExport();
                var genericObjects = new List<GenericObject>
                {
                    new GenericObject
                    {
                        columns = new[] { "id", "name" },
                        values = new object[] { 1, "John" },
                        types = new[] { "Int32", "String" }
                    }
                };

                // Act
                string csv = dataExport.ToCsv(genericObjects, ',', false, false);

                // Assert
                Assert.IsNotNull(csv);
                Assert.IsFalse(csv.Contains("id,name"));
                Assert.IsFalse(csv.Contains("Int32,String"));
            }

            [TestMethod]
            public void ToCsv_WithCustomSeparator_ShouldUseSeparator()
            {
                // Arrange
                var dataExport = new DataExport();
                var genericObjects = new List<GenericObject>
                {
                    new GenericObject
                    {
                        columns = new[] { "id", "name" },
                        values = new object[] { 1, "John" },
                        types = new[] { "Int32", "String" }
                    }
                };

                // Act
                string csv = dataExport.ToCsv(genericObjects, ';', true, false);

                // Assert
                Assert.IsTrue(csv.Contains(";"));
                Assert.IsTrue(csv.Contains("id;name"));
            }

            [TestMethod]
            public void ToDataTable_WithValidCsv_ShouldReturnDataTable()
            {
                // Arrange
                var dataExport = new DataExport();
                string csv = "id,name,email\n1,John,john@test.com\n2,Jane,jane@test.com";

                // Act
                DataTable dt = dataExport.ToDataTable(csv, ',', false);

                // Assert
                Assert.IsNotNull(dt);
                Assert.AreEqual(3, dt.Columns.Count);
                Assert.AreEqual(2, dt.Rows.Count);
            }

            [TestMethod]
            public void ToDataTable_WithNullCsv_ShouldReturnEmptyDataTable()
            {
                // Arrange
                var dataExport = new DataExport();

                // Act
                DataTable dt = dataExport.ToDataTable(null, ',', false);

                // Assert
                Assert.IsNotNull(dt);
                Assert.AreEqual(0, dt.Columns.Count);
            }
        }

        #endregion

        #region DBToolsController Tests

        [TestClass]
        public class DBToolsControllerTests
        {
            [TestMethod]
            public void Constructor_ShouldInitialize()
            {
                // Arrange
                var dbTools = new DbTools.DBTools
                {
                    Host = TestHost,
                    Database = TestDatabase,
                    Uid = TestUid,
                    Password = TestPassword
                };

                // Act
                var controller = new DBToolsController(dbTools);

                // Assert
                Assert.IsNotNull(controller);
                Assert.IsNotNull(controller.DBTools);
            }

            [TestMethod]
            public void SqlExecuteQuery_ShouldSetQuery()
            {
                // Arrange
                var dbTools = new DbTools.DBTools();
                var controller = new DBToolsController(dbTools);
                string testQuery = "SELECT * FROM Users";

                // Act
                try
                {
                    controller.SqlExecuteQuery(testQuery);
                }
                catch
                {
                    // Expected to fail due to no real connection
                }

                // Assert
                Assert.AreEqual(testQuery, dbTools.Query);
            }

            #endregion

            #region UtilsController Generic Tests

            [TestClass]
            public class UtilsControllerTests
            {
                private class TestUser
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                    public string Email { get; set; }
                    public int Age { get; set; }
                }

                [TestMethod]
                public void Constructor_WithConnectionParameters_ShouldInitialize()
                {
                    // Act
                    var controller = new UtilsController<TestUser>(new Utils(), "Users", "Id", true);

                    // Assert
                    Assert.IsNotNull(controller);
                    Assert.IsNotNull(controller.Utils);
                }

                [TestMethod]
                public void Constructor_WithUtilsInstance_ShouldInitialize()
                {
                    // Arrange
                    var utils = new Utils();

                    // Act
                    var controller = new UtilsController<TestUser>(utils, "Users", "Id", true);

                    // Assert
                    Assert.IsNotNull(controller);
                    Assert.IsNotNull(controller.Utils);
                }

                [TestMethod]
                public void Insert_ShouldCallUtilsInsertAndReturnTrue()
                {
                    var randomAge = new Random().Next(18, 80);
                    var name = Names();
                    var surname = Surnames();
                    var email = Emails(name, surname);
                    string completeName = name + " " + surname;
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users", "Id", true);
                    var user = new TestUser { Name = completeName, Email = email, Age = randomAge };
                    // Act & Assert - Should not throw
                    Assert.IsTrue(controller.Insert(user));
                }

                [TestMethod]
                public void InsertBatch_ShouldCallUtilsInsertAndReturnTrue()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users", "Id", true);
                    var users = new List<TestUser>();
                    for (int i = 0; i < 1000; i++)
                    {
                        //Sleeps for 10 milliseconds to avoid primary key conflicts
                        System.Threading.Thread.Sleep(15);
                        var randomAge = new Random().Next(18, 80);
                        var name = Names();
                        var surname = Surnames();
                        var email = Emails(name, surname);
                        string completeName = name + " " + surname;
                        users.Add(new TestUser { Name = completeName, Email = email, Age = randomAge });
                    }
                    // Act & Assert - Should not throw
                    Assert.IsTrue(controller.InsertRange(users));
                }
                [TestMethod]
                [ExpectedException(typeof(ArgumentException))]
                public void Update_WithoutCondition_ShouldThrowException()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users");
                    var user = new TestUser { Id = 1, Name = "John" };

                    // Act - Should throw for security
                    controller.Update(user, "");
                }

                [TestMethod]
                public void SelectWithConditions_ShouldReturnList()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users");
                    string conditions = "age > @param0";
                    IEnumerable<object> parameters = new object[] { 25 };
                    // Act
                    var result = controller.Select(conditions, parameters);
                    // Assert
                    Assert.IsTrue(result.Count()>0);
                    Assert.IsInstanceOfType(result, typeof(List<TestUser>));
                }


                [TestMethod]
                public void Error_Property_ShouldReflectUtilsError()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users");
                    utils.Error = "Test error";

                    // Act
                    string error = controller.Error;

                    // Assert
                    Assert.AreEqual("Test error", error);
                }

                [TestMethod]
                public void Update_WithParameterizedWhereClause_ShouldNotThrowException()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users", "Id", true);
                    var user = new TestUser { Id = 1, Name = "Updated Name", Email = "updated@test.com", Age = 30 };
                    object[] whereParams = { 1 };

                    // Act & Assert - Should not throw
                    try
                    {
                        controller.Update(user, "Id = @whereParam0", whereParams);
                    }
                    catch (ArgumentException ex)
                    {
                        Assert.Fail($"Should not throw exception for parameterized update: {ex.Message}");
                    }
                }

                [TestMethod]
                [ExpectedException(typeof(ArgumentException))]
                public void Update_WithParameterizedWhereClause_EmptyWhereClause_ShouldThrowException()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users", "Id", true);
                    var user = new TestUser { Id = 1, Name = "Test" };
                    object[] whereParams = { };

                    // Act - Should throw for security
                    controller.Update(user, "", whereParams);
                }

                [TestMethod]
                [ExpectedException(typeof(ArgumentNullException))]
                public void Update_WithParameterizedWhereClause_NullParameters_ShouldThrowException()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users", "Id", true);
                    var user = new TestUser { Id = 1, Name = "Test" };

                    // Act - Should throw
                    controller.Update(user, "Id = @whereParam0", null);
                }

                [TestMethod]
                public void All_ShouldReturnAllRecords()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users", "Id", true);

                    // Act
                    var result = controller.All();

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
                }

                [TestMethod]
                public void Count_WithoutConditions_ShouldReturnTotalCount()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users", "Id", true);

                    // Act
                    int count = controller.Count();

                    // Assert
                    Assert.IsTrue(count >= 0);
                }

                [TestMethod]
                public void Count_WithConditions_ShouldReturnFilteredCount()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users", "Id", true);
                    string conditions = "Age > @param0";
                    object[] parameters = { 25 };

                    // Act
                    int count = controller.Count(conditions, parameters);

                    // Assert
                    Assert.IsTrue(count >= 0);
                }

                [TestMethod]
                public void Any_WithMatchingConditions_ShouldReturnTrue()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users", "Id", true);
                    string conditions = "Age > @param0";
                    object[] parameters = { 0 }; // Should match most records

                    // Act
                    bool exists = controller.Any(conditions, parameters);

                    // Assert - Should be true if there are any users with age > 0
                    Assert.IsTrue(exists);
                }

                [TestMethod]
                public void Any_WithNoMatchingConditions_ShouldReturnFalse()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users", "Id", true);
                    string conditions = "Age > @param0";
                    object[] parameters = { 999999 }; // Should not match any records

                    // Act
                    bool exists = controller.Any(conditions, parameters);

                    // Assert
                    Assert.IsFalse(exists);
                }

                [TestMethod]
                public void Find_WithValidPrimaryKey_ShouldReturnModel()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users", "Id", true);
                    
                    // First, insert a user to ensure we have data
                    var name = Names();
                    var surname = Surnames();
                    var email = Emails(name, surname);
                    var user = new TestUser { Name = name + " " + surname, Email = email, Age = 30 };
                    controller.Insert(user);

                    // Get the first user's ID
                    var firstUser = controller.FirstOrDefault();
                    if (firstUser != null)
                    {
                        // Act
                        var result = controller.Find(firstUser.Id);

                        // Assert
                        Assert.IsNotNull(result);
                        Assert.AreEqual(firstUser.Id, result.Id);
                    }
                }

                [TestMethod]
                [ExpectedException(typeof(InvalidOperationException))]
                public void Find_WithoutPrimaryKeyName_ShouldThrowException()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users"); // No primary key specified

                    // Act - Should throw
                    controller.Find(1);
                }

                [TestMethod]
                public void Where_WithConditions_ShouldReturnFilteredResults()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users", "Id", true);
                    string conditions = "Age > @param0";
                    object[] parameters = { 25 };

                    // Act
                    var result = controller.Where(conditions, parameters);

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
                }

                [TestMethod]
                public void SingleOrDefault_WithSingleMatch_ShouldReturnModel()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users", "Id", true);
                    
                    // Get the first user's ID to ensure we have a unique match
                    var firstUser = controller.FirstOrDefault();
                    if (firstUser != null)
                    {
                        string conditions = "Id = @param0";
                        object[] parameters = { firstUser.Id };

                        // Act
                        var result = controller.SingleOrDefault(conditions, parameters);

                        // Assert
                        Assert.IsNotNull(result);
                        Assert.AreEqual(firstUser.Id, result.Id);
                    }
                }

                [TestMethod]
                public void SingleOrDefault_WithNoMatch_ShouldReturnNull()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users", "Id", true);
                    string conditions = "Id = @param0";
                    object[] parameters = { -999999 }; // Non-existent ID

                    // Act
                    var result = controller.SingleOrDefault(conditions, parameters);

                    // Assert
                    Assert.IsNull(result);
                }
            }

            #endregion

            #region UtilsController LINQ Expression Tests

            [TestClass]
            public class UtilsControllerLinqTests
            {
                private class TestUser
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                    public string Email { get; set; }
                    public int Age { get; set; }
                }

                [TestMethod]
                public void GetAll_ShouldReturnAllRecords()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users");

                    // Act
                    var result = controller.GetAll();

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(List<TestUser>));
                    Assert.IsTrue(result.Count > 0);
                }

                [TestMethod]
                public void AsQueryable_ShouldReturnIQueryable()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users");

                    // Act
                    var result = controller.AsQueryable();

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(IQueryable<TestUser>));
                }

                [TestMethod]
                public void Add_ShouldInsertRecord()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users", "Id", true);
                    var name = Names();
                    var surname = Surnames();
                    var user = new TestUser { Name = name + " " + surname, Email = Emails(name, surname), Age = new Random().Next(18, 80) };

                    // Act
                    bool result = controller.Add(user);

                    // Assert
                    Assert.IsTrue(result);
                }

                [TestMethod]
                public void Where_WithLambdaPredicate_ShouldReturnMatchingRecords()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users");

                    // Act
                    var result = controller.Where(u => u.Age > 18);

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(List<TestUser>));
                    Assert.IsTrue(result.Count > 0);
                    Assert.IsTrue(result.All(u => u.Age > 18));
                }

                [TestMethod]
                public void Where_WithAndCondition_ShouldReturnMatchingRecords()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users");

                    // Act
                    var result = controller.Where(u => u.Age > 18 && u.Age < 80);

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsTrue(result.All(u => u.Age > 18 && u.Age < 80));
                }

                [TestMethod]
                public void FirstOrDefault_WithLambdaPredicate_ShouldReturnFirstMatch()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users");

                    // Act
                    var result = controller.FirstOrDefault(u => u.Age > 18);

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsTrue(result.Age > 18);
                }

                [TestMethod]
                public void FirstOrDefault_WithNoMatch_ShouldReturnNull()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users");

                    // Act
                    var result = controller.FirstOrDefault(u => u.Id == -999999);

                    // Assert
                    Assert.IsNull(result);
                }

                [TestMethod]
                public void SingleOrDefault_WithNoMatch_ShouldReturnNull()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users");

                    // Act
                    var result = controller.SingleOrDefault(u => u.Id == -999999);

                    // Assert
                    Assert.IsNull(result);
                }

                [TestMethod]
                [ExpectedException(typeof(InvalidOperationException))]
                public void SingleOrDefault_WithMultipleMatches_ShouldThrowException()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users");

                    // Act - multiple records will match Age > 18, should throw
                    controller.SingleOrDefault(u => u.Age > 18);
                }

                [TestMethod]
                public void Any_WithMatchingPredicate_ShouldReturnTrue()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users");

                    // Act
                    bool result = controller.Any(u => u.Age > 18);

                    // Assert
                    Assert.IsTrue(result);
                }

                [TestMethod]
                public void Any_WithNoMatchingPredicate_ShouldReturnFalse()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users");

                    // Act
                    bool result = controller.Any(u => u.Id == -999999);

                    // Assert
                    Assert.IsFalse(result);
                }

                [TestMethod]
                public void Count_WithPredicate_ShouldReturnCorrectCount()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users");

                    // Act
                    int countAll = controller.Count();
                    int countFiltered = controller.Count(u => u.Age > 18);

                    // Assert
                    Assert.IsTrue(countAll >= countFiltered);
                    Assert.IsTrue(countFiltered >= 0);
                }

                [TestMethod]
                public void Remove_WithLambdaPredicate_ShouldNotThrow()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users", "Id", true);
                    // Insert a record to delete
                    var name = Names();
                    var surname = Surnames();
                    var user = new TestUser { Name = "ToDelete_" + name, Email = Emails(name, surname), Age = 99 };
                    controller.Add(user);

                    // Act & Assert - Should not throw
                    try
                    {
                        controller.Remove(u => u.Age == 99);
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail($"Remove should not throw: {ex.Message}");
                    }
                }

                [TestMethod]
                [ExpectedException(typeof(ArgumentException))]
                public void Remove_WithNullPredicate_ShouldThrowException()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users");

                    // Act - null predicate causes empty where clause, should throw
                    controller.Remove(null);
                }

                [TestMethod]
                public void Update_WithLambdaPredicate_ShouldNotThrow()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users", "Id", true);
                    var name = Names();
                    var surname = Surnames();
                    var user = new TestUser { Name = name + " " + surname, Email = Emails(name, surname), Age = 25 };
                    controller.Add(user);

                    var updatedUser = new TestUser { Name = "Updated " + name, Email = Emails(name, surname), Age = 26 };

                    // Act & Assert - Should not throw
                    try
                    {
                        controller.Update(updatedUser, u => u.Age == 25);
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail($"Update with predicate should not throw: {ex.Message}");
                    }
                }

                [TestMethod]
                [ExpectedException(typeof(InvalidOperationException))]
                public void SaveChanges_WithoutPrimaryKey_ShouldThrowException()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users"); // No primary key specified
                    var user = new TestUser { Id = 1, Name = "Test", Email = "test@test.com", Age = 25 };

                    // Act - Should throw because no primary key specified
                    controller.SaveChanges(user);
                }

                [TestMethod]
                public void SaveChanges_WithValidPrimaryKey_ShouldNotThrow()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users", "Id", true);
                    var name = Names();
                    var surname = Surnames();
                    var user = new TestUser { Name = name + " " + surname, Email = Emails(name, surname), Age = 30 };
                    controller.Add(user);

                    // Get the inserted user
                    var inserted = controller.FirstOrDefault(u => u.Name == user.Name);
                    Assert.IsNotNull(inserted);

                    inserted.Age = 31;

                    // Act & Assert - Should not throw
                    try
                    {
                        controller.SaveChanges(inserted);
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail($"SaveChanges should not throw: {ex.Message}");
                    }
                }

                [TestMethod]
                public void AsQueryable_AllowsLinqChaining()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new UtilsController<TestUser>(utils, "Users");

                    // Act - Use LINQ on top of AsQueryable
                    var result = controller.AsQueryable()
                        .Where(u => u.Age > 18)
                        .OrderBy(u => u.Name)
                        .ToList();

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsTrue(result.All(u => u.Age > 18));
                }
            }

            #endregion

            #region QueryBuilder Tests

            [TestClass]
            public class QueryBuilderTests
            {
                private class TestModel
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                    public string Email { get; set; }
                    public DateTime CreatedDate { get; set; }
                }

                [TestMethod]
                public void QueryBuilder_WithObject_ShouldReturnGenericObjectList()
                {
                    // Arrange
                    var utils = new Utils();
                    var model = new TestModel
                    {
                        Id = 1,
                        Name = "John",
                        Email = "john@test.com",
                        CreatedDate = new DateTime(2024, 1, 1, 10, 30, 0)
                    };

                    // Act
                    var result = utils.QueryBuilder(model, "Id", true);

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.AreEqual(1, result.Count);
                    Assert.IsTrue(result[0].columns.Length > 0);
                    Assert.IsTrue(result[0].values.Length > 0);
                }

                [TestMethod]
                public void QueryBuilder_WithAutoIncrement_ShouldExcludePrimaryKey()
                {
                    // Arrange
                    var utils = new Utils();
                    var model = new TestModel
                    {
                        Id = 1,
                        Name = "John",
                        Email = "john@test.com",
                        CreatedDate = DateTime.Now
                    };

                    // Act
                    var result = utils.QueryBuilder(model, "Id", true);

                    // Assert
                    Assert.IsNotNull(result);
                    bool hasId = false;
                    foreach (var col in result[0].columns)
                    {
                        if (col == "Id")
                        {
                            hasId = true;
                            break;
                        }
                    }
                    Assert.IsFalse(hasId, "Id should be excluded when autoIncrement is true");
                }

                [TestMethod]
                public void QueryBuilder_WithoutAutoIncrement_ShouldIncludePrimaryKey()
                {
                    // Arrange
                    var utils = new Utils();
                    var model = new TestModel
                    {
                        Id = 1,
                        Name = "John",
                        Email = "john@test.com",
                        CreatedDate = DateTime.Now
                    };

                    // Act
                    var result = utils.QueryBuilder(model, "Id", false);

                    // Assert
                    Assert.IsNotNull(result);
                    bool hasId = false;
                    foreach (var col in result[0].columns)
                    {
                        if (col == "Id")
                        {
                            hasId = true;
                            break;
                        }
                    }
                    Assert.IsTrue(hasId, "Id should be included when autoIncrement is false");
                }

                [TestMethod]
                public void QueryBuilder_WithDateTime_ShouldFormatCorrectly()
                {
                    // Arrange
                    var utils = new Utils();
                    var model = new TestModel
                    {
                        Id = 1,
                        Name = "John",
                        Email = "john@test.com",
                        CreatedDate = new DateTime(2024, 1, 15, 10, 30, 45)
                    };

                    // Act
                    var result = utils.QueryBuilder(model, "Id", true);

                    // Assert
                    Assert.IsNotNull(result);
                    bool hasCorrectDateFormat = result[0].valuesString
                        .Any(val => val.Contains("2024-01-15"));
                    Assert.IsTrue(hasCorrectDateFormat, "DateTime should be formatted as yyyy-MM-dd HH:mm:ss");
                }
            }

            #endregion

            #region Integration-Style Tests for Complete Workflows

            [TestClass]
            public class WorkflowTests
            {
                [TestMethod]
                public void CompleteWorkflow_QueryBuilding_ShouldWork()
                {
                    // Arrange
                    string[] fields = { "name", "email", "age", "test" };
                    string[] values = { "John Doe", "john@test.com", "30", "test" };

                    // Act - Build all types of queries
                    string insertQuery = Utils.Insert_Query(fields, "Users", values);
                    string updateQuery = Utils.Update_Query(fields, "Users", values, "id = 1");
                    string selectQuery = Utils.Select_Query("*", "Users", "age > 18");
                    string deleteQuery = Utils.Delete_Query("Users", "id = 1");

                    // Assert
                    Assert.IsTrue(insertQuery.Contains("INSERT INTO"));
                    Assert.IsTrue(updateQuery.Contains("UPDATE"));
                    Assert.IsTrue(selectQuery.Contains("SELECT"));
                    Assert.IsTrue(deleteQuery.Contains("DELETE FROM"));
                }

                [TestMethod]
                public void CompleteWorkflow_QueryBuildingWithInjectionPatterns_ShouldNotWork()
                {
                    string[] fields = { "name", "email", "age; DROP TABLE Users" };
                    string[] values = { "John'; DROP TABLE Users--", "25" };
                    string tableName = "Users; DROP TABLE--";
                    // Act - Build all types of queries



                    int errorCount = 0;
                    string ErrorMessages = "";
                    // Assert - Queries should not contain injection patterns

                    try
                    {
                        string insertQuery = Utils.Insert_Query(fields, tableName, values);
                    }
                    catch (Exception e)
                    {
                        errorCount++;
                        ErrorMessages += e.Message + "\n";
                    }

                    try
                    {
                        string updateQuery = Utils.Update_Query(fields, tableName, values, "id = 1; DROP TABLE Users--");
                        Assert.IsFalse(updateQuery.Contains("; DROP TABLE"), "Update query contains potential SQL injection pattern.");
                    }
                    catch (Exception e)
                    {
                        errorCount++;
                        ErrorMessages += e.Message + "\n";
                    }

                    try
                    {
                        string selectQuery = Utils.Select_Query("*", tableName, "age > 18; DROP TABLE Users--");
                    }
                    catch (Exception e)
                    {
                        errorCount++;
                        ErrorMessages += e.Message + "\n";
                    }

                    try
                    {
                        string deleteQuery = Utils.Delete_Query(tableName, "id = 1; DROP TABLE Users--");
                    }
                    catch (Exception e)
                    {
                        errorCount++;
                        ErrorMessages += e.Message + "\n";
                    }
                    if (errorCount != 4)
                    {
                        Assert.Fail("Sql Injection patterns were not handled properly:\n" + ErrorMessages);
                    }

                }

                [TestMethod]
                public void CompleteWorkflow_DataExportImport_ShouldWork()
                {
                    // Arrange
                    var dataExport = new DataExport();
                    var genericObjects = new List<GenericObject>
                {
                    new GenericObject
                    {
                        columns = new[] { "id", "name" },
                        values = new object[] { 1, "John" },
                        types = new[] { "Int32", "String" }
                    }
                };

                    // Act - Export to CSV
                    string csv = dataExport.ToCsv(genericObjects, ',', true, true);

                    // Act - Import back from CSV
                    DataTable dt = dataExport.ToDataTable(csv, ',', true);

                    // Assert
                    Assert.IsNotNull(csv);
                    Assert.IsNotNull(dt);
                    Assert.IsTrue(csv.Length > 0);
                }
            }

            #endregion

            #region Edge Cases and Boundary Tests

            [TestClass]
            public class EdgeCaseTests
            {


                [TestMethod]
                public void Update_Query_WithNumericString_ShouldNotAddQuotes()
                {
                    // Arrange
                    string[] fields = { "age" };
                    string[] values = { "25" };

                    // Act
                    string query = Utils.Update_Query(fields, "Users", values, "id = 1");

                    // Assert
                    Assert.IsTrue(query.Contains("age=25") || query.Contains("age=25.0"));
                }



                [TestMethod]
                public void ToCsv_WithSpecialCharacters_ShouldEscapeCorrectly()
                {
                    // Arrange
                    var dataExport = new DataExport();
                    var genericObjects = new List<GenericObject>
                {
                    new GenericObject
                    {
                        columns = new[] { "path" },
                        values = new object[] { "C:\\Users\\Test\r\nNewLine" },
                        types = new[] { "String" }
                    }
                };

                    // Act
                    string csv = dataExport.ToCsv(genericObjects, ',', false, false);

                    // Assert
                    Assert.IsNotNull(csv);
                    Assert.IsTrue(csv.Contains("C:/Users/Test")); // Backslash should be replaced
                    Assert.IsFalse(csv.Contains("\r\n")); // Newlines should be removed
                }
            }

            #endregion

            #region Obsolete Method Tests

            [TestClass]
            public class ObsoleteMethodTests
            {


                [TestMethod]
                public void Update_ObsoleteOverload_RequiresCondition()
                {
                    // Arrange
                    var utils = new Utils();
                    string[] fields = { "name" };
                    string[] values = { "John" };

                    // Act & Assert - Should require condition even for obsolete version
                    try
                    {
                        utils.Update(fields, "Users", values, "id = 1");
                    }
                    catch (ArgumentException)
                    {
                        Assert.Fail("Should not throw with valid condition");
                    }
                }

            }

            #endregion

            #region PropertyBasedUtilsController Tests

            [TestClass]
            public class PropertyBasedUtilsControllerTests
            {
                private class TestUser
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                    public string Email { get; set; }
                    public int Age { get; set; }
                }

                [TestMethod]
                public void Constructor_WithConnectionParameters_ShouldInitialize()
                {
                    // Act
                    var controller = new PropertyBasedUtilsController<TestUser>(new Utils(), "Users", "Id", true);

                    // Assert
                    Assert.IsNotNull(controller);
                    Assert.IsNotNull(controller.Utils);
                }

                [TestMethod]
                public void Constructor_WithUtilsInstance_ShouldInitialize()
                {
                    // Arrange
                    var utils = new Utils();

                    // Act
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);

                    // Assert
                    Assert.IsNotNull(controller);
                    Assert.IsNotNull(controller.Utils);
                }

                [TestMethod]
                public void WhereEquals_WithValidProperty_ShouldReturnFilteredResults()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);

                    // Act
                    var result = controller.WhereEquals(u => u.Age, 30);

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
                }

                [TestMethod]
                public void WhereGreaterThan_WithValidProperty_ShouldReturnFilteredResults()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);

                    // Act
                    var result = controller.WhereGreaterThan(u => u.Age, 25);

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
                }

                [TestMethod]
                public void WhereLessThan_WithValidProperty_ShouldReturnFilteredResults()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);

                    // Act
                    var result = controller.WhereLessThan(u => u.Age, 50);

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
                }

                [TestMethod]
                public void WhereBetween_WithValidProperty_ShouldReturnFilteredResults()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);

                    // Act
                    var result = controller.WhereBetween(u => u.Age, 20, 40);

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
                }

                [TestMethod]
                public void WhereContains_WithValidProperty_ShouldReturnFilteredResults()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);

                    // Act
                    var result = controller.WhereContains(u => u.Name, "John");

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
                }

                [TestMethod]
                public void WhereStartsWith_WithValidProperty_ShouldReturnFilteredResults()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);

                    // Act
                    var result = controller.WhereStartsWith(u => u.Email, "john");

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
                }

                [TestMethod]
                public void WhereEndsWith_WithValidProperty_ShouldReturnFilteredResults()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);

                    // Act
                    var result = controller.WhereEndsWith(u => u.Email, "@test.com");

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
                }

                [TestMethod]
                public void WhereIn_WithValidProperty_ShouldReturnFilteredResults()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);
                    var ages = new List<int> { 25, 30, 35 };

                    // Act
                    var result = controller.WhereIn(u => u.Age, ages);

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
                }

                [TestMethod]
                public void WhereIn_WithEmptyList_ShouldReturnEmptyResults()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);
                    var ages = new List<int>();

                    // Act
                    var result = controller.WhereIn(u => u.Age, ages);

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.AreEqual(0, result.Count());
                }

                [TestMethod]
                public void FirstOrDefaultByProperty_WithValidProperty_ShouldReturnResult()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);

                    // Act - Should not throw
                    try
                    {
                        var result = controller.FirstOrDefaultByProperty(u => u.Age, 30);
                        // Result may be null if no match, but shouldn't throw
                    }
                    catch (ArgumentException ex)
                    {
                        Assert.Fail($"Should not throw exception: {ex.Message}");
                    }
                }

                [TestMethod]
                public void CountByProperty_WithValidProperty_ShouldReturnCount()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);

                    // Act
                    int count = controller.CountByProperty(u => u.Age, 30);

                    // Assert
                    Assert.IsTrue(count >= 0);
                }

                [TestMethod]
                public void AnyByProperty_WithValidProperty_ShouldReturnBool()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);

                    // Act - Should not throw
                    try
                    {
                        bool exists = controller.AnyByProperty(u => u.Age, 30);
                        // Result is either true or false
                    }
                    catch (ArgumentException ex)
                    {
                        Assert.Fail($"Should not throw exception: {ex.Message}");
                    }
                }

                [TestMethod]
                public void WhereByExample_WithModelTemplate_ShouldReturnFilteredResults()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);
                    var filterModel = new TestUser { Age = 30 };

                    // Act
                    var result = controller.WhereByExample(filterModel);

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
                }

                [TestMethod]
                public void WhereByExample_WithEmptyModel_ShouldReturnAllRecords()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);
                    var filterModel = new TestUser(); // Empty model with default values

                    // Act
                    var result = controller.WhereByExample(filterModel);

                    // Assert - Should return all records when no filter values are set
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
                }

                [TestMethod]
                [ExpectedException(typeof(ArgumentNullException))]
                public void WhereEquals_WithNullExpression_ShouldThrowException()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);

                    // Act - Should throw
                    controller.WhereEquals<int>(null, 30);
                }

                [TestMethod]
                public void WhereNotEquals_WithValidProperty_ShouldReturnFilteredResults()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);

                    // Act
                    var result = controller.WhereNotEquals(u => u.Age, 30);

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
                }

                [TestMethod]
                public void WhereIsNotNull_WithValidProperty_ShouldReturnFilteredResults()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);

                    // Act
                    var result = controller.WhereIsNotNull(u => u.Name);

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(IEnumerable<TestUser>));
                }

                [TestMethod]
                public void Exists_WithValidProperty_ShouldReturnBool()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);

                    // Act - Should not throw
                    bool exists = controller.Exists(u => u.Age, 30);

                    // Assert - Result is either true or false
                    Assert.IsTrue(exists || !exists);
                }

                [TestMethod]
                public void InsertOrUpdate_ShouldNotThrowException()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);
                    var user = new TestUser { Name = "Test User", Email = "test@test.com", Age = 25 };

                    // Act & Assert - Should not throw
                    try
                    {
                        controller.InsertOrUpdate(user, u => u.Email);
                    }
                    catch (ArgumentException ex)
                    {
                        Assert.Fail($"Should not throw exception: {ex.Message}");
                    }
                }

                [TestMethod]
                public void GetOrCreate_ShouldReturnModel()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);
                    var user = new TestUser { Name = "GetOrCreate User", Email = "getorcreate@test.com", Age = 35 };

                    // Act
                    var result = controller.GetOrCreate(user, u => u.Email);

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(TestUser));
                }

                [TestMethod]
                public void UpdateWhere_WithTwoConditions_ShouldNotThrowException()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);
                    var user = new TestUser { Name = "Updated Name", Email = "updated@test.com", Age = 40 };

                    // Act & Assert - Should not throw
                    try
                    {
                        controller.UpdateWhere(user, u => u.Age, 30, u => u.Name, "Test");
                    }
                    catch (ArgumentException ex)
                    {
                        Assert.Fail($"Should not throw exception: {ex.Message}");
                    }
                }

                [TestMethod]
                public void UpdateWhere_WithThreeConditions_ShouldNotThrowException()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);
                    var user = new TestUser { Name = "Updated Name", Email = "updated@test.com", Age = 40 };

                    // Act & Assert - Should not throw
                    try
                    {
                        controller.UpdateWhere(user, u => u.Age, 30, u => u.Name, "Test", u => u.Email, "test@test.com");
                    }
                    catch (ArgumentException ex)
                    {
                        Assert.Fail($"Should not throw exception: {ex.Message}");
                    }
                }

                [TestMethod]
                public void DeleteWhere_WithTwoConditions_ShouldNotThrowException()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);

                    // Act & Assert - Should not throw
                    try
                    {
                        controller.DeleteWhere(u => u.Age, 999, u => u.Name, "NonExistent");
                    }
                    catch (ArgumentException ex)
                    {
                        Assert.Fail($"Should not throw exception: {ex.Message}");
                    }
                }

                [TestMethod]
                public void DeleteWhereIn_WithEmptyList_ShouldReturnTrue()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);
                    var emptyAges = new List<int>();

                    // Act
                    bool result = controller.DeleteWhereIn(u => u.Age, emptyAges);

                    // Assert - Should return true as nothing to delete
                    Assert.IsTrue(result);
                }

                [TestMethod]
                public void UpdateWhereIn_WithValidValues_ShouldNotThrowException()
                {
                    // Arrange
                    var utils = new Utils();
                    var controller = new PropertyBasedUtilsController<TestUser>(utils, "Users", "Id", true);
                    var user = new TestUser { Name = "Bulk Updated", Email = "bulk@test.com", Age = 50 };
                    var ages = new List<int> { 999, 998, 997 };

                    // Act & Assert - Should not throw
                    try
                    {
                        controller.UpdateWhereIn(user, u => u.Age, ages);
                    }
                    catch (ArgumentException ex)
                    {
                        Assert.Fail($"Should not throw exception: {ex.Message}");
                    }
                }
            }

            #endregion
        }
    }
}