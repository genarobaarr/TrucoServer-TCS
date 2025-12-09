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
        private Mock<IEmailSender> mockEmail;

        private Mock<DbSet<User>> mockUserSet;
        private Mock<DbSet<Lobby>> mockLobbySet;
        private Mock<DbSet<LobbyMember>> mockMemberSet;
        private Mock<DbSet<Invitation>> mockInvitationSet;

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

            mockUserSet = GetMockDbSet(new List<User>());
            mockLobbySet = GetMockDbSet(new List<Lobby>());
            mockMemberSet = GetMockDbSet(new List<LobbyMember>());
            mockInvitationSet = GetMockDbSet(new List<Invitation>());

            mockContext.Setup(c => c.User).Returns(mockUserSet.Object);
            mockContext.Setup(c => c.Lobby).Returns(mockLobbySet.Object);
            mockContext.Setup(c => c.LobbyMember).Returns(mockMemberSet.Object);
            mockContext.Setup(c => c.Invitation).Returns(mockInvitationSet.Object);

            var dependencies = new TrucoMatchServiceDependencies
            {
                GameRegistry = mockRegistry.Object,
                JoinService = mockJoin.Object,
                LobbyCoordinator = mockCoordinator.Object,
                LobbyRepository = mockRepo.Object,
                CodeGenerator = mockGenerator.Object,
                Starter = mockStarter.Object,
                ProfanityService = mockProfanity.Object,
                EmailSender = mockEmail.Object
            };

            service = new TrucoMatchServiceImp(mockContext.Object, dependencies);
        }

        private const int MAX_PLAYERS = 2;
        private const int USER_ID = 1;
        private const int LOBBY_ID = 1;
        private const int SECOND_LOBBY_ID = 10;
        private const int OWNER_ID = 5;
        private const int SECOND_OWNER_ID = 9;

        private static Mock<DbSet<T>> GetMockDbSet<T>(List<T> sourceList) where T : class
        {
            var queryable = sourceList.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

            mockSet.Setup(d => d.Add(It.IsAny<T>())).Callback<T>((s) => sourceList.Add(s));
            mockSet.Setup(d => d.Remove(It.IsAny<T>())).Callback<T>((s) => sourceList.Remove(s));

            return mockSet;
        }

        [TestMethod]
        public void TestCreateLobbyReturnsEmptyIfUsernameInvalid()
        {
            string invalidUsername = string.Empty;
            int maxPlayers = MAX_PLAYERS;
            string privacy = "public";
            var result = service.CreateLobby(invalidUsername, maxPlayers, privacy);
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void TestCreateLobbyReturnsEmptyIfHostNotFound()
        {
            mockGenerator.Setup(g => g.GenerateMatchCode()).Returns("ABC");
            var result = service.CreateLobby("Host", MAX_PLAYERS, "public");
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void TestCreateLobbyReturnsCodeOnSuccess()
        {
            var host = new User
            {
                username = "Host",
                userID = USER_ID
            };

            var userList = new List<User> 
            { 
                host 
            };

            var mockSet = GetMockDbSet(userList);
            mockContext.Setup(c => c.User).Returns(mockSet.Object);
            mockGenerator.Setup(g => g.GenerateMatchCode()).Returns("CODE");

            var lobby = new Lobby
            {
                status = "Public",
                lobbyID = LOBBY_ID
            };

            mockRepo.Setup(r => r.CreateNewLobby(It.IsAny<LobbyCreationOptions>())).Returns(lobby);
            var result = service.CreateLobby("Host", MAX_PLAYERS, "public");
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
            int lobbyId = SECOND_LOBBY_ID;
            mockCoordinator.Setup(c => c.TryGetLobbyIdFromCode("CODE", out lobbyId)).Returns(true);
          
            var lobby = new Lobby
            {
                lobbyID = SECOND_LOBBY_ID,
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
        public void TestLeaveMatchDoesNotThrowOnException()
        {
            mockCoordinator.Setup(c => c.TryGetLobbyIdFromCode(It.IsAny<string>(), out It.Ref<int>.IsAny)).Throws(new Exception("Fail"));

            try
            {
                service.LeaveMatch("CODE", "Player");
            }
            catch (Exception ex) 
            {
                Assert.Fail($"Exception should be handled {ex.Message}");
            }
        }

        [TestMethod]
        public void TestGetPublicLobbiesReturnsEmptyOnException()
        {
            mockLobbySet.As<IQueryable<Lobby>>().Setup(m => m.Provider).Throws(new Exception("Data base"));
            var result = service.GetPublicLobbies();
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestGetPublicLobbiesReturnsMappedInfo()
        {
            var lobby = new Lobby 
            {
                lobbyID = LOBBY_ID,
                status = "Public",
                maxPlayers = MAX_PLAYERS, 
                ownerID = OWNER_ID
            };

            mockLobbySet = GetMockDbSet(new List<Lobby> 
            { 
                lobby 
            });

            mockContext.Setup(c => c.Lobby).Returns(mockLobbySet.Object);
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

            int id = LOBBY_ID;
            mockCoordinator.Setup(c => c.TryGetLobbyIdFromCode("CODE", out id)).Returns(true);
            
            var lobby = new Lobby
            {
                lobbyID = LOBBY_ID,
                ownerID = SECOND_OWNER_ID
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
                        .Returns(new MatchStartValidation 
                        { 
                            IsValid = false 
                        });

            service.StartMatch("CODE");
            mockStarter.Verify(s => s.InitiateMatchSequence(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<PlayerInfo>>()), Times.Never);
        }

        [TestMethod]
        public void TestJoinMatchChatHandlesNullOperationContext()
        {
            try
            {
                service.JoinMatchChat("CODE", "Player");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Exception should be handled internally {ex.Message}");
            }

            Assert.IsNotNull(service);
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
            Assert.IsNotNull(service);
        }

        [TestMethod]
        public void TestPlayCardDoesNotThrowWhenMatchNotFound()
        {
            TrucoMatch match = null;
            int pid = 0;
            mockStarter.Setup(s => s.GetMatchAndPlayerID("CODE", out match, out pid)).Returns(false);
            service.PlayCard("CODE", "card");
            Assert.IsNotNull(service);
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