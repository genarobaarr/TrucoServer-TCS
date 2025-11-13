using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer
{
    public class ActiveMatch
    {
        public string Code { get; set; }
        public List<PlayerInfo> Players { get; set; } = new List<PlayerInfo>();
        public List<TrucoCard> TableCards { get; set; } = new List<TrucoCard>();
        public bool IsHandInProgress { get; set; }
        public int CurrentTurnIndex { get; set; }
        public string CurrentCall { get; set; }
        public int MatchDatabaseId { get; set; }
    }
}
