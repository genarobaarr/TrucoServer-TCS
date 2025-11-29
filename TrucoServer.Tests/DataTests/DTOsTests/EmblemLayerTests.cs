using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Tests.DataTests.DTOsTests
{
    [TestClass]
    public class EmblemLayerTests
    {
        [TestMethod]
        public void TestSetCoordinatesShouldStoreDoubles()
        {
            var layer = new EmblemLayer();
            double xVal = 50.5;
            layer.X = xVal;
            Assert.AreEqual(xVal, layer.X);
        }

        [TestMethod]
        public void TestNegativeScaleShouldStoreValue()
        {
            var layer = new EmblemLayer();
            layer.ScaleX = -1.0;
            Assert.AreEqual(-1.0, layer.ScaleX);
        }

        [TestMethod]
        public void TestHexColorShouldStoreString()
        {
            var layer = new EmblemLayer();
            string white = "#FFFFFF";
            layer.ColorHex = white;
            Assert.AreEqual(white, layer.ColorHex);
        }

        [TestMethod]
        public void TestRotation360ShouldStoreValue()
        {
            var layer = new EmblemLayer();
            layer.Rotation = 360.0;
            Assert.AreEqual(360.0, layer.Rotation);
        }

        [TestMethod]
        public void TestZIndexShouldAcceptIntegers()
        {
            var layer = new EmblemLayer();
            layer.ZIndex = 5;
            Assert.AreEqual(5, layer.ZIndex);
        }
    }
}
