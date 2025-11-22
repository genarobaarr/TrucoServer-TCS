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
    public class TrucoCardSTests
    {
        private const int TEST_EMPTY_STREAM_LENGTH = 0;

        [TestMethod]
        public void TestSerializationReturnsNotEmptyStream()
        {
            var card = new TrucoCard(Rank.Once, Suit.Sword);
            var serializer = new DataContractSerializer(typeof(TrucoCard));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, card);
                Assert.IsTrue(stream.Length > TEST_EMPTY_STREAM_LENGTH);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectRank()
        {
            var originalCard = new TrucoCard(Rank.Siete, Suit.Gold);
            var serializer = new DataContractSerializer(typeof(TrucoCard));
            byte[] data;
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, originalCard);
                data = stream.ToArray();
            }
            using (var stream = new MemoryStream(data))
            {
                var deserializedCard = (TrucoCard)serializer.ReadObject(stream);
                Assert.AreEqual(originalCard.CardRank, deserializedCard.CardRank);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectSuit()
        {
            var originalCard = new TrucoCard(Rank.Siete, Suit.Gold);
            var serializer = new DataContractSerializer(typeof(TrucoCard));
            byte[] data;
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, originalCard);
                data = stream.ToArray();
            }
            using (var stream = new MemoryStream(data))
            {
                var deserializedCard = (TrucoCard)serializer.ReadObject(stream);
                Assert.AreEqual(originalCard.CardSuit, deserializedCard.CardSuit);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectFileName()
        {
            var originalCard = new TrucoCard(Rank.Siete, Suit.Gold);
            var serializer = new DataContractSerializer(typeof(TrucoCard));
            byte[] data;
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, originalCard);
                data = stream.ToArray();
            }
            using (var stream = new MemoryStream(data))
            {
                var deserializedCard = (TrucoCard)serializer.ReadObject(stream);
                Assert.AreEqual(originalCard.FileName, deserializedCard.FileName);
            }
        }
    }
}
