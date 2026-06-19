using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Models;
using System;

namespace DBToolsUnitTest.Models
{
    [TestClass]
    public class GenericObjectTests : TestBase
    {
        [TestMethod]
        public void Constructor_Default_ShouldCreateInstanceWithoutClient()
        {
            var obj = new GenericObject();
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.DbTools);
        }

        [TestMethod]
        public void Constructor_WithClient_ShouldExposeClientViaDbTools()
        {
            var client = new FakeSqlClient();
            var obj = new GenericObject(client);
            Assert.IsNotNull(obj);
            Assert.AreSame(client, obj.DbTools);
        }

        [TestMethod]
        public void Constructor_WithNullClient_ShouldThrow()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new GenericObject(null));
        }

        [TestMethod]
        public void DbTools_Setter_ShouldAllowLateInjection()
        {
            var obj = new GenericObject
            {
                columns = new[] { "id", "name" },
                valuesString = new[] { "1", "John" },
                table = "Users"
            };

            Assert.IsNull(obj.DbTools);

            var client = new FakeSqlClient();
            obj.DbTools = client;

            Assert.AreSame(client, obj.DbTools);
        }

        [TestMethod]
        public void Properties_ShouldStoreValues()
        {
            var obj = new GenericObject
            {
                columns = new[] { "id", "name" },
                values = new object[] { 1, "John" },
                types = new[] { "Int32", "String" },
                valuesString = new[] { "1", "John" },
                table = "Users"
            };
            Assert.AreEqual("Users", obj.table);
            Assert.AreEqual(2, obj.columns.Length);
            Assert.AreEqual(2, obj.values.Length);
            Assert.AreEqual(2, obj.valuesString.Length);
        }

        [TestMethod]
        public void Insert_WithoutClient_ShouldThrowInvalidOperation()
        {
            var obj = new GenericObject
            {
                columns = new[] { "id", "name" },
                valuesString = new[] { "1", "John" },
                table = "Users"
            };

            var ex = Assert.ThrowsException<InvalidOperationException>(() => obj.Insert());
            StringAssert.Contains(ex.Message, "ISqlClient");
        }

        [TestMethod]
        public void Update_WithoutClient_ShouldThrowInvalidOperation()
        {
            var obj = new GenericObject
            {
                columns = new[] { "name" },
                valuesString = new[] { "Jane" },
                table = "Users"
            };

            var ex = Assert.ThrowsException<InvalidOperationException>(() => obj.Update("id = 1"));
            StringAssert.Contains(ex.Message, "ISqlClient");
        }

        [TestMethod]
        public void Insert_WithClient_ShouldForwardArgumentsAndReturnClientResult()
        {
            var client = new FakeSqlClient { InsertResult = true };
            var obj = new GenericObject(client)
            {
                columns = new[] { "id", "name" },
                valuesString = new[] { "1", "John" },
                table = "Users"
            };

            bool result = obj.Insert();

            Assert.IsTrue(result);
            Assert.AreEqual(1, client.InsertCallCount);
            Assert.IsNotNull(client.LastInsert);
            CollectionAssert.AreEqual(new[] { "id", "name" }, client.LastInsert.Fields);
            Assert.AreEqual("Users", client.LastInsert.Table);
            CollectionAssert.AreEqual(new[] { "1", "John" }, client.LastInsert.Values);
        }

        [TestMethod]
        public void Insert_WithClientReturningFalse_ShouldPropagateResult()
        {
            var client = new FakeSqlClient { InsertResult = false };
            var obj = new GenericObject(client)
            {
                columns = new[] { "name" },
                valuesString = new[] { "John" },
                table = "Users"
            };

            Assert.IsFalse(obj.Insert());
            Assert.AreEqual(1, client.InsertCallCount);
        }

        [TestMethod]
        public void Update_WithClient_ShouldForwardArgumentsAndReturnClientResult()
        {
            var client = new FakeSqlClient { UpdateResult = true };
            var obj = new GenericObject(client)
            {
                columns = new[] { "name" },
                valuesString = new[] { "Jane" },
                table = "Users"
            };

            bool result = obj.Update("id = @p0");

            Assert.IsTrue(result);
            Assert.AreEqual(1, client.UpdateCallCount);
            Assert.IsNotNull(client.LastUpdate);
            CollectionAssert.AreEqual(new[] { "name" }, client.LastUpdate.Fields);
            Assert.AreEqual("Users", client.LastUpdate.Table);
            CollectionAssert.AreEqual(new[] { "Jane" }, client.LastUpdate.Values);
            Assert.AreEqual("id = @p0", client.LastUpdate.Condition);
        }

        [TestMethod]
        public void DbTools_Setter_CanDetachClient()
        {
            var client = new FakeSqlClient();
            var obj = new GenericObject(client);

            obj.DbTools = null;

            Assert.IsNull(obj.DbTools);
            Assert.ThrowsException<InvalidOperationException>(() => obj.Insert());
        }
    }
}