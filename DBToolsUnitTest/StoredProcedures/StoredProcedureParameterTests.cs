using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.StoredProcedures;
using System.Data;

namespace DBToolsUnitTest.StoredProcedures
{
    [TestClass]
    public class StoredProcedureParameterTests : TestBase
    {
        #region Input Factory Method Tests

        [TestMethod]
        public void Input_CreatesParameterWithCorrectName()
        {
            var param = StoredProcedureParameter.Input("UserId", 42);
            Assert.AreEqual("UserId", param.Name);
        }

        [TestMethod]
        public void Input_CreatesParameterWithCorrectValue()
        {
            var param = StoredProcedureParameter.Input("UserId", 42);
            Assert.AreEqual(42, param.Value);
        }

        [TestMethod]
        public void Input_CreatesParameterWithInputDirection()
        {
            var param = StoredProcedureParameter.Input("UserId", 42);
            Assert.AreEqual(ParameterDirection.Input, param.Direction);
        }

        [TestMethod]
        public void Input_DbTypeIsNull()
        {
            var param = StoredProcedureParameter.Input("Name", "test");
            Assert.IsNull(param.DbType);
        }

        [TestMethod]
        public void Input_SizeIsNull()
        {
            var param = StoredProcedureParameter.Input("Name", "test");
            Assert.IsNull(param.Size);
        }

        [TestMethod]
        public void Input_SupportsNullValue()
        {
            var param = StoredProcedureParameter.Input("NullParam", null);
            Assert.IsNull(param.Value);
            Assert.AreEqual(ParameterDirection.Input, param.Direction);
        }

        #endregion

        #region Output Factory Method Tests

        [TestMethod]
        public void Output_CreatesParameterWithCorrectName()
        {
            var param = StoredProcedureParameter.Output("ResultCount", DbType.Int32);
            Assert.AreEqual("ResultCount", param.Name);
        }

        [TestMethod]
        public void Output_CreatesParameterWithOutputDirection()
        {
            var param = StoredProcedureParameter.Output("ResultCount", DbType.Int32);
            Assert.AreEqual(ParameterDirection.Output, param.Direction);
        }

        [TestMethod]
        public void Output_CreatesParameterWithSpecifiedDbType()
        {
            var param = StoredProcedureParameter.Output("ResultCount", DbType.Int32);
            Assert.AreEqual(DbType.Int32, param.DbType);
        }

        [TestMethod]
        public void Output_ValueIsNull()
        {
            var param = StoredProcedureParameter.Output("ResultCount", DbType.Int32);
            Assert.IsNull(param.Value);
        }

        [TestMethod]
        public void Output_WithSize_SetsSize()
        {
            var param = StoredProcedureParameter.Output("Message", DbType.String, 255);
            Assert.AreEqual(255, param.Size);
        }

        [TestMethod]
        public void Output_WithoutSize_SizeIsNull()
        {
            var param = StoredProcedureParameter.Output("Count", DbType.Int32);
            Assert.IsNull(param.Size);
        }

        #endregion

        #region InputOutput Factory Method Tests

        [TestMethod]
        public void InputOutput_CreatesParameterWithCorrectName()
        {
            var param = StoredProcedureParameter.InputOutput("Counter", 10, DbType.Int32);
            Assert.AreEqual("Counter", param.Name);
        }

        [TestMethod]
        public void InputOutput_CreatesParameterWithCorrectValue()
        {
            var param = StoredProcedureParameter.InputOutput("Counter", 10, DbType.Int32);
            Assert.AreEqual(10, param.Value);
        }

        [TestMethod]
        public void InputOutput_CreatesParameterWithInputOutputDirection()
        {
            var param = StoredProcedureParameter.InputOutput("Counter", 10, DbType.Int32);
            Assert.AreEqual(ParameterDirection.InputOutput, param.Direction);
        }

        [TestMethod]
        public void InputOutput_CreatesParameterWithSpecifiedDbType()
        {
            var param = StoredProcedureParameter.InputOutput("Counter", 10, DbType.Int32);
            Assert.AreEqual(DbType.Int32, param.DbType);
        }

        [TestMethod]
        public void InputOutput_WithSize_SetsSize()
        {
            var param = StoredProcedureParameter.InputOutput("Buffer", "hello", DbType.String, 100);
            Assert.AreEqual(100, param.Size);
        }

        [TestMethod]
        public void InputOutput_WithoutSize_SizeIsNull()
        {
            var param = StoredProcedureParameter.InputOutput("Counter", 10, DbType.Int32);
            Assert.IsNull(param.Size);
        }

        #endregion

        #region ReturnValue Factory Method Tests

        [TestMethod]
        public void ReturnValue_CreatesParameterWithReturnValueDirection()
        {
            var param = StoredProcedureParameter.ReturnValue();
            Assert.AreEqual(ParameterDirection.ReturnValue, param.Direction);
        }

        [TestMethod]
        public void ReturnValue_HasDefaultName()
        {
            var param = StoredProcedureParameter.ReturnValue();
            Assert.AreEqual("@RETURN_VALUE", param.Name);
        }

        [TestMethod]
        public void ReturnValue_ValueIsNull()
        {
            var param = StoredProcedureParameter.ReturnValue();
            Assert.IsNull(param.Value);
        }

        [TestMethod]
        public void ReturnValue_DbTypeIsInt32()
        {
            var param = StoredProcedureParameter.ReturnValue();
            Assert.AreEqual(DbType.Int32, param.DbType);
        }

        #endregion

        #region Direct Property Assignment Tests

        [TestMethod]
        public void DirectAssignment_AllPropertiesSetCorrectly()
        {
            var param = new StoredProcedureParameter
            {
                Name = "TestParam",
                Value = "TestValue",
                Direction = ParameterDirection.Input,
                DbType = DbType.AnsiString,
                Size = 50
            };

            Assert.AreEqual("TestParam", param.Name);
            Assert.AreEqual("TestValue", param.Value);
            Assert.AreEqual(ParameterDirection.Input, param.Direction);
            Assert.AreEqual(DbType.AnsiString, param.DbType);
            Assert.AreEqual(50, param.Size);
        }

        [TestMethod]
        public void DefaultConstructor_DirectionDefaultsToInput()
        {
            var param = new StoredProcedureParameter();
            // Default enum value for ParameterDirection is Input (0)
            Assert.AreEqual(ParameterDirection.Input, param.Direction);
        }

        #endregion
    }
}
