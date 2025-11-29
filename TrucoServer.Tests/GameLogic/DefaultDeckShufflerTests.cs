using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.GameLogic;

namespace TrucoServer.Tests.GameLogic
{
    [TestClass]
    public class DefaultDeckShufflerTests
    {
        [TestMethod]
        public void TestShuffleValidListShouldMaintainCountAndElements()
        {
            var shuffler = new DefaultDeckShuffler();
            var list = new List<int>
            {
                1,
                2,
                3,
                4,
                5
            };

            var originalCount = list.Count;
            shuffler.Shuffle(list);
            Assert.AreEqual(originalCount, list.Count, "Shuffling should not lose or add elements");
            Assert.IsTrue(list.Contains(1) && list.Contains(5));
        }

        [TestMethod]
        public void TestShuffleNullListShouldNotThrowDueToInternalCatch()
        {
            var shuffler = new DefaultDeckShuffler();
            shuffler.Shuffle<string>(null);
            Assert.IsTrue(true);
        }
    }
}