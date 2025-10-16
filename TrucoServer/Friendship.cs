using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer
{
    public partial class Friendship
    {
        public int friendshipID { get; set; }
        public int userID1 { get; set; }
        public int userID2 { get; set; }
        public string status { get; set; }
        public System.DateTime requestDate { get; set; }
        public virtual User User { get; set; }
        public virtual User User2 { get; set; }
    }
}
