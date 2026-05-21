using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Abstractions;
using DBTools.Providers;
using DBTools.StoredProcedures;
using System.Data;
using System.Data.Common;

namespace DBToolsUnitTest.StoredProcedures
{
    [TestClass]
    public class StoredProcedureClientTests : TestBase
    {
        #region Constructor Tests

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentNullException))]
        public void Constructor_NullProvider_ThrowsArgumentNullException()
        {
            new StoredProcedureClient(null, "connection-string");
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentNullException))]
        public void Constructor_NullConnectionString_ThrowsArgumentNullException()
        {
            var provider = new SqlServerProvider();
            new StoredProcedureClient(provider, null);
        }

        [TestMethod]
        public void Constructor_ValidArguments_CreatesInstance()
        {
            var provider = new SqlServerProvider();
            var client = new StoredProcedureClient(provider, "Data Source=localhost;");
            Assert.IsNotNull(client);
        }

        #endregion

        #region ExecuteStoredProcedure Validation Tests

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void ExecuteStoredProcedure_NullProcedureName_ThrowsArgumentException()
        {
            var provider = new SqlServerProvider();
            var client = new StoredProcedureClient(provider, "Data Source=localhost;");
            client.ExecuteStoredProcedure(null, new StoredProcedureParameter[0]);
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void ExecuteStoredProcedure_EmptyProcedureName_ThrowsArgumentException()
        {
            var provider = new SqlServerProvider();
            var client = new StoredProcedureClient(provider, "Data Source=localhost;");
            client.ExecuteStoredProcedure("", new StoredProcedureParameter[0]);
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void ExecuteStoredProcedureReader_NullProcedureName_ThrowsArgumentException()
        {
            var provider = new SqlServerProvider();
            var client = new StoredProcedureClient(provider, "Data Source=localhost;");
            client.ExecuteStoredProcedureReader(null, new StoredProcedureParameter[0]);
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void ExecuteStoredProcedureReader_EmptyProcedureName_ThrowsArgumentException()
        {
            var provider = new SqlServerProvider();
            var client = new StoredProcedureClient(provider, "Data Source=localhost;");
            client.ExecuteStoredProcedureReader("", new StoredProcedureParameter[0]);
        }

        #endregion

        #region Async Validation Tests

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public async System.Threading.Tasks.Task ExecuteStoredProcedureAsync_NullProcedureName_ThrowsArgumentException()
        {
            var provider = new SqlServerProvider();
            var client = new StoredProcedureClient(provider, "Data Source=localhost;");
            await client.ExecuteStoredProcedureAsync(null, new StoredProcedureParameter[0]);
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public async System.Threading.Tasks.Task ExecuteStoredProcedureAsync_EmptyProcedureName_ThrowsArgumentException()
        {
            var provider = new SqlServerProvider();
            var client = new StoredProcedureClient(provider, "Data Source=localhost;");
            await client.ExecuteStoredProcedureAsync("", new StoredProcedureParameter[0]);
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public async System.Threading.Tasks.Task ExecuteStoredProcedureReaderAsync_NullProcedureName_ThrowsArgumentException()
        {
            var provider = new SqlServerProvider();
            var client = new StoredProcedureClient(provider, "Data Source=localhost;");
            await client.ExecuteStoredProcedureReaderAsync(null, new StoredProcedureParameter[0]);
        }

        #endregion

        #region Parameter Configuration Tests (using SqlServerProvider to inspect command setup)

        [TestMethod]
        public void ParameterSetup_InputParameter_CreatesDbParameterWithInputDirection()
        {
            var provider = new SqlServerProvider();
            var param = provider.CreateParameter("@UserId", 42);

            // Verify the provider creates a valid parameter
            Assert.IsNotNull(param);
            Assert.AreEqual("@UserId", param.ParameterName);
            Assert.AreEqual(42, param.Value);

            // Verify direction can be set
            param.Direction = ParameterDirection.Input;
            Assert.AreEqual(ParameterDirection.Input, param.Direction);
        }

        [TestMethod]
        public void ParameterSetup_OutputParameter_DirectionSetCorrectly()
        {
            var provider = new SqlServerProvider();
            var param = provider.CreateParameter("@ResultCount", System.DBNull.Value);

            param.Direction = ParameterDirection.Output;
            param.DbType = DbType.Int32;
            param.Size = 4;

            Assert.AreEqual(ParameterDirection.Output, param.Direction);
            Assert.AreEqual(DbType.Int32, param.DbType);
            Assert.AreEqual(4, param.Size);
        }

        [TestMethod]
        public void ParameterSetup_InputOutputParameter_DirectionSetCorrectly()
        {
            var provider = new SqlServerProvider();
            var param = provider.CreateParameter("@Counter", 10);

            param.Direction = ParameterDirection.InputOutput;
            param.DbType = DbType.Int32;

            Assert.AreEqual(ParameterDirection.InputOutput, param.Direction);
            Assert.AreEqual(10, param.Value);
        }

        [TestMethod]
        public void ParameterSetup_ReturnValueParameter_DirectionSetCorrectly()
        {
            var provider = new SqlServerProvider();
            var param = provider.CreateParameter("@RETURN_VALUE", System.DBNull.Value);

            param.Direction = ParameterDirection.ReturnValue;
            param.DbType = DbType.Int32;

            Assert.AreEqual(ParameterDirection.ReturnValue, param.Direction);
        }

        [TestMethod]
        public void CommandSetup_StoredProcedureType_IsConfiguredCorrectly()
        {
            var provider = new SqlServerProvider();
            var command = provider.CreateCommand();

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "usp_GetUsers";

            Assert.AreEqual(CommandType.StoredProcedure, command.CommandType);
            Assert.AreEqual("usp_GetUsers", command.CommandText);
        }

        [TestMethod]
        public void CommandSetup_ParametersAdded_CorrectCount()
        {
            var provider = new SqlServerProvider();
            var command = provider.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "usp_CreateUser";

            var param1 = provider.CreateParameter("@Name", "John");
            param1.Direction = ParameterDirection.Input;
            command.Parameters.Add(param1);

            var param2 = provider.CreateParameter("@NewId", System.DBNull.Value);
            param2.Direction = ParameterDirection.Output;
            param2.DbType = DbType.Int32;
            command.Parameters.Add(param2);

            var returnParam = provider.CreateParameter("@RETURN_VALUE", System.DBNull.Value);
            returnParam.Direction = ParameterDirection.ReturnValue;
            command.Parameters.Add(returnParam);

            Assert.AreEqual(3, command.Parameters.Count);
        }

        [TestMethod]
        public void CommandSetup_ParametersAdded_DirectionsPreserved()
        {
            var provider = new SqlServerProvider();
            var command = provider.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "usp_Process";

            var inputParam = provider.CreateParameter("@Input", "value");
            inputParam.Direction = ParameterDirection.Input;
            command.Parameters.Add(inputParam);

            var outputParam = provider.CreateParameter("@Output", System.DBNull.Value);
            outputParam.Direction = ParameterDirection.Output;
            outputParam.DbType = DbType.String;
            outputParam.Size = 100;
            command.Parameters.Add(outputParam);

            var p0 = (DbParameter)command.Parameters[0];
            var p1 = (DbParameter)command.Parameters[1];

            Assert.AreEqual(ParameterDirection.Input, p0.Direction);
            Assert.AreEqual(ParameterDirection.Output, p1.Direction);
            Assert.AreEqual(DbType.String, p1.DbType);
            Assert.AreEqual(100, p1.Size);
        }

        #endregion

        #region Interface Implementation Tests

        [TestMethod]
        public void StoredProcedureClient_ImplementsIStoredProcedureClient()
        {
            var provider = new SqlServerProvider();
            var client = new StoredProcedureClient(provider, "Data Source=localhost;");
            Assert.IsInstanceOfType(client, typeof(IStoredProcedureClient));
        }

        [TestMethod]
        public void StoredProcedureClient_ImplementsIAsyncStoredProcedureClient()
        {
            var provider = new SqlServerProvider();
            var client = new StoredProcedureClient(provider, "Data Source=localhost;");
            Assert.IsInstanceOfType(client, typeof(IAsyncStoredProcedureClient));
        }

        #endregion
    }
}
