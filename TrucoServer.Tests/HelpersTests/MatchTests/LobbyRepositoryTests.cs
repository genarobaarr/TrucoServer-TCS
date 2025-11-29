using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Helpers.Match;

namespace TrucoServer.Tests.HelpersTests.MatchTests
{
    [TestClass]
    public class LobbyRepositoryTests
    {
        private LobbyRepository repository;
        private User testHost;
        private const string HOST_USER = "LobbyHost";

        [TestInitialize]
        public void Setup()
        {
            repository = new LobbyRepository();
            using (var context = new baseDatosTrucoEntities())
            {
                var existing = context.User.FirstOrDefault(u => u.username == HOST_USER);
                if (existing != null)
                {
                    var lobbies = context.Lobby.Where(l => l.ownerID == existing.userID);
                    
                    foreach (var l in lobbies)
                    {
                        context.LobbyMember.RemoveRange(context.LobbyMember.Where(m => m.lobbyID == l.lobbyID));
                    }

                    context.Lobby.RemoveRange(lobbies);
                    context.Invitation.RemoveRange(context.Invitation.Where(i => i.senderID == existing.userID));
                    context.User.Remove(existing);
                    context.SaveChanges();
                }

                testHost = new User 
                {
                    username = HOST_USER, 
                    email = "host@gmail.com", 
                    passwordHash = "password", 
                    nameChangeCount = 0, 
                };
                
                context.User.Add(testHost);
                context.SaveChanges();
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var user = context.User.FirstOrDefault(u => u.username == HOST_USER);
               
                if (user != null)
                {
                    var lobbies = context.Lobby.Where(l => l.ownerID == user.userID).ToList();
                   
                    foreach (var l in lobbies)
                    {
                        context.LobbyMember.RemoveRange(context.LobbyMember.Where(m => m.lobbyID == l.lobbyID));
                    }
                   
                    context.Lobby.RemoveRange(lobbies);
                    context.Invitation.RemoveRange(context.Invitation.Where(i => i.senderID == user.userID));
                    context.User.Remove(user);
                    context.SaveChanges();
                }
            }
        }

        [TestMethod]
        public void TestCreateNewLobbyValidInputShouldCreateRecord()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                int versionId = repository.ResolveVersionId(context, 2);
                var lobby = repository.CreateNewLobby(context, testHost, versionId, 2, "Public");

                Assert.IsNotNull(lobby);
                Assert.AreEqual(testHost.userID, lobby.ownerID);
                Assert.AreEqual("Public", lobby.status);
                Assert.IsTrue(lobby.lobbyID > 0);
            }
        }

        [TestMethod]
        public void TestAddLobbyOwnerShouldCreateMemberRecord()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                int versionId = repository.ResolveVersionId(context, 2);
                var lobby = repository.CreateNewLobby(context, testHost, versionId, 2, "Public");

                repository.AddLobbyOwner(context, lobby, testHost);
                context.SaveChanges();

                var member = context.LobbyMember.FirstOrDefault(m => m.lobbyID == lobby.lobbyID && m.userID == testHost.userID);
                Assert.IsNotNull(member);
                Assert.AreEqual("Owner", member.role);
            }
        }

        [TestMethod]
        public void TestCreatePrivateInvitationShouldInsertPendingInvitation()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                string code = "ABC123";
                repository.CreatePrivateInvitation(context, testHost, code);
                context.SaveChanges();

                var invite = context.Invitation.FirstOrDefault(i => i.senderID == testHost.userID && i.status == "Pending");
                Assert.IsNotNull(invite);
            }
        }

        [TestMethod]
        public void TestCloseLobbyByIdShouldUpdateStatus()
        {
            int lobbyId;
           
            using (var context = new baseDatosTrucoEntities())
            {
                var lobby = repository.CreateNewLobby(context, testHost, 1, 2, "Public");
                lobbyId = lobby.lobbyID;
            }

            bool result = repository.CloseLobbyById(lobbyId);

            Assert.IsTrue(result);
            
            using (var context = new baseDatosTrucoEntities())
            {
                var closedLobby = context.Lobby.Find(lobbyId);
                Assert.AreEqual("Closed", closedLobby.status);
            }
        }

        [TestMethod]
        public void TestResolveVersionIdShouldReturnValidId()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                int vId = repository.ResolveVersionId(context, 2);
                Assert.IsTrue(vId > 0);
            }
        }
    }
}

