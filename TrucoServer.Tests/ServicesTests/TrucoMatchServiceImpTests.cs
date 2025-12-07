using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.GameLogic;
using TrucoServer.Helpers.Match;
using TrucoServer.Helpers.Profanity;
using TrucoServer.Services;
using TrucoServer.Helpers.Email;

namespace TrucoServer.Tests.ServicesTests
{
    [TestClass]
    public class TrucoMatchServiceImpTests
    {
        private Mock<baseDatosTrucoEntities> mockContext;
        private Mock<IGameRegistry> mockRegistry;
        private Mock<IJoinService> mockJoin;
        private Mock<ILobbyCoordinator> mockCoordinator;
        private Mock<ILobbyRepository> mockRepo;
        private Mock<IMatchCodeGenerator> mockGenerator;
        private Mock<IMatchStarter> mockStarter;
        private Mock<IProfanityServerService> mockProfanity;
        private Mock<DbSet<User>> mockUserSet;
        private Mock<DbSet<Lobby>> mockLobbySet;
        private Mock<DbSet<LobbyMember>> mockMemberSet;
        private Mock<IEmailSender> mockEmail;
        private TrucoMatchServiceImp service;

        [TestInitialize]
        public void Setup()
        {
            mockContext = new Mock<baseDatosTrucoEntities>();
            mockRegistry = new Mock<IGameRegistry>();
            mockJoin = new Mock<IJoinService>();
            mockCoordinator = new Mock<ILobbyCoordinator>();
            mockRepo = new Mock<ILobbyRepository>();
            mockGenerator = new Mock<IMatchCodeGenerator>();
            mockStarter = new Mock<IMatchStarter>();
            mockProfanity = new Mock<IProfanityServerService>();
            mockEmail = new Mock<IEmailSender>();
            mockUserSet = new Mock<DbSet<User>>();
            mockLobbySet = new Mock<DbSet<Lobby>>();
            mockMemberSet = new Mock<DbSet<LobbyMember>>();

            mockContext.Setup(c => c.User).Returns(mockUserSet.Object);
            mockContext.Setup(c => c.Lobby).Returns(mockLobbySet.Object);
            mockContext.Setup(c => c.LobbyMember).Returns(mockMemberSet.Object);

            service = new TrucoMatchServiceImp(
                mockContext.Object, mockRegistry.Object, mockJoin.Object,
                mockCoordinator.Object, mockRepo.Object, mockGenerator.Object,
                mockStarter.Object, mockProfanity.Object, mockEmail.Object
            );
        }

        [TestMethod]
        public void TestCreateLobbyReturnsEmptyIfUsernameInvalid()
        {
            var result = service.CreateLobby("", 2, "public");

            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void TestCreateLobbyReturnsEmptyIfHostNotFound()
        {
            mockGenerator.Setup(g => g.GenerateMatchCode()).Returns("ABC");
            var data = new List<User>().AsQueryable();
            mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            var result = service.CreateLobby("Host", 2, "public");

            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void TestCreateLobbyReturnsCodeOnSuccess()
        {
            var host = new User 
            {
                username = "Host",
                userID = 1 
            };

            var data = new List<User>
            { 
                host
            }.AsQueryable();

            mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(data.Provider);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(data.Expression);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            mockGenerator.Setup(g => g.GenerateMatchCode()).Returns("CODE");
            
            mockRepo.Setup(r => r.CreateNewLobby(It.IsAny<LobbyCreationOptions>())).Returns(new Lobby 
            { 
                status = "Public" 
            });

            mockCoordinator.Setup(c => c.GetOrCreateLobbyLock(It.IsAny<int>())).Returns(new object());

            var result = service.CreateLobby("Host", 2, "public");
            Assert.AreEqual("CODE", result);
        }

        [TestMethod]
        public void TestJoinMatchReturnsFalseForInvalidCode()
        {
            var result = service.JoinMatch(null, "Player");
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void TestJoinMatchReturnsFalseIfLobbyClosed()
        {
            int lobbyId = 10;
            mockCoordinator.Setup(c => c.TryGetLobbyIdFromCode("CODE", out lobbyId)).Returns(true);
            var lobby = new Lobby
            {
                lobbyID = 10,
                status = "Closed"
            };

            var data = new List<Lobby>
            { 
                lobby
            }.AsQueryable();

            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.Provider).Returns(data.Provider);
            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.Expression).Returns(data.Expression);
            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            var result = service.JoinMatch("CODE", "Player");
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void TestJoinMatchReturnsTrueOnSuccess()
        {
            int lobbyId = 10;
            int maxPlayers = 4;
            mockCoordinator.Setup(c => c.TryGetLobbyIdFromCode("CODE", out lobbyId)).Returns(true);

            var lobby = new Lobby
            {
                lobbyID = 10,
                status = "Public",
                maxPlayers = maxPlayers
            };

            var data = new List<Lobby>
            { 
                lobby 
            }.AsQueryable();

            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.Provider).Returns(data.Provider);
            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.Expression).Returns(data.Expression);
            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            mockCoordinator.Setup(c => c.GetOrCreateLobbyLock(10)).Returns(new object());
            mockJoin.Setup(j => j.ProcessSafeJoin(10, "CODE", "Player")).Returns(true);

            var result = service.JoinMatch("CODE", "Player");
            Assert.AreEqual(maxPlayers, result);
        }

