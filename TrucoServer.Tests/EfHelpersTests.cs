using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TrucoServer;

namespace TrucoServer.Tests
{
    [TestClass]
    public class EfHelpersTests
    {
        private class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public byte[] Data { get; set; }
            public TestEntity Parent { get; set; }
        }

        [TestMethod]
        public void TestGetPropValueReturnsCorrectInt()
        {
            var entity = new TestEntity { Id = 10 };

            var result = EfHelpers.GetPropValue<int>(entity, "Id");

            Assert.AreEqual(10, result);
        }

        [TestMethod]
        public void TestGetPropValueReturnsCorrectString()
        {
            var entity = new TestEntity { Name = "TestName" };

            var result = EfHelpers.GetPropValue<string>(entity, "Name");

            Assert.AreEqual("TestName", result);
        }

        [TestMethod]
        public void TestGetPropValueConvertsByteArrayToString()
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes("ByteData");
            var entity = new TestEntity { Data = bytes };

            var result = EfHelpers.GetPropValue<string>(entity, "Data");

            Assert.AreEqual("ByteData", result);
        }

        [TestMethod]
        public void TestGetPropValueReturnsDefaultWhenEntityIsNull()
        {
            var result = EfHelpers.GetPropValue<int>(null, "Id");

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void TestGetPropValueReturnsDefaultWhenPropertyNotFound()
        {
            var entity = new TestEntity();

            var result = EfHelpers.GetPropValue<int>(entity, "NonExistentProp");

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void TestGetPropValueReturnsFirstValidProperty()
        {
            var entity = new TestEntity { Name = "FoundMe" };

            var result = EfHelpers.GetPropValue<string>(entity, "WrongName", "Name");

            Assert.AreEqual("FoundMe", result);
        }

        [TestMethod]
        public void TestGetNavigationReturnsCorrectObject()
        {
            var parent = new TestEntity { Id = 99 };
            var entity = new TestEntity { Parent = parent };

            var result = EfHelpers.GetNavigation(entity, "Parent");

            Assert.AreEqual(parent, result);
        }

        [TestMethod]
        public void TestGetNavigationReturnsNullWhenEntityIsNull()
        {
            var result = EfHelpers.GetNavigation(null, "Parent");

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetNavigationReturnsNullWhenPropertyMissing()
        {
            var entity = new TestEntity();

            var result = EfHelpers.GetNavigation(entity, "NonExistent");

            Assert.IsNull(result);
        }
    }
}