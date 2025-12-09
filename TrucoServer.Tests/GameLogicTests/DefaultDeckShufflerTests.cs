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
        private const int LIST_COUNT_1 = 1;
        private const int LIST_COUNT_2 = 2;
        private const int LIST_COUNT_3 = 3;
        private const int LIST_COUNT_4 = 4;
        private const int LIST_COUNT_5 = 5;
        private const int LIST_COUNT_99 = 99;

        [TestMethod]
        public void TestShuffleWithValidListDoesNotThrow()
        {
            var shuffler = new DefaultDeckShuffler();
            
            var list = new List<int> 
            { 
                LIST_COUNT_1,
                LIST_COUNT_2,
                LIST_COUNT_3, 
                LIST_COUNT_4, 
                LIST_COUNT_5 
            };

            try
            {
                shuffler.Shuffle(list);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Shuffle should not throw exception on valid list {ex.Message}");
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
                LIST_COUNT_99
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
            catch (Exception)
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