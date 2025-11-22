using System.Collections.Generic;
using System.Runtime.Serialization;
using TrucoServer.GameLogic;

namespace TrucoServer.Data.DTOs
{
    [DataContract]
    public class PlayerInformation
    {
        [DataMember] public int PlayerID { get; set; }
        [DataMember] public string Username { get; set; }
        [DataMember] public string AvatarId { get; set; }
        [DataMember] public string OwnerUsername { get; set; }
        [DataMember] public string Team { get; set; }
        [DataMember] public List<TrucoCard> Hand { get; set; }

        public PlayerInformation(int playerID, string username, string team)
        {
            PlayerID = playerID;
            Username = username;
            Team = team;
            Hand = new List<TrucoCard>();
        }
    }
}