        [TestMethod]
        public void TestLeaveMatchDoesNotThrowOnException()
        {
            mockCoordinator.Setup(c => c.TryGetLobbyIdFromCode(It.IsAny<string>(), out It.Ref<int>.IsAny)).Throws(new Exception("Fail"));

            try
            {
                service.LeaveMatch("CODE", "Player");
            }
            catch
            {
                Assert.Fail("Exception should be handled");
            }

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestLeaveMatchRemovesMember()
        {
            int lobbyId = 1;
            int userId = 100;
            mockCoordinator.Setup(c => c.TryGetLobbyIdFromCode("CODE", out lobbyId)).Returns(true);

            var lobby = new Lobby
            {
                lobbyID = 1
            };

            var lobbyData = new List<Lobby> 
            {
                lobby 
            }.AsQueryable();

            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.GetEnumerator()).Returns(lobbyData.GetEnumerator());

            var user = new User 
            { 
                userID = userId, 
                username = "Player"
            };

            var userData = new List<User>
            {
                user
            }.AsQueryable();

            mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(userData.Provider);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(userData.Expression);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(userData.ElementType);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(userData.GetEnumerator());

            var member = new LobbyMember 
            { 
                lobbyID = 1, 
                userID = 100
            };

            var memberData = new List<LobbyMember> 
            { 
                member 
            }.AsQueryable();

            mockMemberSet.As<IQueryable<LobbyMember>>().Setup(m => m.Provider).Returns(memberData.Provider);
            mockMemberSet.As<IQueryable<LobbyMember>>().Setup(m => m.Expression).Returns(memberData.Expression);
            mockMemberSet.As<IQueryable<LobbyMember>>().Setup(m => m.ElementType).Returns(memberData.ElementType);
            mockMemberSet.As<IQueryable<LobbyMember>>().Setup(m => m.GetEnumerator()).Returns(memberData.GetEnumerator());

            service.LeaveMatch("CODE", "Player");
            mockMemberSet.Verify(m => m.Remove(It.IsAny<LobbyMember>()), Times.Once);
        }

        [TestMethod]
        public void TestGetPublicLobbiesReturnsEmptyOnException()
        {
            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.Provider).Throws(new Exception("DB"));

