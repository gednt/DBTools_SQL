using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Core;
using DBTools.Controllers;

namespace DBToolsUnitTest.Controllers
{
    [TestClass]
    public class DBToolsControllerTests : TestBase
    {
        [TestMethod]
        public void Constructor_ShouldInitialize()
        {
            var dbTools = new DBTools.Core.DBTools
            {
                Host = TestHost,
                Database = TestDatabase,
                Uid = TestUid,
                Password = TestPassword
            };
            var controller = new DBToolsController(dbTools);
            Assert.IsNotNull(controller);
            Assert.IsNotNull(controller.DBTools);
        }

        [TestMethod]
        public void SqlExecuteQuery_ShouldSetQuery()
        {
            var dbTools = new DBTools.Core.DBTools();
            var controller = new DBToolsController(dbTools);
            string testQuery = "SELECT * FROM Users";
            try
            {
                controller.SqlExecuteQuery(testQuery);
            }
            catch
            {
            }
            Assert.AreEqual(testQuery, dbTools.Query);
        }
    }
}
