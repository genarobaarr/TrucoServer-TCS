using System;
using System.Runtime.Serialization;

namespace TrucoServer.Data.DTOs
{
    [DataContract]
    public class TournamentDTO
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public string Name { get; set; }
        [DataMember] public int Capacity { get; set; }
        [DataMember] public string Status { get; set; }
    }
}