            var result = service.GetPublicLobbies();

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestGetPublicLobbiesReturnsMappedInfo()
        {
            var lobby = new Lobby 
            {
                lobbyID = 1,
                status = "Public",
                maxPlayers = 2,
                ownerID = 5 
            };

            var data = new List<Lobby>
            { 
                lobby
            }.AsQueryable();

            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.Provider).Returns(data.Provider);
            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.Expression).Returns(data.Expression);
            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            mockCoordinator.Setup(c => c.GetMatchCodeFromLobbyId(1)).Returns("CODE");
            var result = service.GetPublicLobbies();
            Assert.AreEqual("CODE", result[0].MatchCode);
        }

        [TestMethod]
        public void TestGetLobbyPlayersReturnsEmptyForInvalidCode()
        {
            var result = service.GetLobbyPlayers(null);

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestGetLobbyPlayersReturnsGamePlayersIfMatchStarted()
        {
            TrucoMatch dummy = null;
            mockRegistry.Setup(r => r.TryGetGame("CODE", out dummy)).Returns(false);

            int id = 1;
            mockCoordinator.Setup(c => c.TryGetLobbyIdFromCode("CODE", out id)).Returns(true);
            var lobby = new Lobby 
            {
                lobbyID = 1, 
                ownerID = 9
            };

            var data = new List<Lobby> 
            { 
                lobby 
            }.AsQueryable();

            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.Provider).Returns(data.Provider);
            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.Expression).Returns(data.Expression);
            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            mockRepo.Setup(r => r.GetLobbyOwnerName(9)).Returns("Owner");
            mockRepo.Setup(r => r.GetDatabasePlayers(lobby, "Owner")).Returns(new List<PlayerInfo>());
            mockCoordinator.Setup(c => c.GetGuestPlayersFromMemory("CODE", "Owner")).Returns(new List<PlayerInfo>());

            var result = service.GetLobbyPlayers("CODE");
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestStartMatchDoesNothingIfValidationFails()
        {
            mockStarter.Setup(s => s.ValidateMatchStart("CODE"))
                        .Returns(new MatchStartValidation { IsValid = false });

            service.StartMatch("CODE");
            mockStarter.Verify(s => s.InitiateMatchSequence(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<PlayerInfo>>()), Times.Never);
        }

        [TestMethod]
        public void TestStartMatchCallsInitiateOnSuccess()
        {
            mockStarter.Setup(s => s.ValidateMatchStart("CODE"))
                        .Returns(new MatchStartValidation 
                        {
                            IsValid = true, 
                            LobbyId = 1,
                            ExpectedPlayers = 0 
                        });

            int id = 1;
            mockCoordinator.Setup(c => c.TryGetLobbyIdFromCode("CODE", out id)).Returns(true);
            var lobby = new Lobby 
            { 
                lobbyID = 1, 
                ownerID = 1
            };

            var data = new List<Lobby>
            { 
                lobby
            }.AsQueryable();

            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.Provider).Returns(data.Provider);
            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.Expression).Returns(data.Expression);
            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            mockRepo.Setup(r => r.GetDatabasePlayers(lobby, It.IsAny<string>())).Returns(new List<PlayerInfo>());
            mockCoordinator.Setup(c => c.GetGuestPlayersFromMemory("CODE", It.IsAny<string>())).Returns(new List<PlayerInfo>());
            service.StartMatch("CODE");
            mockStarter.Verify(s => s.InitiateMatchSequence("CODE", 1, It.IsAny<List<PlayerInfo>>()), Times.Once);
        }

        [TestMethod]
        public void TestJoinMatchChatHandlesNullOperationContext()
        {
            try
            {
                service.JoinMatchChat("CODE", "Player");
            }
            catch
            {
                Assert.Fail("Exception should be handled internally");
            }

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestSendChatMessageReturnsEarlyOnProfanity()
        {
            mockProfanity.Setup(p => p.ContainsProfanity("bad")).Returns(true);
            service.SendChatMessage("CODE", "Player", "bad");
            mockCoordinator.Verify(c => c.BroadcastToMatchCallbacksAsync(It.IsAny<string>(), It.IsAny<Action<ITrucoCallback>>()), Times.Never);
        }

        [TestMethod]
        public void TestLeaveMatchChatHandlesNullOperationContext()
        {
            service.LeaveMatchChat("CODE", "Player");
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestSwitchTeamCallsJoinService()
        {
            mockJoin.Setup(j => j.SwitchUserTeam("CODE", "User")).Returns(true);
            service.SwitchTeam("CODE", "User");
            mockCoordinator.Verify(c => c.BroadcastToMatchCallbacksAsync("CODE", It.IsAny<Action<ITrucoCallback>>()), Times.Once);
        }

        [TestMethod]
        public void TestSwitchTeamHandlesGuestPrefix()
        {
            mockJoin.Setup(j => j.SwitchGuestTeam("CODE", "Guest_User")).Returns(true);
            service.SwitchTeam("CODE", "Guest_User");
            mockCoordinator.Verify(c => c.BroadcastToMatchCallbacksAsync("CODE", It.IsAny<Action<ITrucoCallback>>()), Times.Once);
        }

        [TestMethod]
        public void TestPlayCardDoesNotThrowWhenMatchNotFound()
        {
            TrucoMatch m = null;
            int pid = 0;
            mockStarter.Setup(s => s.GetMatchAndPlayerID("CODE", out m, out pid)).Returns(false);
            service.PlayCard("CODE", "card");
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestCallTrucoExecutesAction()
        {
            TrucoMatch m = null;
            int pid = 0;
            mockStarter.Setup(s => s.GetMatchAndPlayerID("CODE", out m, out pid)).Returns(false);
            service.CallTruco("CODE", "Truco");
            mockStarter.Verify(s => s.GetMatchAndPlayerID("CODE", out m, out pid), Times.Once);
        }

        [TestMethod]
        public void TestGetBannedWordsDelegatesToService()
        {
            mockProfanity.Setup(p => p.GetBannedWordsForClient()).Returns(new BannedWordList());
            var result = service.GetBannedWords();
            Assert.IsNotNull(result);
        }
    }
}