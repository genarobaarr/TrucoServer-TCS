using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class SocialLinksSTests
    {
        [TestMethod]
        public void SocialLinksSerializationTest()
        {
            var original = new SocialLinks
            {
                FacebookHandle = "testFB",
                XHandle = "testX",
                InstagramHandle = "testIG"
            };

            string json = JsonConvert.SerializeObject(original);
            var copy = JsonConvert.DeserializeObject<SocialLinks>(json);

            Assert.AreEqual(original.FacebookHandle, copy.FacebookHandle);
            Assert.AreEqual(original.XHandle, copy.XHandle);
            Assert.AreEqual(original.InstagramHandle, copy.InstagramHandle);
        }
    }
}
