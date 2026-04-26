using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBTools.Models;

namespace DBToolsUnitTest.Models
{
    [TestClass]
    public class GenericObjectSimpleTests : TestBase
    {
        [TestMethod]
        public void Properties_ShouldStoreValues()
        {
            var obj = new GenericObject_Simple
            {
                column = "name",
                value = "John",
                type = "String"
            };
            Assert.AreEqual("name", obj.column);
            Assert.AreEqual("John", obj.value);
            Assert.AreEqual("String", obj.type);
        }
    }
}
