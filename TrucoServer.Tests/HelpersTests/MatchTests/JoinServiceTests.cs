using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;
using TrucoServer.Helpers.Match;

namespace TrucoServer.Tests.HelpersTests.MatchTests
{
    [TestClass]
    public class JoinServiceTests
    {
        private Mock<baseDatosTrucoEntities> mockContext;
        private Mock<ILobbyCoordinator> mockCoordinator;
        private Mock<ILobbyRepository> mockRepo;
        private Mock<DbSet<LobbyMember>> mockLobbyMemberSet;
        private Mock<DbSet<User>> mockUserSet;
        private JoinService service;

        private const int MAX_PLAYERS_2V2 = 4;
        private const int TEAM_1_COUNT = 2;
        private const int TEAM_2_COUNT = 1;
        private const int LOBBY_ID = 1;
        private const int MAX_PLAYERS_1V1 = 2;
        private const int USER_ID = 10;

        [TestInitialize]
        public void Setup()
        {
            mockContext = new Mock<baseDatosTrucoEntities>();
            mockCoordinator = new Mock<ILobbyCoordinator>();
            mockRepo = new Mock<ILobbyRepository>();
            mockLobbyMemberSet = new Mock<DbSet<LobbyMember>>();
            mockUserSet = new Mock<DbSet<User>>();

            mockContext.Setup(c => c.LobbyMember).Returns(mockLobbyMemberSet.Object);
            mockContext.Setup(c => c.User).Returns(mockUserSet.Object);

            service = new JoinService(mockContext.Object, mockCoordinator.Object, mockRepo.Object);
        }

        [TestMethod]
        public void TestDetermineTeamForNewPlayerAssignsTeamTwoWhenTeamOneIsFull()
        {
            var options = new TeamDeterminationOptions
            {
                MaxPlayers = MAX_PLAYERS_2V2,
                Team1Count = TEAM_1_COUNT,
                Team2Count = TEAM_2_COUNT,
                Username = "NewUser"
            };

            string assignedTeam = service.DetermineTeamForNewPlayer(options);
            Assert.AreEqual("Team 2", assignedTeam);
        }

        [TestMethod]
        public void TestValidateJoinConditionsReturnsFalseIfLobbyIsFull()
        {
            var lobby = new Lobby 
            { 
                lobbyID = LOBBY_ID, 
                maxPlayers = MAX_PLAYERS_1V1
            };

            var user = new User 
            { 
                userID = USER_ID, 
                username = "PlayerX"
            };

            var members = new List<LobbyMember>
            {
                new LobbyMember 
                { 
                    lobbyID = LOBBY_ID
                },
                
                new LobbyMember 
                {
                    lobbyID = LOBBY_ID
                }
            }.AsQueryable();

            mockLobbyMemberSet.As<IQueryable<LobbyMember>>().Setup(m => m.Provider).Returns(members.Provider);
            mockLobbyMemberSet.As<IQueryable<LobbyMember>>().Setup(m => m.Expression).Returns(members.Expression);
            mockLobbyMemberSet.As<IQueryable<LobbyMember>>().Setup(m => m.ElementType).Returns(members.ElementType);
            mockLobbyMemberSet.As<IQueryable<LobbyMember>>().Setup(m => m.GetEnumerator()).Returns(members.GetEnumerator());

            bool result = service.ValidateJoinConditions(lobby, user);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestTryJoinAsGuestReturnsFalseIfLobbyIsNotPublic()
        {
            var lobby = new Lobby 
            {
                status = "Private" 
            };
            
            var options = new GuestJoinOptions
            {
                Lobby = lobby,
                MatchCode = "CODE123"
            };

            bool result = service.TryJoinAsGuest(options);
            Assert.IsFalse(result);
        }
    }
}
