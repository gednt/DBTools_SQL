using NUnit.Framework;
using DBTools_Utilities;
using System;

namespace TestDBTools
{
    /// <summary>
    /// Unit tests for the Utils class query builder methods
    /// </summary>
    [TestFixture]
    public class UtilsTests
    {
        [Test]
        public void Select_Query_WithConditions_GeneratesCorrectQuery()
        {
            // Arrange
            string fields = "id, name, email";
            string table = "users";
            string conditions = "id = 1";

            // Act
            string result = Utils.Select_Query(fields, table, conditions);

            // Assert
            Assert.That(result, Is.EqualTo("SELECT id, name, email FROM users WHERE id = 1"));
        }

        [Test]
        public void Select_Query_WithoutConditions_GeneratesCorrectQuery()
        {
            // Arrange
            string fields = "*";
            string table = "products";
            string conditions = "";

            // Act
            string result = Utils.Select_Query(fields, table, conditions);

            // Assert
            Assert.That(result, Is.EqualTo("SELECT * FROM products"));
        }

        [Test]
        public void Select_Query_WithInvalidTableName_ThrowsArgumentException()
        {
            // Arrange
            string fields = "id";
            string table = "users; DROP TABLE users--";
            string conditions = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Utils.Select_Query(fields, table, conditions));
        }

        [Test]
        public void Select_Query_WithInvalidFieldName_ThrowsArgumentException()
        {
            // Arrange
            string fields = "id; DROP TABLE users--";
            string table = "users";
            string conditions = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Utils.Select_Query(fields, table, conditions));
        }

        [Test]
        public void Insert_Query_WithValidData_GeneratesCorrectQuery()
        {
            // Arrange
            string[] fields = new string[] { "name", "email", "age" };
            string table = "users";
            string[] values = new string[] { "John Doe", "john@example.com", "30" };

            // Act
            string result = Utils.Insert_Query(fields, table, values);

            // Assert
            Assert.That(result, Does.StartWith("INSERT INTO users(name,email,age) VALUES("));
            Assert.That(result, Does.Contain("'John Doe'"));
            Assert.That(result, Does.Contain("'john@example.com'"));
            Assert.That(result, Does.Contain("30"));
        }

        [Test]
        public void Insert_Query_WithNullValue_GeneratesQueryWithNull()
        {
            // Arrange
            string[] fields = new string[] { "name", "email" };
            string table = "users";
            string[] values = new string[] { "John", null };

            // Act
            string result = Utils.Insert_Query(fields, table, values);

            // Assert
            Assert.That(result, Does.Contain("null"));
        }

        [Test]
        public void Insert_Query_WithEmptyValue_GeneratesQueryWithNull()
        {
            // Arrange
            string[] fields = new string[] { "name", "description" };
            string table = "products";
            string[] values = new string[] { "Product1", "" };

            // Act
            string result = Utils.Insert_Query(fields, table, values);

            // Assert
            Assert.That(result, Does.Contain("null"));
        }

        [Test]
        public void Insert_Query_WithNumericValue_DoesNotAddQuotes()
        {
            // Arrange
            string[] fields = new string[] { "name", "price" };
            string table = "products";
            string[] values = new string[] { "Widget", "19.99" };

            // Act
            string result = Utils.Insert_Query(fields, table, values);

            // Assert
            Assert.That(result, Does.Contain("19.99"));
            Assert.That(result, Does.Not.Contain("'19.99'"));
        }

        [Test]
        public void Insert_Query_WithSingleQuoteInValue_EscapesQuote()
        {
            // Arrange
            string[] fields = new string[] { "name" };
            string table = "users";
            string[] values = new string[] { "O'Brien" };

            // Act
            string result = Utils.Insert_Query(fields, table, values);

            // Assert
            Assert.That(result, Does.Contain("'O''Brien'"));
        }

        [Test]
        public void Insert_Query_WithInvalidTableName_ThrowsArgumentException()
        {
            // Arrange
            string[] fields = new string[] { "name" };
            string table = "users; DROP TABLE users--";
            string[] values = new string[] { "John" };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Utils.Insert_Query(fields, table, values));
        }

        [Test]
        public void Insert_Query_WithMismatchedArrayLengths_ThrowsArgumentException()
        {
            // Arrange
            string[] fields = new string[] { "name", "email" };
            string table = "users";
            string[] values = new string[] { "John" };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Utils.Insert_Query(fields, table, values));
        }

        [Test]
        public void Update_Query_WithValidData_GeneratesCorrectQuery()
        {
            // Arrange
            string[] fields = new string[] { "name", "email" };
            string table = "users";
            string[] values = new string[] { "Jane Doe", "jane@example.com" };
            string condition = "id = 5";

            // Act
            string result = Utils.Update_Query(fields, table, values, condition);

            // Assert
            Assert.That(result, Does.StartWith("UPDATE users SET"));
            Assert.That(result, Does.Contain("name="));
            Assert.That(result, Does.Contain("email="));
            Assert.That(result, Does.Contain("'Jane Doe'"));
            Assert.That(result, Does.Contain("'jane@example.com'"));
            Assert.That(result, Does.EndWith("WHERE id = 5"));
        }

        [Test]
        public void Update_Query_WithoutCondition_ThrowsArgumentException()
        {
            // Arrange
            string[] fields = new string[] { "name" };
            string table = "users";
            string[] values = new string[] { "John" };
            string condition = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Utils.Update_Query(fields, table, values, condition));
        }

        [Test]
        public void Update_Query_WithInvalidTableName_ThrowsArgumentException()
        {
            // Arrange
            string[] fields = new string[] { "name" };
            string table = "users; DROP TABLE users--";
            string[] values = new string[] { "John" };
            string condition = "id = 1";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Utils.Update_Query(fields, table, values, condition));
        }

        [Test]
        public void Delete_Query_WithValidCondition_GeneratesCorrectQuery()
        {
            // Arrange
            string table = "users";
            string condition = "id = 10";

            // Act
            string result = Utils.Delete_Query(table, condition);

            // Assert
            Assert.That(result, Is.EqualTo("DELETE FROM users WHERE id = 10"));
        }

        [Test]
        public void Delete_Query_WithoutCondition_ThrowsArgumentException()
        {
            // Arrange
            string table = "users";
            string condition = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Utils.Delete_Query(table, condition));
        }

        [Test]
        public void Delete_Query_WithInvalidTableName_ThrowsArgumentException()
        {
            // Arrange
            string table = "users; DROP TABLE users--";
            string condition = "id = 1";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Utils.Delete_Query(table, condition));
        }

        [Test]
        public void Insert_Query_WithCommaInDecimal_ReplacesWithDot()
        {
            // Arrange
            string[] fields = new string[] { "price" };
            string table = "products";
            string[] values = new string[] { "19,99" };

            // Act
            string result = Utils.Insert_Query(fields, table, values);

            // Assert
            Assert.That(result, Does.Contain("19.99"));
            Assert.That(result, Does.Not.Contain("19,99"));
        }

        [Test]
        public void Update_Query_WithNullValue_GeneratesQueryWithNull()
        {
            // Arrange
            string[] fields = new string[] { "description" };
            string table = "products";
            string[] values = new string[] { null };
            string condition = "id = 1";

            // Act
            string result = Utils.Update_Query(fields, table, values, condition);

            // Assert
            Assert.That(result, Does.Contain("description=null"));
        }
    }
}
