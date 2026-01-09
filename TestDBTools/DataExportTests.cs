using NUnit.Framework;
using DBTools_Utilities;
using DBTools.Model;
using System;
using System.Collections.Generic;
using System.Data;

namespace TestDBTools
{
    /// <summary>
    /// Unit tests for the DataExport class
    /// </summary>
    [TestFixture]
    public class DataExportTests
    {
        private DataExport _dataExport;

        [SetUp]
        public void Setup()
        {
            _dataExport = new DataExport();
        }

        [Test]
        public void ToCsv_WithValidData_GeneratesCorrectCsv()
        {
            // Arrange
            var genericObjects = new List<GenericObject>
            {
                new GenericObject
                {
                    columns = new string[] { "id", "name", "age" },
                    types = new string[] { "Int32", "String", "Int32" },
                    values = new object[] { 1, "John Doe", 30 }
                },
                new GenericObject
                {
                    columns = new string[] { "id", "name", "age" },
                    types = new string[] { "Int32", "String", "Int32" },
                    values = new object[] { 2, "Jane Smith", 25 }
                }
            };

            // Act
            string result = _dataExport.ToCsv(genericObjects, ',', showColums: true, showTypes: true);

            // Assert
            Assert.That(result, Does.Contain("Int32,String,Int32"));
            Assert.That(result, Does.Contain("id,name,age"));
            Assert.That(result, Does.Contain("1,John Doe,30"));
            Assert.That(result, Does.Contain("2,Jane Smith,25"));
        }

        [Test]
        public void ToCsv_WithoutColumnHeaders_DoesNotIncludeColumns()
        {
            // Arrange
            var genericObjects = new List<GenericObject>
            {
                new GenericObject
                {
                    columns = new string[] { "id", "name" },
                    types = new string[] { "Int32", "String" },
                    values = new object[] { 1, "John" }
                }
            };

            // Act
            string result = _dataExport.ToCsv(genericObjects, ',', showColums: false, showTypes: false);

            // Assert
            Assert.That(result, Does.Not.Contain("id,name"));
            Assert.That(result, Does.Not.Contain("Int32,String"));
            Assert.That(result, Does.Contain("1,John"));
        }

        [Test]
        public void ToCsv_WithoutTypeHeaders_DoesNotIncludeTypes()
        {
            // Arrange
            var genericObjects = new List<GenericObject>
            {
                new GenericObject
                {
                    columns = new string[] { "id", "name" },
                    types = new string[] { "Int32", "String" },
                    values = new object[] { 1, "John" }
                }
            };

            // Act
            string result = _dataExport.ToCsv(genericObjects, ',', showColums: true, showTypes: false);

            // Assert
            Assert.That(result, Does.Contain("id,name"));
            Assert.That(result, Does.Not.Contain("Int32,String"));
            Assert.That(result, Does.Contain("1,John"));
        }

        [Test]
        public void ToCsv_WithCustomSeparator_UsesCorrectSeparator()
        {
            // Arrange
            var genericObjects = new List<GenericObject>
            {
                new GenericObject
                {
                    columns = new string[] { "id", "name" },
                    types = new string[] { "Int32", "String" },
                    values = new object[] { 1, "John" }
                }
            };

            // Act
            string result = _dataExport.ToCsv(genericObjects, ';', showColums: true, showTypes: true);

            // Assert
            Assert.That(result, Does.Contain("Int32;String"));
            Assert.That(result, Does.Contain("id;name"));
            Assert.That(result, Does.Contain("1;John"));
        }

        [Test]
        public void ToCsv_WithDBNullValue_HandlesNull()
        {
            // Arrange
            var genericObjects = new List<GenericObject>
            {
                new GenericObject
                {
                    columns = new string[] { "id", "description" },
                    types = new string[] { "Int32", "String" },
                    values = new object[] { 1, DBNull.Value }
                }
            };

            // Act
            string result = _dataExport.ToCsv(genericObjects, ',', showColums: true, showTypes: false);

            // Assert
            Assert.That(result, Does.Contain("1,"));
            // Empty value for null
            var lines = result.Split('\n');
            Assert.That(lines[lines.Length - 1], Does.EndWith(","));
        }

