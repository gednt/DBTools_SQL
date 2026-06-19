using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Core;
using System;

namespace DBToolsUnitTest.Core
{
    [TestClass]
    [TestCategory("Integration")]
    public class ObsoleteMethodTests : TestBase
    {
        [TestMethod]
        public void Update_ObsoleteOverload_RequiresCondition()
        {
            SkipIfDatabaseUnavailable();
            var utils = new SqlClient();
            string[] fields = { "name" };
            string[] values = { "John" };
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
}
