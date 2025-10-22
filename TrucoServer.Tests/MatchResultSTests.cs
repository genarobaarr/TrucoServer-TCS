using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class MatchResultSTests
    {
        [TestMethod]
        public void MatchResultSerializationTests()
        {
            var original = new MatchResult
            {
                Player1 = "test",
                Player2 = "test2",
                Winner = "test2",
                Date = "2025-10-22"
            };

            var serializer = new DataContractSerializer(typeof(MatchResult));
            using (var memoryStream = new MemoryStream())
            {
                serializer.WriteObject(memoryStream, original);
                memoryStream.Position = 0;

                var deserialized = (MatchResult)serializer.ReadObject(memoryStream);

                Assert.AreEqual(original.Player1, deserialized.Player1);
                Assert.AreEqual(original.Player2, deserialized.Player2);
                Assert.AreEqual(original.Winner, deserialized.Winner);
                Assert.AreEqual(original.Date, deserialized.Date);
            }
        }
    }
}
