using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.StoredProcedures;
using System.Collections.Generic;

namespace DBToolsUnitTest.StoredProcedures
{
    [TestClass]
    public class StoredProcedureResultTests : TestBase
    {
        [TestMethod]
        public void DefaultConstructor_ReturnValueIsNull()
        {
            var result = new StoredProcedureResult();
            Assert.IsNull(result.ReturnValue);
        }

        [TestMethod]
        public void DefaultConstructor_OutputParametersIsEmptyDictionary()
        {
            var result = new StoredProcedureResult();
            Assert.IsNotNull(result.OutputParameters);
            Assert.AreEqual(0, result.OutputParameters.Count);
        }

        [TestMethod]
        public void DefaultConstructor_RowsAffectedIsZero()
        {
            var result = new StoredProcedureResult();
            Assert.AreEqual(0, result.RowsAffected);
        }

        [TestMethod]
        public void ReturnValue_CanBeSetToInteger()
        {
            var result = new StoredProcedureResult { ReturnValue = 0 };
            Assert.AreEqual(0, result.ReturnValue);
        }

        [TestMethod]
        public void ReturnValue_CanBeSetToNegativeValue()
        {
            var result = new StoredProcedureResult { ReturnValue = -1 };
            Assert.AreEqual(-1, result.ReturnValue);
        }

        [TestMethod]
        public void OutputParameters_CanAddAndRetrieveValues()
        {
            var result = new StoredProcedureResult();
            result.OutputParameters["TotalCount"] = 42;
            result.OutputParameters["Message"] = "Success";

            Assert.AreEqual(42, result.OutputParameters["TotalCount"]);
            Assert.AreEqual("Success", result.OutputParameters["Message"]);
        }

        [TestMethod]
        public void OutputParameters_CanBeReplacedWithNewDictionary()
        {
            var result = new StoredProcedureResult
            {
                OutputParameters = new Dictionary<string, object>
                {
                    { "Key1", "Value1" },
                    { "Key2", 123 }
                }
            };

            Assert.AreEqual(2, result.OutputParameters.Count);
            Assert.AreEqual("Value1", result.OutputParameters["Key1"]);
            Assert.AreEqual(123, result.OutputParameters["Key2"]);
        }

        [TestMethod]
        public void RowsAffected_CanBeSetToPositiveValue()
        {
            var result = new StoredProcedureResult { RowsAffected = 5 };
            Assert.AreEqual(5, result.RowsAffected);
        }

        [TestMethod]
        public void FullResult_AllPropertiesSetCorrectly()
        {
            var result = new StoredProcedureResult
            {
                ReturnValue = 0,
                RowsAffected = 3,
                OutputParameters = new Dictionary<string, object>
                {
                    { "NewId", 100 },
                    { "Status", "Created" }
                }
            };

            Assert.AreEqual(0, result.ReturnValue);
            Assert.AreEqual(3, result.RowsAffected);
            Assert.AreEqual(2, result.OutputParameters.Count);
            Assert.AreEqual(100, result.OutputParameters["NewId"]);
            Assert.AreEqual("Created", result.OutputParameters["Status"]);
        }

        [TestMethod]
        public void OutputParameters_SupportsNullValues()
        {
            var result = new StoredProcedureResult();
            result.OutputParameters["NullParam"] = null;

            Assert.IsTrue(result.OutputParameters.ContainsKey("NullParam"));
            Assert.IsNull(result.OutputParameters["NullParam"]);
        }
    }
}
