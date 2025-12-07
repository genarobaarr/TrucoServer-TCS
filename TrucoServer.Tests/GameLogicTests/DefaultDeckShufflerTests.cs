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
        public void TestShuffleWithValidListDoesNotThrow()
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

            try
            {
                shuffler.Shuffle(list);
            }
            catch
            {
                Assert.Fail("Shuffle should not throw exception on valid list.");
            }

            Assert.AreEqual(5, list.Count);
        }

        [TestMethod]
        public void TestShuffleWithEmptyListDoesNotThrow()
        {
            var shuffler = new DefaultDeckShuffler();
            var list = new List<int>();
            shuffler.Shuffle(list);
            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void TestShuffleWithSingleItemListRemainsUnchanged()
        {
            var shuffler = new DefaultDeckShuffler();
            var list = new List<int>
            { 
                99 
            };

            shuffler.Shuffle(list);
            Assert.AreEqual(99, list[0]);
        }

        [TestMethod]
        public void TestShuffleWithNullListHandlesExceptionGracefully()
        {
            var shuffler = new DefaultDeckShuffler();
            List<int> list = null;
            bool exceptionHandled = true;

            try
            {
                shuffler.Shuffle(list);
            }
            catch
            {
                exceptionHandled = false;
            }

            Assert.IsTrue(exceptionHandled);
        }

        [TestMethod]
        public void TestShufflePreservesElementsContent()
        {
            var shuffler = new DefaultDeckShuffler();
           
            var list = new List<string> 
            { 
                "A", 
                "B", 
                "C" 
            };

            shuffler.Shuffle(list);
            Assert.IsTrue(list.Contains("A") && list.Contains("B") && list.Contains("C"));
        }
    }
}