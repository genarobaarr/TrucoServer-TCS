using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.Entities;

namespace TrucoServer.Tests.DataTests.EntititesTests
{
    [TestClass]
    public class EfHelpersTests
    {
        private const int USER_ID = 1;

        private class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [TestMethod]
        public void TestGetPropValueReturnsCorrectValueForExistingProperty()
        {
            var entity = new TestEntity
            {
                Id = USER_ID,
                Name = "TestName"
            };

            string propName = "Name";
            var result = EfHelpers.GetPropValue<string>(entity, propName);
            Assert.AreEqual("TestName", result);
        }

        [TestMethod]
        public void TestGetPropValueReturnsDefaultForNonExistingProperty()
        {
            var entity = new TestEntity 
            {
                Id = USER_ID 
            };

            string propName = "NonExistent";
            var result = EfHelpers.GetPropValue<string>(entity, propName);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetPropValueReturnsDefaultForNullEntity()
        {
            TestEntity entity = null;
            var result = EfHelpers.GetPropValue<int>(entity, "Id");
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void TestGetNavigationReturnsObjectForExistingProperty()
        {
            var entity = new TestEntity 
            {
                Name = "NavTest" 
            };

            var result = EfHelpers.GetNavigation(entity, "Name");
            Assert.AreEqual("NavTest", result);
        }

        [TestMethod]
        public void TestGetNavigationReturnsNullForNullEntity()
        {
            TestEntity entity = null;
            var result = EfHelpers.GetNavigation(entity, "AnyProp");
            Assert.IsNull(result);
        }
    }
}
