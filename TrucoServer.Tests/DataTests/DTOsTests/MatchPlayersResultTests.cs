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
    public class MatchPlayersResultTests
    {
        [TestMethod]
        public void TestPlayersGetReturnsInitializedList()
        {
            var result = new MatchPlayersResult();
            Assert.IsNotNull(result.Players);
        }

        [TestMethod]
        public void TestCallbacksGetReturnsInitializedDictionary()
        {
            var result = new MatchPlayersResult();
            Assert.IsNotNull(result.Callbacks);
        }

        [TestMethod]
        public void TestIsSuccessReturnsFalseWhenPlayersEmpty()
        {
            var result = new MatchPlayersResult();
            bool success = result.IsSuccess;
            Assert.IsFalse(success);
        }
    }
}
