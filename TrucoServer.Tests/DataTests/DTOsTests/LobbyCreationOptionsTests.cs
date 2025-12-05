using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Helpers.Match;

namespace TrucoServer.Tests.DataTests.DTOsTests
{
    [TestClass]
    public class LobbyCreationOptionsTests
    {
        [TestMethod]
        public void TestMaxPlayersSetPositiveIntegerReturnsInteger()
        {
            var options = new LobbyCreationOptions();
            int maxPlayers = 4;
            options.MaxPlayers = maxPlayers;
            Assert.AreEqual(maxPlayers, options.MaxPlayers);
        }

        [TestMethod]
        public void TestVersionIdSetZeroReturnsZero()
        {
            var options = new LobbyCreationOptions();
            int version = 0;
            options.VersionId = version;
            Assert.AreEqual(0, options.VersionId);
        }

        [TestMethod]
        public void TestStatusSetStringReturnsString()
        {
            var options = new LobbyCreationOptions();
            string status = "Waiting";
            options.Status = status;
            Assert.AreEqual(status, options.Status);
        }

        [TestMethod]
        public void TestHostSetUserObjectReturnsUser()
        {
            var options = new LobbyCreationOptions();
            var host = new User();
            options.Host = host;
            Assert.AreSame(host, options.Host);
        }

        [TestMethod]
        public void TestMaxPlayersSetNegativeValueReturnsNegative()
        {
            var options = new LobbyCreationOptions();
            int invalidMax = -1;
            options.MaxPlayers = invalidMax;
            Assert.AreEqual(invalidMax, options.MaxPlayers);
        }
    }
}