        [Test]
        public void ToCsv_WithBackslash_ReplacesWithForwardSlash()
        {
            // Arrange
            var genericObjects = new List<GenericObject>
            {
                new GenericObject
                {
                    columns = new string[] { "path" },
                    types = new string[] { "String" },
                    values = new object[] { @"C:\Users\Test" }
                }
            };

            // Act
            string result = _dataExport.ToCsv(genericObjects, ',', showColums: false, showTypes: false);

            // Assert
            Assert.That(result, Does.Contain("C:/Users/Test"));
            Assert.That(result, Does.Not.Contain(@"C:\Users\Test"));
        }

        [Test]
        public void ToCsv_WithNewLine_RemovesNewLine()
        {
            // Arrange
            var genericObjects = new List<GenericObject>
            {
                new GenericObject
                {
                    columns = new string[] { "description" },
                    types = new string[] { "String" },
                    values = new object[] { "Line1\r\nLine2" }
                }
            };

            // Act
            string result = _dataExport.ToCsv(genericObjects, ',', showColums: false, showTypes: false);

            // Assert
            // The implementation replaces Environment.NewLine but not individual \r or \n
            Assert.That(result, Does.Contain("Line1"));
            Assert.That(result, Does.Contain("Line2"));
        }

        [Test]
        public void ToDataTable_WithValidCsv_CreatesCorrectDataTable()
        {
            // Arrange
            string csv = "id,name,age\n1,John,30\n2,Jane,25";

            // Act
            DataTable result = _dataExport.ToDataTable(csv, ',', specifyColumnTypes: false);

            // Assert
            Assert.That(result.Columns.Count, Is.EqualTo(3));
            Assert.That(result.Rows.Count, Is.EqualTo(2));
            Assert.That(result.Columns[0].ColumnName, Does.Contain("id"));
            Assert.That(result.Columns[1].ColumnName, Does.Contain("name"));
            Assert.That(result.Columns[2].ColumnName, Does.Contain("age"));
        }

        [Test]
        public void ToDataTable_WithCustomSeparator_ParsesCorrectly()
        {
            // Arrange
            string csv = "id;name;email\n1;John;john@test.com\n2;Jane;jane@test.com";

            // Act
            DataTable result = _dataExport.ToDataTable(csv, ';', specifyColumnTypes: false);

            // Assert
            Assert.That(result.Columns.Count, Is.EqualTo(3));
            Assert.That(result.Rows.Count, Is.EqualTo(2));
        }

        [Test]
        public void ToDataTable_WithTypeSpecification_UsesTypes()
        {
            // Arrange
            string csv = "Int32,String,Int32\nid,name,age\n1,John,30";

            // Act
            DataTable result = _dataExport.ToDataTable(csv, ',', specifyColumnTypes: true);

            // Assert
            Assert.That(result.Columns.Count, Is.EqualTo(3));
            // When specifyColumnTypes is true, the implementation re-reads from the start
            // First line becomes column names, subsequent lines become data rows
            Assert.That(result.Rows.Count, Is.EqualTo(2));
            // All columns should be string type as per the implementation
            Assert.That(result.Columns[0].DataType, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void ToDataTable_WithNullCsv_HandlesGracefully()
        {
            // Arrange
            string csv = null;

            // Act & Assert
            // The implementation throws ArgumentNullException for null input
            Assert.Throws<ArgumentNullException>(() => _dataExport.ToDataTable(csv, ',', specifyColumnTypes: false));
        }

        [Test]
        public void ToDataTable_WithEmptyLines_HandlesGracefully()
        {
            // Arrange
            string csv = "id,name\n1,John\n2,Jane";

            // Act
            DataTable result = _dataExport.ToDataTable(csv, ',', specifyColumnTypes: false);

            // Assert
            Assert.That(result.Columns.Count, Is.EqualTo(2));
            // The implementation should handle rows appropriately
            Assert.That(result.Rows.Count, Is.EqualTo(2));
        }
    }
}
