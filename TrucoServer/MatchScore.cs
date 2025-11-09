using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer
{
    [DataContract]
    public class MatchScore
    {
        [DataMember] public string MatchID { get; set; }
        [DataMember] public DateTime EndedAt { get; set; }
        [DataMember] public bool IsWin { get; set; }
        [DataMember] public int FinalScore { get; set; }
    }
}
