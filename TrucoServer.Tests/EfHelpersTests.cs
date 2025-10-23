using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using TrucoServer;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class EfHelpersTests
    {
        private class Dummy
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public byte[] DataBytes { get; set; }
            public Dummy Child { get; set; }
        }

        [TestMethod]
        public void GetPropValueCorrectStringValue()
        {
            var obj = new Dummy { Name = "test" };
            var result = EfHelpers.GetPropValue<string>(obj, "Name");
            Assert.AreEqual("test", result);
        }

        [TestMethod]
        public void GetPropValueCorrectIntValue()
        {
            var obj = new Dummy { Age = 25 };
            var result = EfHelpers.GetPropValue<int>(obj, "Age");
            Assert.AreEqual(25, result);
        }

        [TestMethod]
        public void GetPropValueHandleByteArrayString()
        {
            var obj = new Dummy { DataBytes = Encoding.UTF8.GetBytes("TestData") };
            var result = EfHelpers.GetPropValue<string>(obj, "DataBytes");
            Assert.AreEqual("TestData", result);
        }

        [TestMethod]
        public void GetPropValueDefaultPropertyNotExist()
        {
            var obj = new Dummy { Name = "test" };
            var result = EfHelpers.GetPropValue<string>(obj, "InvalidProp");
            Assert.AreEqual(default(string), result);
        }

        [TestMethod]
        public void GetPropValueDefaultWhenValueIsNull()
        {
            var obj = new Dummy { Name = null };
            var result = EfHelpers.GetPropValue<string>(obj, "Name");
            Assert.AreEqual(null, result);
        }

        [TestMethod]
        public void GetPropValueDefaultWhenObjectIsNull()
        {
            Dummy obj = null;
            var result = EfHelpers.GetPropValue<int>(obj, "Age");
            Assert.AreEqual(default(int), result);
        }

        [TestMethod]
        public void GetNavigationChildObject()
        {
            var child = new Dummy { Name = "Child" };
            var parent = new Dummy { Child = child };

            var result = EfHelpers.GetNavigation(parent, "Child") as Dummy;

            Assert.IsNotNull(result);
            Assert.AreEqual("Child", result.Name);
        }

        [TestMethod]
        public void GetNavigationNullWhenPropertyNotFound()
        {
            var obj = new Dummy { Name = "test" };
            var result = EfHelpers.GetNavigation(obj, "InvalidProp");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetNavigationNullWhenEntityIsNull()
        {
            Dummy obj = null;
            var result = EfHelpers.GetNavigation(obj, "Child");
            Assert.IsNull(result);
        }
    }
}

