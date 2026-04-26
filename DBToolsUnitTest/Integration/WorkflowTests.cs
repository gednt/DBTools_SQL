using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Core;
using DBTools.Models;
using DBTools.Export;
using System;
using System.Collections.Generic;
using System.Data;

namespace DBToolsUnitTest.Integration
{
    [TestClass]
    public class WorkflowTests : TestBase
    {
        [TestMethod]
        public void CompleteWorkflow_QueryBuilding_ShouldWork()
        {
            string[] fields = { "name", "email", "age", "test" };
            string[] values = { "John Doe", "john@test.com", "30", "test" };
            string insertQuery = SqlClient.Insert_Query(fields, "Users", values);
            string updateQuery = SqlClient.Update_Query(fields, "Users", values, "id = 1");
            string selectQuery = SqlClient.Select_Query("*", "Users", "age > 18");
            string deleteQuery = SqlClient.Delete_Query("Users", "id = 1");
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

            int errorCount = 0;
            string ErrorMessages = "";

            try
            {
                string insertQuery = SqlClient.Insert_Query(fields, tableName, values);
            }
            catch (Exception e)
            {
                errorCount++;
                ErrorMessages += e.Message + "\n";
            }

            try
            {
                string updateQuery = SqlClient.Update_Query(fields, tableName, values, "id = 1; DROP TABLE Users--");
                Assert.IsFalse(updateQuery.Contains("; DROP TABLE"), "Update query contains potential SQL injection pattern.");
            }
            catch (Exception e)
            {
                errorCount++;
                ErrorMessages += e.Message + "\n";
            }

            try
            {
                string selectQuery = SqlClient.Select_Query("*", tableName, "age > 18; DROP TABLE Users--");
            }
            catch (Exception e)
            {
                errorCount++;
                ErrorMessages += e.Message + "\n";
            }

            try
            {
                string deleteQuery = SqlClient.Delete_Query(tableName, "id = 1; DROP TABLE Users--");
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
            string csv = dataExport.ToCsv(genericObjects, ',', true, true);
            DataTable dt = dataExport.ToDataTable(csv, ',', true);
            Assert.IsNotNull(csv);
            Assert.IsNotNull(dt);
            Assert.IsTrue(csv.Length > 0);
        }
    }
}
