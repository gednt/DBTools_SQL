using NUnit.Framework;
using DBTools.Model;
using System;

namespace TestDBTools
{
    /// <summary>
    /// Unit tests for the Model classes
    /// </summary>
    [TestFixture]
    public class ModelTests
    {
        [Test]
        public void GenericObject_DefaultConstructor_InitializesSuccessfully()
        {
            // Act
            var obj = new GenericObject();

            // Assert
            Assert.That(obj, Is.Not.Null);
        }

        [Test]
        public void GenericObject_SetColumns_StoresCorrectly()
        {
            // Arrange
            var obj = new GenericObject();
            string[] columns = new string[] { "id", "name", "email" };

            // Act
            obj.columns = columns;

            // Assert
            Assert.That(obj.columns, Is.EqualTo(columns));
            Assert.That(obj.columns.Length, Is.EqualTo(3));
        }

        [Test]
        public void GenericObject_SetValues_StoresCorrectly()
        {
            // Arrange
            var obj = new GenericObject();
            object[] values = new object[] { 1, "John Doe", "john@example.com" };

            // Act
            obj.values = values;

            // Assert
            Assert.That(obj.values, Is.EqualTo(values));
            Assert.That(obj.values.Length, Is.EqualTo(3));
        }

        [Test]
        public void GenericObject_SetValuesString_StoresCorrectly()
        {
            // Arrange
            var obj = new GenericObject();
            string[] valuesString = new string[] { "1", "John", "john@example.com" };

            // Act
            obj.valuesString = valuesString;

            // Assert
            Assert.That(obj.valuesString, Is.EqualTo(valuesString));
            Assert.That(obj.valuesString.Length, Is.EqualTo(3));
        }

        [Test]
        public void GenericObject_SetTypes_StoresCorrectly()
        {
            // Arrange
            var obj = new GenericObject();
            string[] types = new string[] { "Int32", "String", "String" };

            // Act
            obj.types = types;

            // Assert
            Assert.That(obj.types, Is.EqualTo(types));
            Assert.That(obj.types.Length, Is.EqualTo(3));
        }

        [Test]
        public void GenericObject_SetTable_StoresCorrectly()
        {
            // Arrange
            var obj = new GenericObject();
            string table = "users";

            // Act
            obj.table = table;

            // Assert
            Assert.That(obj.table, Is.EqualTo(table));
        }

        [Test]
        public void GenericObject_SetAllProperties_StoresAllCorrectly()
        {
            // Arrange
            var obj = new GenericObject();
            
            // Act
            obj.columns = new string[] { "id", "name" };
            obj.values = new object[] { 1, "Test" };
            obj.valuesString = new string[] { "1", "Test" };
            obj.types = new string[] { "Int32", "String" };
            obj.table = "test_table";

            // Assert
            Assert.That(obj.columns.Length, Is.EqualTo(2));
            Assert.That(obj.values.Length, Is.EqualTo(2));
            Assert.That(obj.valuesString.Length, Is.EqualTo(2));
            Assert.That(obj.types.Length, Is.EqualTo(2));
            Assert.That(obj.table, Is.EqualTo("test_table"));
        }

        [Test]
        public void GenericObject_Simple_SetColumn_StoresCorrectly()
        {
            // Arrange
            var obj = new GenericObject_Simple();
            string column = "username";

            // Act
            obj.column = column;

            // Assert
            Assert.That(obj.column, Is.EqualTo(column));
        }

        [Test]
        public void GenericObject_Simple_SetValue_StoresCorrectly()
        {
            // Arrange
            var obj = new GenericObject_Simple();
            object value = "testuser";

            // Act
            obj.value = value;

            // Assert
            Assert.That(obj.value, Is.EqualTo(value));
        }

        [Test]
        public void GenericObject_Simple_SetType_StoresCorrectly()
        {
            // Arrange
            var obj = new GenericObject_Simple();
            string type = "String";

            // Act
            obj.type = type;

            // Assert
            Assert.That(obj.type, Is.EqualTo(type));
        }

        [Test]
        public void GenericObject_Simple_SetAllProperties_StoresAllCorrectly()
        {
            // Arrange
            var obj = new GenericObject_Simple();

            // Act
            obj.column = "age";
            obj.value = 25;
            obj.type = "Int32";

            // Assert
            Assert.That(obj.column, Is.EqualTo("age"));
            Assert.That(obj.value, Is.EqualTo(25));
            Assert.That(obj.type, Is.EqualTo("Int32"));
        }

        [Test]
        public void GenericObject_Simple_WithNullValue_HandlesCorrectly()
        {
            // Arrange
            var obj = new GenericObject_Simple();

            // Act
            obj.column = "optional_field";
            obj.value = null;
            obj.type = "String";

            // Assert
            Assert.That(obj.column, Is.EqualTo("optional_field"));
            Assert.That(obj.value, Is.Null);
            Assert.That(obj.type, Is.EqualTo("String"));
        }

        [Test]
        public void GenericObject_WithNullArrays_HandlesCorrectly()
        {
            // Arrange
            var obj = new GenericObject();

            // Act
            obj.columns = null;
            obj.values = null;
            obj.valuesString = null;
            obj.types = null;

            // Assert
            Assert.That(obj.columns, Is.Null);
            Assert.That(obj.values, Is.Null);
            Assert.That(obj.valuesString, Is.Null);
            Assert.That(obj.types, Is.Null);
        }

        [Test]
        public void GenericObject_Simple_DefaultConstructor_InitializesSuccessfully()
        {
            // Act
            var obj = new GenericObject_Simple();

            // Assert
            Assert.That(obj, Is.Not.Null);
            Assert.That(obj.column, Is.Null);
            Assert.That(obj.value, Is.Null);
            Assert.That(obj.type, Is.Null);
        }
    }
}
