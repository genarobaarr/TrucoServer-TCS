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
        public void TestShapeIdSetValidValueReturnsValue()
        {
            var layer = new EmblemLayer();
            int expectedId = 10;
            layer.ShapeId = expectedId;
            Assert.AreEqual(expectedId, layer.ShapeId);
        }

        [TestMethod]
        public void TestColorHexSetStringReturnsString()
        {
            var layer = new EmblemLayer();
            string color = "#FFFFFF";
            layer.ColorHex = color;
            Assert.AreEqual(color, layer.ColorHex);
        }

        [TestMethod]
        public void TestScaleXSetDoubleValueReturnsDouble()
        {
            var layer = new EmblemLayer();
            double scale = 1.55;
            layer.ScaleX = scale;
            Assert.AreEqual(scale, layer.ScaleX);
        }

        [TestMethod]
        public void TestZIndexSetMinValueReturnsMinValue()
        {
            var layer = new EmblemLayer();
            int minVal = int.MinValue;
            layer.ZIndex = minVal;
            Assert.AreEqual(minVal, layer.ZIndex);
        }

        [TestMethod]
        public void TestRotationSetZeroReturnsZero()
        {
            var layer = new EmblemLayer();
            double rotation = 0.0;
            layer.Rotation = rotation;
            Assert.AreEqual(rotation, layer.Rotation);
        }
    }
}
