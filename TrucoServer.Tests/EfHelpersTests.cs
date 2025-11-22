using Microsoft.VisualStudio.TestTools.UnitTesting;
using TrucoServer.Data.Entities;

namespace TrucoServer.Tests
{
    [TestClass]
    public class EfHelpersTests
    {
        private const int TEST_ENTITY_ID_INT = 10;
        private const int TEST_ENTITY_SECOND_ID_INT = 99;
        private const int TEST_ENTITY_IS_NULL = 0;
        private const string TEST_ENTITY_ID_STRING= "Id";
        private const string TEST_ENTITY_TEST_NAME_STRING = "TestName";
        private const string TEST_ENTITY_NAME_STRING = "Name";
        private const string TEST_ENTITY_PARENT = "Parent";
        private const string TEST_ENTITY_BYTE_DATA = "ByteData";
        private const string TEST_ENTITY_DATA = "Data";
        private const string TEST_ENTITY_NON_EXISTENT_PROP = "NonExistentProp";
        private const string TEST_ENTITY_FOUND_ME = "FoundMe";
        private const string TEST_ENTITY_WRONG_NAME = "WrongName";

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
            var entity = new TestEntity { Id = TEST_ENTITY_ID_INT };

            var result = EfHelpers.GetPropValue<int>(entity, TEST_ENTITY_ID_STRING);

            Assert.AreEqual(10, result);
        }

        [TestMethod]
        public void TestGetPropValueReturnsCorrectString()
        {
            var entity = new TestEntity { Name = TEST_ENTITY_TEST_NAME_STRING };

            var result = EfHelpers.GetPropValue<string>(entity, TEST_ENTITY_NAME_STRING);

            Assert.AreEqual(TEST_ENTITY_TEST_NAME_STRING, result);
        }

        [TestMethod]
        public void TestGetPropValueConvertsByteArrayToString()
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(TEST_ENTITY_BYTE_DATA);
            var entity = new TestEntity { Data = bytes };

            var result = EfHelpers.GetPropValue<string>(entity, TEST_ENTITY_DATA);

            Assert.AreEqual(TEST_ENTITY_BYTE_DATA, result);
        }

        [TestMethod]
        public void TestGetPropValueReturnsDefaultWhenEntityIsNull()
        {
            var result = EfHelpers.GetPropValue<int>(null, TEST_ENTITY_ID_STRING);

            Assert.AreEqual(TEST_ENTITY_IS_NULL, result);
        }

        [TestMethod]
        public void TestGetPropValueReturnsDefaultWhenPropertyNotFound()
        {
            var entity = new TestEntity();

            var result = EfHelpers.GetPropValue<int>(entity, TEST_ENTITY_NON_EXISTENT_PROP);

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void TestGetPropValueReturnsFirstValidProperty()
        {
            var entity = new TestEntity { Name = TEST_ENTITY_FOUND_ME };

            var result = EfHelpers.GetPropValue<string>(entity, TEST_ENTITY_WRONG_NAME, TEST_ENTITY_NAME_STRING);

            Assert.AreEqual(TEST_ENTITY_FOUND_ME, result);
        }

        [TestMethod]
        public void TestGetNavigationReturnsCorrectObject()
        {
            var parent = new TestEntity { Id = TEST_ENTITY_SECOND_ID_INT };
            var entity = new TestEntity { Parent = parent };

            var result = EfHelpers.GetNavigation(entity, TEST_ENTITY_PARENT);

            Assert.AreEqual(parent, result);
        }

        [TestMethod]
        public void TestGetNavigationReturnsNullWhenEntityIsNull()
        {
            var result = EfHelpers.GetNavigation(null, TEST_ENTITY_PARENT);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetNavigationReturnsNullWhenPropertyMissing()
        {
            var entity = new TestEntity();

            var result = EfHelpers.GetNavigation(entity, TEST_ENTITY_NON_EXISTENT_PROP);

            Assert.IsNull(result);
        }
    }
}