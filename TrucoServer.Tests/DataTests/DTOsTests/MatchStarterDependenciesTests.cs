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
    public class MatchStarterDependenciesTests
    {
        [TestMethod]
        public void TestContextSetNullReturnsNull()
        {
            var deps = new MatchStarterDependencies();
            deps.Context = null;
            Assert.IsNull(deps.Context);
        }

        [TestMethod]
        public void TestGameRegistrySetNullReturnsNull()
        {
            var deps = new MatchStarterDependencies();
            deps.GameRegistry = null;
            Assert.IsNull(deps.GameRegistry);
        }

        [TestMethod]
        public void TestCoordinatorSetNullReturnsNull()
        {
            var deps = new MatchStarterDependencies();
            deps.Coordinator = null;
            Assert.IsNull(deps.Coordinator);
        }

        [TestMethod]
        public void TestParticipantBuilderSetNullReturnsNull()
        {
            var deps = new MatchStarterDependencies();
            deps.ParticipantBuilder = null;
            Assert.IsNull(deps.ParticipantBuilder);
        }

        [TestMethod]
        public void TestConstructorInstanceIsNotNull()
        {
            var deps = new MatchStarterDependencies();
            Assert.IsNotNull(deps);
        }
    }
}
