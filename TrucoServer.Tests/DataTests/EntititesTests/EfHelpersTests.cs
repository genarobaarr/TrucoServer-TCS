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
        private class DummyEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public byte[] DataBlob { get; set; }
            public DummyChild Child { get; set; }
        }

        private class DummyChild
        {
            public string ChildName { get; set; }
        }

        [TestMethod]
        public void TestGetPropValueValidPropertyShouldReturnCorrectValue()
        {
            var entity = new DummyEntity 
            { 
                Id = 100,
                Name = "Test" };

            int result = EfHelpers.GetPropValue<int>(entity, "Id");
            Assert.AreEqual(100, result);
        }

        [TestMethod]
        public void TestGetPropValueByteArrayToStringShouldDecodeUtf8()
        {
            string expected = "Popcorn";
            var entity = new DummyEntity { DataBlob = Encoding.UTF8.GetBytes(expected) };
            string result = EfHelpers.GetPropValue<string>(entity, "DataBlob");
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestGetPropValueNonExistentPropertyShouldReturnDefault()
        {
            var entity = new DummyEntity 
            { 
                Name = "Test" 
            };

            string result = EfHelpers.GetPropValue<string>(entity, "EmptyPropperty");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetPropValueNullEntityShouldReturnDefault()
        {
            int result = EfHelpers.GetPropValue<int>(null, "Id");
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void TestGetNavigationValidChildShouldReturnObject()
        {
            var child = new DummyChild 
            { 
                ChildName = "Child" 
            };

            var entity = new DummyEntity 
            { 
                Child = child 
            };
            
            var result = EfHelpers.GetNavigation(entity, "Child");
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(DummyChild));
        }
    }
}
