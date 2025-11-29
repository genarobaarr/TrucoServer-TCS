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
    public class PublicLobbyInfoTests
    {
        [TestMethod]
        public void TestPublicLobbyInfoSetMaxPlayersShouldStoreValue()
        {
            var lobby = new PublicLobbyInfo();
            lobby.MaxPlayers = 4;
            Assert.AreEqual(4, lobby.MaxPlayers);
        }
    }
}
